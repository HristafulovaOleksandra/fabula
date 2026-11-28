import re
from dataclasses import dataclass, field

import numpy as np
import torch
from transformers import AutoTokenizer, RobertaConfig, RobertaForSequenceClassification

from . import settings

LABELS = ["toxicity", "severe_toxicity", "obscene", "identity_attack", "insult", "threat", "sexual_explicit"]
RUDE_LABELS = ["toxicity", "obscene", "insult", "threat"]
WORD_PATTERN = re.compile(r"[A-Za-z]+(?:['’][A-Za-z]+)*")
AGE_ORDER = ["6+", "12+", "16+"]
VERDICT_ORDER = ["ok", "review", "blocked"]


@dataclass
class FlaggedWord:
    text: str
    start: int
    end: int
    kind: str


@dataclass
class TextVerdict:
    age: str
    verdict: str
    reasons: list[str]
    scores: dict[str, float]
    words: list[FlaggedWord] = field(default_factory=list)


@dataclass
class ImageVerdict:
    age: str
    verdict: str
    reasons: list[str]
    detections: list[dict]


def stricter(first: str, second: str, order: list[str]) -> str:
    return first if order.index(first) >= order.index(second) else second


class TextModerator:
    def __init__(self) -> None:
        checkpoint = torch.load(settings.MODERATION_CHECKPOINT, map_location="cpu", weights_only=False)
        num_classes = checkpoint["config"]["arch"]["args"]["num_classes"]
        config = RobertaConfig.from_pretrained(settings.MODERATION_CONFIG_DIR, num_labels=num_classes)
        self.model = RobertaForSequenceClassification(config)
        self.model.load_state_dict(checkpoint["state_dict"], strict=False)
        self.model.eval()
        self.tokenizer = AutoTokenizer.from_pretrained(settings.MODERATION_CONFIG_DIR)

    @torch.no_grad()
    def logits(self, texts: list[str]) -> np.ndarray:
        encoded = self.tokenizer(texts, return_tensors="pt", truncation=True, padding=True, max_length=256)
        return self.model(**encoded).logits[:, : len(LABELS)].numpy()

    def scores(self, texts: list[str]) -> list[dict[str, float]]:
        if not texts:
            return []
        probabilities = 1 / (1 + np.exp(-self.logits(texts)))
        return [{label: float(row[index]) for index, label in enumerate(LABELS)} for row in probabilities]

    def judge(self, original: str) -> TextVerdict:
        text = WORD_PATTERN.sub(lambda m: m.group().lower() if len(m.group()) > 1 and m.group().isupper() else m.group(), original)
        scores = self.scores([text])[0]
        verdict, reasons = self.verdict(scores)
        rude = max(scores[label] for label in RUDE_LABELS)
        age = "12+" if rude >= settings.RUDE_THRESHOLD else "6+"
        words = self.flag_words(text, scores) if rude >= settings.RUDE_THRESHOLD or verdict != "ok" else []
        flagged_starts = {word.start for word in words}
        for match in WORD_PATTERN.finditer(text):
            if match.group().lower() in settings.SEXUAL_TERMS:
                if match.start() not in flagged_starts:
                    words.append(FlaggedWord(match.group(), match.start(), match.end(), "sexual"))
                else:
                    words = [FlaggedWord(w.text, w.start, w.end, "sexual") if w.start == match.start() else w for w in words]
                if verdict == "ok":
                    verdict = "review"
                    reasons.append("можливо, сексуальний зміст")
        words = sorted((FlaggedWord(original[w.start:w.end], w.start, w.end, w.kind) for w in words), key=lambda w: w.start)
        if any(word.kind == "strong" for word in words) or scores["severe_toxicity"] >= settings.SEVERE_THRESHOLD:
            age = "16+"
        if verdict != "ok":
            age = "16+"
        return TextVerdict(age, verdict, reasons, {key: round(value, 3) for key, value in scores.items()}, words)

    def verdict(self, scores: dict[str, float]) -> tuple[str, list[str]]:
        reasons = []
        verdict = "ok"
        for label, name in (("sexual_explicit", "сексуальний зміст"), ("identity_attack", "ненависть до групи людей")):
            review, block = settings.FORBIDDEN_THRESHOLDS[label]
            if scores[label] >= block:
                verdict = "blocked"
                reasons.append(name)
            elif scores[label] >= review:
                verdict = stricter(verdict, "review", VERDICT_ORDER)
                reasons.append(f"можливо, {name}")
        return verdict, reasons

    def flag_words(self, text: str, scores: dict[str, float]) -> list[FlaggedWord]:
        forbidden = [label for label in ("sexual_explicit", "identity_attack") if scores[label] >= settings.FORBIDDEN_THRESHOLDS[label][0]]
        driving = forbidden or RUDE_LABELS
        threshold = min(settings.FORBIDDEN_THRESHOLDS[label][0] for label in forbidden) if forbidden else settings.RUDE_THRESHOLD
        matches = list(WORD_PATTERN.finditer(text))
        removed: list[re.Match] = []

        def without(skip: list[re.Match]) -> str:
            parts, cursor = [], 0
            for match in sorted(skip, key=lambda m: m.start()):
                parts.append(text[cursor:match.start()])
                cursor = match.end()
            parts.append(text[cursor:])
            return "".join(parts)

        driving_index = [LABELS.index(label) for label in driving]
        threshold_logit = float(np.log(threshold / (1 - threshold)))
        current = float(self.logits([text])[0, driving_index].max())
        for _ in range(settings.MAX_FLAGGED_WORDS):
            if current < threshold_logit:
                break
            candidates = [match for match in matches if match not in removed]
            if not candidates:
                break
            results = self.logits([without([*removed, match]) for match in candidates])[:, driving_index].max(axis=1)
            best = int(np.argmin(results))
            if current - float(results[best]) < settings.MIN_WORD_DROP:
                break
            removed.append(candidates[best])
            current = float(results[best])

        context_kind = "sexual" if "sexual_explicit" in forbidden else "hate" if forbidden else "mild"
        kinds = {match.start(): context_kind for match in removed}
        if scores[max(RUDE_LABELS, key=lambda label: scores[label])] >= settings.RUDE_THRESHOLD and matches:
            alone = self.scores([match.group() for match in matches])
            for match, word_scores in zip(matches, alone):
                if max(word_scores[label] for label in RUDE_LABELS) >= settings.RUDE_THRESHOLD:
                    kinds.setdefault(match.start(), "mild")
        for match in matches:
            if self.is_strong(match.group()):
                kinds[match.start()] = "strong" if kinds.get(match.start(), "mild") == "mild" else kinds[match.start()]
        flagged = [match for match in matches if match.start() in kinds]
        return [
            FlaggedWord(
                match.group(),
                match.start(),
                match.end(),
                "strong" if kinds[match.start()] == "mild" and self.is_strong(match.group()) else kinds[match.start()],
            )
            for match in flagged
        ]

    @staticmethod
    def is_strong(word: str) -> bool:
        return word.lower().replace("’", "'") in settings.STRONG_PROFANITY


class ImageModerator:
    def __init__(self) -> None:
        from nudenet import NudeDetector

        self.detector = NudeDetector()

    def judge(self, data: bytes) -> ImageVerdict:
        detections = [
            {"label": item["class"], "score": round(float(item["score"]), 3)}
            for item in self.detector.detect(data)
        ]
        verdict, age, reasons = "ok", "6+", []
        for item in detections:
            if item["label"] in settings.EXPLICIT_LABELS:
                if item["score"] >= settings.IMAGE_BLOCK_THRESHOLD:
                    verdict = "blocked"
                    reasons.append("відверта оголеність")
                elif item["score"] >= settings.IMAGE_REVIEW_THRESHOLD:
                    verdict = stricter(verdict, "review", VERDICT_ORDER)
                    reasons.append("можлива оголеність")
            elif item["label"] in settings.SUGGESTIVE_LABELS and item["score"] >= settings.IMAGE_SUGGESTIVE_THRESHOLD:
                age = stricter(age, "16+", AGE_ORDER)
                reasons.append("відверте вбрання")
        if verdict != "ok":
            age = "16+"
        return ImageVerdict(age, verdict, sorted(set(reasons)), detections)
