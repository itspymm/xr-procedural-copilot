# XR Procedural Copilot

A voice-controlled tutorial assistant for Meta Quest. Follow instructions alongside a video, advance at your own pace, and ask for clarification without leaving the tutorial.

The Mac companion turns a **YouTube link and pasted timestamped transcript** into an editable tutorial using **Gemma 3 4B through Ollama**. Send the reviewed tutorial to Quest over USB, then navigate or ask questions by voice.

## Features

- **AI-generated tutorial drafts:** numbered steps with titles, instructions, additional guidance, and video start/end times.
- **Automatic video-length detection:** reads duration from the visible YouTube player, with a manual override when needed.
- **Copied-transcript cleanup:** converts labels such as `0:033 secondsText` and `1:061 minute, 6 secondsText` into clean timestamps without rewriting the speech.
- **Constrained timestamps:** generation uses the transcript boundaries and video end as its timestamp choices. Invalid output gets one correction attempt before being rejected.
- **Review before publishing:** edit instructions and timings, preview the video, export JSON, and explicitly approve a draft before sending it to Quest.
- **Tutorial updates without rebuilding:** the headset polls the companion, loads a published revision, and acknowledges receipt.
- **Step-synchronized video:** playback seeks to the active segment and pauses at its end. Progress advances only when the user requests it.
- **Voice and controller navigation:** Next, Back, and Repeat share the same procedure state.
- **Spoken clarification:** say “help,” ask about the current step, read the answer in the headset, and ask follow-up questions.
- **Readable XR panels:** Atkinson Hyperlegible typography, separate question and answer areas, progress indicators, and paginated long text.
- **USB recovery:** the companion checks its forwarding rule every three seconds and restores it when possible after a disconnection.
- **Local persistence:** generated drafts and the last published tutorial are saved on the Mac.

## How it works

```text
YouTube URL + pasted transcript
              │
              ▼
    Mac companion → local Ollama
              │
       Review and edit draft
              │
         Send to Quest
              │ USB / ADB reverse
              ▼
    Quest instructions + YouTube player
              │
    “help” → spoken question → Mac / Ollama
              │
              ▼
       Text answer in headset
```

The LLM runs on the Mac. YouTube streams through an official embedded player. Speech transcription uses the existing Meta Voice SDK / Wit.ai integration, so the experience is **not fully offline**.

## Requirements

| Component | Requirement |
|---|---|
| Headset | Meta Quest with developer mode, USB debugging authorized, and Wi-Fi |
| Mac | Runs the companion and Ollama; remains connected to Quest by USB |
| Unity | **6000.3.28f1**, with Android Build Support, SDK/NDK, and OpenJDK |
| Python | Python 3; the companion uses the standard library |
| Local model | Ollama with **`gemma3:4b`** downloaded |
| Android tools | `adb` on `PATH`, or the supported Unity installation described below |
| Voice | A configured `AppVoiceExperience` / Wit.ai configuration and microphone permission |
| Tests | Node.js for the JavaScript checks |

Unity package dependencies are declared in [`Packages/manifest.json`](XRProcedualCopilot/Packages/manifest.json), including OpenXR, Meta Voice SDK, and the pinned TLab WebView packages. Let Unity resolve them when opening the project.

## Quick start

### 1. Prepare the Mac

Install and open Ollama, then download the model once:

```sh
ollama pull gemma3:4b
```

Connect Quest by USB, accept the headset's USB debugging prompt, and verify the connection:

```sh
adb devices
```

The companion also searches this Unity installation if `adb` is not on `PATH`:

```text
/Applications/Unity/Hub/Editor/6000.3.28f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
```

### 2. Build and install the Quest app

1. Open **`XRProcedualCopilot/`** as the Unity project.
2. Select Android as the build target.
3. Build **only `Assets/Scenes/TutorialInput.unity`** with **Development Build** enabled.
4. Build to `XRProcedualCopilot/Builds/QuestPolished.apk` and install it:

   ```sh
   adb install -r XRProcedualCopilot/Builds/QuestPolished.apk
   ```

5. Launch the app from **Unknown Sources** on Quest and allow microphone access.

