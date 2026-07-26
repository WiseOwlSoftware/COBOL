#!/usr/bin/env python3
"""PreToolUse hook — a commit without a staged DEVLOG entry asks for confirmation.

Project rule: every commit carries at least one DEVLOG.md entry (DEVLOG is the project's ONLY history, and the
owner's article series depends on the in-the-moment reasoning). Commits have shipped without one and been
corrected; this makes the omission visible at the moment it happens.

Decision is "ask", not "deny" — amends and doc-only fixups are legitimate exceptions the owner can wave through.
"""
import json
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]


def bail() -> None:
    sys.exit(0)


try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    bail()

cmd = (data.get("tool_input") or {}).get("command", "")
if "git commit" not in cmd:
    bail()

# --amend on an existing entry, and explicit -F of a prepared message, are still subject to the rule;
# only skip when there is genuinely nothing staged (git itself will complain).
try:
    staged = subprocess.run(
        ["git", "diff", "--cached", "--name-only"],
        capture_output=True, text=True, cwd=str(REPO), timeout=30,
    ).stdout.split()
except Exception:  # noqa: BLE001
    bail()

if not staged or "DEVLOG.md" in staged:
    bail()

json.dump(
    {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "ask",
            "permissionDecisionReason": (
                "DEVLOG.md is not staged. Project rule: every commit carries a DEVLOG entry — inserted at the TOP "
                "(descending), headed '## Entry NNN — YYYY-MM-DD HH:MM TZ — Title' with a real stamp from "
                "`date \"+%Y-%m-%d %H:%M %Z\"`. Write it and stage it, or confirm this is a deliberate exception."
            ),
        }
    },
    sys.stdout,
)
