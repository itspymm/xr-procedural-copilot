# Embedded YouTube test

`Assets/Scenes/YouTubeTest.unity` is a separate copy of LocalAITest. Its MP4
VideoPlayer and MP4 synchronizer are removed. A TLabWebView browser renders the
official YouTube IFrame player into the same RawImage. No video file is assigned
in this scene. The ten AI-generated instructions are unchanged, so their known
accuracy problems remain visible.

The `YouTubeProcedurePlayer` component on `YouTubeBrowser` holds the HTTPS YouTube
URL. It accepts standard watch, short-link, embed, and shorts URLs. Change that
field and assign matching tutorial JSON before building a different tutorial.
This is Editor configuration, not an in-headset video-selection interface.

Controls:

- Voice Next/Back/Repeat and the existing A/B/X controls call the procedure
  controller, which sends seek/play commands to the embedded player.
- Left Y taps the center of the visible player if YouTube requires a user gesture.
- The player polls its video time and pauses at the step end. It never advances
  the procedure automatically. Seeking is approximate and depends on streaming.
- Normal YouTube controls and branding remain visible. No downloading, stream
  extraction, or ad filtering is performed.

The bundled HTML uses the HTTPS Android application identifier as its base URL,
following Google's guidance for identifying mobile embedded players. Player
errors, readiness, and segment boundaries are logged under `[YouTube]` in ADB.
Embedding may fail for private, removed, age-restricted, or embedding-disabled
videos; network access and the headset's system browser capabilities also matter.

Dependencies, pinned through Unity Package Manager:

- `com.tlabaltoh.webview` at `3bd84ecb1796312ae871ed94f2501a63e0b25492`
  (appstore-compatible branch, MIT).
- `com.tlabaltoh.vkeyborad` at `2b2755158be4a28d13384f22415782d57b9efab4`
  (required dependency, MIT).

This uses WebView's ByteBuffer capture at 960x540 / 24 fps. It is Android-only;
Unity Editor does not render this browser. No paid browser license is required.

`CopilotWebView` adapts the pinned package's native HTML loader: the supplied AAR
exports `LoadHtml`, while its C# wrapper calls `LoadHTML`. The adapter calls the
actual exported method without modifying the immutable package cache. Startup
also waits for FragmentCapture's managed state to become Initialized, rather
than just the earlier native initialization signal. Loading before that managed
transition is silently ignored by the upstream wrapper.

Build only YouTubeTest to `Builds/QuestYouTubeTest.apk`. Installing it updates the
same Android app package. The MP4 demo and LocalAITest scenes/APKs remain available.

Checks:

```sh
node tests/youtube-player.test.cjs
```

This tests player-control logic with a simulated iframe, not actual streaming.
Device playback must be tested separately.

Device check after the compatibility fix: Quest 3S logs confirmed the iframe
ready callback, playback state 1 (playing), and `segment ended` followed by state
2 (paused) at two successive boundaries. This verifies real streamed playback
and boundary callbacks. Visual rendering and user comfort still require the
headset wearer's confirmation.

Transcript fetching is not part of this change. Continue supplying a timestamped
transcript to the local generator. The IFrame API controls video playback; it does
not provide a general transcript download endpoint.

References:

- https://github.com/TLabAltoh/TLabWebView
- https://developers.google.com/youtube/iframe_api_reference
- https://developers.google.com/youtube/terms/required-minimum-functionality
