"""Generate transport schemas from validated Python models. Run from any directory."""
from pathlib import Path
import json
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'src/AgentService'))
from app.models import CadPlan, PlateInputs, ThicknessInputs, CadVerificationRequest, ModelContext, ChatRequest, ChatResponse
out=ROOT/'src/Shared/contracts'
for name,cls in [('cad-plan',CadPlan),('verification',CadVerificationRequest),('model-context',ModelContext),('chat-request',ChatRequest),('chat-response',ChatResponse)]:
    schema=cls.model_json_schema()
    schema.update({'$schema':'https://json-schema.org/draft/2020-12/schema','$id':f'https://mechra.local/contracts/{name}.schema.json'})
    if 'CadOperation' in schema.get('$defs',{}):
        operation=schema['$defs']['CadOperation']
        operation['allOf']=[{'if':{'properties':{'kind':{'const':kind}}},'then':{'properties':{'inputs':inputs.model_json_schema()}}}
                            for kind,inputs in [('create_plate',PlateInputs),('modify_plate_thickness',ThicknessInputs)]]
    (out/f'{name}.schema.json').write_text(json.dumps(schema,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
