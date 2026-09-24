#!/usr/bin/env python3
"""SessionStart hook — inject the mechanical live-state probe into the session.

Plan §0's bootstrap step ③ is "run session-probe.ps1". As a manual ritual it gets skipped; as a hook it cannot be.
Never fails the session: any error is reported as context, not raised.

In a claude.ai cloud session (CLAUDE_CODE_REMOTE=true) it first initializes the private `specs-private` submodule:
the cloud VM starts from a fresh clone WITHOUT submodules, and without the spec every §/GR citation (CLAUDE.md
rule 1, `cite.py --check`) is impossible. The VM toolchain itself comes from scripts/cloud/setup-env.sh. Locally the
hook stays read-only.
"""
import json
import os
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PROBE = REPO / "scripts" / "session-probe.ps1"


def init_cloud_submodules() -> str:
    if os.environ.get("CLAUDE_CODE_REMOTE") != "true":
        return ""
    try:
        r = subprocess.run(
            ["git", "submodule", "update", "--init", "--recursive", "--depth", "1"],
            capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=180, cwd=str(REPO),
        )
        status = "ok" if r.returncode == 0 else (
            f"FAILED (exit {r.returncode}): {(r.stderr or r.stdout).strip()}\n"
            "FIX: the cloud GitHub proxy only serves repositories attached to the session — attach "
            "BrentRector/CobolSharp-private to this session (or the routine's sources), then re-run "
            "`git submodule update --init --recursive --depth 1`.")
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        status = f"FAILED: {exc}"
    return f"cloud session: git submodule update --init → {status}\n\n"


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


text = init_cloud_submodules() + (
    "Mechanical live state (scripts/session-probe.ps1). Plan §0 is the live-state SSOT; "
    "this is the computed half.\n\n" + probe()
)
json.dump(
    {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": text}},
    sys.stdout,
)
