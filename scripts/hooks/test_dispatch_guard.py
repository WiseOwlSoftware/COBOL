#!/usr/bin/env python3
"""Self-test for dispatch_guard.py — a loop unit launches no fleet after its supervisor's STOP-UNIT, and the hook refuses
nothing else: how a session delegates is its own judgment (kb/Work PB5112).
Run: python scripts/hooks/test_dispatch_guard.py"""
import json
import os
import pathlib
import subprocess
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from scratch_repo import scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()

HOOK = pathlib.Path(__file__).with_name("dispatch_guard.py")
TMP = scratch_dir("test-dispatch-guard-")
COORD = TMP / "coord"   # the hook reads STOP-UNIT there: never the real coordination directory from a test
COORD.mkdir()
TRANSCRIPT = TMP / "session.jsonl"   # a session that loaded no skill at all: nothing the hook reads any more
TRANSCRIPT.write_text(json.dumps({"type": "tool_use", "name": "Bash", "input": {"command": "ls"}}) + "\n",
                      encoding="utf-8", newline="\n")


def agent(role: str, prompt: str) -> dict:
    return {"tool_name": "Agent", "tool_input": {"subagent_type": role, "prompt": prompt},
            "transcript_path": str(TRANSCRIPT)}


WORKFLOW = {"tool_name": "Workflow", "tool_input": {"script": "export const meta = {}"}, "transcript_path": str(TRANSCRIPT)}
IMPLEMENTER = agent("cobol-implementer", "Fix PB1 at its root; the note names the site.")   # an inline brief
FINISHER = agent("cobol-finisher", "Resolve the conflict in x.cs and land it")
REFUTER = agent("cobol-refuter", "Try to overturn this verdict")

# The cases choose the session kind themselves (UNIT adds COBOL_LOOP_UNIT), so the caller's own kind never leaks in: a gate
# run inside a loop unit made the "attended session" case a loop unit and turned this self-test red on a correct hook
# (kb/Work PB2702).
ATTENDED = {k: v for k, v in os.environ.items() if k != "COBOL_LOOP_UNIT"} | {"COBOL_COORD_DIR": str(COORD)}
UNIT = dict(ATTENDED, COBOL_LOOP_UNIT="wave")


PASSES, REFUSED = 0, 2   # the hook's two answers; any other exit code is the hook crashing, which no case expects


def answer(payload: object, env: dict) -> int:
    """The hook's exit code for one call (kb/Work PB5112 review R1-4: a case that only asked "was it refused?" counted a
    crash, exit 1, as a pass, and three planted mutants survived)."""
    r = subprocess.run([sys.executable, str(HOOK)], input=payload if isinstance(payload, str) else json.dumps(payload),
                       capture_output=True, text=True, timeout=180, env=env)
    return r.returncode


# No rule about delegating (kb/Work PB5112): with no skill loaded and a hand-written brief, every dispatch passes.
FREE = [
    ("attended: a Workflow with no skill loaded passes", WORKFLOW, ATTENDED, PASSES),
    ("attended: an implementer with an inline brief passes", IMPLEMENTER, ATTENDED, PASSES),
    ("attended: a finisher with an inline brief passes", FINISHER, ATTENDED, PASSES),
    ("attended: a read-only role passes", REFUTER, ATTENDED, PASSES),
    ("loop unit, no STOP-UNIT: a Workflow passes", WORKFLOW, UNIT, PASSES),
    ("loop unit, no STOP-UNIT: an implementer passes", IMPLEMENTER, UNIT, PASSES),
    ("another tool is not guarded", {"tool_name": "Bash", "tool_input": {"command": "ls"}}, UNIT, PASSES),
    ("unparseable input never wedges a session", "not json", UNIT, PASSES),
]
# The one rule (kb/Work PB2597): in a LOOP UNIT with STOP-UNIT present, no fleet launch; a read-only role still passes, and
# an attended session sharing the coordination directory is never refused.
STOPPED = [
    ("loop unit after STOP-UNIT: a Workflow is refused", WORKFLOW, UNIT, REFUSED),
    ("loop unit after STOP-UNIT: an implementer is refused", IMPLEMENTER, UNIT, REFUSED),
    ("loop unit after STOP-UNIT: a finisher is refused", FINISHER, UNIT, REFUSED),
    ("loop unit after STOP-UNIT: a read-only role passes", REFUTER, UNIT, PASSES),
    ("attended session, the loop's STOP-UNIT present: a Workflow passes", WORKFLOW, ATTENDED, PASSES),
    ("attended session, the loop's STOP-UNIT present: an implementer passes", IMPLEMENTER, ATTENDED, PASSES),
]

fails = [f"{n} (exit {got})" for n, p, env, want in FREE if (got := answer(p, env)) != want]
(COORD / "STOP-UNIT").write_text("", encoding="utf-8", newline="\n")
fails += [f"{n} (exit {got})" for n, p, env, want in STOPPED if (got := answer(p, env)) != want]
(COORD / "STOP-UNIT").unlink()
# The hook writes nothing (kb/Work PB3186): the coordination directory holds what the test put there, and no more.
if sorted(p.name for p in COORD.iterdir()):
    fails.append("the hook wrote into the coordination directory")
for n in fails:
    print(f"FAIL: {n}")
TOTAL = len(FREE) + len(STOPPED) + 1
print(f"dispatch guard self-test: {TOTAL - len(fails)}/{TOTAL} cases OK")
sys.exit(1 if fails else 0)
