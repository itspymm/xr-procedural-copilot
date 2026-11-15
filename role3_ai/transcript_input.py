"""Normalize pasted timestamp labels without rewriting tutorial speech."""
import re

# Copied YouTube accessibility labels may immediately follow the timestamp:
# 0:033 secondsText, 1:061 minute, 6 secondsText.
STAMP = re.compile(r"^\s*(?:\[(\d+):([0-5]\d)\]|(\d+):([0-5]\d))(.*)$")
LABEL = re.compile(r"(?:(\d+)\s*minutes?(?:,\s*)?)?(?:(\d+)\s*seconds?)?")


def normalize_transcript(transcript):
    lines = []
    for line in transcript.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        match = STAMP.match(line)
        if not match:
            lines.append(line)
            continue
        bracket_minutes, bracket_seconds, minutes, seconds, text = match.groups()
        minute = int(bracket_minutes if bracket_minutes is not None else minutes)
        second = int(bracket_seconds if bracket_seconds is not None else seconds)
        # Strip only a glued-on accessibility label that agrees with the stamp.
        # In clean transcripts, speech such as "3 seconds later" must remain intact.
        if bracket_minutes is None and text and text[0].isdigit():
            label = LABEL.match(text)
            if not label or not label.group(0):
                raise ValueError("Ambiguous copied timestamp. Use [mm:ss] followed by the spoken text.")
            label_minutes, label_seconds = label.groups()
            if int(label_minutes or 0) * 60 + int(label_seconds or 0) != minute * 60 + second:
                raise ValueError("Copied timestamp and its time label disagree. Check that line or use [mm:ss].")
            text = text[label.end():]
        lines.append(f"[{minute:02d}:{second:02d}]" + (" " + text.lstrip() if text.strip() else ""))
    return "\n".join(lines)


def transcript_boundaries(transcript, duration):
    # Only timestamped line starts are timing data; times mentioned in speech
    # and chapter descriptions must not silently become video boundaries.
    times = {int(m) * 60 + int(s) for m, s in re.findall(r"^\[(\d+):([0-5]\d)\]", transcript, re.MULTILINE)}
    if not times:
        raise ValueError("Transcript needs timestamped lines, for example [00:03] Spoken instructions.")
    if any(t > duration for t in times):
        raise ValueError("A transcript timestamp is beyond the video length. Check the detected length or transcript.")
    boundaries = sorted(times | {duration})
    if len(boundaries) < 2:
        raise ValueError("At least one transcript timestamp must be before the video ends.")
    return boundaries
