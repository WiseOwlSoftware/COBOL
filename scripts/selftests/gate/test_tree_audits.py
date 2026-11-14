#!/usr/bin/env python3
"""The audits' own checks on the real tree and on planted ones: ONE SHAPE and the CI invariant (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432; kb/Work PB1708, PB1721, PB3022, PB3536, PB4328, PB4395)."""

import sys

from fixtures import audit_shape_arms
import selftest_cases
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
REPO = gate_mod.REPO
from gate_audits import ci_audits_not_in_gate


def case_00_one_shape_kb_work_pb3022_audit_names(expect):
    shape = audit_shape_arms()
    expect("ONE SHAPE (kb/Work PB3022): the audit names every line of the live trees that tells anyone to run a gate mode, "
        "skips history and frozen evidence, and finds the tree it walks", shape == "", shape)
    planted_ci = ("jobs:\n  audits:\n    runs-on: ubuntu-latest\n    steps:\n      - run: git submodule update --init sub/a\n"
                  "      - run: |\n          python3 scripts/a.py --check\n          python3 scripts/devlog.py check\n"
                  "      # python3 scripts/commented.py\n      - run: python3 scripts/b.py\n")
    planted_gate_audits = (("A", ["scripts/a.py", "--check"]),)
    planted_elsewhere = {"scripts/devlog.py": "x", "scripts/gone.py": "x"}
    planted_audits = ci_audits_not_in_gate(planted_ci, planted_gate_audits, (), planted_elsewhere, 3, "sub/a")
    expect("the CI invariant (kb/Work PB1957 row 44): a CI audit the gate does not run, a submodule it does not make ready "
        "and a recorded exception CI no longer runs are each named; a recorded exception CI runs and a commented-out "
        "command are not", planted_audits == [
            "CI's audits job runs scripts/b.py and the gate's AUDITS do not",
            "CI's audits job makes the submodule sub/a ready and the gate's AUDIT_PREREQUISITES do not",
            "scripts/gone.py is recorded as running elsewhere (CI_AUDITS_ELSEWHERE) but CI's audits job no longer runs it"],
        str(planted_audits))
    blind = ci_audits_not_in_gate(planted_ci, planted_gate_audits, (), planted_elsewhere, 4, "sub/z")[:2]
    expect("a reading of CI's audits job that saw too few commands, or not the submodule it must make ready, is a finding, "
        "never a clean answer over nothing", blind == [
            "CI's audits job runs only 3 `python3 <script>` commands: the reading is broken",
            "CI's audits job no longer makes sub/z ready: the reading is broken"], str(blind))
    real_ci = ci_audits_not_in_gate((REPO / ".github" / "workflows" / "build-and-test.yml").read_text(encoding="utf-8"))
    expect("THE REAL TREE: every command of CI's audits job is a gate audit, an audit prerequisite or a recorded exception "
        "(the next audit CI gains reaches the gate; kb/Work PB4395 moved this from a compiler test)", real_ci == [],
        str(real_ci))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
