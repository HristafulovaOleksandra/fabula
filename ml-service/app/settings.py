import os
import sys
from pathlib import Path

SERVICE_DIR = Path(__file__).resolve().parents[1]
RESEARCH_DIR = Path(os.environ.get("FABULA_RESEARCH_DIR", SERVICE_DIR.parent / "ml-research")).resolve()
MODELS_DIR = RESEARCH_DIR / "models"
FINAL_MODEL_FILE = RESEARCH_DIR / "results" / "05" / "final_model.json"
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

os.environ.setdefault("HF_HUB_OFFLINE", "1")
if str(RESEARCH_DIR) not in sys.path:
    sys.path.insert(0, str(RESEARCH_DIR))
