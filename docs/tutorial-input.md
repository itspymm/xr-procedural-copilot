# Prepare on Mac, run on Quest

The local companion accepts a YouTube URL, tutorial title, and a pasted transcript
with `mm:ss` timestamps. The visible embedded player detects the duration automatically;
you can override it in whole seconds if detection fails. Nothing fetches captions
or downloads the video. Copied YouTube labels such as `0:033 secondsText` and
`1:061 minute, 6 secondsText` are normalized to `[00:03] Text` and
`[01:06] Text` before generation. Speech, measurements, and chapter headings are
preserved. The cleaned transcript appears in the form when generation finishes
and is included when publishing for chat context. Ambiguous labels are rejected
with a formatting message instead of guessing a timestamp.
Ollama generates a draft using the transcript only. Timestamp fields in its output
schema are restricted to the transcript's boundaries and the video end. If the
result still has invalid ordering, numbering, or structure, the companion makes
one correction request and validates again. Failed drafts are never published;
instruction accuracy and exact pause placement still need human review.

## Start

Open Ollama. Connect the authorized Quest by USB and run from the repository root:

```sh
python3 role3_ai/tutorial_server.py
```

Open http://127.0.0.1:8765 in your Mac browser. Keep the server running. It sets up
`adb reverse tcp:8765 tcp:8765` when Android tools are available, which lets the
headset reach the Mac's loopback-only server through USB. The companion checks the USB forwarding rule every three seconds and
restores it automatically after a reconnect or if another tool removes it. The included Mac fallback finds the same
Unity 6000.3.28f1 Android tools used for this project; other machines need `adb`
on PATH. Quest must still have Wi-Fi for YouTube and the existing Wit.ai voice
commands.

Install the `TutorialInput` scene build once (`Builds/QuestTutorialInput.apk`). It
contains a `TutorialReceiver` alongside the existing controller and embedded
YouTube player. It starts with the bundled fallback while no tutorial has been
published. After that, changing tutorials does not require another Unity build.

## Use

1. Enter the URL and title, then paste the timestamped transcript. Wait for the
   detected length; if needed, press Play in the preview or enter seconds manually.
2. Click **Generate steps**. The Mac calls the local Gemma 3 4B model.
3. Review the draft alongside the embedded video. Titles, instructions, guidance,
   and start/end times are editable. AI accuracy is not guaranteed.
4. Check the review checkbox and click **Send to Quest**.
5. The headset polls for a new publication, validates it, updates the controller
   and embedded video, then acknowledges receipt. The page shows when Quest has
   loaded that revision. That receipt does not prove YouTube playback succeeded.
6. Use the existing Next/Back/Repeat voice or controller inputs on Quest.

**Save JSON** exports the draft without publishing. Generation alone never changes
the headset. Invalid publications are rejected without replacing the last one.
Tutorial changes wait until the controller exits Help mode.

Generated drafts and the last published tutorial are saved locally in ignored
`role3_ai/local_data/`. The last published tutorial is restored when the server
restarts. The original generator CLI and MP4/YouTube test scenes are preserved.

## Build

Build only `Assets/Scenes/TutorialInput.unity` for Android with **Development Build**
enabled. Unity's HTTP setting is **Development Only**. A build hook limits Android
cleartext network access to `127.0.0.1` and `localhost`; HTTPS remains available for
YouTube and voice. This is a USB demo connection, not a public or LAN service.

## Checks

```sh
python3 -m unittest discover -s role3_ai -p 'test_*.py'
node tests/youtube-player.test.cjs
```

The duration helper checks common URL forms, delayed metadata, cancellation of
stale lookups, player errors, and manual fallback. Run `node tests/youtube-duration.test.cjs`.
Use recorded videos; a live stream has no fixed final duration.

The server tests cover background generation, required review, validation,
preservation of the last valid publication, revision acknowledgement, restart
persistence, and request-origin checks. Browser and device checks are separate.

Verified in this workspace: the browser form generated a real ten-step draft
through Ollama. A separate one-step connection fixture was published over USB;
Quest logged `Loaded Mac companion connection test (1 steps)`, acknowledged the
same revision to the server, and the embedded YouTube player logged playing and
segment-end pause events. That test publication was then backed up outside the
server data folder, and the app restarted with its bundled fallback. The generated
draft remains unpublished until reviewed and sent from the form.
