# Local transcript generation

This path uses Gemma 3 4B through Ollama on the developer's Mac. No cloud AI
API key is needed. The existing `generate_steps.py` is still the original
rule-based generator; `generate_steps_local.py` makes a real model request.

Install/open Ollama, then download the model once:

```sh
ollama pull gemma3:4b
```

From the repository root, generate a new review draft:

```sh
python3 role3_ai/generate_steps_local.py \
  role3_ai/inputs/airplane_video_transcript.txt \
  --title "Easy Paper Airplane" --duration 89 \
  --output role3_ai/drafts/airplane_local_ai.json
```

Use a fresh output filename for each attempt; the script refuses to overwrite
existing files. It checks the field types, numbering, nonempty instructions,
chronological nonoverlapping segments, and video duration. This checks structure,
not whether the model understood the task correctly.

The model receives only the timestamped transcript. It cannot see folds or other
actions in the video. Review every instruction, ambiguous passage, and pause
point against the actual video. The supplied transcript timestamps are coarse,
so the pause points will usually need adjustment. No automatic speech transcription,
YouTube downloading, or video embedding is implemented by this script.

After review, copy the approved JSON into `XRProcedualCopilot/Assets/Role3_TutorialData/`
and assign it as the ProcedurePanel's tutorial JSON in the review scene. Keep
`video_url` empty for a bundled VideoClip. Drafts stay outside Unity Assets until
reviewed. The working Quest tutorial is not replaced automatically.

For the judges, generate and review the instructions on the Mac ahead of time,
then build the approved JSON into the Quest app. Ollama is not running on the
headset. The current Wit.ai voice-command feature still uses its network service.

Run validator tests:

```sh
python3 -m unittest discover -s role3_ai -p 'test_generate_steps_local.py'
```
