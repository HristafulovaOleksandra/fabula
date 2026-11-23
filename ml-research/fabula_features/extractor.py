from collections.abc import Iterable
from pathlib import Path

import pyphen
import spacy
from spacy.tokens import Doc, Token
from wordfreq import zipf_frequency

from .lexicon import CefrLexicon

NON_LEXICAL_POS = {"PUNCT", "SYM", "NUM", "PROPN", "SPACE", "X"}
CONTENT_POS = {"NOUN", "VERB", "ADJ", "ADV"}
SUBORDINATE_DEPS = {"advcl", "ccomp", "acl", "relcl", "csubj", "csubjpass"}
PASSIVE_DEPS = {"auxpass", "nsubjpass", "csubjpass"}
FINITE_TAGS = {"VBD", "VBP", "VBZ", "MD"}
FUTURE_MODALS = {"will", "shall", "'ll"}
CONTRACTION_SUFFIXES = ("'", "’")
CONTRACTION_FORMS = {"n't", "n’t"}
PERSONAL_PRONOUNS = {"i", "me", "my", "you", "your", "we", "us", "our"}
ADDRESS_WORDS = {
    "mom", "mum", "dad", "sir", "madam", "ma'am", "miss", "guys", "honey",
    "darling", "dear", "buddy", "man", "mate", "kids", "everyone", "folks",
}

LENGTH_FEATURES = ["n_words", "chars_per_word", "n_syllables", "syllables_per_word"]
LEXICAL_FEATURES = [
    "share_a1", "share_a2", "share_b1", "share_b2", "share_c", "share_oov",
    "max_word_level", "mean_word_level", "mean_zipf", "min_zipf",
    "type_token_ratio", "lexical_density",
]
GRAMMAR_FEATURES = [
    "tree_depth", "n_subordinate_clauses", "n_finite_verbs",
    "has_past", "has_present", "has_future", "has_perfect", "has_progressive",
    "n_passive", "n_modals",
]
DIALOGUE_FEATURES = [
    "n_contractions", "n_interjections", "is_question", "is_exclamation",
    "has_vocative", "is_fragment", "share_personal_pronouns",
]
FEATURE_GROUPS = {
    "length": LENGTH_FEATURES,
    "lexical": LEXICAL_FEATURES,
    "grammar": GRAMMAR_FEATURES,
    "dialogue": DIALOGUE_FEATURES,
}
FEATURE_NAMES = [name for group in FEATURE_GROUPS.values() for name in group]


def _mean(values: list[float]) -> float:
    return sum(values) / len(values) if values else 0.0


def _depth(token: Token) -> int:
    depth = 0
    while token.head.i != token.i:
        token = token.head
        depth += 1
    return depth


def _is_word(token: Token) -> bool:
    return not token.is_punct and not token.is_space and any(ch.isalnum() for ch in token.text)


def _is_contraction(token: Token) -> bool:
    lowered = token.text.lower()
    return lowered in CONTRACTION_FORMS or (lowered.startswith(CONTRACTION_SUFFIXES) and len(lowered) > 1)


def _has_vocative(doc: Doc) -> bool:
    words = [token for token in doc if not token.is_space]
    if len(words) < 3:
        return False
    opening = words[1:] if words[0].pos_ == "INTJ" and words[1].text != "," else words
    if len(opening) >= 2 and opening[1].text == ",":
        first = opening[0]
        if first.pos_ == "PROPN" or first.lower_ in ADDRESS_WORDS:
            return True
    closing = words[:-1] if words[-1].is_punct else words
    if len(closing) >= 2 and closing[-2].text == ",":
        last = closing[-1]
        return last.pos_ == "PROPN" or last.lower_ in ADDRESS_WORDS
    return False


def _auxiliaries(token: Token) -> list[Token]:
    return [child for child in token.children if child.dep_ in {"aux", "auxpass"}]


