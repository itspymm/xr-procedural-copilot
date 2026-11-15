
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = ROOT / "XRProcedualCopilot" / "Assets" / "Role3_TutorialData"

# Choose a transcript from the command line
filename = sys.argv[1] if len(sys.argv) > 1 else "paper_airplane_transcript.txt"
transcript_path = DATA_DIR / filename

transcript = transcript_path.read_text()

# Read the tutorial title
title_match = re.search(r"^Tutorial:\s*(.+)$", transcript, re.MULTILINE)
title = title_match.group(1).strip() if title_match else transcript_path.stem

# Find timestamped instructions
pattern = r"\[(\d{2}):(\d{2})\s*-\s*(\d{2}):(\d{2})\]\s*([\s\S]*?)(?=\[\d{2}:\d{2}|\Z)"
def generate_guidance(instruction):
    text = instruction.lower()

    if "fold" in text:
        return "Make sure the edges are aligned and crease firmly."
    elif "soap" in text or "rub" in text:
        return "Pay attention to the spaces between your fingers."
    elif "rinse" in text:
        return "Make sure all the soap is washed away."
    elif "dry" in text:
        return "Use a clean, dry towel."
    else:
        return ""

steps = []

for number, match in enumerate(re.finditer(pattern, transcript), start=1):
    start_min, start_sec, end_min, end_sec, instruction = match.groups()
    clean_instruction = " ".join(instruction.split())
    step_title = clean_instruction.split(".")[0].split(",")[0][:45]

    steps.append({
        "step_number": number,
        "title": step_title,
        "instruction": " ".join(instruction.split()),
        "start_time": int(start_min) * 60 + int(start_sec),
        "end_time": int(end_min) * 60 + int(end_sec),
        "additional_guidance": generate_guidance(clean_instruction)
    })

procedure = {
    "tutorial_id": transcript_path.stem.replace("_transcript", ""),
    "title": title,
    "video_url": "",
    "steps": steps
}

output = DATA_DIR / f"{transcript_path.stem}_generated.json"
output.write_text(json.dumps(procedure, indent=2))

print(f"Generated {len(steps)} steps!")
print(f"Saved to {output}")
