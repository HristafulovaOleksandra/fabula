import random
from dataclasses import dataclass

import numpy as np
import torch
from torch import nn
from transformers import AutoModel, get_linear_schedule_with_warmup

N_LEVELS = 5


def set_seed(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)


@dataclass
class FeatureScaler:
    mean: np.ndarray
    std: np.ndarray

    @classmethod
    def fit(cls, values: np.ndarray) -> "FeatureScaler":
        std = values.std(axis=0)
        return cls(mean=values.mean(axis=0), std=np.where(std > 0, std, 1.0))

    def transform(self, values: np.ndarray) -> np.ndarray:
        return ((values - self.mean) / self.std).astype(np.float32)

    def to_dict(self) -> dict:
        return {"mean": self.mean.tolist(), "std": self.std.tolist()}

    @classmethod
    def from_dict(cls, data: dict) -> "FeatureScaler":
        return cls(mean=np.asarray(data["mean"]), std=np.asarray(data["std"]))


class HybridClassifier(nn.Module):
    def __init__(self, model_name: str, n_features: int = 0, feature_dim: int = 32, dropout: float = 0.1):
        super().__init__()
        self.encoder = AutoModel.from_pretrained(model_name)
        hidden_size = self.encoder.config.hidden_size
        self.n_features = n_features
        self.feature_net = (
            nn.Sequential(nn.Linear(n_features, feature_dim), nn.ReLU()) if n_features > 0 else None
        )
        self.classifier = nn.Sequential(
            nn.Dropout(dropout),
            nn.Linear(hidden_size + (feature_dim if n_features > 0 else 0), N_LEVELS),
        )

    def encode(self, input_ids: torch.Tensor, attention_mask: torch.Tensor) -> torch.Tensor:
        return self.encoder(input_ids=input_ids, attention_mask=attention_mask).last_hidden_state[:, 0]

    def head(self, pooled: torch.Tensor, features: torch.Tensor | None = None) -> torch.Tensor:
        if self.feature_net is not None:
            pooled = torch.cat([pooled, self.feature_net(features.to(pooled.dtype))], dim=-1)
        return self.classifier(pooled)

    def forward(self, input_ids, attention_mask, features=None) -> torch.Tensor:
        return self.head(self.encode(input_ids, attention_mask), features)


def balanced_class_weights(labels: np.ndarray) -> torch.Tensor:
    counts = np.bincount(labels, minlength=N_LEVELS).astype(np.float64)
    weights = len(labels) / (N_LEVELS * np.clip(counts, 1, None))
    return torch.tensor(weights, dtype=torch.float32)


def batches(size: int, batch_size: int, shuffle: bool, generator: torch.Generator | None = None):
    order = torch.randperm(size, generator=generator) if shuffle else torch.arange(size)
    for start in range(0, size, batch_size):
        yield order[start:start + batch_size]


def autocast(device: torch.device):
    return torch.autocast(device_type="cuda", dtype=torch.float16, enabled=device.type == "cuda")


@torch.no_grad()
def predict_logits(model, input_ids, attention_mask, features, device, batch_size=128) -> np.ndarray:
    model.eval()
    outputs = []
    for index in batches(len(input_ids), batch_size, shuffle=False):
        with autocast(device):
            logits = model(
                input_ids[index].to(device),
                attention_mask[index].to(device),
                features[index].to(device) if features is not None else None,
            )
        outputs.append(logits.float().cpu())
    return torch.cat(outputs).numpy()


@torch.no_grad()
def encode_all(model, input_ids, attention_mask, device, batch_size=128) -> torch.Tensor:
    model.eval()
    outputs = []
    for index in batches(len(input_ids), batch_size, shuffle=False):
        with autocast(device):
            outputs.append(model.encode(input_ids[index].to(device), attention_mask[index].to(device)).float())
    return torch.cat(outputs)


@torch.no_grad()
def predict_from_pooled(model, pooled: torch.Tensor, features: torch.Tensor | None, device) -> np.ndarray:
    model.eval()
    logits = model.head(pooled, features.to(device) if features is not None else None)
    return logits.argmax(dim=-1).cpu().numpy()


def train_classifier(
    model: HybridClassifier,
    train_inputs: dict,
    train_labels: np.ndarray,
    val_inputs: dict,
    score_fn,
    device: torch.device,
    epochs: int,
    learning_rate: float,
    batch_size: int,
    seed: int,
    class_weights: torch.Tensor | None = None,
    warmup_ratio: float = 0.1,
    weight_decay: float = 0.01,
):
    model.to(device)
    generator = torch.Generator().manual_seed(seed)
    labels = torch.tensor(train_labels, dtype=torch.long)
    steps_per_epoch = (len(labels) + batch_size - 1) // batch_size
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate, weight_decay=weight_decay)
    scheduler = get_linear_schedule_with_warmup(
        optimizer, int(warmup_ratio * steps_per_epoch * epochs), steps_per_epoch * epochs
    )
    scaler = torch.amp.GradScaler("cuda", enabled=device.type == "cuda")
    loss_fn = nn.CrossEntropyLoss(weight=class_weights.to(device) if class_weights is not None else None)

    best_score, best_state, history = -1.0, None, []
    for epoch in range(1, epochs + 1):
        model.train()
        total_loss = 0.0
        for index in batches(len(labels), batch_size, shuffle=True, generator=generator):
            optimizer.zero_grad(set_to_none=True)
            with autocast(device):
                logits = model(
                    train_inputs["input_ids"][index].to(device),
                    train_inputs["attention_mask"][index].to(device),
                    train_inputs["features"][index].to(device) if train_inputs["features"] is not None else None,
                )
                loss = loss_fn(logits.float(), labels[index].to(device))
            scaler.scale(loss).backward()
            scaler.unscale_(optimizer)
            nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            scaler.step(optimizer)
            scaler.update()
            scheduler.step()
            total_loss += loss.item() * len(index)
        logits = predict_logits(
            model, val_inputs["input_ids"], val_inputs["attention_mask"], val_inputs["features"], device
        )
        score = score_fn(logits.argmax(axis=1))
        history.append({"epoch": epoch, "train_loss": total_loss / len(labels), "val_macro_f1": score})
        if score > best_score:
            best_score = score
            best_state = {key: value.detach().cpu().clone() for key, value in model.state_dict().items()}
    model.load_state_dict(best_state)
    return model, history


def export_state(model: HybridClassifier) -> dict:
    state = {}
    for key, value in model.state_dict().items():
        value = value.detach().cpu().clone()
        state[key] = value.half() if value.is_floating_point() else value
    return state
