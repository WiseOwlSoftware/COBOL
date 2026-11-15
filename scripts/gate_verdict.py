#!/usr/bin/env python3
"""gate_verdict.py - THE GATE'S VERDICT VOCABULARY AND ITS ONE READER (kb/Work PB5433, PB4328; docs/rearchitecture/DESIGN-orchestrator-loop.md
section 16.5): GREEN, RED, BUSY and NOT RUN with its causes, a `Reading`, and what a host's and an audit's verdict line says. The
driver (`gate.py`) writes a verdict through these; `enqueue.py`, `land_lane.py` and the await read one through `read_verdict`."""
from __future__ import annotations

import concurrent.futures
import re
import sys
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import NamedTuple

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
from test_population import TRX_NS, Population, trx_root  # noqa: E402
from gate_audits import AUDITS, AUDIT_PREREQUISITES  # noqa: E402


def result_outcomes(path) -> Counter:
    """EVERY RESULT A TRX RECORDS, by outcome (kb/Work PB5205): what a host's own verdict line counts (`Failed: F, Passed:
    P, Skipped: S, Total: T` are results, never definitions: a theory whose rows share a display name is one definition
    with several results). It lives here, beside its one caller (`--rederive`), and reads the file with
    `test_population.trx_root`, so a change to it is no change to what plans the population the compiler tests run. A file
    that cannot be read raises `test_population.UnreadableTrx` naming it."""
    return Counter(r.get("outcome", "") for r in trx_root(path).iter(TRX_NS + "UnitTestResult"))


VERDICT = "=== BUILD-LOCAL GATE: "


#: A gate started while ANOTHER gate holds this worktree's gate lock: nothing about the change was measured and nothing
#: is wrong with it, so it is neither RED nor a NOT RUN that needs a fix — it is BUSY, with its own exit code, and a
#: caller waits for the lock or yields (enqueue.py read it as RED on 2026-10-22 12:30 and stopped PB3511's landing for
#: a finisher with nothing to fix: kb/Work PB3527).
BUSY = "BUSY"


EXIT_BUSY = 3


#: A gate that MEASURED NOTHING about the change prints `NOT RUN [<cause>]`, the cause's TAG naming one row of
#: NOT_RUN_CAUSES, which classifies it ONCE (kb/Work PB3526; review R2-1 of wave 1150). The landing's merge of main into
#: the branch (git_merge.merge: a clean merge, then the merge commit) is
#: a step that can measure nothing too, and its failure names its cause by the same two classes through the `merge-*` rows
#: (kb/Work PB3673): a merge that could not be completed for a cause outside the tree is never reported as a CONFLICT.
#: ENVIRONMENT: a cause outside the code the gated tree carries (the coordination directory's shared gate settings, the host's WSL, Python, .NET SDK or
#: git): no finisher can fix it, so it never blocks the branch; the landing retries a merge it stopped once and a second
#: at the same head is the operator's. BRANCH: a failure of code the gated tree CARRIES (the tree's own build-local,
#: gate.py and linux-gate.sh, or its merged tree's ABI pin): the driver itself, the listing of the population its test
#: projects build, the verdict writer, the arguments its script accepts. That blocks like a RED and goes to a finisher.
#: The landing runs no gate (kb/Work PB4331), so the only NOT RUN it reads is its own merge's. Every printer of a NOT RUN
#: verdict names its cause by tag (the self-test reads build-local.ps1, build-local.sh and linux-gate.sh to hold them to
#: it); a line with no known tag is UNCLASSIFIED and reads as BRANCH, since the gate that printed it is the tree's own code.
NOT_RUN = "NOT RUN"


ENVIRONMENT, BRANCH = "environment", "branch"


@dataclass(frozen=True)
class NotRunCause:
    """One cause of a NOT RUN verdict: its TAG (printed `NOT RUN [<tag>]`), the class it is CARRIED by (ENVIRONMENT or
    BRANCH), and what it is."""
    tag: str
    carried: str
    what: str

    @property
    def verdict(self) -> str:
        """The verdict word a gate prints for this cause."""
        return f"{NOT_RUN} [{self.tag}]"


