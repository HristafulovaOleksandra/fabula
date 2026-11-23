from fastapi import FastAPI

app = FastAPI(title="Fabula ML Service")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "fabula-ml"}
