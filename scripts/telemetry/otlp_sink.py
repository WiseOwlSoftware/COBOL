#!/usr/bin/env python3
"""A local OTLP/HTTP-JSON receiver for Claude Code telemetry — no collector to install.

Owner decision 2026-09-25 (tooling recommendation 5): record tokens and cost per agent, skill and model, replacing the
hand-kept usage tally. Claude Code exports with

    CLAUDE_CODE_ENABLE_TELEMETRY=1  OTEL_METRICS_EXPORTER=otlp  OTEL_LOGS_EXPORTER=otlp
    OTEL_EXPORTER_OTLP_PROTOCOL=http/json  OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:4318

(set per machine in the USER settings ~/.claude/settings.json; Claude Code ignores telemetry-enabling variables in
project settings files) and this process appends every POSTed body, one JSON line per export,
to ~/.claude/telemetry/<UTC date>.jsonl. `python scripts/telemetry/usage_report.py` summarizes it.

    python scripts/telemetry/otlp_sink.py --ensure   # start detached unless something already listens (SessionStart)
    python scripts/telemetry/otlp_sink.py            # run in the foreground

Loopback only. The weekly-quota percentage is not in the telemetry; the claude.ai meter stays the pacing authority.
"""
import datetime
import http.server
import json
import pathlib
import socket
import subprocess
import sys

HOST, PORT = "127.0.0.1", 4318
OUT_DIR = pathlib.Path.home() / ".claude" / "telemetry"


def listening() -> bool:
    with socket.socket() as s:
        s.settimeout(0.5)
        return s.connect_ex((HOST, PORT)) == 0


class Handler(http.server.BaseHTTPRequestHandler):
    def do_POST(self):  # noqa: N802 (http.server naming)
        body = self.rfile.read(int(self.headers.get("Content-Length") or 0))
        try:
            payload = json.loads(body or b"{}")
        except ValueError:
            payload = {"unparsed_bytes": len(body), "content_type": self.headers.get("Content-Type")}
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        now = datetime.datetime.now(datetime.timezone.utc)
        rec = {"received": now.isoformat(timespec="seconds"), "path": self.path, "body": payload}
        with open(OUT_DIR / f"{now:%Y-%m-%d}.jsonl", "a", encoding="utf-8") as f:
            f.write(json.dumps(rec, separators=(",", ":")) + "\n")
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(b"{}")

    def log_message(self, *args):  # silence per-request stderr lines
        pass


def main(argv):
    if "--ensure" in argv:
        if listening():
            return 0
        flags = 0
        if sys.platform == "win32":
            flags = subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.CREATE_NO_WINDOW
        subprocess.Popen([sys.executable, __file__], creationflags=flags, stdin=subprocess.DEVNULL,
                         stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, close_fds=True,
                         start_new_session=(sys.platform != "win32"))
        return 0
    http.server.ThreadingHTTPServer((HOST, PORT), Handler).serve_forever()
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
