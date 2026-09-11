"""Start a real local agent, test UTF-8 planning/verification, then stop only that child."""
import json
import socket
import subprocess
import sys
import tempfile
import time
from pathlib import Path
from urllib.request import Request, urlopen

ROOT=Path(__file__).resolve().parents[1]
with socket.socket() as s:
    s.bind(('127.0.0.1',0)); port=s.getsockname()[1]
base=f'http://127.0.0.1:{port}'

def request(path,payload=None):
    req=Request(base+path, data=None if payload is None else json.dumps(payload,ensure_ascii=False).encode('utf-8'),
                headers={'Content-Type':'application/json; charset=utf-8'})
    with urlopen(req,timeout=4) as r: return json.load(r)

with tempfile.TemporaryFile() as log:
    process=subprocess.Popen([sys.executable,'-m','uvicorn','app.main:app','--host','127.0.0.1','--port',str(port)],cwd=ROOT/'src/AgentService',stdout=log,stderr=log)
    try:
        health=None
        for i in range(50):
            if process.poll() is not None: raise RuntimeError('agent exited before startup')
            try: health=request('/health');break
            except OSError: time.sleep(.1)
        assert health and health['service']=='mechra-agent'
        metadata=json.loads((ROOT/'src/Shared/tests/feature-scope-cases.json').read_text())[0]['features']
        context={'document_type':'part','document_id':'smoke-doc','update_stamp':1,'features':metadata}
        def chat(message):return request('/v1/chat',{'message':message,'context':context,'session_id':'smoke-session'})
        assert chat('Tạo plate 100 × 60 mm')['status']=='clarification_required'
        created=chat('5 mm')
        assert created['plan']['operations'][0]['inputs']['thickness_mm']==5
        assert created['requires_confirmation'] is True
        assert chat('Tạo plate -100 x 60 x 5 mm')['plan'] is None
        context['features']=metadata+[{'name':'Boss-Extrude1','type_name':'Extrusion'}]
        assert chat('Tạo plate 100 x 60 x 5 mm')['plan'] is None
        context['features']=metadata+[{'name':'Mechra-Plate-Extrude','type_name':'Boss'}]
        assert chat('Đổi chiều dày thành 8 mm')['plan']['operations'][0]['inputs']['thickness_mm']==8
        cases=json.loads((ROOT/'src/Shared/tests/verification-cases.json').read_text())
        assert request('/v1/verify',cases[0]['snapshot'])['passed'] is True
        assert request('/v1/verify',cases[2]['snapshot'])['passed'] is False
        print(f"Real HTTP smoke PASS: {health['version']} build={health['build_id']}; Design Binder template, UTF-8 clarify/create/edit, nonblank refusal, invalid input and volume checks.")
    except Exception:
        log.seek(0);sys.stderr.write(log.read().decode('utf-8',errors='replace'));raise
    finally:
        process.terminate()
        try:process.wait(timeout=5)
        except subprocess.TimeoutExpired:process.kill();process.wait()
