"""Local-only tutorial preparation screen and USB-connected Quest feed."""
import argparse
import json
import re
import shutil
import subprocess
import threading
import time
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse
from generate_steps_local import generate_procedure, validate_steps

ROOT = Path(__file__).resolve().parent
DATA = ROOT / "local_data"


def youtube_id(url):
    if not isinstance(url, str):
        raise ValueError("Enter a YouTube URL.")
    parsed = urlparse(url.strip())
    video = None
    if parsed.scheme == "https" and parsed.hostname == "youtu.be":
        video = parsed.path.strip("/")
    elif parsed.scheme == "https" and parsed.hostname in ("youtube.com", "www.youtube.com", "m.youtube.com"):
        parts = parsed.path.strip("/").split("/")
        if parsed.path == "/watch":
            video = parse_qs(parsed.query).get("v", [None])[0]
        elif len(parts) == 2 and parts[0] in ("embed", "shorts"):
            video = parts[1]
    if not video or not re.fullmatch(r"[A-Za-z0-9_-]{11}", video):
        raise ValueError("Use a valid HTTPS YouTube watch or share link.")
    return video


def validate_input(data):
    title, transcript, duration = data.get("title"), data.get("transcript"), data.get("duration")
    video = youtube_id(data.get("url"))
    if not isinstance(title, str) or not title.strip() or len(title) > 160:
        raise ValueError("Enter a title of 1–160 characters.")
    if not isinstance(transcript, str) or not transcript.strip() or len(transcript) > 24000:
        raise ValueError("Paste a timestamped transcript of up to 24,000 characters.")
    if type(duration) is not int or not 1 <= duration <= 86400:
        raise ValueError("Enter the video duration in whole seconds (1–86400).")
    return {"title": title.strip(), "transcript": transcript, "duration": duration,
            "url": "https://www.youtube.com/watch?v=" + video}


def validate_publication(data):
    if data.get("reviewed") is not True:
        raise ValueError("Review the instructions and pause times before sending.")
    procedure, duration = data.get("procedure"), data.get("duration")
    if not isinstance(procedure, dict) or set(procedure) != {"tutorial_id", "title", "video_url", "steps"}:
        raise ValueError("Tutorial fields are incomplete.")
    if type(duration) is not int or not 1 <= duration <= 86400:
        raise ValueError("Invalid video duration.")
    for key in ("tutorial_id", "title", "video_url"):
        if not isinstance(procedure[key], str) or not procedure[key].strip():
            raise ValueError("Tutorial title, identifier and YouTube link are required.")
    youtube_id(procedure["video_url"])
    validate_steps(procedure["steps"], duration)
    return procedure


class State:
    def __init__(self, directory=DATA):
        self.lock = threading.Lock()
        self.busy = threading.Lock()
        self.jobs = {}
        self.active = {"revision": "", "procedure_json": ""}
        self.last_seen = 0
        self.acknowledged = ""
        self.directory = directory
        saved = directory / "active.json"
        if saved.exists():
            try:
                value = json.loads(saved.read_text())
                validate_publication({"reviewed": True, "procedure": json.loads(value["procedure_json"]), "duration": value["duration"]})
                self.active = value
            except (ValueError, KeyError, TypeError):
                print("Ignored invalid saved tutorial.")

    def generate(self, job_id, source):
        try:
            procedure = generate_procedure(source["transcript"], source["title"], source["duration"],
                                           source["url"], tutorial_id="tutorial_" + job_id)
            self.directory.mkdir(parents=True, exist_ok=True)
            (self.directory / (job_id + "_draft.json")).write_text(json.dumps(procedure, indent=2))
            with self.lock:
                self.jobs[job_id] = {"status": "ready", "procedure": procedure, "duration": source["duration"]}
        except Exception as error:
            with self.lock:
                self.jobs[job_id] = {"status": "failed", "error": "Local generation failed: " + str(error)}
        finally:
            self.busy.release()


