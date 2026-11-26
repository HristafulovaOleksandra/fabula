import json
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.isotonic import IsotonicRegression

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "ml-research" / "results"
OUTPUT = RESULTS / "05" / "above_certainty.json"
MIN_DIALOGUE_C_THRESHOLD = 2.5
MARGIN_BINS = [0.1, 0.2, 0.3, 0.45, 0.6, 0.8]
RED_FROM = 0.8
SEEDS = [42, 43, 44]


def dialogue_edges() -> list[float]:
    final = json.loads((RESULTS / "05" / "final_model.json").read_text(encoding="utf-8"))
    edges = list(final["thresholds"])
    edges[-1] = max(edges[-1], MIN_DIALOGUE_C_THRESHOLD)
    return edges


def margins_above(scores: np.ndarray, gold: np.ndarray, edges: list[float]) -> pd.DataFrame:
    rows = []
    for target, edge in enumerate(edges):
        above = scores > edge
        rows.append(pd.DataFrame({"margin": scores[above] - edge, "correct": gold[above] > target}))
    return pd.concat(rows, ignore_index=True)


def dialogue_margins() -> pd.DataFrame:
    scores = pd.read_csv(RESULTS / "05" / "dialog_scores.csv")
    gold = pd.read_csv(RESULTS / "05" / "gold.csv")[["item_id", "level_id"]]
    data = scores.merge(gold, on="item_id")
    return margins_above(data["M4-final"].to_numpy(), data["level_id"].to_numpy(), dialogue_edges())


def narration_margins() -> pd.DataFrame:
    frames = []
    for seed in SEEDS:
        data = pd.read_csv(RESULTS / "04" / f"M4_final_seed{seed}_validation.csv")
        logits = data[[column for column in data.columns if column.startswith("logit_")]].to_numpy()
        probabilities = np.exp(logits - logits.max(axis=1, keepdims=True))
        probabilities /= probabilities.sum(axis=1, keepdims=True)
        scores = probabilities @ np.arange(logits.shape[1])
        frames.append(margins_above(scores, data["level_id"].to_numpy(), [step + 0.5 for step in range(logits.shape[1] - 1)]))
    return pd.concat(frames, ignore_index=True)


def certainty_table(margins: pd.DataFrame) -> list[dict]:
    limits = [*MARGIN_BINS, np.inf]
    margins = margins.assign(bin=np.searchsorted(MARGIN_BINS, margins["margin"], side="left"))
    grouped = margins.groupby("bin")["correct"].agg(["size", "mean"]).reindex(range(len(limits)), fill_value=0)
    smooth = IsotonicRegression(y_min=0.5, y_max=1.0).fit_transform(
        np.arange(len(limits)), grouped["mean"].to_numpy(), sample_weight=np.maximum(grouped["size"].to_numpy(), 1)
    )
    return [
        {
            "maxMargin": None if np.isinf(limit) else limit,
            "observed": round(float(observed), 3),
            "certainty": round(float(value), 3),
            "count": int(count),
        }
        for limit, observed, value, count in zip(limits, grouped["mean"], smooth, grouped["size"])
    ]


def main() -> None:
    table = {
        "redFrom": RED_FROM,
        "source": {
            "dialogue": "300 DailyDialog utterances annotated by the author, M4-final scores, calibrated thresholds with C >= 2.5",
            "narration": "CEFR-SP validation split, M4-final seeds 42-44, CEFR-SP scale",
        },
        "dialogue": {"edges": dialogue_edges(), "bins": certainty_table(dialogue_margins())},
        "narration": {"edges": [0.5, 1.5, 2.5, 3.5], "bins": certainty_table(narration_margins())},
    }
    OUTPUT.write_text(json.dumps(table, indent=2, ensure_ascii=False), encoding="utf-8")
    for scale in ("dialogue", "narration"):
        print(scale, [(row["maxMargin"], row["observed"], row["certainty"], row["count"]) for row in table[scale]["bins"]])


if __name__ == "__main__":
    main()
