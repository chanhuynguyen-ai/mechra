import json
from pathlib import Path
import unittest

from pydantic import ValidationError
from app.agent import MechraAgent
from app.feature_policy import equation_blocker
from app.models import ChatRequest, EquationState


EMPTY = {'count': 0, 'disabled_count': 0, 'linked_to_file': False}
SHARED = Path(__file__).resolve().parents[2] / 'Shared/tests'


class EquationTests(unittest.TestCase):
    def setUp(self):
        self.agent = MechraAgent()

    def ask(self, text, equations=EMPTY, edit=False):
        features = json.loads((SHARED / 'feature-scope-cases.json').read_text())[0]['features']
        if edit:
            features += [{'name': 'Mechra-Plate-Extrude', 'type_name': 'Extrusion'}]
        return self.agent.reply(ChatRequest(message=text, session_id='equation-session', context={
            'document_type': 'part', 'document_id': 'same-document', 'update_stamp': 1,
            'configuration': 'Default', 'features': features, 'equations': equations}))

    def test_empty_equations_template_supports_create_clarify_and_edit(self):
        self.assertIsNotNone(self.ask('Tạo plate 100 x 60 x 5 mm').plan)
        self.assertEqual(self.ask('Tạo plate 100 x 60 mm').status, 'clarification_required')
        self.assertEqual(self.ask('5 mm').plan.operations[0].inputs['thickness_mm'], 5)
        self.assertEqual(self.ask('Đổi chiều dày thành 8 mm', edit=True).plan.operations[0].kind,
                         'modify_plate_thickness')

    def test_equations_disabled_entries_and_file_links_block_both_operations(self):
        for state in [dict(EMPTY, count=1), dict(EMPTY, disabled_count=1), dict(EMPTY, linked_to_file=True)]:
            for edit, text in [(False, 'Tạo plate 100 x 60 x 5 mm'), (True, 'Đổi chiều dày thành 8 mm')]:
                with self.subTest(state=state, edit=edit):
                    result = self.ask(text, state, edit)
                    self.assertIsNone(result.plan)
                    self.assertIn('phương trình', result.message)

    def test_new_equation_cancels_pending_without_update_stamp_change(self):
        self.ask('Tạo plate 100 x 60 mm')
        self.assertIsNone(self.ask('5 mm', dict(EMPTY, count=1)).plan)
        self.assertIsNone(self.ask('5 mm').plan)

    def test_missing_snapshot_defers_to_required_native_preflight(self):
        # Backward-compatible transport. This result is not native readiness.
        self.assertIsNotNone(self.ask('Tạo plate 100 x 60 x 5 mm', None).plan)

    def test_shared_equation_states(self):
        for case in json.loads((SHARED / 'equation-state-cases.json').read_text()):
            with self.subTest(case=case['name']):
                try:
                    state = None if case['state'] is None else EquationState.model_validate(case['state'])
                    accepted = equation_blocker(state) is None
                except ValidationError:
                    accepted = False
                self.assertEqual(accepted, case['planner_accepted'])

    def test_equation_contract_rejects_coercion_and_extra_fields(self):
        for key, value in [('count', True), ('count', '0'), ('count', 0.5),
                           ('disabled_count', False), ('disabled_count', '0'),
                           ('linked_to_file', 'false'), ('linked_to_file', 0), ('surprise', 1)]:
            with self.subTest(key=key, value=value), self.assertRaises(ValidationError):
                EquationState.model_validate(dict(EMPTY, **{key: value}))

    def test_equations_do_not_disable_help_or_cancel(self):
        state = dict(EMPTY, count=1)
        self.assertIsNone(self.ask('hi', state).plan)
        self.assertIn('hủy', self.ask('hủy', state).message)
