#!/usr/bin/env python3
"""The layout rule's refusals on planted trees (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5428): each breach
section 16.2 lists is RED, the clean tree is not, and the ratchet's set is held in sorted order, one path per line."""
import re
import sys

import selftest_cases

layout = selftest_cases.subject(__file__)   # scripts/selftest_layout.py

RUN = '\n\nif __name__ == "__main__":\n    sys.exit(selftest_cases.run(sys.modules[__name__]))\n'
GOOD = 'import selftest_cases\n\n\ndef case_a(expect):\n    expect("a", True)\n\n\ndef case_b(expect):\n    expect("b", True)\n' + RUN
SUBJECT = "scripts/foo.py"
CASE = "scripts/selftests/foo/test_a.py"


def _handles(text):
    return '"--self-test"' in text


def _check(files, remaining=()):
    """The layout rule over a planted tree: `files` is {path: text}; every Python file is a source."""
    sources = {p: t for p, t in files.items() if p.endswith(".py")}
    return layout.check(sources, sorted(files), lambda p: p in files, _handles, remaining)


def _tree(**changes):
    """The clean planted tree with `changes` ({path: text, or None to drop})."""
    files = {SUBJECT: "X = 1\n", CASE: GOOD}
    for p, t in changes.items():
        if t is None:
            files.pop(p, None)
        else:
            files[p] = t
    return files


def case_00_the_clean_tree_is_not_refused(expect):
    expect.equal("a subject, one case file of two cases in name order and nothing else: no refusal", _check(_tree()), [])
    files = {SUBJECT: "X = 1\n", "scripts/selftests/_tree/held/test_a.py": GOOD,
             "scripts/selftests/foo/fixtures.py": "def world():\n    return 1\n"}
    expect.equal("a `_tree` test needs no subject, and a fixtures.py of functions holds no case", _check(files), [])


def case_01_a_duplicate_case_name_is_refused(expect):
    text = GOOD.replace("def case_b", "def case_a")
    found = _check(_tree(**{CASE: text}))
    expect("two top-level functions of one name (Python keeps the second silently)",
           any("defines `case_a` twice" in f for f in found), str(found))


def case_02_a_test_function_is_refused(expect):
    text = GOOD.replace("def case_b(expect):", "def test_b(expect):")
    found = _check(_tree(**{CASE: text}))
    expect("a `test_x` function the collector would never call",
           any("`test_b` is neither `case_*` nor `_`-prefixed" in f for f in found), str(found))
    expect("a `_`-prefixed helper is no refusal",
           _check(_tree(**{CASE: GOOD.replace('def case_b(expect):\n    expect("b", True)',
                                              "def _helper(x):\n    return x")})) == [])


def case_03_a_module_level_expectation_is_refused(expect):
    text = GOOD.replace("\n\ndef case_a", '\nexpect("x", True)\n\n\ndef case_a', 1)
    found = _check(_tree(**{CASE: text}))
    expect("an `expect(` outside a case function (a module-level expectation is a case list again)",
           any("`expect` is called outside a `case_` function" in f for f in found), str(found))
    found = _check(_tree(**{CASE: GOOD.replace('expect("b", True)', 'expect.equal("b", 1, 1)')}))
    expect("`expect.equal(` inside a case is no refusal", found == [], str(found))
    found = _check(_tree(**{CASE: GOOD.replace("\n\ndef case_a", '\nexpect.equal("x", 1, 1)\n\n\ndef case_a', 1)}))
    expect("`expect.equal(` at module level is refused like `expect(`",
           any("outside a `case_` function" in f for f in found), str(found))


def case_04_an_out_of_order_case_is_refused(expect):
    text = GOOD.replace("case_a", "case_x").replace("case_b", "case_a")   # x then a
    found = _check(_tree(**{CASE: text}))
    expect("`case_` functions kept out of name order", any("out of name order" in f for f in found), str(found))


def case_05_a_stray_file_is_refused(expect):
    found = _check(_tree(**{"scripts/selftests/foo/helpers.py": "X = 1\n"}))
    expect("a file under selftests/ that is neither `fixtures.py` nor `test_*.py`",
           any("scripts/selftests/foo/helpers.py: a file under selftests/ that is neither" in f for f in found), str(found))
    found = _check(_tree(**{"scripts/selftests/foo/data.json": "{}"}))
    expect("... whatever its extension", any("data.json: a file under selftests/" in f for f in found), str(found))


