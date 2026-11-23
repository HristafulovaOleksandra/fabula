from collections.abc import Sequence

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from sklearn.metrics import accuracy_score, cohen_kappa_score, confusion_matrix, f1_score

LEVELS = ["A1", "A2", "B1", "B2", "C"]
LABEL_IDS = list(range(len(LEVELS)))


def adjacent_accuracy(y_true: Sequence[int], y_pred: Sequence[int]) -> float:
    return float(np.mean(np.abs(np.asarray(y_true) - np.asarray(y_pred)) <= 1))


def macro_f1(y_true: Sequence[int], y_pred: Sequence[int]) -> float:
    return float(f1_score(y_true, y_pred, labels=LABEL_IDS, average="macro", zero_division=0))


ANNOTATOR_TO_LEVEL_ID = {1: 0, 2: 1, 3: 2, 4: 3, 5: 4, 6: 4}


def annotator_level_ids(labels: Sequence[int]) -> np.ndarray:
    return np.array([ANNOTATOR_TO_LEVEL_ID[int(label)] for label in labels])


def lenient_reference(
    y_true: Sequence[int], y_pred: Sequence[int], level_a: Sequence[int], level_b: Sequence[int]
) -> np.ndarray:
    y_true, y_pred = np.asarray(y_true), np.asarray(y_pred)
    matches_annotator = (y_pred == np.asarray(level_a)) | (y_pred == np.asarray(level_b))
    return np.where(matches_annotator, y_pred, y_true)


def evaluate(
    y_true: Sequence[int],
    y_pred: Sequence[int],
    level_a: Sequence[int] | None = None,
    level_b: Sequence[int] | None = None,
) -> dict[str, float]:
    per_class = f1_score(y_true, y_pred, labels=LABEL_IDS, average=None, zero_division=0)
    scores = {
        "macro_f1": macro_f1(y_true, y_pred),
        "accuracy": float(accuracy_score(y_true, y_pred)),
        "adjacent_accuracy": adjacent_accuracy(y_true, y_pred),
        "qwk": float(cohen_kappa_score(y_true, y_pred, weights="quadratic")),
        **{f"f1_{level}": float(score) for level, score in zip(LEVELS, per_class)},
    }
    if level_a is not None and level_b is not None:
        reference = lenient_reference(y_true, y_pred, level_a, level_b)
        scores["macro_f1_lenient"] = macro_f1(reference, y_pred)
        scores["accuracy_lenient"] = float(accuracy_score(reference, y_pred))
    return scores


def bootstrap_ci(
    y_true: Sequence[int],
    y_pred: Sequence[int],
    metric=macro_f1,
    n_resamples: int = 1000,
    seed: int = 42,
    alpha: float = 0.05,
) -> tuple[float, float]:
    y_true = np.asarray(y_true)
    y_pred = np.asarray(y_pred)
    rng = np.random.default_rng(seed)
    scores = []
    for _ in range(n_resamples):
        index = rng.integers(0, len(y_true), len(y_true))
        scores.append(metric(y_true[index], y_pred[index]))
    return float(np.quantile(scores, alpha / 2)), float(np.quantile(scores, 1 - alpha / 2))


def class_f1(level_id: int):
    def metric(y_true, y_pred) -> float:
        return float(f1_score(y_true, y_pred, labels=[level_id], average="macro", zero_division=0))

    return metric


def plot_confusion(y_true: Sequence[int], y_pred: Sequence[int], title: str, ax=None):
    matrix = confusion_matrix(y_true, y_pred, labels=LABEL_IDS)
    normalized = matrix / matrix.sum(axis=1, keepdims=True).clip(min=1)
    if ax is None:
        _, ax = plt.subplots(figsize=(5, 4.5))
    ax.imshow(normalized, cmap="Blues", vmin=0, vmax=1)
    for row in range(len(LEVELS)):
        for column in range(len(LEVELS)):
            ax.text(
                column, row, f"{normalized[row, column]:.2f}\n({matrix[row, column]})",
                ha="center", va="center", fontsize=8,
                color="white" if normalized[row, column] > 0.5 else "#1E2766",
            )
    ax.set_xticks(LABEL_IDS, LEVELS)
    ax.set_yticks(LABEL_IDS, LEVELS)
    ax.set_xlabel("Передбачений рівень")
    ax.set_ylabel("Справжній рівень")
    ax.set_title(title)
    return ax


def results_frame(rows: dict[str, dict[str, float]]) -> pd.DataFrame:
    return pd.DataFrame(rows).T.round(3)
