import copy
import json
from pathlib import Path
import unittest
from pydantic import ValidationError
from app.models import CadPlan, CadVerificationRequest, ChatRequest
from app.agent import MechraAgent
from app.verification import verify

SHARED = Path(__file__).resolve().parents[2] / 'Shared'


class ContractTests(unittest.TestCase):
    def setUp(self):
        self.plan = MechraAgent().reply(ChatRequest(message='create plate 100 x 60 x 5 mm', context={'document_type':'part'})).plan.model_dump()

    def test_rejects_malformed_plans(self):
        for mutate in [lambda p:p.update(version='0.1'), lambda p:p.update(operations=[]),
                       lambda p:p['operations'].append(copy.deepcopy(p['operations'][0])),
                       lambda p:p.update(requires_confirmation=False), lambda p:p.update(requires_confirmation=1),
                       lambda p:p['operations'][0].update(kind='delete_file'),
                       lambda p:p['operations'][0]['inputs'].update(width_mm=-1),
                       lambda p:p['operations'][0]['inputs'].update(thickness_mm=True),
                       lambda p:p['operations'][0]['inputs'].update(thickness_mm='5'),
                       lambda p:p['operations'][0]['inputs'].update(thickness_mm=float('nan')),
                       lambda p:p['operations'][0]['inputs'].update(thickness_mm=float('inf')),
                       lambda p:p['operations'][0]['inputs'].update(macro='run arbitrary script'),
                       lambda p:p['operations'][0].update(depends_on=['op-2'])]:
            p=copy.deepcopy(self.plan);mutate(p)
            with self.subTest(plan=p), self.assertRaises(ValidationError): CadPlan.model_validate(p)

    def test_all_shared_verification_vectors(self):
        for case in json.loads((SHARED/'tests/verification-cases.json').read_text()):
            with self.subTest(case=case['name']):
                try: passed = verify(CadVerificationRequest.model_validate(case['snapshot'])).passed
                except ValidationError: passed = False
                self.assertEqual(passed,case['passed'])

    def test_json_schemas_and_emitted_payloads(self):
        from jsonschema import Draft202012Validator
        for path in (SHARED/'contracts').glob('*.json'):
            schema=json.loads(path.read_text());Draft202012Validator.check_schema(schema)
        validator=Draft202012Validator(json.loads((SHARED/'contracts/cad-plan.schema.json').read_text()))
        validator.validate(self.plan)
        for changes in [{'width_mm':-1},{'width_mm':True},{'macro':'x'},{'width_mm':'100'}]:
            p=copy.deepcopy(self.plan);p['operations'][0]['inputs'].update(changes)
            self.assertTrue(list(validator.iter_errors(p)))
        spec=MechraAgent().reply(ChatRequest(message='create plate 100 x 60 x 5 mm',context={'document_type':'part'})).design_spec.model_dump()
        Draft202012Validator(json.loads((SHARED/'contracts/design-spec.schema.json').read_text())).validate(spec)