class FeatureExtractor:
    def __init__(self, wordlist_dir: Path, spacy_model: str = "en_core_web_sm") -> None:
        self.lexicon = CefrLexicon.load(wordlist_dir)
        self.nlp = spacy.load(spacy_model, disable=["ner"])
        self.hyphenator = pyphen.Pyphen(lang="en_US")

    def syllables(self, word: str) -> int:
        return max(1, len(self.hyphenator.positions(word)) + 1)

    def word_levels(self, doc: Doc) -> list[tuple[Token, int | None]]:
        return [
            (token, self.lexicon.level_of(token.lemma_, token.text, token.pos_))
            for token in doc
            if _is_word(token) and token.pos_ not in NON_LEXICAL_POS
        ]

    def extract_many(self, texts: Iterable[str], batch_size: int = 256) -> list[dict[str, float]]:
        return [self.features(doc) for doc in self.nlp.pipe(texts, batch_size=batch_size)]

    def extract(self, text: str) -> dict[str, float]:
        return self.features(self.nlp(text))

    def features(self, doc: Doc) -> dict[str, float]:
        result: dict[str, float] = {}
        result.update(self._length(doc))
        result.update(self._lexical(doc))
        result.update(self._grammar(doc))
        result.update(self._dialogue(doc))
        return result

    def _length(self, doc: Doc) -> dict[str, float]:
        words = [token for token in doc if _is_word(token)]
        alpha_words = [token.text for token in words if token.is_alpha]
        syllables = [self.syllables(word) for word in alpha_words]
        return {
            "n_words": len(words),
            "chars_per_word": _mean([len(word) for word in alpha_words]),
            "n_syllables": sum(syllables),
            "syllables_per_word": _mean(syllables),
        }

    def _lexical(self, doc: Doc) -> dict[str, float]:
        leveled = self.word_levels(doc)
        total = len(leveled)
        levels = [level for _, level in leveled if level is not None]

        def share(condition) -> float:
            return sum(1 for _, level in leveled if condition(level)) / total if total else 0.0

        zipfs = [zipf_frequency(token.text.lower(), "en") for token, _ in leveled if token.is_alpha]
        lemmas = [token.lemma_.lower() for token, _ in leveled]
        return {
            "share_a1": share(lambda level: level == 0),
            "share_a2": share(lambda level: level == 1),
            "share_b1": share(lambda level: level == 2),
            "share_b2": share(lambda level: level == 3),
            "share_c": share(lambda level: level is not None and level >= 4),
            "share_oov": share(lambda level: level is None),
            "max_word_level": max(levels) if levels else 0,
            "mean_word_level": _mean(levels),
            "mean_zipf": _mean(zipfs),
            "min_zipf": min(zipfs) if zipfs else 0.0,
            "type_token_ratio": len(set(lemmas)) / total if total else 0.0,
            "lexical_density": sum(1 for token, _ in leveled if token.pos_ in CONTENT_POS) / total if total else 0.0,
        }

    def _grammar(self, doc: Doc) -> dict[str, float]:
        verbs = [token for token in doc if token.pos_ in {"VERB", "AUX"}]
        has_perfect = any(
            token.tag_ == "VBN" and any(aux.lemma_ == "have" for aux in _auxiliaries(token)) for token in verbs
        )
        has_progressive = any(
            token.tag_ == "VBG" and any(aux.lemma_ == "be" for aux in _auxiliaries(token)) for token in verbs
        )
        has_future = any(
            any(aux.lower_ in FUTURE_MODALS for aux in _auxiliaries(token))
            or (token.lemma_ == "go" and token.tag_ == "VBG" and any(child.dep_ == "xcomp" for child in token.children))
            for token in verbs
        )
        return {
            "tree_depth": max((_depth(token) for token in doc), default=0),
            "n_subordinate_clauses": sum(1 for token in doc if token.dep_ in SUBORDINATE_DEPS),
            "n_finite_verbs": sum(1 for token in doc if token.tag_ in FINITE_TAGS),
            "has_past": int(any(token.tag_ == "VBD" for token in doc)),
            "has_present": int(any(token.tag_ in {"VBP", "VBZ"} for token in doc)),
            "has_future": int(has_future),
            "has_perfect": int(has_perfect),
            "has_progressive": int(has_progressive),
            "n_passive": sum(1 for token in doc if token.dep_ == "auxpass"),
            "n_modals": sum(1 for token in doc if token.tag_ == "MD" and token.lower_ not in FUTURE_MODALS),
        }

    def _dialogue(self, doc: Doc) -> dict[str, float]:
        words = [token for token in doc if _is_word(token)]
        stripped = doc.text.rstrip()
        return {
            "n_contractions": sum(1 for token in doc if _is_contraction(token)),
            "n_interjections": sum(1 for token in doc if token.pos_ == "INTJ"),
            "is_question": int(stripped.endswith("?")),
            "is_exclamation": int(stripped.endswith("!")),
            "has_vocative": int(_has_vocative(doc)),
            "is_fragment": int(not any(token.tag_ in FINITE_TAGS for token in doc)),
            "share_personal_pronouns": (
                sum(1 for token in words if token.lower_ in PERSONAL_PRONOUNS) / len(words) if words else 0.0
            ),
        }
