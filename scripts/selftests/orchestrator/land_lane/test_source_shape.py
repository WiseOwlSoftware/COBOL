#!/usr/bin/env python3
"""The shape of land_lane.py's own code (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5434; kb/Work PB3310, PB3640 review
R1-1 and R1-3, PB3714, PB5109): exit 13 is a wait, no shared file is one, and the conflict brief foresees the merge with git's dry
run and reads the outcome through the verdict line's one home. Read from the subject's whole code, which holds no case any more.
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/land_lane/test_source_shape.py"""
from __future__ import annotations

import pathlib
import sys

import selftest_cases
from standard_streams import ensure_utf8

ll = selftest_cases.subject(__file__)   # scripts/orchestrator/land_lane.py


def case_000_shape_a_second_land_exit_13_is_a_wait_answered_b(expect):
    code = pathlib.Path(ll.__file__).read_text(encoding="utf-8").replace("\r\n", "\n")
    expect("shape: a second land (exit 13) is a WAIT answered by its own reader, and no wait is a shared file (owner, "
           "kb/Work PB5109)",
           ll.EXIT_LAND_BUSY in ll.WAITS and "if rc == EXIT_LAND_BUSY:" in code and "driver_file" not in code
           and 16 not in ll.WAITS)
    expect("shape: the conflict brief foresees the merge with git_merge.merge_conflicts and reads the outcome with "
           "verdict_line.outcome_files, never a merge-tree or a CONFLICT parse of its own",
           "git_merge.merge_conflicts(" in code and "verdict_line.outcome_files(" in code
           and '"merge' + '-tree"' not in code and "CONFLICT" + " in " not in code)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
