import io
import json
import unittest
from unittest.mock import patch

from generate_steps_local import generate_procedure
from transcript_input import normalize_transcript, transcript_boundaries
from tutorial_server import validate_input


class TranscriptInputTests(unittest.TestCase):
    def test_copied_youtube_labels_from_button_tutorial(self):
        samples = [
            ('0:033 secondsHow to Sew a Button by Hand.', '[00:03] How to Sew a Button by Hand.'),
            ('0:1212 secondsYou will need A button', '[00:12] You will need A button'),
            ('0:2020 secondsSelect an appropriate button.', '[00:20] Select an appropriate button.'),
            ('0:2828 secondsCut a suitable length (12 to18 inches).', '[00:28] Cut a suitable length (12 to18 inches).'),
            ('0:3636 secondsPosition the button.', '[00:36] Position the button.'),
            ('0:4646 secondsStep 4. Make your first stitch.', '[00:46] Step 4. Make your first stitch.'),
            ('0:5454 secondsPull the thread completely through.', '[00:54] Pull the thread completely through.'),
            ('1:001 minuteTake a pin or a toothpick.', '[01:00] Take a pin or a toothpick.'),
            ('1:061 minute, 6 secondsThis will create space.', '[01:06] This will create space.'),
            ('1:141 minute, 14 secondsInsert the needle.', '[01:14] Insert the needle.'),
            ('1:211 minute, 21 secondsInsert it back up.', '[01:21] Insert it back up.'),
            ('1:291 minute, 29 secondsStep 8. Repeat.', '[01:29] Step 8. Repeat.'),
            ('1:371 minute, 37 secondsEnd with a double knot.', '[01:37] End with a double knot.'),
            ('1:461 minute, 46 secondsGently pull up.', '[01:46] Gently pull up.'),
            ('1:531 minute, 53 secondsDid you know...', '[01:53] Did you know...'),
        ]
        for pasted, expected in samples:
            with self.subTest(pasted=pasted):
                self.assertEqual(normalize_transcript(pasted), expected)
        normalized = normalize_transcript('Chapter 1: Preparation and materials\n' + '\n'.join(s[0] for s in samples))
        self.assertTrue(normalized.startswith('Chapter 1: Preparation and materials\n'))
        self.assertEqual(transcript_boundaries(normalized, 122),
                         [3, 12, 20, 28, 36, 46, 54, 60, 66, 74, 81, 89, 97, 106, 113, 122])
        self.assertEqual(normalize_transcript(normalized), normalized)

    def test_clean_speech_and_numbers_are_preserved(self):
        source = '[00:03] 3 seconds later, cut 12 to 18 inches.\n[01:06] At 1:20, check the stitch.'
        self.assertEqual(normalize_transcript(source), source)
        self.assertEqual(normalize_transcript('0:03 3 seconds later, pull.'), '[00:03] 3 seconds later, pull.')
        self.assertEqual(transcript_boundaries(source, 122), [3, 66, 122])
        self.assertEqual(normalize_transcript('2:002 minutesUse eight stitches.\r\n'), '[02:00] Use eight stitches.\n')

    def test_rejects_ambiguous_labels_and_out_of_range_times(self):
        for source in ['0:034 secondsText', '0:0333Text']:
            with self.assertRaises(ValueError): normalize_transcript(source)
        for source in ['No timestamps', '[02:10] Beyond the end.']:
            with self.assertRaises(ValueError): transcript_boundaries(source, 122)

    def test_companion_normalizes_before_model_and_chat_context(self):
        value = validate_input({'title': 'Sew a button', 'url': 'https://youtu.be/MmcEF2GR584',
                                'duration': 122, 'transcript': '0:033 secondsSelect a button.'})
        self.assertEqual(value['transcript'], '[00:03] Select a button.')

    def test_generator_sends_cleaned_text_and_real_boundaries(self):
        result = {'message': {'content': json.dumps({'steps': [{
            'step_number': 1, 'title': 'Select a button', 'instruction': 'Select a matching button.',
            'start_time': 3, 'end_time': 12, 'additional_guidance': ''}]})}}
        with patch('generate_steps_local.urllib.request.urlopen', return_value=io.BytesIO(json.dumps(result).encode())) as call:
            generate_procedure('0:033 secondsSelect a matching button.\n0:1212 secondsThread the needle.', 'Sew a button', 122)
            body = json.loads(call.call_args.args[0].data)
            context = json.loads(body['messages'][1]['content'])
            self.assertEqual(context['allowed_boundary_seconds'], [3, 12, 122])
            self.assertEqual(context['transcript'], '[00:03] Select a matching button.\n[00:12] Thread the needle.')


if __name__ == '__main__':
    unittest.main()