def case_06_a_missing_or_unconverted_subject_is_refused(expect):
    found = _check(_tree(**{SUBJECT: None}))
    expect("a tests directory whose subject does not exist",
           any("the tests directory has no subject" in f for f in found), str(found))
    found = _check(_tree(**{SUBJECT: 'import sys\nif "--self-test" in sys.argv:\n    pass\n'}))
    expect("... one whose Python subject still handles `--self-test`",
           any("still handles `--self-test`" in f for f in found), str(found))
    found = _check(_tree(**{SUBJECT: "def self_test():\n    return 0\n"}))
    expect("... one whose Python subject still defines a `self_test`",
           any("still defines `self_test`" in f for f in found), str(found))
    expect("a subject that is not Python (linux-gate.sh) satisfies its directory",
           _check({"scripts/linux-gate.sh": "echo\n", "scripts/selftests/linux-gate/test_a.py": GOOD}) == [])


def case_07_the_ratchet_holds_the_set(expect):
    files = _tree(**{"scripts/old.py": 'import sys\nif "--self-test" in sys.argv:\n    pass\n'})
    found = _check(files)
    expect("a path in the tree and not in REMAINING is a new in-file self-test",
           any("scripts/old.py: a new in-file self-test" in f for f in found), str(found))
    expect("... and listed in REMAINING it passes", _check(files, ["scripts/old.py"]) == [])
    found = _check(_tree(), ["scripts/gone.py"])
    expect("a REMAINING path not in the tree is a conversion that did not remove its line",
           any("scripts/gone.py: listed in REMAINING" in f for f in found), str(found))
    files = _tree(**{"scripts/test_s.py": "X = 1\n", "scripts/docchecks/x.py": "def self_test():\n    return 0\n",
                     "scripts/docchecks/y.py": "def a_arms():\n    return 0\n"})
    found = _check(files)
    expect("a standalone `test_*.py` and a delegate defining `self_test*` or `*_arms` are counted holders",
           sorted(f.split(":")[0] for f in found) == ["scripts/docchecks/x.py", "scripts/docchecks/y.py",
                                                       "scripts/test_s.py"], str(found))


def case_08_a_module_outside_the_tree_importing_one_is_refused(expect):
    found = _check(_tree(**{"scripts/tool.py": "import selftest_cases\n"}))
    expect("a module outside selftests/ importing the collector",
           any("scripts/tool.py: a module outside selftests/ imports `selftest_cases`" in f for f in found), str(found))
    found = _check(_tree(**{"scripts/tool.py": "from fixtures import world\n",
                            "scripts/selftests/foo/fixtures.py": "def world():\n    return 1\n"}))
    expect("... or a module of a tree (fixtures)",
           any("imports `fixtures`" in f for f in found), str(found))
    found = _check(_tree(**{"scripts/tool.py": "import foo\n"}))
    expect("an ordinary import is no refusal", found == [], str(found))


def case_09_a_case_file_must_run_and_hold_a_case(expect):
    found = _check(_tree(**{CASE: GOOD.replace(RUN, "\n")}))
    expect("a case file that does not end in the `selftest_cases.run` call",
           any("does not end in the `selftest_cases.run` call" in f for f in found), str(found))
    found = _check(_tree(**{CASE: "import selftest_cases\n" + RUN}))
    expect("a case file with no `case_` function (an empty file is not a pass)",
           any("defines no `case_` function" in f for f in found), str(found))


def case_10_fixtures_hold_no_case_and_no_expectation(expect):
    found = _check(_tree(**{"scripts/selftests/foo/fixtures.py": "def case_x(expect):\n    expect('a', True)\n"}))
    expect("a fixtures.py defining a case or calling expect",
           any("defines the case `case_x`" in f for f in found) and any("calls `expect`" in f for f in found), str(found))


def case_11_remaining_is_one_sorted_path_per_line(expect):
    source = open(layout.__file__, encoding="utf-8").read()
    block = source.split("REMAINING: tuple[str, ...] = (", 1)[1].split("\n)", 1)[0]
    paths = re.findall(r'^    "([^"]+)",$', block, re.MULTILINE)
    expect("REMAINING lists each path on its own line, in sorted order, with no duplicate",
           (paths == sorted(set(paths)), len(re.findall(r'"', block)) == 2 * len(paths)), (True, True))
    expect.equal("REMAINING is the set the tree holds (the ratchet itself, over the real tree)",
           layout.check_repo(__import__("self_tests").py_handles_flag), [])


if __name__ == "__main__":
    sys.exit(selftest_cases.run(sys.modules[__name__]))
