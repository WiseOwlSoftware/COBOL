#!/usr/bin/env python3
"""PreToolUse hook — REFUSE to build or test while a subagent fleet is live.

⛔ WHY THIS EXISTS. On 2026-08-04 a ten-agent measurement fan-out was dispatched over the §15 result-type
population, and the fix for what it was measuring was then implemented WHILE IT RAN: ~6 `dotnet build`
invocations plus both test gates, over ~1.75 h and 60 agents. Its finders probed a binary that changed under
them and its refuters re-ran probes against a tree where the defects were already fixed, so not one of its
verdicts was usable. One build even failed outright with `MSB3027: cobol.exe locked by another process` — a live
agent — and that alarm was recorded as "transient" and the building continued.

The owner's correction: each repetition wasted tokens. Memory alone did not stop it, so the guard is mechanical.

⚖ THE RULE IT ENFORCES is the ORDER of two decisions, not just the concurrency:
  1. Measure INLINE first. A fan-out for work an inline probe already did is pure waste — PB15's entire
     population was derived and measured in three tool calls before the fleet was ever dispatched.
  2. Once a fleet IS running, the tree is FROZEN. Build/test only after the completion signal, or stop the fleet.

Detection is by TRANSCRIPT MTIME, which is the one signal that cannot be faked by a stale process: a live agent
writes to its own `agent-*.jsonl` continuously, so a file touched within the window means an agent is thinking
right now. Keyed on this session's id, so another project's fleet never blocks this one.

FAIL-OPEN BY CONSTRUCTION. Any error, any missing path, any unreadable directory ⇒ exit 0 and allow the build. A
guard that blocks the build because IT broke would be worse than the defect it prevents.
"""
import json
import os
import pathlib
import sys
import time

WINDOW_SECONDS = 120

# `dotnet clean/publish/run` rewrite or delete the same outputs a live agent is executing, so they are in scope
# too. `dotnet --version`, `dotnet tool`, `dotnet nuget` etc. are not.
BUILD_VERBS = ("build", "test", "clean", "publish", "run", "msbuild")


def bail() -> None:
    """Allow the tool call. Every failure path lands here."""
    sys.exit(0)


def is_build_command(cmd: str) -> bool:
    low = cmd.lower()
    if "dotnet" not in low:
        return False
    # Match `dotnet <verb>` allowing intervening flags is overkill; the real invocations in this repo are
    # `dotnet build ...` / `dotnet test ...`, plus `timeout NNN dotnet build ...`.
    return any(f"dotnet {verb}" in low for verb in BUILD_VERBS)


def live_agent_transcripts(session_id: str) -> list[pathlib.Path]:
    cfg = os.environ.get("CLAUDE_CONFIG_DIR")
    root = pathlib.Path(cfg) if cfg else pathlib.Path.home() / ".claude"
    projects = root / "projects"
    if not projects.is_dir():
        return []

    cutoff = time.time() - WINDOW_SECONDS
    live: list[pathlib.Path] = []
    # ~/.claude/projects/<sanitized-cwd>/<session-id>/subagents/**/agent-*.jsonl
    # Globbing on the session id rather than deriving the sanitized cwd keeps this correct whatever the
    # sanitization rule is, and scopes the guard to THIS session.
    for path in projects.glob(f"*/{session_id}/subagents/**/agent-*.jsonl"):
        try:
            if path.stat().st_mtime >= cutoff:
                live.append(path)
        except OSError:
            continue
    return live


try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    bail()

command = (data.get("tool_input") or {}).get("command") or ""
if not command or not is_build_command(command):
    bail()

session_id = data.get("session_id") or ""
if not session_id:
    bail()

try:
    live = live_agent_transcripts(session_id)
except Exception:  # noqa: BLE001
    bail()

if not live:
    bail()

names = sorted({p.parent.name for p in live})
detail = ", ".join(names[:4]) + (f" (+{len(names) - 4} more)" if len(names) > 4 else "")

json.dump(
    {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": (
                f"BLOCKED — {len(live)} subagent transcript(s) were written in the last {WINDOW_SECONDS}s, so a "
                f"fleet is LIVE: {detail}. Building or testing now changes the binary those agents are probing, "
                f"which is what made a 60-agent run unusable on 2026-08-04 (PB15). "
                f"Do ONE of: (a) wait for the completion notification, then build; "
                f"(b) stop the fleet with TaskStop if its results are no longer worth having; "
                f"(c) if you are about to fix what it measures, stop it now — you already have the answer, and a "
                f"fleet measuring a tree you are editing produces verdicts you will have to disclaim. "
                f"Meanwhile, doc / DEVLOG / design-doc / kb-note work is safe."
            ),
        }
    },
    sys.stdout,
)
