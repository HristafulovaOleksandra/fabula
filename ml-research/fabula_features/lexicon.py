import csv
from dataclasses import dataclass, field
from pathlib import Path

WORD_LEVELS = ["A1", "A2", "B1", "B2", "C1", "C2"]
LEVEL_INDEX = {level: index for index, level in enumerate(WORD_LEVELS)}

SPACY_TO_WORDLIST_POS = {
    "NOUN": {"noun"},
    "VERB": {"verb"},
    "AUX": {"verb", "be-verb", "do-verb", "have-verb", "modal auxiliary"},
    "ADJ": {"adjective"},
    "ADV": {"adverb"},
    "PRON": {"pronoun"},
    "ADP": {"preposition"},
    "DET": {"determiner"},
    "CCONJ": {"conjunction"},
    "SCONJ": {"conjunction"},
    "NUM": {"number"},
    "INTJ": {"interjection"},
    "PART": {"adverb", "preposition", "infinitive-to"},
}

WORDLIST_FILES = (
    "cefrj-vocabulary-profile-1.5.csv",
    "octanove-vocabulary-profile-c1c2-1.0.csv",
)


@dataclass
class CefrLexicon:
    by_word_and_pos: dict[tuple[str, str], int] = field(default_factory=dict)
    by_word: dict[str, int] = field(default_factory=dict)

    @classmethod
    def load(cls, wordlist_dir: Path) -> "CefrLexicon":
        lexicon = cls()
        for file_name in WORDLIST_FILES:
            with open(wordlist_dir / file_name, encoding="utf-8-sig", newline="") as handle:
                for row in csv.DictReader(handle):
                    level = LEVEL_INDEX.get(row["CEFR"].strip())
                    if level is None:
                        continue
                    pos = row["pos"].strip().lower()
                    for variant in row["headword"].split("/"):
                        word = variant.strip().lower()
                        if not word or " " in word:
                            continue
                        lexicon._add(word, pos, level)
        return lexicon

    def _add(self, word: str, pos: str, level: int) -> None:
        key = (word, pos)
        self.by_word_and_pos[key] = min(level, self.by_word_and_pos.get(key, level))
        self.by_word[word] = min(level, self.by_word.get(word, level))

    def level_of(self, lemma: str, text: str, spacy_pos: str) -> int | None:
        lemma = lemma.lower()
        text = text.lower()
        for wordlist_pos in SPACY_TO_WORDLIST_POS.get(spacy_pos, ()):
            level = self.by_word_and_pos.get((lemma, wordlist_pos))
            if level is not None:
                return level
        for candidate in (lemma, text):
            level = self.by_word.get(candidate)
            if level is not None:
                return level
        return None