#: THE ONE CLASSIFICATION of every NOT RUN cause (above; kb/Work PB3526). enqueue.py reads it through read_verdict and
#: never re-derives it.
NOT_RUN_CAUSES: dict[str, NotRunCause] = {c.tag: c for c in (
    NotRunCause("settings", ENVIRONMENT, "the shared gate settings in the coordination directory are malformed (at the "
                                         "start, or while the gate queued)"),
    NotRunCause("interpreter", ENVIRONMENT, "no runnable Python (or one without PyYAML) on the host"),
    NotRunCause("toolchain", ENVIRONMENT, "a tool the gate runs reports a version other than toolchain.env's, or is "
                                          "absent (scripts/toolchain.py check --scope gate, kb/Work PB4007)"),
    NotRunCause("host", ENVIRONMENT, "the host is not Linux and has no WSL to run the Linux gate on"),
    NotRunCause("sdk", ENVIRONMENT, "no .NET SDK in the Linux environment"),
    NotRunCause("disk", ENVIRONMENT, "the Linux filesystem the gate clones onto has too few free inodes or too little "
                                     "free space, after the gate removed every ended gate's leftovers (linux-gate.sh "
                                     "check_disk, kb/Work PB3194)"),
    NotRunCause("tree", ENVIRONMENT, "the tree's git state could not be read or cloned (not at a repository root, no "
                                     "HEAD, no reachable Windows git, a failed clone)"),
    NotRunCause("launch", ENVIRONMENT, "the host refused to make the gate's run directory (an unwritable run root, or "
                                       "its lock's holder hung) or to start its process detached (scripts/detached.py)"),
    NotRunCause("run", ENVIRONMENT, "`--await` was handed no gate run directory (none at that path: a wrong path, or a "
                                    "run pruned since)"),
    NotRunCause("merge-blocked", ENVIRONMENT, "the landing's merge of main (git_merge.merge) could not be "
                                              "completed for a cause outside the merged tree: the ABI-pin settle's "
                                              "measuring build met a build output locked by another process (MSB3027, "
                                              "MSB3021: a language server or an antivirus scan), a full disk or no .NET "
                                              "SDK to start, or git refused a write of the merge (kb/Work PB3673)"),
    NotRunCause("driver", BRANCH, "the gate driver (gate.py, the tree's own) failed"),
    NotRunCause("merge-failed", BRANCH, "the landing's clean merge of main could not be completed because of the "
                                        "merged tree itself: the ABI pin or an owned file the settle writes is "
                                        "malformed, the merged tree does not build, or the measuring test is red or "
                                        "wrote no pin (kb/Work PB3673)"),
    NotRunCause("population", BRANCH, "the population could not be listed after a GREEN build (the tree's test "
                                      "projects: `--list-tests` failed, or not exactly one built test assembly)"),
    NotRunCause("arguments", BRANCH, "the tree's gate script refused the arguments it was given"),
    NotRunCause("verdict", BRANCH, "the gate printed no verdict line, or its process ended with no verdict file "
                                  "(`--await`, the follower: the tree's gate script died first)"),
    NotRunCause("unclassified", BRANCH, "a NOT RUN verdict naming no known cause (the tree's gate printed it)"),
)}


UNCLASSIFIED = NOT_RUN_CAUSES["unclassified"]


_NOT_RUN_TAG = re.compile(r"^NOT RUN \[([a-z-]+)\]")


#: A passing verdict: GREEN, or the `LEG 1 ONLY (...): GREEN` a gate before PB4328 wrote into a record still on disk.
GREEN_VERDICT = re.compile(r"^(?:LEG 1 ONLY \([^)]*\): )?GREEN\b")


@dataclass(frozen=True)
class Reading:
    """What a gate's verdict line says: its WORD (GREEN, BUSY, NOT RUN or RED) and, for NOT RUN only, its CAUSE."""
    word: str
    cause: NotRunCause | None = None

    @property
    def branch_carried(self) -> bool:
        """A NOT RUN of code the gated tree carries (NOT_RUN_CAUSES' BRANCH class)."""
        return self.cause is not None and self.cause.carried == BRANCH


GREEN_READING, BUSY_READING, RED_READING = Reading("GREEN"), Reading(BUSY), Reading("RED")


def not_run(cause: str) -> Reading:
    """The reading of a NOT RUN for the cause tagged `cause`: a caller that MAKES one (enqueue.py's missing verdict line,
    its refused gate rows) names its cause here. KeyError for a tag NOT_RUN_CAUSES lacks."""
    return Reading(NOT_RUN, NOT_RUN_CAUSES[cause])


def read_verdict(text: str) -> Reading:
    """THE ONE READER of a gate's verdict, `text` being its verdict line after the prefix (`=== BUILD-LOCAL GATE: ` or
    a hand-run linux-gate.sh's `=== LINUX GATE: `, the same vocabulary): GREEN (a pass), BUSY (another gate holds the
    worktree's lock: wait, never red), NOT RUN with its cause from NOT_RUN_CAUSES (UNCLASSIFIED when it names none), else RED: every other verdict
    (RED, RED/INCOMPLETE, LEG 1 ONLY: RED, BUILD FAILED) is the change's."""
    if GREEN_VERDICT.match(text):
        return GREEN_READING
    if text == BUSY or text.startswith(BUSY + " "):
        return BUSY_READING
    if text == NOT_RUN or text.startswith(NOT_RUN + " "):
        tag = _NOT_RUN_TAG.match(text)
        return Reading(NOT_RUN, NOT_RUN_CAUSES.get(tag.group(1), UNCLASSIFIED) if tag else UNCLASSIFIED)
    return RED_READING


#: A stopped gate's remainder is named in full in the run directory; the console shows this many per assembly.
NOT_RUN_SHOWN = 10


