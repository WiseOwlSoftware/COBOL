#!/usr/bin/env python3
"""verdict_line.py — THE ONE HOME OF THE LANDING'S VERDICT LINE (kb/Work PB3714).

    python scripts/verdict_line.py --self-test

Every `enqueue.py land` ends on ONE line, `=== ENQUEUE: <OUTCOME> (<branch>) ===` (MANDATORY-PRACTICES P2: an agent
blocks on it), and three programs touch that line: `enqueue.py` WRITES it, the landing lane (`land_lane.py`) READS its
outcome out of a run's log to classify the attempt, and `merge_drivers.py` reads the files a CONFLICT outcome names out
of the landing logs. Until PB3714 the prefix was a literal in each of the three and the line's grammar a regex in the
second, so a change to the line's shape (a field, a terminator) had to be made in all three and no test failed when one
was missed: a writer and a reader could drift apart unseen.

This module is a LEAF (of the repository it imports only `standard_streams`, itself a leaf, kb/Work PB3637), so `merge_drivers.py` (which `enqueue.py` imports) can use
it without importing the landing script. It owns:
  * the line: `PREFIX`, `render(outcome, branch)` (the one writer) and `parse(line)` / `outcomes(text)` (the readers);
  * the CONFLICT outcome inside it: `conflict_outcome(files)` (the one writer: `enqueue.py land`'s exit 4 and
    `merge_drivers.py merge`) and `outcome_files(outcome)` / `conflict_files(text)` (the readers: the lane's conflict
    brief, the drivers' coverage report).
`LandingLaneDriftTests` holds the rule: no other file under scripts/ spells the prefix.
"""
from __future__ import annotations

import re
import sys
from typing import NamedTuple

from standard_streams import ensure_utf8

#: The line's prefix and terminator; `render` and `LINE` are the only places that spell them.
PREFIX = "=== ENQUEUE: "
_TERMINATOR = " ==="
LINE = re.compile(r"^" + re.escape(PREFIX) + r"(?P<outcome>.*?)(?: \((?P<branch>[^()]*)\))?" + re.escape(_TERMINATOR) + r"\s*$")
#: The outcome of a merge of main that conflicted and was aborted (`conflict_outcome`).
CONFLICT_OUTCOME = re.compile(r"^CONFLICT in (?P<files>.+?) — merge aborted")


class Verdict(NamedTuple):
    """A parsed verdict line: the outcome text and the branch it names (None when the line names none)."""
    outcome: str
    branch: str | None


def render(outcome: str, branch: str) -> str:
    """The verdict line of a landing of `branch` that ended `outcome`: the one writer."""
    return f"{PREFIX}{outcome} ({branch}){_TERMINATOR}"


def parse(line: str) -> Verdict | None:
    """The outcome and branch of one verdict line, or None when `line` is not one."""
    m = LINE.match(line.strip())
    return Verdict(m["outcome"].strip(), m["branch"]) if m else None


def outcomes(text: str) -> list[str]:
    """The outcome of every verdict line in `text` (a landing's log), in order."""
    return [v.outcome for line in text.splitlines() if (v := parse(line))]


def conflict_outcome(files: list[str]) -> str:
    """The outcome of a merge of main that conflicted and was aborted: `CONFLICT in <a>, <b> — merge aborted, branch
    unchanged`."""
    return f"CONFLICT in {', '.join(files)} — merge aborted, branch unchanged"


def outcome_files(outcome: str | None) -> list[str]:
    """The files a CONFLICT outcome (`conflict_outcome`) names, or [] for any other outcome."""
    m = CONFLICT_OUTCOME.match(outcome or "")
    return [f.strip() for f in m["files"].split(",") if f.strip()] if m else []


def conflict_files(text: str) -> list[str]:
    """The files the CONFLICT verdict lines of a landing log `text` name, in order."""
    return [f for o in outcomes(text) for f in outcome_files(o)]


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    expect("the line is the prefix, the outcome, the branch in parentheses and the terminator",
           render("ENQUEUED", "fix") == "=== ENQUEUE: ENQUEUED (fix) ===", render("ENQUEUED", "fix"))
    expect("the reader reads back what the writer wrote, outcome and branch",
           parse(render("GATE RED (Windows) on x", "w9a")) == Verdict("GATE RED (Windows) on x", "w9a"),
           repr(parse(render("GATE RED (Windows) on x", "w9a"))))
    expect("a line without a branch parses with branch None, and surrounding whitespace is ignored",
           parse("  === ENQUEUE: NO ENTRY ===  ") == Verdict("NO ENTRY", None), repr(parse("  === ENQUEUE: NO ENTRY ===  ")))
    expect("a line that is not a verdict line parses to None (prose naming the prefix, a missing terminator, no prefix)",
           parse("see the === ENQUEUE: line") is None and parse("=== ENQUEUE: ENQUEUED (fix)") is None
           and parse("ENQUEUED pr=1") is None and parse("") is None)
    log = "\n".join(["land lane: attempt 1", render("GATE BUSY", "b"), "noise", render("ENQUEUED", "b"), ""])
    expect("outcomes: every verdict line of a log in order, the last one being the landing's result",
           outcomes(log) == ["GATE BUSY", "ENQUEUED"], repr(outcomes(log)))
    expect("the CONFLICT outcome reads back its own files; any other outcome, and None, names none",
           outcome_files(conflict_outcome(["scripts/x.py", "kb/a.json"])) == ["scripts/x.py", "kb/a.json"]
           and outcome_files("GATE RED (Windows) on x") == [] and outcome_files(None) == [])
    clog = render(conflict_outcome(["tests/version-matrix/constructs.json", "src/A.cs"]), "b") + "\n" + render("ENQUEUED", "b")
    expect("conflict_files: the files the CONFLICT lines of a log name, through the same line parser",
           conflict_files(clog) == ["tests/version-matrix/constructs.json", "src/A.cs"], repr(conflict_files(clog)))
    print(f"=== VERDICT LINE SELF-TEST: {'RED' if failures else 'GREEN'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(self_test() if sys.argv[1:] == ["--self-test"] else 2)
