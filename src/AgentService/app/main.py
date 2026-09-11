from pathlib import Path
import hashlib
import os
from fastapi import FastAPI
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError
from .agent import MechraAgent
from .models import CadVerificationRequest, CadVerificationResponse, ChatRequest, ChatResponse
from .verification import verify

VERSION = '0.2.0-dev.4'
_source = Path(__file__).parent
BUILD_ID = hashlib.sha256(b''.join(p.read_bytes() for p in sorted(_source.glob('*.py')))).hexdigest()[:16]
app = FastAPI(title='Mechra Agent', version=VERSION)
agent = MechraAgent()


@app.exception_handler(RequestValidationError)
async def invalid_request(request, exc):
    # Do not echo invalid request content (e.g. NaN or user text) into JSON errors.
    return JSONResponse(status_code=422, content={'detail': [
        {'loc': list(e['loc']), 'msg': e['msg'], 'type': e['type']} for e in exc.errors()]})


@app.get('/health')
def health():
    return {'ok': True, 'service': 'mechra-agent', 'version': VERSION, 'build_id': BUILD_ID,
            'pid': os.getpid(), 'planner': 'deterministic', 'contract_version': '0.2'}


@app.post('/v1/chat', response_model=ChatResponse)
def chat(request: ChatRequest):
    return agent.reply(request)


@app.post('/v1/verify', response_model=CadVerificationResponse)
def verify_endpoint(request: CadVerificationRequest):
    return verify(request)
