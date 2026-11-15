# Paper-airplane video draft

Source: https://www.youtube.com/watch?v=1PVA2nPU60U

The draft in `XRProcedualCopilot/Assets/Role3_TutorialData/paper_airplane_video_draft.json`
was prepared from the transcript supplied in chat. It is not visually verified.
`Assets/Scenes/VideoReview.unity` pairs this draft with the supplied MP4 and Role 4's video
display. The original three-step `SampleScene` is preserved. The build scene list currently
selects VideoReview for testing.

The imported clip is approximately 89.92 seconds, 1920 by 1080, and 17 MB. Its final music
and outro extend beyond the last instruction segment at 77 seconds.

## Review before use

- Watch each proposed segment and move its end to after the demonstrated action finishes.
- Confirm the fold direction in step 1.
- Clarify the extra fold near 31 seconds and whether it belongs before the triangle folds.
- Identify the exact edge/crease alignments in steps 4 and 5.
- Confirm pocket orientation in step 6 and wing fold lines in step 7.
- Replace vague wording with visually verified instructions and remove draft guidance.
- Check final timestamps against the exact playable video file, including its introduction.

`video_url` is intentionally empty because the review scene uses a bundled VideoClip.
A YouTube watch/share URL is not a direct video stream for Unity VideoPlayer.
Keep the YouTube link as source attribution.

The JSON, clip, render texture, and ProcedureVideoSynchronizer are connected in VideoReview.
Automatic playback and looping are disabled on VideoPlayer; the procedure starts the segment.
Build and run this scene, then test pause at each boundary, Next, Back, and Repeat on Quest.
The video appears below the existing instruction panel. Tell the integrator where a fold is
cut off or an instruction doesn't match; timestamps and wording remain draft until reviewed.
