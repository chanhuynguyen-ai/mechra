import copy
import itertools
import json
from pathlib import Path
import unittest
from app.rectangle_geometry import measure_rectangle

CASES = json.loads((Path(__file__).resolve().parents[2] / 'Shared/tests/rectangle-cases.json').read_text())

class RectangleTests(unittest.TestCase):
    def test_shared_geometry_cases(self):
        for case in CASES:
            with self.subTest(case=case['name']):
                actual = measure_rectangle(case['edges'])
                self.assertEqual(actual is not None, case['valid'])
                if actual:
                    self.assertAlmostEqual(actual[0], case['width_mm'], places=7)
                    self.assertAlmostEqual(actual[1], case['height_mm'], places=7)

    def test_every_edge_order_and_direction(self):
        edges = CASES[0]['edges']
        for order in itertools.permutations(edges):
            for directions in itertools.product((False, True), repeat=4):
                variant = []
                for edge, reverse in zip(order, directions):
                    variant.append({f'{axis}{i}': edge[f'{axis}{3-i if reverse else i}']
                                    for axis in 'xyz' for i in (1, 2)})
                self.assertEqual(measure_rectangle(variant), (100, 60))

    def test_nonfinite_coordinates(self):
        for coordinate in CASES[0]['edges'][0]:
            for value in (float('nan'), float('inf'), -float('inf')):
                edges = copy.deepcopy(CASES[0]['edges'])
                edges[0][coordinate] = value
                self.assertIsNone(measure_rectangle(edges))

if __name__ == '__main__':
    unittest.main()
