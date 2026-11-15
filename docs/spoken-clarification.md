# Spoken step clarification

This feature is available in `Assets/Scenes/TutorialInput.unity`. Other scenes
retain their navigation-only voice behavior. Answers appear as text; no speech
synthesis is included.

1. Open Ollama with `gemma3:4b` installed, connect Quest by USB, and start
   `python3 role3_ai/tutorial_server.py` from the repository root.
2. Open http://127.0.0.1:8765, generate/review a tutorial from a pasted transcript,
   and send it to Quest. New publications save the source transcript for chat.
3. Build only TutorialInput for Android with Development Build enabled, following
   the existing tutorial-input instructions. The default build list still selects
   VideoReview, so explicitly choose TutorialInput.
4. Say **help**. The video pauses and the instruction panel prompts for a question.
5. Ask a question and wait for its text answer. Ask follow-ups as needed.
6. Say **continue tutorial** to restore the instruction and prior playback state.

Help requires a tutorial loaded from the companion. Older publications without a
saved transcript can answer from their steps; republish to include the transcript.
Voice transcription uses the existing Wit.ai service and needs internet access.
Question answering uses Ollama on the Mac over the existing USB reverse connection.
Navigation phrases inside question mode are treated as questions, except the exact
exit phrase “continue tutorial”. History is limited to four question/answer pairs
and resets when leaving help. The companion serializes model work; if generation
or another question is running, ask again after it finishes.

## Device acceptance check

- Confirm help pauses midway through a segment and displays the question prompt.
- Ask “What does this instruction mean?” and confirm transcription then answer.
- Ask a follow-up and confirm the current step stays unchanged.
- Say “continue tutorial” and verify the instruction and video position return.
- Exit while an answer is pending; confirm the late answer does not overwrite
  the restored instruction. Enter help again after that request finishes.
- Stop Ollama or disconnect USB; confirm a readable error and working exit.
- Confirm Next/Back/Repeat still work outside help and segment boundaries pause.

Python tests mock model output; they do not establish speech recognition, model
quality, headset readability, or actual streaming playback.

## If “help” does nothing

The headset needs a newly built and installed APK after changing C# scripts.
Restarting the companion alone does not update Quest code. Build TutorialInput
explicitly (the default scene list is VideoReview), install that APK, then launch
the app again. The clarification build is `Builds/QuestClarification.apk`.

A video pause alone does not indicate Help mode: each video segment normally
pauses at its end. Help mode changes the instruction text to a question prompt.
If the voice status asks you to send a tutorial, publish from the companion and
wait for its “Loaded on Quest” acknowledgement first.
