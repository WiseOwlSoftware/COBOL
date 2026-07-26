#!/usr/bin/env python3
"""SessionStart hook — inject the mechanical live-state probe into the session.

Plan §0's bootstrap step ③ is "run session-probe.ps1". As a manual ritual it gets skipped; as a hook it cannot be.
Read-only. Never fails the session: any error is reported as context, not raised.
"""
import json
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PROBE = REPO / "scripts" / "session-probe.ps1"


def probe() -> str:
    if not PROBE.exists():
        return f"session-probe.ps1 not found at {PROBE}"
    try:
        # -Command (not -File) so the console encoding can be set first: the probe emits '·' and '⚠',
        # which the default OEM code page turns into mojibake.
        r = subprocess.run(
            ["pwsh", "-NoProfile", "-Command",
             f"[Console]::OutputEncoding=[Text.Encoding]::UTF8; & '{PROBE.as_posix()}'"],
            capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=90, cwd=str(REPO),
        )
        return ((r.stdout or "") + (r.stderr or "")).strip() or "session-probe produced no output"
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        return f"session-probe failed: {exc}"


text = (
    "Mechanical live state (scripts/session-probe.ps1). Plan §0 is the live-state SSOT; "
    "this is the computed half.\n\n" + probe()
)
json.dump(
    {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}},
    sys.stdout,
)