#: A test host's own verdict line when every case it ran passed (`dotnet test`'s summary, `test_leg_report.verdict_of`).
HOST_PASSED = re.compile(r"Passed!\s+-\s+Failed:\s+0,\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)")


#: A reason an AUDIT gives a verdict (`Host.audits`: `<AUDIT> RED`, `SELF-TESTS HANG`): what an old record that kept no
#: `audit_reds` is read by (`audit_reasons`).
AUDIT_REASON = re.compile(r"(?P<name>.+) (?:RED|HANG)")


def audit_reasons(reasons: list[str]) -> tuple[list[str], list[str]]:
    """(the reasons an AUDIT gave, the others): an audit's reason names one of AUDITS or AUDIT_PREREQUISITES and says RED
    or HANG (`Host.audits`, `Host._prepared`)."""
    names = {n for n, _ in AUDITS} | {n for n, _ in AUDIT_PREREQUISITES}
    audit = [r for r in reasons if (m := AUDIT_REASON.fullmatch(r)) is not None and m["name"] in names]
    return audit, [r for r in reasons if r not in audit]


@dataclass
class HostRun:
    """One assembly's host: its exit code, whether its report was red, its seconds and when it finished."""
    asm: str
    rc: int
    red: bool
    wall_s: float
    finished_s: float  # since the hosts started


@dataclass
class Outcome:
    """What one gate did — the verdict file's content, and what the self-test asserts on."""
    verdict: str = NOT_RUN
    line: str = ""
    exit_code: int = 2
    reasons: list[str] = field(default_factory=list)
    slot: str | None = None
    gate_class: str | None = None  # the class the slot was queued under (gate_slot.GATE_CLASSES, kb/Work PB4888)
    #: The test classes this gate ran first (`Legs.first`): red at the previous run or in the merge queue.
    first: list[str] = field(default_factory=list)
    runs: list[HostRun] = field(default_factory=list)
    populations: dict[str, Population] = field(default_factory=dict)
    timings: dict[str, float] = field(default_factory=dict)
    #: Work running BESIDE the gate's critical path (`Gate._beside`), joined once before the verdict: (name, future).
    beside: list[tuple[str, concurrent.futures.Future]] = field(default_factory=list)
    #: WHAT THE GATE MEASURED (kb/Work PB3536): the commit, the working tree at the start and at the end, the base its
    #: lane was decided against; and, for a reused verdict, the run that measured it.
    head: str | None = None
    tree: str | None = None
    tree_end: str | None = None
    base: str | None = None
    reused_from: str | None = None
    reused_detail: str | None = None  # the measuring gate's own verdict detail, carried along a chain of reuses
    #: The change's lane (scripts/lane_classification.py, kb/Work PB2878): `inert` (no test reads it: the audits, then the
    #: verdict) or `full`; None until the lane is decided.
    lane: str | None = None
    #: Whether the change touches tooling (`Lane.tooling`, kb/Work PB4326): the script self-tests ran, or were left to
    #: CI's merge group; None until the lane is decided.
    tooling: bool | None = None
    #: The RED reasons of the audits, set by the audit work BESIDE the critical path before it stops the hosts (kb/Work
    #: PB4326); empty while they run or when none is red.
    audit_reds: list[str] = field(default_factory=list)
    head_tree: str | None = None
    #: The toolchain the gate ran under (kb/Work PB4007): toolchain.env's versions of the tools it runs, which this
    #: host's tools reported before anything ran (`Host.toolchain`); None until then.
    toolchain: str | None = None
    #: THE RUNS THIS GATE COMPOSES WITH (kb/Work PB5099; `PassCarry.carry`): earlier runs of this worktree at the same code
    #: state whose passes this verdict counts; and, per assembly, how many cases it carried from them. Empty when the
    #: gate measured the whole population itself.
    composed_from: list[str] = field(default_factory=list)
    carried: dict[str, int] = field(default_factory=dict)
    #: EACH TARGET'S KEY at the measured tree, by assembly (kb/Work PB5218; `Host.target_keys`): what the store and an
    #: earlier run's passes are matched on. Set by a full-lane gate; empty in another lane or when git cannot say.
    keys: dict[str, str] = field(default_factory=dict)
    #: The targets the store showed UP TO DATE, by assembly: the pass that stands (its worktree, run, counts, evidence).
    up_to_date: dict[str, dict] = field(default_factory=dict)
    #: The assemblies whose pass THIS gate recorded in the store.
    recorded: list[str] = field(default_factory=list)
    #: EACH GENERATED TARGET this gate brought up to date (kb/Work PB5231; `Host.generate`), by name: its key, its output
    #: digest, its output directory, whether it ran and why (`buildgraph.Generation.record`).
    generated: dict[str, dict] = field(default_factory=dict)


class Ending(NamedTuple):
    """A verdict as a value: the verdict word, its detail line and its exit code. A collaborator of the gate that reaches its own
    end returns one, and `Gate._finish` is the one place a verdict line and record are written (kb/Work PB5411)."""
    verdict: str
    detail: str
    code: int
