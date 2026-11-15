import copy
import unittest

from generate_steps_local import validate_steps


class ValidationTests(unittest.TestCase):
    def setUp(self):
        self.steps = [{
            "step_number": 1, "title": "Fold", "instruction": "Fold in half.",
            "start_time": 8, "end_time": 15, "additional_guidance": "",
        }]

    def test_valid_and_adjacent_segments(self):
        next_step = dict(self.steps[0], step_number=2, start_time=15, end_time=22)
        validate_steps(self.steps + [next_step], 89)

    def test_rejects_invalid_model_output(self):
        for changes in (
            {"step_number": 2}, {"start_time": -1}, {"end_time": 8},
            {"end_time": 90}, {"start_time": 8.0}, {"start_time": True},
            {"instruction": " "}, {"additional_guidance": None},
        ):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                step = dict(self.steps[0], **changes)
                validate_steps([step], 89)
        with self.assertRaises(ValueError):
            validate_steps([], 89)
        missing = copy.deepcopy(self.steps)
        del missing[0]["title"]
        with self.assertRaises(ValueError):
            validate_steps(missing, 89)

    def test_rejects_overlapping_steps(self):
        next_step = dict(self.steps[0], step_number=2, start_time=14, end_time=22)
        with self.assertRaises(ValueError):
            validate_steps(self.steps + [next_step], 89)

    def test_rejects_invented_timestamp(self):
        validate_steps(self.steps, 89, [0, 8, 15, 89])
        with self.assertRaises(ValueError):
            validate_steps(self.steps, 89, [0, 8, 22, 89])


if __name__ == "__main__":
    unittest.main()
