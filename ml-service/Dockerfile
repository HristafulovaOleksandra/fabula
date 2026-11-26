FROM python:3.14-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    HF_HOME=/hf \
    HF_HUB_OFFLINE=1 \
    FABULA_RESEARCH_DIR=/research \
    FABULA_NLTK_DATA=/nltk_data

WORKDIR /service
COPY requirements.txt .
RUN pip install -r requirements.txt
COPY nltk_data /nltk_data
RUN python -m zipfile -e /nltk_data/corpora/wordnet.zip /nltk_data/corpora/ && rm /nltk_data/corpora/wordnet.zip && chmod -R go-w /nltk_data
COPY app ./app

EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
