from fastapi import FastAPI
from .agent import FoundationAgent
from .models import ChatRequest, ChatResponse

app = FastAPI(title="Mechra Agent", version="0.1.0")
agent = FoundationAgent()


@app.get("/health")
def health():
    return {"ok": True, "service": "mineural-agent", "version": "0.1.0"}


@app.post("/v1/chat", response_model=ChatResponse)
def chat(request: ChatRequest):
    return agent.reply(request)
