import json
import unittest
from fastapi.testclient import TestClient
from app.main import app, VERSION, BUILD_ID


class ApiTests(unittest.TestCase):
    def setUp(self):
        self.client=TestClient(app)
    def tearDown(self):
        self.client.close()
    def test_health_identifies_build(self):
        response=self.client.get('/health')
        self.assertEqual(response.status_code,200)
        self.assertEqual(response.json()['version'],VERSION)
        self.assertEqual(response.json()['build_id'],BUILD_ID)
        self.assertEqual(response.json()['planner'],'deterministic')
    def test_unicode_http_round_trip(self):
        payload={'message':'Tạo tấm 100 × 60 × 5 mm','context':{'document_type':'part','document_id':'http-doc'},'session_id':'http-session'}
        response=self.client.post('/v1/chat',content=json.dumps(payload,ensure_ascii=False).encode('utf-8'),headers={'Content-Type':'application/json; charset=utf-8'})
        self.assertEqual(response.status_code,200)
        self.assertEqual(response.json()['plan']['operations'][0]['inputs']['thickness_mm'],5)
        self.assertTrue(response.json()['requires_confirmation'])
    def test_bad_payloads_are_422(self):
        for payload in [{'message':''},{'message':'   '},{'message':'x'*4001},{'message':'test','surprise':True},{'message':'test','context':{'document_type':'invalid'}}]:
            with self.subTest(payload=str(payload)[:80]):
                self.assertEqual(self.client.post('/v1/chat',json=payload).status_code,422)
    def test_malformed_json(self):
        self.assertEqual(self.client.post('/v1/chat',content=b'{bad',headers={'Content-Type':'application/json'}).status_code,422)
    def test_nonfinite_verification_is_safe_422(self):
        response=self.client.post('/v1/verify',content=b'{"operation":"create_plate","measured_width_mm":NaN}',headers={'Content-Type':'application/json'})
        self.assertEqual(response.status_code,422)
        self.assertIn('detail',response.json())
