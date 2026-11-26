import json
from dataclasses import dataclass

import numpy as np
import torch
from transformers import AutoTokenizer

from . import settings
from fabula_transformer import FeatureScaler, HybridClassifier


@dataclass
class LinePrediction:
    level: str
    index: int
    score: float
    confidence: float


class CefrModel:
    def __init__(self) -> None:
        final = json.loads(settings.FINAL_MODEL_FILE.read_text(encoding="utf-8"))
        checkpoint = torch.load(settings.MODELS_DIR / final["file"], map_location="cpu", weights_only=False)
        self.name = final["model"]
        self.levels: list[str] = final["levels"]
        thresholds = list(final["thresholds"])
        thresholds[-1] = max(thresholds[-1], settings.MIN_DIALOGUE_C_THRESHOLD)
        self.thresholds = np.asarray(thresholds, dtype=np.float64)
        self.feature_names: list[str] = checkpoint["features"]
        self.max_length: int = checkpoint["max_length"]
        self.scaler = FeatureScaler.from_dict(checkpoint["scaler"])
        self.tokenizer = AutoTokenizer.from_pretrained(checkpoint["model_name"])
        self.model = HybridClassifier(checkpoint["model_name"], n_features=len(self.feature_names))
        self.model.load_state_dict(
            {key: value.float() if value.is_floating_point() else value for key, value in checkpoint["state_dict"].items()}
        )
        self.model.eval()

    @torch.no_grad()
    def predict(self, texts: list[str], features: list[dict[str, float]], calibrated: list[bool]) -> list[LinePrediction]:
        if not texts:
            return []
        encoded = self.tokenizer(texts, padding=True, truncation=True, max_length=self.max_length, return_tensors="pt")
        matrix = np.asarray([[row[name] for name in self.feature_names] for row in features], dtype=np.float64)
        logits = self.model(encoded["input_ids"], encoded["attention_mask"], torch.tensor(self.scaler.transform(matrix)))
        probabilities = torch.softmax(logits.float(), dim=-1).numpy()
        scores = probabilities @ np.arange(len(self.levels))
        result = []
        for score, use_thresholds in zip(scores, calibrated):
            index = self.level_index(float(score), use_thresholds)
            confidence = self.confidence(float(score), index, use_thresholds)
            result.append(LinePrediction(level=self.levels[index], index=index, score=float(score), confidence=confidence))
        return result

    def boundaries(self, use_thresholds: bool) -> list[float]:
        inner = self.thresholds.tolist() if use_thresholds else [step + 0.5 for step in range(len(self.levels) - 1)]
        return [0.0, *inner, float(len(self.levels) - 1)]

    def confidence(self, score: float, index: int, use_thresholds: bool) -> float:
        edges = self.boundaries(use_thresholds)
        lower, upper = edges[index], edges[index + 1]
        if index == 0:
            distance = upper - score
            half = upper - lower
        elif index == len(self.levels) - 1:
            distance = score - lower
            half = upper - lower
        else:
            distance = min(score - lower, upper - score)
            half = (upper - lower) / 2
        return float(0.5 + 0.5 * np.clip(distance / half, 0.0, 1.0)) if half > 0 else 0.5

    def level_index(self, score: float, use_thresholds: bool) -> int:
        if use_thresholds:
            return int(np.searchsorted(self.thresholds, score, side="right"))
        return int(np.clip(np.floor(score + 0.5), 0, len(self.levels) - 1))
