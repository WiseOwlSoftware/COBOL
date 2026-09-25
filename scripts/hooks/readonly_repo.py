#!/usr/bin/env python3
"""PreToolUse hook for READ-ONLY agent roles (.claude/agents/cobol-refuter.md, cobol-adjudicator.md) — BLOCK any
Write/Edit/NotebookEdit whose target lies inside a git working tree (the main checkout or any worktree).

Owner decision 2026-09-25 (tooling recommendations 4 + 9): "the repo is read-only to you" was a sentence in every
refuter/adjudicator brief, and a sentence is forgotten; this makes it structural. The roles still need Write for their
checkpoint files (`<out>/<stage>-<slug>.jsonl`, reports) — those live in the scratchpad, which is not a git tree, so
they pass. Anything unparseable passes (a hook must never wedge an agent).
"""
import json
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

try:
    top = subprocess.run(["git", "-C", str(d), "rev-parse", "--show-toplevel"], capture_output=True, text=True,
                         timeout=15)
except Exception:  # noqa: BLE001
    sys.exit(0)
if top.returncode == 0 and top.stdout.strip():
    sys.stderr.write(f"BLOCKED by scripts/hooks/readonly_repo.py: this role is READ-ONLY — {target} is inside the git "
                     f"working tree {top.stdout.strip()}. Write only your checkpoint/report files, under the scratchpad "
                     "path your brief names. Report proposed changes in your result instead of making them.\n")
    sys.exit(2)
sys.exit(0)
