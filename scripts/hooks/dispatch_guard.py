#!/usr/bin/env python3
"""PreToolUse hook (matcher Agent|Workflow) — a unit of the orchestrator loop launches NO fleet once its supervisor has
signalled the unit's wind-down.

In a unit of the orchestrator loop (the supervisor exports COBOL_LOOP_UNIT), a Workflow call or an implementer or
finisher Agent call is refused while the supervisor's wind-down signal <coord>/STOP-UNIT exists (kb/Work PB2597): wave
1038's unit started an eight-implementer Workflow 17 s after STOP-UNIT and the loop's fleet stop, and all eight returned
SPLIT before any work. An attended session sharing the coordination directory is never refused, and neither is a
read-only or mechanical role.

That is the hook's ONE rule. It says nothing about WHEN or HOW a session delegates: the owner removed every such rule on
2026-11-10 (kb/Work PB5112: "You decide when to delegate"), and with them this hook's former checks that the
`workstream` skill was loaded and that an implementer's brief was a rendered, checked spec. The skill, the spec renderer
and `check_practices.py` remain tools a session may use.

Exit 2 blocks the call and returns stderr to the agent. Anything unparseable passes (a hook must never wedge a session).
The hook writes NOTHING (kb/Work PB3186): the dispatch ledger is the planner's and the spec renderer's.
"""
import json
import os
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "orchestrator"))
import coord  # noqa: E402  (the one definition of STOP-UNIT and the loop-unit marker)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()

try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    sys.exit(0)

#: The roles that start write-capable work: with a Workflow, the calls a winding-down unit may not make.
FLEET_ROLES = {"cobol-implementer", "cobol-finisher"}

tool = data.get("tool_name", "")
args = data.get("tool_input") or {}
if tool not in ("Agent", "Workflow"):
    sys.exit(0)
role = args.get("subagent_type", "") if tool == "Agent" else ""
if (tool == "Workflow" or role in FLEET_ROLES) and os.environ.get(coord.LOOP_UNIT_ENV) and coord.stop_unit().exists():
    print(f"DISPATCH GUARD: the supervisor has signalled this {os.environ[coord.LOOP_UNIT_ENV]} unit's wind-down "
          f"({coord.stop_unit()} exists): launch no fleet. Write the handoff naming the plan (its specs and args file are "
          "reusable) and every branch, then end; a fresh unit launches it (kb/Work PB2597, units/common.md).",
          file=sys.stderr)
    sys.exit(2)
sys.exit(0)