**The repository's default scene list selects `VideoReview`, not `TutorialInput`.** Choose TutorialInput explicitly for the live companion, chatbot, and YouTube workflow. APKs are local build outputs and are not committed.

The USB companion uses HTTP on loopback. Keep Unity's HTTP access set to **Development Only**. The build hook permits cleartext access to `127.0.0.1` and `localhost` for development builds while keeping other connections on HTTPS.

### 3. Start the companion

From the repository root:

```sh
python3 role3_ai/tutorial_server.py
```

Open **[http://127.0.0.1:8765](http://127.0.0.1:8765)**. Keep the companion and Ollama running.

The companion configures `adb reverse tcp:8765 tcp:8765` and maintains that mapping. The server listens on the Mac's loopback interface; this setup is a USB demo connection, not a LAN-hosted service.

### 4. Prepare a tutorial

1. Paste a YouTube URL and enter a tutorial title.
2. Wait for the detected duration. If necessary, press Play in the preview, retry detection, or enter the length in seconds manually.
3. Paste the timestamped transcript.
4. Click **Generate steps**.
5. Review each instruction and pause point against the video. Edit the draft as needed.
6. Check the review checkbox and click **Send to Quest**.
7. Wait for the loaded acknowledgement, then follow the tutorial in the headset.

**Save JSON** exports the draft without publishing it. Generating a draft does not change the active headset tutorial. The loaded acknowledgement confirms receipt of tutorial data, not successful YouTube playback.

## Transcript input

Clean timestamped lines are supported:

```text
[00:03] Select a matching button and thread.
[00:12] Thread the needle and tie a knot.
[01:06] Hold the pin in place with your finger.
```

You can also paste copied YouTube labels directly:

```text
0:033 secondsSelect a matching button and thread.
0:1212 secondsThread the needle and tie a knot.
1:061 minute, 6 secondsHold the pin in place with your finger.
```

The companion normalizes those labels before generation and displays the cleaned transcript when the draft is ready. Chapter headings and spoken measurements are preserved. Ambiguous labels are rejected rather than silently guessed.

Transcripts are limited to **24,000 characters**. Video duration must be **1–86,400 whole seconds**. Use recorded videos with a fixed duration. Neither automatic caption fetching nor video downloading is part of the YouTube companion workflow.

## Headset controls

| Action | Voice | Controller |
|---|---|---|
| Next step | “Next” / “Next step” | Right A |
| Previous step | “Back” / “Go back” / “Previous step” | Right B |
| Replay current segment | “Repeat” / “Repeat step” | Left X |
| Ask for clarification | “Help,” then speak a question | — |
| Exit clarification | “Continue tutorial” | — |
| Next text page | “Read more” / “Next page” | Right thumbstick down |
| Previous text page | “Previous page” | Right thumbstick up |
| Tap embedded player if playback is blocked | — | Left Y |

A segment ending pauses the video without advancing the step. Say “next step” when ready to proceed.

### Asking questions

Say **“help”** to pause playback and open question mode. Ask something like “Can you explain this instruction more simply?” The answer appears as text in the headset.

The model receives the current step number, tutorial instructions, saved transcript, and up to four prior question/answer pairs. Ask follow-ups, then say **“continue tutorial”** to restore the instruction and prior playback state. Long text uses pages rather than shrinking to an unreadable size.

Help requires a tutorial loaded from the companion. Inside Help mode, navigation phrases are treated as questions; page commands and “continue tutorial” remain controls. History resets when leaving Help, and new tutorial publications wait until Help mode ends.

## Demo walkthrough

1. Show the YouTube link and pasted transcript in the Mac companion.
2. Generate a draft, edit an instruction or timing, and send it to Quest.
3. Show the step counter, instruction, and corresponding video segment.
4. Let playback pause at a step boundary.
5. Demonstrate Next, Back, and Repeat.
6. Say “help,” ask a question, and show the answer.
7. Ask a follow-up, then say “continue tutorial.”

For a time-limited presentation, generate and review the tutorial ahead of time. Model generation and streamed video depend on the current machine and network conditions.

## Project layout

| Path | Purpose |
|---|---|
| `XRProcedualCopilot/` | Unity project |
| `Assets/Scripts/Procedure/` inside the Unity project | Procedure state, UI, voice routing, playback, and companion receiver |
| `Assets/Scenes/TutorialInput.unity` | Main live companion demo |
| `Assets/Scenes/VideoReview.unity` | Bundled MP4 review demo |
| `Assets/Scenes/LocalAITest.unity` | Bundled AI-draft test scene |
| `Assets/Scenes/YouTubeTest.unity` | Embedded YouTube playback test scene |
| `Assets/UI/` | UI font assets and font license |
| `role3_ai/tutorial_server.py` | Local companion HTTP server, publication, chat, and USB recovery |
| `role3_ai/web/` | Browser form and duration detection |
| `role3_ai/generate_steps_local.py` | Ollama step generation, timing constraints, and correction attempt |
| `role3_ai/transcript_input.py` | Copied-transcript cleanup and boundary extraction |
| `role3_ai/clarification.py` | Step-specific Ollama question answering |
| `role3_ai/generate_steps.py` | Earlier rule-based generator |
| `role3_ai/local_data/` | Local drafts and persisted publication; excluded from version control |
| `tests/` | JavaScript player and duration checks |
| `docs/` | Workflow, device checks, and tutorial-review notes |

## Validation

Run from the repository root:

```sh
python3 -m unittest discover -s role3_ai -p 'test_*.py'
node tests/youtube-player.test.cjs
node tests/youtube-duration.test.cjs
```

Tests cover tutorial validation, copied timestamp formats, constrained generation and correction, publication and review requirements, persistence, chat context, USB recovery, duration lookup, and simulated video controls.

Automated tests do not establish instruction accuracy, headset comfort, real speech recognition, or actual YouTube streaming. Check those on the device. The tutorial, spoken clarification, and polished panel have been exercised on Quest during development.

## Troubleshooting

| Symptom | Check |
|---|---|
| Quest cannot reach the companion | Keep the Mac server running and USB connected. Check `adb devices`; automatic forwarding recovery requires an authorized device and working `adb`. |
| LLM does not answer | Confirm Ollama is running with `gemma3:4b` installed. Generation and chat share one model-work lock; wait for current work to finish, then ask again. |
| “Help” does nothing | Install a current TutorialInput build and publish a tutorial from the companion. Restarting the Mac server does not update C# code on Quest. |
| Video pauses unexpectedly | It normally pauses at each step boundary. Use Next or Repeat. |
| YouTube will not play | Check Wi-Fi, embedding availability, and the player display. Left Y can provide a playback gesture. |
| Duration is not detected | Press Play in the visible preview or click Detect length. Enter seconds manually if metadata remains unavailable. |
| Copied timestamps are rejected | Use `[mm:ss] Spoken text` on each timestamped line if the copied accessibility label is ambiguous. |
| Generated timings are rejected | The model already gets one correction attempt. Try generating again or shorten the transcript; review all successful drafts before sending. |
| Voice is unavailable | Check microphone permission, Wi-Fi, and the scene's voice configuration. A/B/X navigation remains available. |
| A new build crashes at startup | Inspect device logs before assuming a voice or server failure. The UI font loads at runtime to preserve the existing serialized panel layout. |

## Limitations

- The model reads the transcript; it cannot see the video or the user's workspace.
- It cannot verify whether the user performed a physical action correctly.
- Instructions, interpretations, and pause points may be inaccurate despite passing structural validation.
- Clarification answers are text only; speech synthesis is not included.
- YouTube videos must permit embedded playback. Private, removed, age-restricted, or embedding-disabled videos may not play.
- The Mac and USB companion remain necessary for live tutorial updates and chatbot answers.
- YouTube playback and Wit.ai transcription require internet access.
- This prototype does not provide hand tracking, object recognition, or annotations anchored to real-world objects.

## Further documentation

- [Companion setup and tutorial input](docs/tutorial-input.md)
- [Spoken clarification and UI controls](docs/spoken-clarification.md)
- [YouTube integration and device checks](docs/youtube-embed-test.md)
- [Local model generation](role3_ai/LOCAL_AI.md)
- [Bundled video review](docs/video-tutorial-review.md)

The UI font is distributed with its [SIL Open Font License](XRProcedualCopilot/Assets/UI/Fonts/OFL.txt). Other dependencies retain their respective licenses.
