#!/usr/bin/env python3
"""PreToolUse hook for READ-ONLY agent roles (.claude/agents/cobol-refuter.md, cobol-adjudicator.md) — BLOCK any
Write/Edit/NotebookEdit whose target lies inside a git working tree (the main checkout or any worktree).

Owner decision 2026-09-25 (tooling recommendations 4 + 9): "the repo is read-only to you" was a sentence in every
refuter/adjudicator brief, and a sentence is forgotten; this makes it structural. The roles still need Write for their
checkpoint files (`<out>/<stage>-<slug>.jsonl`, reports) — those live in the scratchpad, which is not a git tree, so
they pass. Anything unparseable passes (a hook must never wedge an agent), but a git that does not ANSWER in time
REFUSES the write (kb/Work PB3536): it used to pass it, so under a loaded host a read-only role could write inside a
tree; dispatch_guard.py's practices check failed open the same way. A refusal wedges nothing: the write is re-issued.
"""
import json
import os
import pathlib
import subprocess
import sys

try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    sys.exit(0)

# the harness reads hook stderr as UTF-8; Windows would otherwise encode it in the console code page (an em dash arrived
# mangled in the first live block, 2026-09-25)
sys.stderr.reconfigure(encoding="utf-8")

target = (data.get("tool_input") or {}).get("file_path") or (data.get("tool_input") or {}).get("notebook_path")
if not target:
    sys.exit(0)

d = pathlib.Path(target)
if not d.is_absolute():
    d = pathlib.Path(data.get("cwd") or ".") / d
d = d.parent
while not d.exists() and d != d.parent:
    d = d.parent

#: How long `git rev-parse` may take before the write is REFUSED (never passed). A hang guard, not a verdict: the
#: self-test sets the variable so that git's answer decides each case, and plants a tiny value to see the refusal.
GIT_WAIT_S = float(os.environ.get("COBOL_READONLY_GIT_WAIT_S") or 15)
try:
    top = subprocess.run(["git", "-C", str(d), "rev-parse", "--show-toplevel"], capture_output=True, text=True,
                         timeout=GIT_WAIT_S)
except subprocess.TimeoutExpired:
    sys.stderr.write(f"BLOCKED by scripts/hooks/readonly_repo.py: git gave no answer within {GIT_WAIT_S:g} s (a loaded "
                     f"host), so it is not known whether {target} is inside a git working tree; this role is READ-ONLY, "
                     "so the write is refused: re-issue it (kb/Work PB3536).\n")
    sys.exit(2)
except Exception:  # noqa: BLE001 — no git to ask at all: the environment is broken, and a hook never wedges
    sys.exit(0)
if top.returncode == 0 and top.stdout.strip():
    sys.stderr.write(f"BLOCKED by scripts/hooks/readonly_repo.py: this role is READ-ONLY — {target} is inside the git "
                     f"working tree {top.stdout.strip()}. Write only your checkpoint/report files, under the scratchpad "
                     "path your brief names. Report proposed changes in your result instead of making them.\n")
    sys.exit(2)
sys.exit(0)
