from contextlib import asynccontextmanager
from functools import lru_cache
from threading import Lock
from typing import Literal

from fastapi import FastAPI
from pydantic import BaseModel, Field

from . import settings
import nltk

nltk.data.path.insert(0, str(settings.NLTK_DATA_DIR))

from fabula_features import FeatureExtractor
from .cefr_model import CefrModel
from .words import WordAnalyzer

Level = Literal["A1", "A2", "B1", "B2", "C"]
state: dict = {}
analysis_lock = Lock()


@asynccontextmanager
async def lifespan(_: FastAPI):
    extractor = FeatureExtractor(settings.WORDLIST_DIR)
    state["extractor"] = extractor
    state["model"] = CefrModel()
    state["words"] = WordAnalyzer(extractor.lexicon)
    yield
    state.clear()


app = FastAPI(title="Fabula ML Service", lifespan=lifespan)


class AnalyzeRequest(BaseModel):
    texts: list[str] = Field(max_length=100)
    target_level: Level = Field(default="B2", alias="targetLevel")
    narration: list[bool] | None = Field(default=None, max_length=100)


class WordResult(BaseModel):
    text: str
    start: int
    end: int
    lemma: str
    level: str | None
    above_target: bool = Field(serialization_alias="aboveTarget")
    rare: bool
    simpler: list[str]


class SentenceResult(BaseModel):
    start: int
    end: int
    level: str
    score: float


class LineResult(BaseModel):
    level: str | None
    score: float | None
    confidence: float | None
    scale: Literal["dialogue", "narration"]
    too_short: bool = Field(serialization_alias="tooShort")
    sentences: list[SentenceResult]
    words: list[WordResult]


class AnalyzeResponse(BaseModel):
    model: str
    results: list[LineResult]


def word_count(features: dict[str, float]) -> int:
    return int(features["n_words"])


@lru_cache(maxsize=4096)
def analyze_one(text: str, target: str, narration: bool) -> LineResult:
    extractor: FeatureExtractor = state["extractor"]
    doc = extractor.nlp(text[: settings.MAX_TEXT_CHARS])
    words = [WordResult(**vars(word)) for word in state["words"].analyze(doc, extractor.word_levels(doc), target)]
    scale = "narration" if narration else "dialogue"
    whole = extractor.features(doc)
    split = narration or word_count(whole) > settings.MAX_WORDS_WHOLE_DIALOGUE
    units = [(span.start_char, span.end_char, extractor.features(span.as_doc())) for span in doc.sents] if split else [(0, len(doc.text), whole)]
    units = [unit for unit in units if word_count(unit[2]) >= settings.MIN_WORDS_FOR_LEVEL]
    if not units:
        return LineResult(level=None, score=None, confidence=None, scale=scale, too_short=True, sentences=[], words=words)
    predictions = state["model"].predict(
        [doc.text[start:end] for start, end, _ in units], [features for _, _, features in units], [not narration] * len(units)
    )
    hardest = max(predictions, key=lambda p: (p.index, p.score))
    sentences = [
        SentenceResult(start=start, end=end, level=p.level, score=round(p.score, 4))
        for (start, end, _), p in zip(units, predictions)
    ]
    return LineResult(
        level=hardest.level,
        score=round(hardest.score, 4),
        confidence=round(hardest.confidence, 4),
        scale=scale,
        too_short=False,
        sentences=sentences if split else [],
        words=words,
    )


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "fabula-ml", "model": state["model"].name if "model" in state else "loading"}


@app.post("/analyze", response_model=AnalyzeResponse, response_model_by_alias=True)
def analyze(request: AnalyzeRequest) -> AnalyzeResponse:
    flags = request.narration or [False] * len(request.texts)
    with analysis_lock:
        results = [
            analyze_one(text.strip(), request.target_level, bool(flags[index]) if index < len(flags) else False)
            for index, text in enumerate(request.texts)
        ]
    return AnalyzeResponse(model=state["model"].name, results=results)
