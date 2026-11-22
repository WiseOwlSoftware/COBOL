#!/usr/bin/env python3
"""The suite status line on planted terms (DESIGN-conformance-suite.md section 8 Stage 0, kb/Work PB6047): each of the
five terms planted red on its own, the term whose tool does not exist, a tool that cannot answer and a malformed ledger."""
import importlib
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/suite_status.py
ZERO = {"non-support": 0, "suite": 0, "determination": 0, "derivation": 0, "accepted-uncleared": 0, "refusal-only": 0,
        "other-edition-only": 0, "header-omits": 0, "shared-test": 0, "no-test": 0, "rows": 0, "observable": 0, "unmet": 0}


class World:
    """A planted tree: the measurement of tests a and b (b failing with `cause`), a ledger, and stub tools on sys.path."""

    def __init__(self, cause="non-portable-test", ledger=None):
        self.root = scratch_dir("status-")
        (self.root / "harness").mkdir()
        (self.root / "measurements").mkdir()
        (self.root / "measurements" / f"{T.SECOND_COMPILER}.json").write_text(json.dumps({
            "compiler": T.SECOND_COMPILER, "editions": {"85": {
                "verdicts": {"a": "PASS", "b": "FAIL"},
                "failures": {"b": {"bucket": "output: x", "cause": cause}}}}}), encoding="utf-8", newline="\n")
        self.ledger = self.root / "LEDGER.json"
        self.ledger.write_text(json.dumps(ledger if ledger is not None else
                                          {"book_pages": 3, "second_pass": {"pages_swept": [1, 2, 3], "findings": 0}}),
                               encoding="utf-8", newline="\n")
        sys.dont_write_bytecode = True  # a stub rewritten within a second at one size must not be read from a stale .pyc
        sys.path.insert(0, str(self.root / "harness"))

    def tool(self, name, body):
        sys.modules.pop(name, None)
        (self.root / "harness" / f"{name}.py").write_text(body, encoding="utf-8", newline="\n")
        importlib.invalidate_caches()

    def terms(self, rows=None, population=("a", "b"), ledger=True):
        return T.status("85", rows=lambda: rows or ZERO, population=lambda: set(population), root=self.root,
                        ledger=self.ledger if ledger else None)[0]

    def close(self):
        sys.path.remove(str(self.root / "harness"))
        for name in ("coverage", "crosswalk"):
            sys.modules.pop(name, None)


def _world(expect, **kw):
    w = World(**kw)
    w.tool("coverage", "def unmet(edition):\n    return 0\n")
    w.tool("crosswalk", "def unmet(edition):\n    return 0\n")
    return w


def case_01_every_term_zero_is_complete(expect):
    w = _world(expect)
    try:
        expect.equal("the line", T.verdict_line("85", w.terms()), "=== SUITE 85: COMPLETE ===")
    finally:
        w.close()


def case_02_the_rows_term_planted_red(expect):
    w = _world(expect)
    try:
        line = T.verdict_line("85", w.terms(rows=ZERO | {"unmet": 4, "observable": 9}))
        expect("rows red", line.startswith("=== SUITE 85: INCOMPLETE — rows 4 ·"), line)
    finally:
        w.close()


def case_03_the_second_compiler_term_planted_red(expect):
    w = _world(expect)
    try:
        expect.equal("an unrecorded test", w.terms(population=("a", "b", "c"))["second compiler"], 1)
    finally:
        w.close()
    w = _world(expect, cause="unclassified")
    try:
        expect.equal("a failure with no cause", w.terms()["second compiler"], 1)
    finally:
        w.close()


def case_04_the_transcription_term_planted_red(expect):
    w = _world(expect, ledger={"book_pages": 3, "second_pass": {"pages_swept": [1, 2], "findings": 2}})
    try:
        expect.equal("findings and an unswept page", w.terms()["transcription"], 3)
    finally:
        w.close()


def case_05_the_points_and_nist_terms_planted_red(expect):
    w = _world(expect)
    try:
        w.tool("coverage", "def unmet(edition):\n    return 5\n")
        w.tool("crosswalk", "def unmet(edition):\n    return 2\n")
        t = w.terms()
        expect.equal("points red", t["points"], 5)
        expect.equal("NIST red", t["NIST"], 2)
        w.tool("crosswalk", "x = 1\n")
        try:
            w.terms()
            expect("a tool that cannot answer fails loudly", False, "no error")
        except T.StatusError:
            expect("a tool that cannot answer fails loudly", True)
    finally:
        w.close()


def case_06_a_term_whose_tool_does_not_exist_is_unmeasured_never_zero(expect):
    w = World()
    try:
        t = w.terms(ledger=False)
        expect("points, NIST and the transcription print UNMEASURED",
               (t["points"], t["NIST"], t["transcription"]) == (T.UNMEASURED,) * 3, str(t))
        line = T.verdict_line("85", {"rows": 0, "points": T.UNMEASURED, "NIST": 0, "second compiler": 0, "transcription": 0})
        expect("an unmeasured term keeps the line INCOMPLETE even when every number is zero",
               line.startswith("=== SUITE 85: INCOMPLETE"), line)
    finally:
        w.close()


def case_07_a_ledger_with_no_second_pass_is_unmeasured_and_a_malformed_one_fails_loudly(expect):
    w = World(ledger={"book_pages": 3})
    try:
        expect.equal("no second pass", T.transcription_term(w.ledger), T.UNMEASURED)
        w.ledger.write_text(json.dumps({"book_pages": 3, "second_pass": {"findings": "none"}}), encoding="utf-8", newline="\n")
        try:
            T.transcription_term(w.ledger)
            expect("a malformed second pass fails loudly", False, "no error")
        except T.StatusError:
            expect("a malformed second pass fails loudly", True)
    finally:
        w.close()


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
