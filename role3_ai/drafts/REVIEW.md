# Local model trial — not approved for the Quest scene

Model: `gemma3:4b` through local Ollama.
Input: `../inputs/airplane_video_transcript.txt` (raw timestamped speech).
Output: `airplane_local_ai_v2.json`, generated in approximately 8 seconds.

The output passes structural validation: required fields, integer timestamps,
numbering, duration, nonoverlap, and boundaries drawn from the transcript.
Four validator tests pass, including rejection of invented timestamp boundaries.

It does **not** pass content review:

- The model adds “lengthwise,” which is not stated in this transcript.
- It retains the transcription error “fold the game” without flagging it.
- It misses opening the triangle folds and folding to the new creases.
- Some later instructions are shifted into the wrong time interval: the wing
  action starts around 60 seconds, and the throw is spoken at 74 seconds. The
  model places these at 74 and 77 seconds respectively.

Do not copy this draft into Unity or claim it is visually verified. Review and
correct it against the video, or improve the model/prompt and regenerate into a
new file. The existing working tutorial and Unity scenes were not modified.
