#!/usr/bin/env python3
"""The collector and `subject` (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5428): `run` finds the `case_*`
functions of a module and runs them in name order, FAILs an exception and a case that expects nothing, refuses a module
with no case; `subject` finds a case file's subject by the layout rule; a case file run bare fails at its import."""
import contextlib
import io
import os
import subprocess
import sys
import types
from pathlib import Path

import selftest_cases

REPO = Path(__file__).resolve().parents[3]
PLANT = str(REPO / "scripts" / "selftests" / "planted" / "test_p.py")


def _module(source):
    """A module defining the cases of `source`, as a case file's own module would."""
    m = types.ModuleType("planted_cases")
    m.__file__ = PLANT
    exec(compile(source, PLANT, "exec"), m.__dict__)  # noqa: S102 — planted case source, run to prove the collector
    return m


def _run(source):
    """(the exit code, the printed output) of `selftest_cases.run` over the planted module."""
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        rc = selftest_cases.run(_module(source))
    return rc, out.getvalue()


def case_00_passing_cases_run_in_name_order(expect):
    rc, out = _run("def case_b(expect):\n    expect('second', True)\n\n"
                   "def case_a(expect):\n    expect('first', True)\n    expect.equal('equal', 1, 1)\n")
    expect("every expectation passes: exit 0 and the verdict line counts cases and expectations",
           (rc, "(2 cases, 3 expectations) ===" in out, out.index("first") < out.index("second")), (0, True, True))
    expect("the cases run in NAME order whatever the order they are defined in", out.index("first") < out.index("second"))


def case_01_a_failed_expectation_fails_the_file(expect):
    rc, out = _run("def case_a(expect):\n    expect('bad', False, 'why')\n    expect.equal('eq', 1, 2)\n")
    expect("a failed expectation: exit 1, its label and detail printed, the verdict FAIL",
           (rc, "FAIL     bad: why" in out, "FAIL     eq: got 1, want 2" in out, "FAIL (1 cases" in out),
           (1, True, True, True))


def case_02_an_exception_is_a_failure_with_its_traceback(expect):
    rc, out = _run("def case_a(expect):\n    raise ValueError('boom')\n\ndef case_b(expect):\n    expect('after', True)\n")
    expect("an exception FAILs its case with the traceback, and the next case still runs",
           (rc, "case_a raised" in out, "ValueError: boom" in out, "PASS     after" in out), (1, True, True, True))


def case_03_a_case_with_no_expectation_fails(expect):
    rc, out = _run("def case_a(expect):\n    return\n")
    expect("a case that returns before its first expect passes nothing",
           (rc, "case_a made no expectation" in out), (1, True))


def case_04_a_module_with_no_case_is_refused(expect):
    rc, out = _run("def helper():\n    pass\n")
    expect("an empty file is not a pass", (rc, "no case_ function" in out), (1, True))


def case_05_subject_follows_the_layout_rule(expect):
    found = selftest_cases.subject(REPO / "scripts" / "selftests" / "orchestrator" / "plan_wave" / "test_planner.py")
    expect("the subject of a case file is the script its directory names, imported once",
           (found.__name__, Path(found.__file__).resolve() == REPO / "scripts" / "orchestrator" / "plan_wave.py"),
           ("plan_wave", True))
    expect("the subject's directory is first on sys.path, so its own sibling imports resolve",
           str(REPO / "scripts" / "orchestrator") in sys.path)


def case_06_subject_refuses_what_has_none(expect):
    refused = {}
    for label, path in (("not Python", REPO / "scripts" / "selftests" / "linux-gate" / "test_a.py"),
                        ("no subject", REPO / "scripts" / "selftests" / "no_such_script" / "test_a.py"),
                        ("_tree", REPO / "scripts" / "selftests" / "_tree" / "held" / "test_a.py"),
                        ("outside the repository", Path(os.path.abspath(os.sep)) / "x" / "test_a.py")):
        try:
            selftest_cases.subject(path)
            refused[label] = False
        except LookupError:
            refused[label] = True
    expect("a subject that is not Python, one that does not exist, a `_tree` test and a file outside the repository "
           "are each refused with a LookupError", refused,
           {"not Python": True, "no subject": True, "_tree": True, "outside the repository": True})


def case_07_a_case_file_run_bare_fails_at_its_import(expect):
    env = {k: v for k, v in os.environ.items() if k != "PYTHONPATH"}
    case_file = REPO / "scripts" / "selftests" / "selftest_layout" / "test_refusals.py"
    r = subprocess.run([sys.executable, str(case_file)], capture_output=True, text=True, env=env, cwd=case_file.parent,
                       check=False)
    expect("a case file run with no PYTHONPATH fails at `import selftest_cases`, loudly",
           (r.returncode != 0, "No module named 'selftest_cases'" in r.stderr), (True, True))


if __name__ == "__main__":
    sys.exit(selftest_cases.run(sys.modules[__name__]))
