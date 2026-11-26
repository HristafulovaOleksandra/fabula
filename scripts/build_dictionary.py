import re
import sys
import urllib.request
from collections import Counter
from pathlib import Path

import pymorphy3

ROOT = Path(__file__).resolve().parents[1]
SOURCE_URL = "https://dl.fbaipublicfiles.com/arrival/dictionaries/en-uk.txt"
SOURCE = ROOT / "ml-research" / "data" / "dictionary" / "en-uk.txt"
TARGET = ROOT / "backend" / "Fabula.Api" / "Resources" / "en-uk.tsv"
MAX_TRANSLATIONS = 3
ENGLISH = re.compile(r"^[a-z][a-z'-]*$")
UKRAINIAN = re.compile(r"^[а-щьюяєіїґ'’-]+$")


def download():
    if not SOURCE.exists():
        SOURCE.parent.mkdir(parents=True, exist_ok=True)
        urllib.request.urlretrieve(SOURCE_URL, SOURCE)


def read_pairs():
    pairs = {}
    for line in SOURCE.read_text(encoding="utf-8").splitlines():
        parts = line.split()
        if len(parts) != 2:
            continue
        english, ukrainian = parts[0].lower(), parts[1].lower()
        if ENGLISH.match(english) and UKRAINIAN.match(ukrainian):
            pairs.setdefault(english, []).append(ukrainian)
    return pairs


def lemma(word, morph):
    parses = morph.parse(word)
    return word if any(parse.normal_form == word for parse in parses) else parses[0].normal_form


def rank_lemmas(candidates, morph):
    lemmas = [lemma(word, morph) for word in candidates]
    counts = Counter(lemmas)
    first_seen = {lemma: index for index, lemma in reversed(list(enumerate(lemmas)))}
    return sorted(counts, key=lambda lemma: (-counts[lemma], first_seen[lemma]))[:MAX_TRANSLATIONS]


def main():
    download()
    morph = pymorphy3.MorphAnalyzer(lang="uk")
    pairs = read_pairs()
    rows = [f"{english}\t{'|'.join(rank_lemmas(candidates, morph))}" for english, candidates in sorted(pairs.items())]
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    TARGET.write_text("\n".join(rows) + "\n", encoding="utf-8")
    print(f"{len(rows)} words -> {TARGET.relative_to(ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