def make_handler(state):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, format, *args):
            pass

        def respond(self, status, value, content_type="application/json"):
            body = (json.dumps(value) if content_type == "application/json" else value).encode()
            self.send_response(status)
            self.send_header("Content-Type", content_type + "; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Referrer-Policy", "strict-origin-when-cross-origin")
            self.end_headers()
            self.wfile.write(body)

        def trusted(self):
            allowed = {f"127.0.0.1:{self.server.server_port}", f"localhost:{self.server.server_port}"}
            if self.headers.get("Host") not in allowed:
                self.respond(403, {"error": "Use the local address shown in Terminal."})
                return False
            origin = self.headers.get("Origin")
            if origin and origin not in {"http://" + host for host in allowed}:
                self.respond(403, {"error": "This screen only accepts local requests."})
                return False
            return True

        def do_GET(self):
            if not self.trusted():
                return
            if self.path == "/":
                self.respond(200, (ROOT / "web" / "index.html").read_text(), "text/html")
            elif self.path == "/api/example":
                self.respond(200, {"title": "Easy Paper Airplane", "url": "https://youtu.be/1PVA2nPU60U", "duration": 89,
                    "transcript": (ROOT / "inputs" / "airplane_video_transcript.txt").read_text()})
            elif self.path == "/api/status":
                with state.lock:
                    self.respond(200, {"quest_seen_seconds_ago": round(time.monotonic() - state.last_seen) if state.last_seen else None,
                        "active_revision": state.active["revision"], "loaded_revision": state.acknowledged})
            elif self.path == "/api/active":
                with state.lock:
                    state.last_seen = time.monotonic()
                    self.respond(200, state.active)
            elif self.path.startswith("/api/jobs/"):
                with state.lock:
                    job = state.jobs.get(self.path.rsplit("/", 1)[1])
                    self.respond(200 if job else 404, job or {"error": "Generation not found."})
            else:
                self.respond(404, {"error": "Not found"})

        def do_POST(self):
            if not self.trusted():
                return
            try:
                size = int(self.headers.get("Content-Length", "0"))
                if not 0 < size <= 160000:
                    raise ValueError("Request is empty or too large.")
                data = json.loads(self.rfile.read(size))
                if not isinstance(data, dict):
                    raise ValueError("Expected an object.")
                if self.path == "/api/generate":
                    source = validate_input(data)
                    if not state.busy.acquire(blocking=False):
                        self.respond(409, {"error": "The model is still generating. Please wait."})
                        return
                    job = uuid.uuid4().hex
                    with state.lock:
                        state.jobs[job] = {"status": "generating"}
                        if len(state.jobs) > 20:
                            del state.jobs[next(iter(state.jobs))]
                    threading.Thread(target=state.generate, args=(job, source), daemon=True).start()
                    self.respond(202, {"job": job})
                elif self.path == "/api/publish":
                    procedure = validate_publication(data)
                    value = {"revision": uuid.uuid4().hex, "procedure_json": json.dumps(procedure), "duration": data["duration"]}
                    with state.lock:
                        state.directory.mkdir(parents=True, exist_ok=True)
                        temp = state.directory / "active.tmp"
                        temp.write_text(json.dumps(value))
                        temp.replace(state.directory / "active.json")
                        state.active = value
                    self.respond(200, {"revision": value["revision"]})
                elif self.path == "/api/ack":
                    with state.lock:
                        if data.get("revision") != state.active["revision"]:
                            raise ValueError("That tutorial has been replaced.")
                        state.acknowledged = data["revision"]
                    self.respond(200, {"ok": True})
                else:
                    self.respond(404, {"error": "Not found"})
            except (ValueError, KeyError, TypeError) as error:
                self.respond(400, {"error": str(error)})

    return Handler


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8765)
    args = parser.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", args.port), make_handler(State()))
    adb = shutil.which("adb")
    bundled = Path("/Applications/Unity/Hub/Editor/6000.3.28f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb")
    if not adb and bundled.exists():
        adb = str(bundled)
    if adb:
        try:
            result = subprocess.run([adb, "reverse", f"tcp:{args.port}", f"tcp:{args.port}"],
                                    capture_output=True, text=True, timeout=10)
            print("Quest USB link ready." if result.returncode == 0 else "Quest USB link unavailable; connect the headset and restart this server.", flush=True)
        except (OSError, subprocess.TimeoutExpired):
            print("Quest USB link unavailable. You can still prepare a tutorial.", flush=True)
    print(f"Open http://127.0.0.1:{args.port} — leave this Terminal running.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
