import unittest
from app.agent import MechraAgent
from app.models import ChatRequest, FeatureInfo, ModelContext


class AgentTests(unittest.TestCase):
    def setUp(self):
        self.agent = MechraAgent()
        self.context = ModelContext(document_type='part', document_title='Part1', document_id='doc-A', update_stamp=1, configuration='Default')

    def ask(self, message, session='session-A', context=None):
        return self.agent.reply(ChatRequest(message=message, session_id=session, context=context or self.context))

    def test_vietnamese_english_and_numeric_forms(self):
        for text in ['Tạo một plate 100 x 60 x 5 mm', 'Create a 100 x 60 x 5 mm rectangular plate',
                     'Dựng tấm hình chữ nhật 100 × 60 × 5 mm', 'Tạo plate 100 * 60 * 5 mm',
                     'create plate 100,0 x 60.0 x 5 mm']:
            with self.subTest(text=text):
                result = self.ask(text)
                self.assertEqual(result.action, 'execute_cad_plan')
                self.assertTrue(result.requires_confirmation)
                self.assertTrue(result.plan.requires_confirmation)
                self.assertEqual(result.plan.operations[0].inputs['width_mm'], 100)
                self.assertEqual(result.plan.operations[0].inputs['height_mm'], 60)
                self.assertEqual(result.plan.operations[0].inputs['thickness_mm'], 5)
                self.assertTrue(all(i.status == 'confirmed' for i in result.design_spec.items))

    def test_invalid_or_unsupported_intents_do_not_create_partial_plan(self):
        for text in ['create plate -100 x 60 x 5 mm', 'create plate 100 x -60 x 5 mm',
                     'create plate 100 x 60 x -5 mm', 'create plate 0 x 60 x 5 mm',
                     'create plate 10001 x 60 x 5 mm', 'create plate 100 x 60 x 5 cm',
                     'create plate 100 mm x 6 cm x 5 mm', 'create plate 100 x 60 x 5 inches',
                     'create plate 100 x 60 x 5 x 3 mm', 'create plate 100 x 60 x 5 mm and drill 4 holes',
                     'create plate 100 x 60 x 5 mm or 8 mm', 'create plate NaN x 60 x 5 mm',
                     'do not create plate 100 x 60 x 5 mm', 'create plate 1e3 x 60 x 5 mm']:
            with self.subTest(text=text):
                self.assertIsNone(self.ask(text).plan)

    def test_small_dimensions_and_square_plate(self):
        for text in ['create plate 60 x 60 x 5 mm', 'create plate .5 x ,6 x .1 mm']:
            self.assertIsNotNone(self.ask(text).plan)

    def test_missing_thickness_clarifies_then_continues(self):
        result = self.ask('Tạo plate 100 x 60 mm')
        self.assertEqual(result.status, 'clarification_required')
        self.assertEqual(result.design_spec.items[2].status, 'missing')
        self.assertIsNone(result.design_spec.items[2].source)
        result = self.ask('5 mm')
        self.assertEqual(result.plan.operations[0].inputs['thickness_mm'], 5)
        self.assertEqual(result.design_spec.items[2].source, '5 mm')
        self.assertIsNone(self.ask('8 mm').plan)  # continuation consumed once

    def test_units_keep_independent_provenance(self):
        first = self.ask('create plate 100 x 60')
        self.assertEqual(first.design_spec.items[0].status, 'assumed')
        second = self.ask('5 mm')
        self.assertEqual([i.status for i in second.design_spec.items], ['assumed', 'assumed', 'confirmed'])
        self.ask('create plate 100 x 60 mm')
        second = self.ask('5')
        self.assertEqual([i.status for i in second.design_spec.items], ['confirmed', 'confirmed', 'assumed'])
        self.assertIn('giả định', second.message)

    def test_invalid_thickness_does_not_consume_pending_spec(self):
        self.ask('create plate 100 x 60 mm')
        self.assertIsNone(self.ask('-5 mm').plan)
        self.assertIsNotNone(self.ask('5 mm').plan)

    def test_session_isolation(self):
        self.ask('create plate 100 x 60 mm')
        self.assertIsNone(self.ask('5 mm', session='session-B').plan)
        self.assertIsNotNone(self.ask('5 mm').plan)

    def test_same_title_different_document_cannot_inherit(self):
        self.ask('create plate 100 x 60 mm')
        other = self.context.model_copy(update={'document_id':'doc-B'})
        self.assertIsNone(self.ask('5 mm', context=other).plan)
        self.assertIsNotNone(self.ask('5 mm').plan)

    def test_stamp_configuration_and_document_type_guards(self):
        for change in [{'update_stamp':2}, {'configuration':'Other'}, {'document_type':'assembly'}]:
            with self.subTest(change=change):
                self.agent = MechraAgent()
                self.ask('create plate 100 x 60 mm')
                self.assertIsNone(self.ask('5 mm', context=self.context.model_copy(update=change)).plan)

    def test_existing_plate_blocks_create_and_continuation(self):
        self.ask('create plate 100 x 60 mm')
        with_plate = self.context.model_copy(update={'features':[FeatureInfo(name='Mechra-Plate-Extrude')]})
        self.assertIsNone(self.ask('5 mm', context=with_plate).plan)
        self.assertIsNone(self.ask('create plate 100 x 60 x 5 mm', context=with_plate).plan)

    def test_existing_user_geometry_is_rejected_before_plan(self):
        for name, kind in [('Boss-Extrude1', 'Extrusion'), ('Sketch1', 'ProfileFeature'),
                           ('Imported1', 'Imported'), ('UnknownFeature', 'Unknown')]:
            with self.subTest(kind=kind):
                context = self.context.model_copy(update={'document_title': 'tesst.SLDPRT',
                    'features': [FeatureInfo(name=name, type_name=kind)]})
                result = self.ask('Tạo một plate 100 x 60 x 5 mm', context=context)
                self.assertIsNone(result.plan)
                self.assertIn(name, result.message)
                self.assertIn('File > New > Part', result.message)

    def test_blank_template_folders_and_planes_do_not_block_create(self):
        from app.agent import BLANK_PART_TYPES
        context = self.context.model_copy(update={'features': [
            FeatureInfo(name=f'localized-{i}', type_name=kind)
            for i, kind in enumerate(BLANK_PART_TYPES)]})
        self.assertIsNotNone(self.ask('create plate 100 x 60 x 5 mm', context=context).plan)

    def test_added_geometry_cancels_pending_even_with_same_stamp(self):
        self.ask('create plate 100 x 60 mm')
        context = self.context.model_copy(update={'features': [FeatureInfo(name='Boss-Extrude1', type_name='Extrusion')]})
        result = self.ask('5 mm', context=context)
        self.assertIsNone(result.plan)
        self.assertIn('File > New > Part', result.message)
        self.assertIsNone(self.ask('5 mm').plan)

    def test_cancel_and_new_intent_abandon_pending_spec(self):
        for interruption in ['hủy', 'cancel', 'check model', 'create plate 200 x 80 x 6 mm']:
            self.ask('create plate 100 x 60 mm')
            self.ask(interruption)
            self.assertIsNone(self.ask('5 mm').plan)

    def test_no_legacy_session_sharing(self):
        first = self.ask('create plate 100 x 60 mm', session=None)
        self.assertIn('đầy đủ', first.message)
        self.assertIsNone(self.ask('5 mm', session=None).plan)

    def test_ttl_and_bounded_pending_store(self):
        now = [0.0]
        self.agent = MechraAgent(clock=lambda: now[0], ttl=10, max_pending=2)
        self.ask('create plate 100 x 60 mm')
        now[0] = 11
        self.assertIsNone(self.ask('5 mm').plan)
        for session in ['one','two','three']:
            self.ask('create plate 100 x 60 mm', session=session)
        self.assertIsNone(self.ask('5 mm', session='one').plan)
        self.assertIsNotNone(self.ask('5 mm', session='two').plan)

    def test_modify_existing_plate_only(self):
        ctx = self.context.model_copy(update={'features':[FeatureInfo(name='Mechra-Plate-Extrude')]})
        for text in ['Đổi chiều dày thành 8 mm', 'đổi độ dày 8 mm', 'Change thickness to 8 mm']:
            result = self.ask(text, context=ctx)
            self.assertEqual(result.plan.operations[0].kind, 'modify_plate_thickness')
            self.assertEqual(result.plan.operations[0].inputs['thickness_mm'], 8)
        for text in ['change thickness to -8 mm','change thickness to 8 cm','change thickness from 5 to 8 mm']:
            self.assertIsNone(self.ask(text, context=ctx).plan)
        self.assertIsNone(self.ask('change thickness to 8 mm').plan)

    def test_concurrent_conversations_are_isolated(self):
        from concurrent.futures import ThreadPoolExecutor
        def conversation(i):
            self.ask(f'create plate {i+1} x 60 mm', session=str(i))
            return self.ask('5 mm', session=str(i)).plan.operations[0].inputs['width_mm']
        with ThreadPoolExecutor(max_workers=8) as pool:
            self.assertEqual(list(pool.map(conversation, range(24))), list(range(1,25)))
