# Test the local AI draft on Quest

`Assets/Scenes/LocalAITest.unity` copies the working VideoReview scene and assigns
`Assets/Role3_TutorialData/airplane_local_ai_test.json` to ProcedurePanel. That JSON
is the unedited output of `role3_ai/drafts/airplane_test_01.json` (10 steps).

This test checks how the app presents AI output, including known content and
timing mistakes. It is not the approved tutorial. The original VideoReview scene
and its reviewed draft are preserved. No model inference happens on Quest.

Build only LocalAITest for this test. The generated APK is
`XRProcedualCopilot/Builds/QuestLocalAITest.apk`. It uses the same Android package
as the demo and replaces that installed version when installed with `adb install -r`.
To restore the demo, install `Builds/QuestVideoReview.apk` if present, or build
only `Assets/Scenes/VideoReview.unity` and install that APK.

Check on the headset:

1. Confirm the counter says STEP 1 OF 10 and the text asks for 8.5 by 11 inch paper.
2. Allow the video to reach the first pause near 8 seconds.
3. Use Next (voice or right A) to advance. Back (voice or right B) returns a step.
4. Repeat (voice or left X) replays the current video segment.
5. Confirm later steps reproduce the draft, including its known timing errors.

The headset test establishes playback/navigation behavior. It does not establish
the accuracy of the AI-generated instructions. See `role3_ai/drafts/REVIEW.md`.
