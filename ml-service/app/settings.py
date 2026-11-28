import os
import sys
from pathlib import Path

SERVICE_DIR = Path(__file__).resolve().parents[1]
RESEARCH_DIR = Path(os.environ.get("FABULA_RESEARCH_DIR", SERVICE_DIR.parent / "ml-research")).resolve()
MODELS_DIR = RESEARCH_DIR / "models"
FINAL_MODEL_FILE = RESEARCH_DIR / "results" / "05" / "final_model.json"
CERTAINTY_FILE = RESEARCH_DIR / "results" / "05" / "above_certainty.json"
MODERATION_DIR = MODELS_DIR / "moderation"
MODERATION_CHECKPOINT = MODERATION_DIR / "toxic_debiased-c7548aa0.ckpt"
MODERATION_CONFIG_DIR = MODERATION_DIR / "roberta-base"
RUDE_THRESHOLD = 0.5
SEVERE_THRESHOLD = 0.1
FORBIDDEN_THRESHOLDS = {"sexual_explicit": (0.5, 0.8), "identity_attack": (0.4, 0.6)}
MAX_FLAGGED_WORDS = 3
MIN_WORD_DROP = 1.0
STRONG_PROFANITY = {
    "fuck", "fucking", "fucked", "fucker", "motherfucker", "shit", "shitty", "bullshit", "bitch", "bitches",
    "asshole", "bastard", "cunt", "dick", "dickhead", "piss", "pissed", "cock", "prick", "slut", "whore", "wanker",
}
SEXUAL_TERMS = {
    "sex", "sexy", "sexual", "porn", "porno", "pornography", "nude", "nudes", "nudity", "naked", "nsfw", "xxx",
    "erotic", "orgasm", "horny", "boobs", "tits", "pussy", "penis", "vagina", "condom", "stripper", "blowjob",
}
EXPLICIT_LABELS = {"FEMALE_GENITALIA_EXPOSED", "MALE_GENITALIA_EXPOSED", "FEMALE_BREAST_EXPOSED", "ANUS_EXPOSED", "BUTTOCKS_EXPOSED"}
SUGGESTIVE_LABELS = {"FEMALE_BREAST_COVERED", "FEMALE_GENITALIA_COVERED", "BUTTOCKS_COVERED"}
IMAGE_BLOCK_THRESHOLD = 0.6
IMAGE_REVIEW_THRESHOLD = 0.4
IMAGE_SUGGESTIVE_THRESHOLD = 0.5
WORDLIST_DIR = RESEARCH_DIR / "data" / "raw" / "cefr-j"
NLTK_DATA_DIR = Path(os.environ.get("FABULA_NLTK_DATA", SERVICE_DIR / "nltk_data")).resolve()

MIN_WORDS_FOR_LEVEL = 3
MAX_WORDS_WHOLE_DIALOGUE = 30
MAX_TEXT_CHARS = 600
MAX_SIMPLER_WORDS = 3
MAX_WORD_SENSES = 2
MAX_SIMPLER_PARTS = 2
MIN_SIMPLER_ZIPF = 3.5
RARE_WORD_ZIPF = 3.0
MIN_DIALOGUE_C_THRESHOLD = 2.5

os.environ.setdefault("HF_HUB_OFFLINE", "1")
if str(RESEARCH_DIR) not in sys.path:
    sys.path.insert(0, str(RESEARCH_DIR))
