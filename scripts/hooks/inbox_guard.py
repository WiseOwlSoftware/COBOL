#!/usr/bin/env python3
"""Stop hook: an attended lane's turn does not end while it has no inbox watcher armed (kb/Work PB3387).

The watcher (`mailbox.py watch --inbox <lane> --timeout 0`, a BACKGROUND Bash task) is the only wake-up between the two
attended lanes, and only the session itself can arm it (kb/Work PB2814). It exits each time it fires, so the session
must act on the inbox and arm it again. PB2814 made the state visible at every session start; nothing held it during a
session. On 2026-10-18 the operator's watcher fired at 14:40 on a Mythos message, was never re-armed, and nine messages
(three of them landing requests) waited until 17:14.

So at every turn's end this hook asks `mailbox.status` (the one rule for this session's lane and whether THIS session
has a watcher armed) and, when it reads false, refuses the stop with that ⛔ line as the reason: the session acts on the
inbox and arms the watcher before its turn ends. A loop unit (COBOL_LOOP_UNIT), a host without the coordination
directory and a session with a watcher armed pass silently. The refusal is made once per stop: when Claude Code reports
`stop_hook_active` (this turn already continued for a Stop hook) the line becomes a warning, so the hook never loops.
A failure of the hook itself is a warning too, never an exception: a hook must not break the session.

`python scripts/hooks/inbox_guard.py --self-test` drives every arm without a session.
"""
from __future__ import annotations

import json
import os
import pathlib
import sys
from typing import Any, Callable

REPO = pathlib.Path(__file__).resolve().parents[2]


def live_status() -> tuple[str, bool] | None:
    """mailbox.status for this session, or None when no attended lane applies (a loop unit, no coordination dir)."""
    sys.path.insert(0, str(REPO / "scripts" / "orchestrator"))
    import coord  # noqa: PLC0415
    import mailbox  # noqa: PLC0415
    if os.environ.get(coord.LOOP_UNIT_ENV):
        return None
    cdir = coord.coord_path(None)
    if not cdir.is_dir():
        return None
    return mailbox.status(cdir)


def decide(payload: dict[str, Any], status: Callable[[], tuple[str, bool] | None]) -> dict[str, Any] | None:
    """The hook's JSON answer, or None to let the turn end silently."""
    try:
        got = status()
    except Exception as exc:  # noqa: BLE001 - a hook must never break the session
        return {"systemMessage": f"⚠ inbox guard failed ({type(exc).__name__}: {exc}): run "
                                 "`python scripts/orchestrator/mailbox.py status` by hand (kb/Work PB3387)"}
    if got is None or got[1]:
        return None
    line = got[0]
    if payload.get("stop_hook_active"):
        return {"systemMessage": line}
    return {"decision": "block", "reason": f"{line}. Do it now, before this turn ends (kb/Work PB3387)."}


def _self_test() -> int:
    fails: list[str] = []

    def check(name: str, got: Any, want: Any) -> None:
        if got != want:
            fails.append(f"{name}: want {want!r}, got {got!r}")

    unarmed = "⛔ mailbox (operator): 2 open in to-operator (oldest 14:40); NO watcher armed for this session"
    check("a loop unit or a host without the coordination dir ends its turn silently", decide({}, lambda: None), None)
    check("a session with a watcher armed ends its turn silently",
          decide({}, lambda: ("mailbox (operator): 0 open; watcher armed (pid 12)", True)), None)
    blocked = decide({"stop_hook_active": False}, lambda: (unarmed, False))
    check("no watcher armed: the stop is refused", (blocked or {}).get("decision"), "block")
    check("the refusal carries the status line that names the arm command", unarmed in (blocked or {}).get("reason", ""),
          True)
    again = decide({"stop_hook_active": True}, lambda: (unarmed, False))
    check("a turn already continued for a Stop hook is warned, never refused again (no loop)",
          (again or {}).get("decision"), None)
    check("the warning still shows the line", (again or {}).get("systemMessage"), unarmed)

    def broken() -> tuple[str, bool]:
        raise OSError("coordination dir unreadable")
    failed = decide({}, broken)
    check("a failing status is a warning, not an exception or a refusal",
          ((failed or {}).get("decision"), "inbox guard failed" in (failed or {}).get("systemMessage", "")), (None, True))
    # The live path reads the real lane rule and the real process table, and never raises.
    try:
        live = decide({"stop_hook_active": True}, live_status)
        check("the live check answers None or a JSON object", live is None or isinstance(live, dict), True)
    except Exception as exc:  # noqa: BLE001
        fails.append(f"the live check raised {type(exc).__name__}: {exc}")
    for f in fails:
        print(f"FAIL  {f}")
    print(f"=== INBOX GUARD SELF-TEST: {'GREEN' if not fails else f'RED ({len(fails)})'} ===")
    return 1 if fails else 0


def main() -> int:
    if sys.argv[1:] == ["--self-test"]:
        return _self_test()
    try:
        payload = json.loads(sys.stdin.read() or "{}")
    except ValueError:
        payload = {}
    answer = decide(payload if isinstance(payload, dict) else {}, live_status)
    if answer:
        json.dump(answer, sys.stdout)
    return 0


if __name__ == "__main__":
    sys.exit(main())
