from dataclasses import dataclass, field

from nltk.corpus import wordnet
from spacy.tokens import Doc, Token
from wordfreq import zipf_frequency

from . import settings
from fabula_features import CefrLexicon

STORY_LEVELS = ["A1", "A2", "B1", "B2", "C"]
WORDNET_POS = {"NOUN": [wordnet.NOUN], "VERB": [wordnet.VERB], "ADJ": [wordnet.ADJ, wordnet.ADJ_SAT], "ADV": [wordnet.ADV]}


def story_level_index(word_level: int) -> int:
    return min(word_level, len(STORY_LEVELS) - 1)


@dataclass
class WordInfo:
    text: str
    start: int
    end: int
    lemma: str
    level: str | None
    above_target: bool
    rare: bool
    simpler: list[str] = field(default_factory=list)


class WordAnalyzer:
    def __init__(self, lexicon: CefrLexicon) -> None:
        self.lexicon = lexicon
        wordnet.ensure_loaded()

    def analyze(self, doc: Doc, leveled: list[tuple[Token, int | None]], target: str) -> list[WordInfo]:
        target_index = STORY_LEVELS.index(target)
        result = []
        for token, level in leveled:
            index = None if level is None else story_level_index(level)
            rare = level is None and zipf_frequency(token.text.lower(), "en") < settings.RARE_WORD_ZIPF
            above = (index is not None and index > target_index) or rare
            result.append(
                WordInfo(
                    text=token.text,
                    start=token.idx,
                    end=token.idx + len(token.text),
                    lemma=token.lemma_.lower(),
                    level=None if index is None else STORY_LEVELS[index],
                    above_target=above,
                    rare=rare,
                    simpler=self.simpler(token, target_index) if above else [],
                )
            )
        return result

    def simpler(self, token: Token, target_index: int) -> list[str]:
        lemma = token.lemma_.lower()
        min_zipf = max(settings.MIN_SIMPLER_ZIPF, zipf_frequency(lemma, "en"))
        candidates: dict[str, tuple[int, float]] = {}
        for pos in WORDNET_POS.get(token.pos_, []):
            senses = wordnet.synsets(lemma, pos=pos)[: settings.MAX_WORD_SENSES]
            for synset in senses:
                for name in synset.lemma_names():
                    phrase = name.lower().replace("_", " ")
                    parts = phrase.split()
                    if "-" in phrase or phrase == lemma or phrase in candidates or len(parts) > settings.MAX_SIMPLER_PARTS:
                        continue
                    if len(parts) == 1 and not self.shares_main_sense(name, pos, senses):
                        continue
                    level = self.phrase_level(parts, token.pos_)
                    frequency = zipf_frequency(phrase, "en")
                    if level is not None and story_level_index(level) <= target_index and frequency >= min_zipf:
                        candidates[phrase] = (level, frequency)
        single = {word: rank for word, rank in candidates.items() if " " not in word}
        pool = single or candidates
        ranked = sorted(pool.items(), key=lambda item: (item[1][0], -item[1][1]))
        return [word for word, _ in ranked[: settings.MAX_SIMPLER_WORDS]]

    @staticmethod
    def shares_main_sense(name: str, pos: str, senses: list) -> bool:
        main = wordnet.synsets(name, pos=pos)[: settings.MAX_WORD_SENSES]
        return any(sense in senses for sense in main)

    def phrase_level(self, parts: list[str], spacy_pos: str) -> int | None:
        levels = [self.lexicon.level_of(part, part, spacy_pos if index == 0 else "ADP") for index, part in enumerate(parts)]
        return None if any(level is None for level in levels) else max(levels)
