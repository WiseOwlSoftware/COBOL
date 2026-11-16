#!/usr/bin/env python3
"""The GnuCOBOL differential's planted worlds (kb/Work PB5673; DESIGN-orchestrator-loop.md section 16.2): `corpus()` is a
scratch directory holding two `.at` wrappers, `syn_a.at` (two cases) and `syn_b.at` (one), and `result(case_id, verdict)`
is one run record as `run_case` returns it. No case lives here."""
from __future__ import annotations

from pathlib import Path

from scratch_repo import scratch_dir

WRAPPER = """AT_BANNER([planted])
{setups}
"""

SETUP = """AT_SETUP([planted case])
AT_DATA([prog.cob], [
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROG.
])
AT_CHECK([$COMPILE prog.cob], [0], [], [])
AT_CLEANUP
"""


def corpus() -> Path:
    """A scratch corpus directory: `syn_a.at` with two cases, `syn_b.at` with one, and a file that is no wrapper."""
    root = scratch_dir("gcdiff-selftest-")
    (root / "syn_a.at").write_text(WRAPPER.format(setups=SETUP + "\n" + SETUP), encoding="utf-8", newline="\n")
    (root / "syn_b.at").write_text(WRAPPER.format(setups=SETUP), encoding="utf-8", newline="\n")
    (root / "notes.txt").write_text("not a wrapper\n", encoding="utf-8", newline="\n")
    return root


def result(case_id: str, verdict: str) -> dict:
    """One case's run record, with the keys `diff_baseline` reads."""
    return {"id": case_id, "tier": "DEFAULT_DIALECT", "verdict": verdict, "ourFirstError": ""}
