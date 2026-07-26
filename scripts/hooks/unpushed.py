#!/usr/bin/env python3
"""Stop hook — surface unpushed commits.

Project rule: commit AND push every checkpoint. Work that is committed but not pushed is invisible to the owner and
lost to a machine failure. Read-only; silent when the branch is up to date or has no upstream.
"""
import json
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]


def bail() -> None:
    sys.exit(0)


try:
    r = subprocess.run(
        ["git", "log", "--oneline", "@{u}..HEAD"],
        capture_output=True, text=True, cwd=str(REPO), timeout=30,
    )
except Exception:  # noqa: BLE001
    bail()

if r.returncode != 0:  # no upstream configured
    bail()

count = len([line for line in r.stdout.splitlines() if line.strip()])
if count == 0:
    bail()

json.dump(
    {"systemMessage": f"⚠ {count} unpushed commit(s) — project rule: commit AND push every checkpoint."},
    sys.stdout,
)
