import copy
import io
import json
from unittest.mock import patch
import unittest

from generate_steps_local import SCHEMA, generate_procedure, validate_steps


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


class GenerationTimingTests(unittest.TestCase):
    @staticmethod
    def response(start=3, end=11):
        return io.BytesIO(json.dumps({"message": {"content": json.dumps({"steps": [{
            "step_number": 1, "title": "Position gift", "instruction": "Place the gift at an angle.",
            "start_time": start, "end_time": end, "additional_guidance": ""}]})}}).encode())

    def test_schema_limits_times_to_this_transcript_only(self):
        with patch("generate_steps_local.urllib.request.urlopen", return_value=self.response()) as call:
            generate_procedure('[00:03] Place gift at an angle.\n[00:11] Fold a corner.', 'Wrap a gift', 20)
            payload = json.loads(call.call_args.args[0].data)
            properties = payload['format']['properties']['steps']['items']['properties']
            self.assertEqual(properties['start_time']['enum'], [3, 11, 20])
            self.assertEqual(properties['end_time']['enum'], [3, 11, 20])
            self.assertNotIn('enum', SCHEMA['properties']['steps']['items']['properties']['start_time'])

    def test_invalid_timestamp_gets_one_targeted_correction(self):
        with patch("generate_steps_local.urllib.request.urlopen", side_effect=[self.response(end=10), self.response()]) as call:
            result = generate_procedure('[00:03] Position gift.\n[00:11] Fold corner.', 'Wrap a gift', 20)
            self.assertEqual(result['steps'][0]['end_time'], 11)
            self.assertEqual(call.call_count, 2)
            repair = json.loads(call.call_args.args[0].data)['messages'][-1]['content']
            self.assertIn('[3, 11, 20]', repair)
            self.assertIn('3–10', repair)

    def test_repeated_invalid_output_is_rejected(self):
        with patch("generate_steps_local.urllib.request.urlopen", side_effect=[self.response(end=10), self.response(end=10)]) as call:
            with self.assertRaisesRegex(ValueError, 'after a correction attempt'):
                generate_procedure('[00:03] Position gift.\n[00:11] Fold corner.', 'Wrap a gift', 20)
            self.assertEqual(call.call_count, 2)

    def test_overlapping_steps_are_corrected_not_accepted(self):
        # A zero-length interval uses allowed values but still must be rejected.
        with patch("generate_steps_local.urllib.request.urlopen", side_effect=[self.response(end=3), self.response()]) as call:
            result = generate_procedure('[00:03] Position gift.\n[00:11] Fold corner.', 'Wrap a gift', 20)
            self.assertEqual(result['steps'][0]['end_time'], 11)
            self.assertEqual(call.call_count, 2)


if __name__ == "__main__":
    unittest.main()
