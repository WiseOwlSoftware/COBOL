#!/usr/bin/env python3
"""Cases of session_probe.py (kb/Work PB5939): the sections run at once and print in the list's order, a failing section
says UNREAD without costing the others theirs, and a GitHub timestamp reads as the sequential probe printed it.
Run: python scripts/self_tests.py --only scripts/selftests/session_probe/test_session_probe.py"""
from __future__ import annotations

import sys
import threading
import time

import selftest_cases
from standard_streams import ensure_utf8

sp = selftest_cases.subject(__file__)   # scripts/session_probe.py


def _boom() -> list[str]:
    raise RuntimeError("no")


def case_banner_and_timestamp(expect) -> None:
    text = sp.run(sections=[("a", lambda: ["x"])])
    lines = text.splitlines()
    expect("the banner opens and closes the text",
           lines[0].startswith("=== SESSION PROBE (") and lines[-1] == sp.PROBE_BANNER)
    expect.equal("a GitHub timestamp reads as the sequential probe printed it",
                 sp.gh_time("2026-11-18T18:01:56Z"), "11/18/2026 18:01:56")


def _merge_queue_with(protection: str) -> list[str]:
    """merge_queue() with no ruleset on main (exit 3) and `protection` as the classic-protection API's answer."""
    saved = sp.py, sp.gh, sp.have_gh
    sp.py = lambda *a: sp.Cmd(3, "ABSENT: no ruleset on main")
    sp.gh = lambda *a: sp.Cmd(0, protection)
    sp.have_gh = lambda: True
    try:
        return sp.merge_queue()
    finally:
        sp.py, sp.gh, sp.have_gh = saved


def case_protection_without_required_checks_is_drift_not_unread(expect) -> None:
    # GitHub omits required_status_checks when none are enabled (kb/Work PB5939 review R1-1): that is the DRIFTED alarm
    # with contexts=[], as the pwsh probe printed it, never the "did not parse" line.
    got = _merge_queue_with('{"enforce_admins": {"enabled": true}}')
    expect("an absent required_status_checks is the DRIFTED alarm", len(got) == 1 and "DRIFTED" in got[-1]
           and "contexts=[]" in got[-1], got)
    got = _merge_queue_with('{"required_status_checks": {"contexts": ["ci-gate"]}, "enforce_admins": {"enabled": true}}')
    expect("ci-gate with enforce_admins is the queue-not-applied alarm", "requires ci-gate" in got[-1], got)
    for bad in ('not json', '[]', '{"required_status_checks": {"contexts": []}}'):
        got = _merge_queue_with(bad)
        expect(f"a response that does not parse stays UNREAD: {bad}", "did not parse" in got[-1], got)


def case_sections_are_listed_once(expect) -> None:
    names = [n for n, _ in sp.SECTIONS]
    expect("every section is listed once", len(names) == len(set(names)))


def case_sections_run_at_once_and_print_in_order(expect) -> None:
    # Three sections that each wait for the other two can only all finish if they run at the same time (a structural
    # proof of concurrency, never a measured duration); the first finishes last, so the order printed is the list's.
    meet = threading.Barrier(3, timeout=10)

    def together(tag: str, pause: float):
        def section() -> list[str]:
            meet.wait()
            time.sleep(pause)
            return [tag]
        return section

    lines = sp.run(sections=[("a", together("first", 0.2)), ("b", _boom), ("c", together("third", 0.0)),
                             ("d", together("fourth", 0.0))]).splitlines()
    expect.equal("the lines print in the sections' order, not the order they finish",
                 [lines[1], lines[3], lines[4]], ["first", "third", "fourth"])
    expect("a raising section says UNREAD and the others still print", "b      : UNREAD (RuntimeError: no)" in lines[2])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
