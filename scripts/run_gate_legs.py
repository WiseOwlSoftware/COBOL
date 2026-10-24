#!/usr/bin/env python3
"""run_gate_legs.py — THE GATE DRIVER: the ordered gate (kb/Work PB1708, PB1721, PB3022;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.1, §3.14.3–3.14.4, §3.14.6, §3.17.3).

    python scripts/run_gate_legs.py [--base <sha>]          # the gate, DETACHED, its output followed to the verdict
    python scripts/run_gate_legs.py --await <run> [--worktree W] [--wait-s N]  # a gate's verdict, from its run dir
    python scripts/run_gate_legs.py --self-test
    (python scripts/run_gate_legs.py --run <run> [--base <sha>] is the detached gate itself; never run it by hand)

Called by `scripts/build-local.ps1` and `scripts/build-local.sh`, which only set the process priority. Ordering changes
WHEN a case runs, never WHETHER (owner, 2026-09-28: "order, don't skip").

⛔ ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; §3.17.3): there is no mode. Every gate — an
implementer's, a re-gate after a fix, a landing's — makes the order plan (scripts/gate_plan.py), holds a GATE SLOT
(scripts/gate_slot.py) from before the build to the verdict, so at most N gates build or test at once,
repository-wide, runs its audits BEFORE it queues for the slot (kb/Work PB2524), and runs leg 1 FAIL-FAST. Leg 1 is
the change's own tests and goldens and the previous gates' reds, either host's (tier 0a, kb/Work PB3155), the
host-only witnesses (tier 0h: the Windows file-system race witnesses CI's NTFS runners may not reproduce), the cheap
tree-wide sweeps (tier 0c), the drift tests the drift-rule index says govern a changed file or a register citing
what the change removes (tier 0d, kb/Work PB3531, PB3131) and the cheapest cases the change reaches; so a re-gate
after a fix runs leg 1 again — seconds for a bookkeeping fix, never a population run. The local whole-population
run is GONE (it was the lander's: 7-53 min, 8 of 15 RED under load on
2026-10-10); CI's merge-group run is the whole-population regression net.
Its SCOPE is a shared gate setting (gate_slot.py, read once at the start): `leg1` (the coded default, batched gating,
kb/Work PB2515) runs leg 1 only and names every leg-2 case NOT RUN, and its verdict line says
`LEG 1 ONLY (batched gating, PB2515)` so it is never read as a whole-population GREEN; `whole`
(`gate_slot.py set-implementer-scope whole`, the owner's on-demand switch and the battery's form) runs both legs,
stopping RED/INCOMPLETE at a red in leg 1 with the remainder named.

ONE GATE, in this order (§3.14.3):
  1. the worktree's GATE LOCK (`<worktree git dir>/cobol-gate.lock`): a second gate in the same worktree is REFUSED at
     once, naming the holder — its build would overwrite the binaries between this gate's legs — with the verdict
     BUSY (exit 3), never RED: it measured nothing (kb/Work PB3527);
  2. a fresh RUN DIRECTORY `TestResults/build-local/<n>-<UTC stamp>Z/` (n = the run's number in its run root, taken under
     `runs.lock`, so start order never depends on the clock) holding the listings, the plan, every trx,
     log and identity record, and the verdict file — nothing at a fixed name, so no earlier gate's file is read as
     this one's; the newest earlier run holding a verdict gives the plan its timings and reds, and the worktree's
     newest Linux gate run (TestResults/linux-gate/, gate_plan.previous_gates) its Linux reds; five are kept;
  3. the audits (the one list below, run CONCURRENTLY; one of them is every script self-test, found by
     scripts/self_tests.py — kb/Work PB2563), FAIL-FAST: a red audit ends the gate RED right there, with no leg run
     (kb/Work PB2523) — then the GnuCOBOL corpus, by the ONE rule scripts/external_corpus.py (kb/Work PB2611). Both
     run BEFORE the slot (kb/Work PB2524): they read only the tree, and inside the slot they were 35-45 % of its hold
     while every other gate queued;
  4. the gate slot, always after the lock, so no two gates can wait on each other in a cycle; then
     the solution build and the SHA-256 of every binary the legs will run;
  5. `--list-tests` per assembly (scrubbed), then BESIDE the legs the self-tests that read the built assemblies
     (`self_tests.py --built`, joined after the legs; a red one makes the gate RED), the plan, then per leg the three
     assemblies CONCURRENTLY, each through the
     leg handshake (all three COBOLNET_GATE_* variables, or none) and printed through scripts/test_leg_report.py;
     an assembly the plan gives no case in a leg is not invoked for it;
  6. the POPULATION CHECK (scripts/test_population.py) per assembly over its legs' trx files, the publish of every
     measured duration to the SHARED TIMINGS STORE that times the next gate's plan, even in a fresh worktree
     (`<git common dir>/cobol-gate-timings/`, gate_plan.py, kb/Work PB2527), the IDENTITY check (each
     leg host's record names this plan's digest and the binaries step 3 hashed), then the verdict line:
     `=== BUILD-LOCAL GATE: GREEN — Conformance 9,317/9,317 · Unit … cases ran (skipped 0) in 2 legs · … ===`.
It is GREEN only when every leg ran, every leg is green, every population is exact and every identity matches. Under
the `leg1` scope the verdict is `LEG 1 ONLY (batched gating, PB2515): GREEN` or `…: RED` instead, whenever the
plan had a leg 2 it did not run (a gate with no plan runs its one leg — the whole population — and says plain GREEN).

⛔ A GATE OUTLIVES ITS CALLER, AND A MEASURED TREE IS GATED ONCE (kb/Work PB3536; DESIGN-test-build-ci.md §3.14.12).
  * DETACHED: `build-local` makes the run directory, starts the gate itself (`--run <run>`) through scripts/detached.py
    (the one launch that outlives its caller's tree kill) with its output in `<run>/gate.log` and its pid in
    `<run>/gate.pid`, and FOLLOWS it: the log is echoed as it grows and the caller exits with the gate's own exit code
    once `<run>/verdict.json` is written. Killing the caller (a workflow agent's return kills the process tree of every
    background command it started: wave 1150 group A's finisher gate died mid-audit with no verdict) leaves the gate
    running to its verdict, and `--await <run>` reads it: the verdict line and exit code once the verdict file is there,
    `NOT RUN [verdict]` (exit 2) when the gate's process DIED without one, `PENDING` (exit 4) when `--wait-s` passes
    first. The rolling wave awaits a gate an agent hands it this way (status GATE-PENDING), never the log's tail. To stop a gate,
    end the process `gate.pid` names.
  * REUSED: every verdict file records the HEAD and the TREE the gate measured (the working tree's content, uncommitted
    and untracked files included, ignored ones not, and coord.NEVER_WORK's paths at HEAD's content, never the harness's
    edit of them: `Host.measured_tree`) at its start and again at its end, the base it
    planned against and its exit code. A gate whose tree and base equal those of an earlier GREEN run in this worktree
    whose tree did not change while it ran, under a scope that run covered (a `LEG 1 ONLY` GREEN covers the scope
    `leg1`; a plain GREEN covers both), answers from that record WITHOUT running anything, and says so in its verdict
    line (`… GREEN — REUSED: …`, naming the run that measured it) — the self-test reuse rule (kb/Work PB2914, PB3022)
    extended to the gate. A RED is never reused, and a landing's gate on a merged head (another tree) always runs.

Exit codes: 0 GREEN (or LEG 1 ONLY: GREEN, reused or run) · 1 RED (or RED/INCOMPLETE, LEG 1 ONLY: RED, or the build
failed) · 2 NOT RUN [<cause>] (the shared gate settings are malformed: ENVIRONMENT; the population could not be
listed, the driver failed, the gate's process DIED with no verdict (`--await`, or the follower): BRANCH, the tree's
own code; NOT_RUN_CAUSES classifies every cause once) · 3 BUSY (another gate holds this worktree's gate lock: nothing
ran and nothing is red; wait for it and re-run, or `--await` its run, kb/Work PB3527) · 4 PENDING (`--await`: no
verdict within `--wait-s`). `read_verdict` is the ONE reader of a verdict line's text (GREEN, BUSY, NOT RUN with its
cause, or RED); enqueue.py's landing gate reads through it, and so does `--await` for a record's exit code.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import tempfile
import threading
import time
import traceback
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
import external_corpus  # noqa: E402
import gate_plan  # noqa: E402
import impacted_tests  # noqa: E402
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT  # noqa: E402
from gate_slot import _pid_alive  # noqa: E402  — the first-red arm checks that a stopped audit's tree is gone
from gate_slot import (DEFAULT_SCOPE, ExclusiveLock, GateSettings, GateSlots, Setting, Slot,  # noqa: E402
                       git_common_dir, slot_dir)
from detached import spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536)
sys.path.append(str(REPO / "scripts" / "orchestrator"))  # appended: no module of it shadows one of scripts/
from coord import NEVER_WORK  # noqa: E402  — THE paths that are never work, so never part of a measured tree
from test_population import (GATED_ASSEMBLIES, HANDSHAKE, Population, PopulationError, check as check_population,  # noqa: E402
                             list_population, scrubbed_env, synthetic_listing, synthetic_trx)

VERDICT = "=== BUILD-LOCAL GATE: "
#: The verdict prefix of a gate that ran leg 1 only under the `leg1` scope (kb/Work PB2515): never a whole-population
#: GREEN, and never mistaken for one — `GATE: GREEN` does not match it.
LEG1_ONLY = "LEG 1 ONLY (batched gating, PB2515)"
#: Who runs the leg 2 a `leg1` gate leaves: the whole-population regression net (kb/Work PB3015, PB3022, §3.17.2).
LEG2_OWNER = "CI's whole-population run"
RUN_ROOT = Path("TestResults") / "build-local"
#: A run directory's name: `<n>-<UTC start, to the second>Z`, where n is the run's NUMBER in its run root, one more
#: than every run there, taken under the run root's lock (`Gate.new_run`). THE ORDER OF RUNS IS THE ORDER OF THEIR
#: NUMBERS (`run_order`), and "the newest earlier run" reads it: the plan's reds and timings, the reuse rule, the
#: pruning. Never the clock's: a stamp orders nothing finer than the clock's resolution, and on a CI runner three runs
#: started in the same MICROSECOND, so a stamp-and-nonce name sorted by its random nonce (kb/Work PB3536, merge-group
#: run 39888287569). A run named before the numbers (`<stamp>Z-<nonce>`) is older than every numbered run, and is still
#: read and pruned.
RUN_DIR = re.compile(r"(?P<n>\d{6,})-\d{8}T\d{6}Z|\d{8}T\d{6}Z-[0-9a-f]{6}")
RUNS_LOCK = "runs.lock"
RUNS_LOCK_WAIT_S = 60.0  # the lock is held for one directory listing and one mkdir; a minute means a hung holder
KEEP_RUNS = 5
LOCK_NAME = "cobol-gate.lock"
VERDICT_FILE = "verdict.json"
#: A gate started while ANOTHER gate holds this worktree's gate lock: nothing about the change was measured and nothing
#: is wrong with it, so it is neither RED nor a NOT RUN that needs a fix — it is BUSY, with its own exit code, and a
#: caller waits for the lock or yields (enqueue.py read it as RED on 2026-10-22 12:30 and stopped PB3511's landing for
#: a finisher with nothing to fix: kb/Work PB3527).
BUSY = "BUSY"
EXIT_BUSY = 3
#: A gate that MEASURED NOTHING about the change prints `NOT RUN [<cause>]`, the cause's TAG naming one row of
#: NOT_RUN_CAUSES, which classifies it ONCE (kb/Work PB3526; review R2-1 of wave 1150). ENVIRONMENT: a cause outside the
#: code the gated tree carries (the coordination directory's shared gate settings, the host's WSL, Python, .NET SDK or
#: git): no finisher can fix it, so it never blocks the branch; the landing retries it once and a second at the same head
#: is the operator's. BRANCH: a failure of code the gated tree CARRIES (a landing gates the branch merged with main, and
#: runs that tree's own build-local, run_gate_legs.py and linux-gate.sh): the driver itself, the listing of the
#: population its test projects build, the verdict writer, the gate rows' slot driver, the arguments its script accepts.
#: That blocks like a RED and goes to a finisher, UNLESS main fails the same way at the same step, which enqueue.py
#: measures (a defect main carries is not the branch's). Every printer of a NOT RUN verdict names its cause by tag
#: (the self-test reads build-local.ps1, build-local.sh and linux-gate.sh to hold them to it); a line with no known tag
#: is UNCLASSIFIED and reads as BRANCH, since the gate that printed it is the tree's own code.
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
    NotRunCause("host", ENVIRONMENT, "the host is not Linux and has no WSL to run the Linux gate on"),
    NotRunCause("sdk", ENVIRONMENT, "no .NET SDK in the Linux environment"),
    NotRunCause("tree", ENVIRONMENT, "the tree's git state could not be read or cloned (not at a repository root, no "
                                     "HEAD, no reachable Windows git, a failed clone)"),
    NotRunCause("launch", ENVIRONMENT, "the host refused to make the gate's run directory (an unwritable run root, or "
                                       "its lock's holder hung) or to start its process detached (scripts/detached.py)"),
    NotRunCause("run", ENVIRONMENT, "`--await` was handed no gate run directory (none at that path: a wrong path, or a "
                                    "run pruned since)"),
    NotRunCause("driver", BRANCH, "the gate driver (run_gate_legs.py, the tree's own) failed"),
    NotRunCause("population", BRANCH, "the population could not be listed after a GREEN build (the tree's test "
                                      "projects: `--list-tests` failed, or not exactly one built test assembly)"),
    NotRunCause("arguments", BRANCH, "the tree's gate script refused the arguments it was given"),
    NotRunCause("verdict", BRANCH, "the gate printed no verdict line, or its process ended with no verdict file "
                                  "(`--await`, the follower: the tree's gate script died first)"),
    NotRunCause("unslotted", BRANCH, "a gate row would run outside the shared gate slot (the tree's gate driver no "
                                     "longer takes one, kb/Work PB3104)"),
    NotRunCause("unclassified", BRANCH, "a NOT RUN verdict naming no known cause (the tree's gate printed it)"),
)}
UNCLASSIFIED = NOT_RUN_CAUSES["unclassified"]
_NOT_RUN_TAG = re.compile(r"^NOT RUN \[([a-z-]+)\]")
#: A passing verdict: plain GREEN (every leg ran) or `LEG 1 ONLY (...): GREEN` (batched gating).
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
    the Linux gate's `=== LINUX GATE: `): GREEN (a pass), BUSY (another gate holds the worktree's lock: wait, never
    red), NOT RUN with its cause from NOT_RUN_CAUSES (UNCLASSIFIED when it names none), else RED: every other verdict
    (RED, RED/INCOMPLETE, LEG 1 ONLY: RED, BUILD FAILED) is the change's."""
    if GREEN_VERDICT.match(text):
        return GREEN_READING
    if text == BUSY or text.startswith(BUSY + " "):
        return BUSY_READING
    if text == NOT_RUN or text.startswith(NOT_RUN + " "):
        tag = _NOT_RUN_TAG.match(text)
        return Reading(NOT_RUN, NOT_RUN_CAUSES.get(tag.group(1), UNCLASSIFIED) if tag else UNCLASSIFIED)
    return RED_READING


#: A run directory's other fixed names (kb/Work PB3536): the gate process's pid, and its output when it runs detached.
PID_FILE = "gate.pid"
LOG_FILE = "gate.log"
#: `--await`'s answer while the gate still runs, and its default window (under a tool call's 10-minute limit).
AWAIT_PENDING = "=== GATE AWAIT: PENDING"
AWAIT_PENDING_EXIT = 4
AWAIT_WAIT_S = 540
#: The verdict record's schema: 3 adds the measured head, tree (start and end), base, exit code and reuse (PB3536).
RECORD_SCHEMA = 3
#: A stopped gate's remainder is named in full in the run directory; the console shows this many per assembly.
NOT_RUN_SHOWN = 10

#: ⛔ THE AUDITS, in order — THE list for the gate (build-local.ps1 and .sh used to carry a copy each); battery.sh PHASE
#: -1 and CI's `audits` job run the same scripts. A wrong § is the one defect no test can catch, and each costs about a
#: second. None needs a build or a test result, so they run FIRST: a gate stops on a red one before it builds
#: (kb/Work PB2523 — four of wave 1034's eight first-run reds were audit-only, each found after a whole ~6-min
#: population). And they run OUTSIDE the gate slot (kb/Work PB2524): the slot rations builds and test legs, not
#: reading the tree.
#: They run CONCURRENTLY, each its own process with its output captured and printed whole (kb/Work PB2563), so the
#: phase costs its slowest audit, not their sum.
#: ⛔ AND THE FIRST RED STOPS THE REST (kb/Work PB3061): the moment one audit exits non-zero, every audit still running
#: is stopped — its whole process tree, so the self-test runner's children go with it — and reported STOPPED, never
#: RED. Wave 1062's agents A and B each had CITATIONS RED at 23 s and their verdict 5–7 min later, after the
#: self-tests. A stopped self-test run loses nothing: every self-test that finished has recorded its PASS, which the
#: re-gate reuses; the rest run then.
#: ⛔ SELF-TESTS is EVERY script self-test, found by discovery (scripts/self_tests.py; kb/Work PB2563) — never a self-test
#: named here by hand: this tuple once named five while eleven orchestrator self-tests ran in no gate at all. The few
#: that read the BUILT assemblies run after the build (`self_tests.py --built`, `Host.built_self_tests`).
AUDITS = (
    ("CITATIONS", ["scripts/spec/audit_code_citations.py", "--check"]),
    ("DOC CITATIONS", ["scripts/spec/audit_doc_citations.py", "--check"]),
    ("EVIDENCE SUPERSESSION", ["scripts/spec/audit_evidence_supersession.py", "--check"]),
    ("RENDERED VIEWS", ["scripts/rendered_views.py", "--check"]),
    ("WITNESS LOSS", ["scripts/spec/audit_witness_loss.py", "--check"]),
    ("RULE CATALOG", ["scripts/spec/extract_rule_catalog.py", "--check"]),
    ("SPEC CORRECTIONS", ["scripts/spec/verify_publishable.py"]),
    ("LINE ENDINGS", ["scripts/line_endings.py", "--check"]),
    ("SELF-TESTS", ["scripts/self_tests.py"]),
    # kb/Work PB1957 row 44, the CI invariant: CI's `audits` job ran these two and the gate did not, so train 1067's
    # PR 9 went RED in CI on a `blocked: true` the work register refuses, behind a GREEN gate.
    ("WORK REGISTER", ["scripts/spec/work.py", "check"]),
    ("FLEET PRACTICES", [".claude/skills/workstream/check_practices.py"]),
    # DESIGN-conformance-suite.md §9 (kb/Work PB3174): the conformance suite's generator refuses a malformed test
    # (its header, its rules, its control, its expected outputs, the portability lint) reading only the tree, so a
    # refused test is red here in a fraction of a second, before the slot and the build.
    ("CONFORMANCE SUITE", ["conformance-suite/harness/generate.py", "--check"]),
)
#: ⛔ WHAT AN AUDIT NEEDS IN THE TREE, made ready before any audit runs, exactly as CI's `audits` job makes it ready:
#: FLEET PRACTICES reads the pinned public skills submodule, which a fresh worktree does not have. A prerequisite that
#: fails is a RED and no audit runs. `SelfTestDiscoveryDriftTests` maps every command of CI's `audits` job to an AUDITS
#: entry, to one of these, or to its recorded reason for running elsewhere, so the next audit CI gains reaches the gate.
AUDIT_PREREQUISITES = (
    ("PUBLIC SKILLS SUBMODULE", ["git", "submodule", "update", "--init", "tools/claude-skills"]),
)
#: ⛔ EVERY GATE REUSES A SELF-TEST'S RECORDED PASS (kb/Work PB2914, generalized to the one gate by PB3022;
#: DESIGN-test-build-ci.md §3.14.10, §3.17.3): the gate's audits add this to SELF-TESTS, so a self-test whose inputs
#: are unchanged since a recorded PASS is REUSED, not run, and one that does run records its PASS for the next gate.
#: CI's `audits` job, the Linux gate and a hand run never pass it: they run every self-test.
SELF_TESTS_REUSE = "--reuse"
#: The post-build half of the self-tests: the ones declaring `SELF-TEST-NEEDS: build` (scripts/self_tests.py).
BUILT_SELF_TESTS = ["scripts/self_tests.py", "--built"]
#: ⛔ THE WORK THAT RUNS BESIDE THE GATE'S CRITICAL PATH (`Gate._beside`, kb/Work PB2880), each joined once before the
#: verdict: the post-build self-tests, which read the built assemblies, beside the legs. A step that needs nothing the
#: critical path is still making belongs here, not in a serial valley ahead of it. (The audits stay AHEAD of the slot
#: and the build on purpose: they fail fast, kb/Work PB2523.)
BESIDE = ("post_build_self_tests",)

Spawn = Callable[..., dict]


def _no_slot(**kwargs) -> dict:
    return kwargs


def _stop_tree(p: subprocess.Popen) -> None:
    """Stop `p` and every process it started (kb/Work PB3061): on Windows `taskkill /T /F` walks the tree by parent
    id; on POSIX the process group `start_new_session` gave it. A process already gone is no error."""
    if p.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(p.pid), "/T", "/F"], capture_output=True)
    else:
        try:
            os.killpg(p.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass


class _Live:
    """The audit processes of one `Host.audits()` call (kb/Work PB3061), so the first RED can stop the rest. `add` under
    the lock stops a process that starts after `stop_all` ran, so no audit slips through the gap between its spawn and
    its registration; `stopped` holds the pids stopped, so their exits are reported STOPPED, never RED."""

    def __init__(self) -> None:
        self.lock = threading.Lock()
        self.stopping = threading.Event()
        self.procs: set[subprocess.Popen] = set()
        self.stopped: set[int] = set()

    def add(self, p: subprocess.Popen) -> None:
        with self.lock:
            self.procs.add(p)
            if self.stopping.is_set():
                self.stopped.add(p.pid)
                _stop_tree(p)

    def discard(self, p: subprocess.Popen) -> None:
        with self.lock:
            self.procs.discard(p)

    def stop_all(self) -> int:
        """Stop every registered process still running, and every one registered from now on; returns how many."""
        with self.lock:
            self.stopping.set()
            running = [p for p in self.procs if p.poll() is None]
            for p in running:
                self.stopped.add(p.pid)
                _stop_tree(p)
            return len(running)


# ── the host: everything the driver does to the machine (the self-test plants a fake one) ────────────────────────


class Host:
    """The real machine: git, the build, the listings, the test hosts."""

    def __init__(self, repo: Path = REPO):
        self.repo = repo

    def git(self, *args: str) -> str:
        return subprocess.run(["git", *args], cwd=self.repo, check=True, capture_output=True, text=True,
                              encoding="utf-8").stdout.strip()

    def lock_path(self) -> Path:
        """`<worktree git dir>/cobol-gate.lock` — per WORKTREE (a linked worktree's git dir is its own)."""
        return Path(self.git("rev-parse", "--absolute-git-dir")) / LOCK_NAME

    def take_slot(self, label: str, say: Callable[[str], None]) -> Slot:
        return GateSlots.for_repo(self.repo).acquire(label, say=say)

    def implementer_scope(self) -> tuple[str, Setting | None]:
        """The shared implementer scope (gate_slot.py; kb/Work PB2515): `leg1` or `whole`, and the live setting that
        chose it (None: no live setting, so the coded default gate_slot.DEFAULT_SCOPE). Malformed settings raise
        ValueError: the gate is NOT RUN, with the reason."""
        return GateSettings.read(slot_dir(self.repo)).effective_scope()

    def default_base(self) -> str:
        return self.git("merge-base", "HEAD", "origin/main")

    def head(self) -> str | None:
        try:
            return self.git("rev-parse", "HEAD")
        except (subprocess.CalledProcessError, OSError):
            return None

    def measured_tree(self) -> str | None:
        """THE TREE A GATE MEASURES (kb/Work PB3536): the git tree id of the WORKING TREE — every tracked file as it is on
        disk, every untracked file git does not ignore, no ignored one — written through a private copy of the index,
        so the worktree's own index is never touched. A path coord.NEVER_WORK names is never work (the harness edits a
        TRACKED `.claude/settings.local.json` in every agent's worktree, and an agent may not commit it): it measures
        at HEAD's content whatever is on disk or staged, so a worktree whose only dirt is the harness's measures as its
        commit's own tree, the tree a GATE-PENDING report's HEAD is checked against (plan_wave.Report.gate_state;
        review R1-2 of wave 1152). None when git cannot say (then nothing is reused)."""
        try:
            index = Path(self.git("rev-parse", "--git-path", "index"))
            index = index if index.is_absolute() else self.repo / index
            with tempfile.TemporaryDirectory(prefix="gate-tree-") as tmp:
                private = Path(tmp) / "index"
                if index.exists():
                    # copy2, never copyfile: the copy keeps the index file's own mtime, which is git's RACY-GIT
                    # reference. An entry whose mtime is not older than the index file's is re-read by content,
                    # because an edit in that same timestamp unit (one whole second for Git for Windows) keeps
                    # the size and the mtime the index cached. A fresh mtime on the copy made such an entry look
                    # settled, and `add -A` then kept the cached blob for an edited file: under load, 13 of 200
                    # same-second edits measured as the unedited tree (the merged-head gate of PB3536).
                    shutil.copy2(index, private)
                env = {**os.environ, "GIT_INDEX_FILE": str(private)}

                def git(*argv: str, stdin: bytes | None = None) -> bytes | None:
                    # bytes, never text: a text-mode pipe on Windows writes CRLF, and `update-index` then ignores
                    # the path it was handed (`Ignoring path <path>\r`)
                    p = subprocess.run(["git", *argv], cwd=self.repo, env=env, input=stdin, capture_output=True)
                    return p.stdout if p.returncode == 0 else None

                never = list(NEVER_WORK)
                at_head = git("ls-tree", "-r", "-z", "HEAD", "--", *never)   # NUL-ended ls-tree lines are
                # `update-index -z --index-info`'s own input form
                # every path, then the NEVER_WORK ones back to HEAD's: an exclusion pathspec on `add` naming an ignored
                # path (STATUS.md) is refused (`The following paths are ignored`), so none is passed to it
                if (at_head is None or git("add", "-A", "--", ".") is None
                        or git("rm", "--cached", "-r", "-q", "--ignore-unmatch", "--", *never) is None
                        or (at_head and git("update-index", "-z", "--index-info", stdin=at_head) is None)):
                    return None
                return (git("write-tree") or b"").decode("ascii", errors="replace").strip() or None
        except (subprocess.CalledProcessError, OSError):
            return None

    def _captured(self, cmd: list[str], spawn: Spawn, live: "_Live | None" = None) -> tuple[int | None, str, float]:
        """Run `cmd` under this interpreter to its end: (exit code, its output, seconds). With `live` — one `audits()`
        call's registry (kb/Work PB3061) — the process is registered while it runs so the first RED can stop it; an
        exit code of None means STOPPED: it was stopped, or never started because the stop came first."""
        t = time.monotonic()
        if live is not None and live.stopping.is_set():
            return None, "", 0.0
        p = subprocess.Popen([sys.executable, *cmd], cwd=self.repo, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                             text=True, encoding="utf-8", errors="replace", start_new_session=os.name != "nt",
                             **spawn())
        if live is not None:
            live.add(p)
        try:
            out = p.communicate()[0]
        finally:
            if live is not None:
                live.discard(p)
        if live is not None and p.pid in live.stopped:
            return None, out or "", time.monotonic() - t
        return p.returncode, out or "", time.monotonic() - t

    def audit_commands(self) -> tuple[tuple[str, list[str]], ...]:
        """The audits this gate runs: AUDITS (the first-red arm of the self-test plants its own)."""
        return AUDITS

    def audit_prerequisites(self) -> tuple[tuple[str, list[str]], ...]:
        """What the audits need in the tree: AUDIT_PREREQUISITES (the self-test's arms plant their own)."""
        return AUDIT_PREREQUISITES

    def _prepared(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """Run every prerequisite (`audit_prerequisites`) in order; the first that fails is its RED reason."""
        for name, cmd in self.audit_prerequisites():
            p = subprocess.run(cmd, cwd=self.repo, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                               encoding="utf-8", errors="replace", **spawn())
            if p.returncode != 0:
                say(f"--- audit prerequisite {name} ({' '.join(cmd)}) ---")
                for line in (p.stdout or "").rstrip().splitlines():
                    say(line)
                say(f"=== {name}: RED (exit {p.returncode}; no audit ran) ===")
                return [f"{name} RED"]
        return []

    def audits(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """The audits (`audit_commands`), every one of them, CONCURRENTLY, once their prerequisites
        (`audit_prerequisites`, run first and in order) are ready; the self-tests reuse recorded PASSes
        (SELF_TESTS_REUSE). Each audit's whole output is printed as one block when it finishes, so no two interleave.
        ⛔ THE FIRST RED STOPS THE REST (kb/Work PB3061): the audits still running are stopped, process trees and all
        (`_Live.stop_all`), and reported STOPPED — never RED — so a red found in 23 s is the verdict in 23 s and not
        after the slowest audit (the self-tests: 5–9 min in every first gate of wave 1062). Returns the RED reasons in
        AUDITS order; a hung self-test is `SELF-TESTS HANG`, never RED (kb/Work PB2914)."""
        if not_ready := self._prepared(spawn, say):
            return not_ready
        reds: dict[str, str] = {}
        live = _Live()
        commands = [(name, [*cmd, SELF_TESTS_REUSE] if name == "SELF-TESTS" else cmd)
                    for name, cmd in self.audit_commands()]
        with concurrent.futures.ThreadPoolExecutor(max_workers=len(commands)) as pool:
            futures = {pool.submit(self._captured, cmd, spawn, live): name for name, cmd in commands}
            for f in concurrent.futures.as_completed(futures):
                name = futures[f]
                rc, text, secs = f.result()
                if rc is None:
                    say(f"--- audit {name}: STOPPED after {secs:.1f}s — another audit was already RED (kb/Work PB3061); "
                        f"the re-gate runs it ---")
                    continue
                say(f"--- audit {name} ({secs:.1f}s) ---")
                for line in text.rstrip().splitlines():
                    say(line)
                if rc != 0:
                    state = "HANG" if name == "SELF-TESTS" and rc == SELF_TESTS_HANG_EXIT else "RED"
                    say(f"=== {name}: {state} (see above) ===")
                    reds[name] = f"{name} {state}"
                    stopped = live.stop_all()
                    if stopped:
                        say(f"=== {name} is {state}: {stopped} audit(s) still running stopped, process trees and all "
                            f"(kb/Work PB3061) ===")
        return [reds[name] for name, _ in commands if name in reds]

    def built_self_tests(self, spawn: Spawn) -> tuple[list[str], list[str]]:
        """The self-tests that read the built assemblies (BUILT_SELF_TESTS), after the build. Returns (the RED reasons,
        the output lines): the gate prints them when it joins this, so they never interleave with a leg's report."""
        rc, text, secs = self._captured(BUILT_SELF_TESTS, spawn)
        lines = [f"--- post-build self-tests ({secs:.1f}s) ---", *text.rstrip().splitlines()]
        return ([] if rc == 0 else ["POST-BUILD SELF-TESTS RED"]), lines

    def fetch_corpus(self, spawn: Spawn, say: Callable[[str], None]) -> list[str]:
        """The GnuCOBOL corpus when the worktree lacks it — THE one rule, scripts/external_corpus.py (kb/Work
        PB2611), which battery.sh and every other caller apply too. Returns the RED reason, if any."""
        return external_corpus.ensure(self.repo, say,
                                      lambda cmd, cwd: subprocess.run(cmd, cwd=cwd, **spawn()).returncode)

    def build(self, spawn: Spawn) -> bool:
        return subprocess.run(["dotnet", "build", "Cobol.Net.sln", "-v", "quiet"], cwd=self.repo,
                              **spawn()).returncode == 0

    def binaries(self) -> dict[str, dict[str, str]]:
        """Per assembly, the SHA-256 of its test assembly and of every product assembly beside it — the set a leg
        host's identity record names (tests/_shared/GateLegs.cs `GateIdentityRecord.ProductAssemblies`)."""
        out = {}
        for asm, project in GATED_ASSEMBLIES.items():
            tests = sorted((self.repo / project / "bin" / "Debug").glob(f"*/Cobol.Net.Tests.{asm}.dll"))
            if len(tests) != 1:
                raise PopulationError(f"{project}: expected one built Cobol.Net.Tests.{asm}.dll under bin/Debug, "
                                      f"found {len(tests)}")
            test_dll = tests[0]
            hashes = {"<test>": _sha256(test_dll)}
            for dll in sorted(test_dll.parent.glob("*.dll")):
                n = dll.name
                if dll != test_dll and (n.startswith("Cobol.Net.") or n == "cobol.dll"):
                    hashes[n] = _sha256(dll)
            out[asm] = hashes
        return out

    def listing(self, asm: str, spawn: Spawn) -> tuple[Counter, str]:
        return list_population(self.repo / GATED_ASSEMBLIES[asm], cwd=self.repo, **spawn())

    def plan(self, listings: dict[str, Counter], base: str, previous: Path | None, out: Path) -> tuple[dict, str]:
        """The order plan (DESIGN §3.14.2), written to `out`; returns (plan, the SHA-256 of the file's bytes)."""
        impacted_tests.HEAD = None  # the worktree, uncommitted edits included, against its cut point
        analysis = impacted_tests.analyse(base)
        # REDS from this worktree's own previous gates on BOTH hosts (kb/Work PB3155): its previous run here and the
        # Linux gate's newest run (the one a landing runs after this gate), so a finisher's re-gate after a landing's
        # Linux red runs that red in leg 1 (tier 0a).
        own, reds = gate_plan.previous_gates(self.repo, previous)
        # TIMINGS from the shared store every gate publishes to (kb/Work PB2527), so a FRESH worktree runs leg 2
        # longest first too; this worktree's own previous run only before any gate on this machine has published.
        shared = gate_plan.read_shared_timings(gate_plan.timings_store(git_common_dir(self.repo)))
        timings = shared if shared else own
        # TRAITS from the test sources (kb/Work PB3022): the host-only witnesses (tier 0h) and the cheap sweeps (0c).
        plan = gate_plan.build_plan({a: list(c.elements()) for a, c in listings.items()}, analysis, timings, reds,
                                    base, gate_plan.gated_traits(self.repo))
        return plan, gate_plan.write_plan(plan, out)

    def publish_timings(self, run: Path, listings: dict[str, Counter]) -> tuple[int, str]:
        """Merge this run's measured durations into the shared timings store (kb/Work PB2527)."""
        return gate_plan.publish_timings(
            gate_plan.timings_store(git_common_dir(self.repo)), run, {d for c in listings.values() for d in c},
            by=f"{self.repo} run {run.name}", now=dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"))

    def run_leg(self, asm: str, leg: int, run: Path, env: dict[str, str], spawn: Spawn) -> int:
        """One assembly's leg: `dotnet test --no-build` with a trx logger, its whole output to `<leg file>.log`."""
        stem = leg_stem(leg, asm)
        cmd = ["dotnet", "test", str(self.repo / GATED_ASSEMBLIES[asm]), "--no-build", "--results-directory",
               str(run), "--logger", f"trx;LogFileName={stem}.trx"]
        with open(run / f"{stem}.log", "wb") as fh:
            return subprocess.run(cmd, cwd=self.repo, stdout=fh, stderr=subprocess.STDOUT, **spawn(env=env)).returncode


def _sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def leg_stem(leg: int, asm: str) -> str:
    """The file stem of one assembly's leg — the name the leg host's identity record also takes (leg-<n>-<key>)."""
    return f"leg-{leg}-{asm}"


def leg_env(handshake: dict[str, str], observations: Path) -> dict[str, str]:
    """⛔ THE ONLY PLACE A TEST HOST'S ENVIRONMENT IS MADE (§3.14.3): the scrubbed environment — no stray
    COBOLNET_GATE_*, VSTest* or RunSettingsFilePath — in English, with this gate's handshake merged over it: all three
    variables, or none. The test harness's observation log (every retry and non-observation of a child process,
    tests/_shared/ProcessObservation.cs) goes to `observations` in the run directory, one per leg host, so a
    non-observation is read beside the leg's own log instead of in one temp file every gate on the host appends to
    (kb/Work PB3108)."""
    if handshake and set(handshake) != set(HANDSHAKE):
        raise ValueError(f"a partial handshake {sorted(handshake)}: the driver hands all three variables or none")
    return {**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en", **handshake, "COBOLNET_HARNESS_LOG": str(observations)}


# ── a run directory: the gate's pid, its verdict record, and the one way to wait for it (kb/Work PB3536) ──────────


def write_pid(run: Path, pid: int) -> None:
    (run / PID_FILE).write_text(json.dumps({"pid": pid}) + "\n", encoding="utf-8", newline="\n")


def read_pid(run: Path) -> int | None:
    try:
        return int(json.loads((run / PID_FILE).read_text(encoding="utf-8"))["pid"])
    except (OSError, ValueError, KeyError, TypeError):
        return None


def read_record(run: Path) -> dict | None:
    """The run's verdict record, or None while it has none (a verdict file is written whole, once, at the end)."""
    try:
        rec = json.loads((run / VERDICT_FILE).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    return rec if isinstance(rec, dict) and "verdict" in rec and "line" in rec else None


def _record_exit(rec: dict) -> int:
    """A record's exit code; a record written before schema 3 carries none, so it is derived from its verdict through
    the one verdict reader."""
    if isinstance(rec.get("exit_code"), int):
        return rec["exit_code"]
    word = read_verdict(str(rec.get("verdict", ""))).word
    return {GREEN_READING.word: 0, BUSY: EXIT_BUSY, NOT_RUN: 2}.get(word, 1)


def resolve_run(run: str | os.PathLike, worktree: str | os.PathLike | None) -> Path:
    """THE ONE RESOLUTION of a handed gate run's name (kb/Work PB3536, review R1-1): an absolute path is itself; a
    relative one (`TestResults/build-local/<run>`, the form an agent returns) names a run of the WORKTREE that gated it,
    never of the reader's checkout: the rolling wave and the planner read it from the main checkout. With no worktree a
    relative name is the current directory's."""
    p = Path(run)
    return p if p.is_absolute() else (Path(worktree) if worktree else Path.cwd()) / p


def watch(run: Path, wait_s: float | None, echo: Callable[[str], None] | None = None,
          alive: Callable[[int], bool] = _pid_alive, poll_s: float = 0.5) -> tuple[int, str]:
    """THE ONE WAY TO WAIT FOR A GATE (kb/Work PB3536): its run directory's VERDICT FILE, never a log's tail.
    Returns (exit code, verdict line): the record's own once `verdict.json` is there; NOT RUN [verdict] (2) when the
    process `gate.pid` names DIED without one; PENDING (AWAIT_PENDING_EXIT) once `wait_s` passes (None: no limit); NOT
    RUN [run] (2) when there is no such run directory (`resolve_run` makes a worktree-relative name absolute first). `echo` (the follower) is handed the run's `gate.log` line by line as it grows, to its end."""
    log, offset, t = run / LOG_FILE, 0, time.monotonic()

    def drain(final: bool = False) -> None:
        nonlocal offset
        if echo is None:
            return
        try:
            data = log.read_bytes()[offset:]
        except OSError:
            return
        cut = len(data) if final else data.rfind(b"\n") + 1
        for line in data[:cut].decode("utf-8", errors="replace").splitlines():
            echo(line)
        offset += cut

    if not run.is_dir():
        return 2, f"{VERDICT}{NOT_RUN_CAUSES['run'].verdict} — no gate run directory {run} ==="
    while True:
        drain()
        rec = read_record(run)
        pid = read_pid(run)
        if rec is not None:
            # the gate writes its record, then prints the line and exits: the follower echoes its output to the end
            while echo is not None and pid is not None and alive(pid):
                drain()
                time.sleep(poll_s)
            drain(final=True)
            return _record_exit(rec), str(rec["line"])
        if pid is not None and not alive(pid):
            if (rec := read_record(run)) is not None:  # it wrote the record and ended between the two reads
                continue
            drain(final=True)
            return 2, (f"{VERDICT}{NOT_RUN_CAUSES['verdict'].verdict} — the gate process {pid} DIED with no verdict "
                       f"(run {run}; its output: "
                       f"{log}) ===")
        if wait_s is not None and time.monotonic() - t >= wait_s:
            who = (f"the gate process {pid} is running" if pid is not None else
                   f"no {PID_FILE} names its process (one not started yet, or a run from before kb/Work PB3536, which "
                   "cannot be told from a dead one)")
            return AWAIT_PENDING_EXIT, f"{AWAIT_PENDING} — no verdict in {run} after {wait_s:.0f} s; {who}; await it again ==="
        time.sleep(poll_s)


def launch_and_follow(new_run: Callable[[], Path], argv_for: Callable[[Path], list[str]],
                      say: Callable[[str], None]) -> int:
    """`build-local`'s gate (kb/Work PB3536): make the run directory, start the gate DETACHED in it (scripts/detached.py:
    it outlives this process and whatever kills this process's tree), name it, and follow it to its verdict. Returns the
    gate's exit code. `argv_for(run)` is the gate's command line; the self-test plants its own."""
    try:
        run = new_run()
    except OSError as e:  # an unwritable run root, or a hung holder of its lock (Gate.new_run): still a verdict line
        say(f"{VERDICT}{NOT_RUN_CAUSES['launch'].verdict} — the gate's run directory could not be made: {e} ===")
        return 2
    try:
        pid = spawn_detached(argv_for(run), run / LOG_FILE, cwd=REPO)
    except OSError as e:
        say(f"{VERDICT}{NOT_RUN_CAUSES['launch'].verdict} — the gate could not be started detached: {e} ===")
        return 2
    if read_pid(run) is None:
        write_pid(run, pid)
    say(f"build-local: the gate runs DETACHED (pid {pid}, run directory {run}): it outlives this process; "
        f"`python scripts/run_gate_legs.py --await {run}` reads its verdict (kb/Work PB3536)")
    code, line = watch(run, None, echo=say)
    if not (run / LOG_FILE).exists() or line not in (run / LOG_FILE).read_text(encoding="utf-8", errors="replace"):
        say(line)  # a verdict the gate's own output does not carry (DIED): the caller's last line is still the verdict
    return code


# ── the gate ─────────────────────────────────────────────────────────────────────────────────────────────────────


@dataclass
class LegRun:
    asm: str
    leg: int
    rc: int
    red: bool
    wall_s: float
    finished_s: float  # since the legs started


@dataclass
class Outcome:
    """What one gate did — the verdict file's content, and what the self-test asserts on."""
    verdict: str = NOT_RUN
    line: str = ""
    exit_code: int = 2
    reasons: list[str] = field(default_factory=list)
    slot: str | None = None
    scope: str = DEFAULT_SCOPE
    scope_until: str | None = None
    leg1_only: bool = False  # the `leg1` scope cut a planned leg 2 (never the fail-fast stop, which is `stopped`)
    plan_sha256: str | None = None
    plan_note: str = ""
    legs_invoked: dict[int, list[str]] = field(default_factory=dict)
    handshakes: dict[tuple[int, str], dict[str, str]] = field(default_factory=dict)
    runs: list[LegRun] = field(default_factory=list)
    populations: dict[str, Population] = field(default_factory=dict)
    not_run: dict[str, int] = field(default_factory=dict)
    identity: list[str] = field(default_factory=list)
    stopped: bool = False
    timings: dict[str, float] = field(default_factory=dict)
    timings_store: str = ""  # what the publish to the shared timings store did (kb/Work PB2527)
    #: Work running BESIDE the gate's critical path (`Gate._beside`), joined once before the verdict: (name, future).
    beside: list[tuple[str, concurrent.futures.Future]] = field(default_factory=list)
    #: WHAT THE GATE MEASURED (kb/Work PB3536): the commit, the working tree at the start and at the end, the base it
    #: planned against; and, for a reused verdict, the run that measured it.
    head: str | None = None
    tree: str | None = None
    tree_end: str | None = None
    base: str | None = None
    reused_from: str | None = None
    reused_detail: str | None = None  # the measuring gate's own verdict detail, carried along a chain of reuses


def run_order(name: str) -> tuple[int, str]:
    """The order of a run directory's name (RUN_DIR): its number, then the name. A run named before the numbers has
    number 0, so it is older than every numbered run (and such runs are ordered among themselves by their stamps)."""
    m = RUN_DIR.fullmatch(name)
    if m is None:
        raise ValueError(f"{name!r} is not a run directory's name (RUN_DIR)")
    return int(m["n"] or 0), name


class Gate:
    def __init__(self, host: Host, run_root: Path, base: str | None = None,
                 say: Callable[[str], None] = lambda s: print(s, flush=True), run_dir: Path | None = None,
                 now: Callable[[], dt.datetime] = lambda: dt.datetime.now(dt.timezone.utc)):
        """`run_dir`: the run directory a detached gate's launcher made (`--run`); None makes a fresh one. `now`: the
        UTC clock a new run's name shows, never its order (`new_run`); the self-test stops it and turns it back."""
        self.host, self.run_root, self.base, self.say, self.run_dir = host, run_root, base, say, run_dir
        self.now = now
        self._pool: concurrent.futures.ThreadPoolExecutor | None = None  # `run` owns it for the work `_beside` starts
        self._base_sha: str | None = None

    def _base(self) -> str:
        """The change's cut point, resolved once: `--base`, else the merge base with origin/main (raises when none)."""
        if self._base_sha is None:
            self._base_sha = self.base or self.host.default_base()
        return self._base_sha

    # The verdict line and exit code are decided here, once, for every path through the gate.
    def _finish(self, out: Outcome, run: Path | None, verdict: str, detail: str, code: int) -> Outcome:
        self._join_beside(out)  # an early end (a failed build, an unlistable population) still reports work beside it
        out.verdict, out.exit_code = verdict, code
        out.line = f"{VERDICT}{verdict} — {detail} ==="
        if run is not None:
            (run / VERDICT_FILE).write_text(json.dumps(self._record(out, run), indent=1, sort_keys=True) + "\n",
                                            encoding="utf-8", newline="\n")
        self.say(out.line)
        return out

    def run(self) -> Outcome:
        out = Outcome()
        started = time.monotonic()
        run = self.run_dir
        if run is not None:  # a detached gate: its pid, before anything can fail, so `--await` can tell it from a death
            write_pid(run, os.getpid())
        label = (f"gate {self.host.repo} (pid {os.getpid()}, since {time.strftime('%Y-%m-%d %H:%M:%S')}"
                 + (f", run {run}" if run is not None else "") + ")")
        lock_path = self.host.lock_path()
        lock = ExclusiveLock.try_take(lock_path, label)
        if lock is None:
            return self._finish(out, run, BUSY, f"another gate holds this worktree's gate lock ({lock_path}): "
                                f"{ExclusiveLock.holder(lock_path) or '(no label)'}; nothing ran, nothing is red: "
                                "re-run once it ends, or `python scripts/run_gate_legs.py --await <its run>` reads "
                                "its verdict", EXIT_BUSY)
        with lock:
            try:
                out.scope, setting = self.host.implementer_scope()
            except ValueError as e:  # malformed shared gate settings stop the gate, with the reason
                return self._finish(out, run, NOT_RUN_CAUSES["settings"].verdict, str(e), 2)
            if setting is not None and setting.until is not None:
                out.scope_until = setting.until.isoformat()
            # The pool the work BESIDE the critical path runs in (`_beside`); leaving it waits for that work, so no
            # gate returns while a thread it started still runs.
            with concurrent.futures.ThreadPoolExecutor(max_workers=len(BESIDE), thread_name_prefix="beside") as pool:
                self._pool = pool
                try:
                    if run is None:
                        run = self.new_run()
                        write_pid(run, os.getpid())
                    self.say(f"build-local: gate, run directory {run}")
                    out.head, out.tree = self.host.head(), self.host.measured_tree()
                    try:
                        out.base = self._base()
                    except Exception:  # noqa: BLE001 — no base: no plan (`_plan` names why), and nothing is reused
                        out.base = None
                    if (prior := self.reusable(run, out)) is not None:
                        return self._reused(out, run, started, *prior)
                    return self._gate(out, label, started, run)
                except Exception as e:  # noqa: BLE001 — a defect in the driver still ends in a verdict line, never
                    # silence: every caller BLOCKS on that line (MANDATORY-PRACTICES P2), so a traceback alone would
                    # hang it.
                    self.say(traceback.format_exc().rstrip())
                    return self._finish(out, run, NOT_RUN_CAUSES["driver"].verdict,
                                        f"the gate driver failed: {type(e).__name__}: {e}", 2)

    def reusable(self, current: Path, out: Outcome) -> tuple[Path, dict] | None:
        """THE GATE'S REUSE RULE (kb/Work PB3536; the self-test reuse rule of kb/Work PB2914/PB3022 extended to the
        gate): the newest earlier run of this worktree whose verdict file is GREEN (exit 0) for THIS tree — measured at
        its start and unchanged at its end — against THIS base, under a scope it covers (a `LEG 1 ONLY` GREEN covers
        `leg1` only; a plain GREEN ran every planned leg and covers both). None when the tree could not be measured or
        no run qualifies: a RED is never reused, and another tree (a landing's merged head) always runs."""
        if not out.tree:
            return None
        for p in reversed(self._runs()):
            rec = read_record(p) if p != current else None
            if (rec is not None and rec.get("exit_code") == 0 and rec.get("tree") == out.tree
                    and rec.get("tree_end") == out.tree and rec.get("base") == out.base
                    and (not rec.get("leg1_only") or out.scope == "leg1")):
                return p, rec
        return None

    def _reused(self, out: Outcome, run: Path, started: float, prior: Path, rec: dict) -> Outcome:
        """Answer from a recorded GREEN (`reusable`): no audit, slot, build or leg runs, and the line says so. Along a
        chain of reuses the line names the run that MEASURED the tree and that run's own verdict detail."""
        out.tree_end, out.reused_from = out.tree, rec.get("reused_from") or prior.name
        out.reused_detail = rec.get("reused_detail") or str(rec.get("line", "")).removeprefix(VERDICT).removesuffix(" ===")
        out.scope_until, out.leg1_only, out.plan_note = rec.get("scope_until"), bool(rec.get("leg1_only")), rec.get("plan", "")
        out.timings["wall_s"] = round(time.monotonic() - started, 1)  # this gate's own seconds (landing_measure.py)
        return self._finish(out, run, str(rec["verdict"]),
                            f"REUSED (kb/Work PB3536): tree {out.tree[:12]} at head {(out.head or '?')[:12]} was gated "
                            f"in run {out.reused_from} and has not changed since, so nothing ran (run {run.name}); "
                            f"that gate: {out.reused_detail}", 0)

    def _beside(self, out: Outcome, name: str, work: Callable[[], tuple[list[str], list[str]]]) -> None:
        """Start `work` BESIDE the gate's critical path (kb/Work PB2880): a step that needs nothing the critical path
        is still making runs concurrently with it instead of in a serial valley ahead of it. `work` returns (its RED
        reasons, its output lines); the lines are printed whole when it is joined (`_join_beside`, once, before the
        verdict), so they never interleave with a leg's report. `name` is one of BESIDE."""
        if name not in BESIDE:
            raise ValueError(f"beside work {name!r} is not one of BESIDE {BESIDE}")
        if self._pool is None:
            raise RuntimeError("beside work can only start inside Gate.run, which owns its pool")
        out.beside.append((name, self._pool.submit(work)))

    def _join_beside(self, out: Outcome) -> None:
        """Join every piece of work started beside the critical path, in the order it was started: print its lines,
        merge its RED reasons, time the wait as `<name>_wait_s`. A piece that RAISED is a named RED with its
        traceback — never a silent pass, never an escape that leaves the gate without its verdict line."""
        while out.beside:
            name, future = out.beside.pop(0)
            t = time.monotonic()
            try:
                reds, lines = future.result()
            except Exception as e:  # noqa: BLE001 — see the docstring
                reds = [f"{name.upper().replace('_', ' ')} FAILED ({type(e).__name__}: {e})"]
                lines = traceback.format_exception(e)
            out.timings[f"{name}_wait_s"] = round(time.monotonic() - t, 1)
            for line in "".join(f"{l}\n" for l in lines).rstrip().splitlines():
                self.say(line)
            out.reasons += reds

    def new_run(self) -> Path:
        """A fresh run directory, numbered one past every run in the run root (RUN_DIR). The number is taken under the
        run root's lock, so two gates starting at once (two `build-local` launches in one worktree: the second ends
        BUSY at the gate lock, but its run directory is made before that) never share one, and the order of runs is
        the order they were made in, whatever the clock reads. TimeoutError (an OSError) when the lock stays held for
        RUNS_LOCK_WAIT_S: its holder hung."""
        self.run_root.mkdir(parents=True, exist_ok=True)
        lock_path = self.run_root / RUNS_LOCK
        deadline = time.monotonic() + RUNS_LOCK_WAIT_S
        while (lock := ExclusiveLock.try_take(lock_path, f"new run (pid {os.getpid()})")) is None:
            if time.monotonic() >= deadline:
                raise TimeoutError(f"the run root's lock {lock_path} stayed held for {RUNS_LOCK_WAIT_S:.0f} s, by "
                                   f"{ExclusiveLock.holder(lock_path) or '(no label)'}")
            time.sleep(0.01)
        with lock:
            runs = self._runs()
            run = self.run_root / f"{(run_order(runs[-1].name)[0] if runs else 0) + 1:06d}-{self.now():%Y%m%dT%H%M%SZ}"
            run.mkdir()
        return run

    def _runs(self) -> list[Path]:
        """Every run directory in the run root, oldest first (`run_order`)."""
        return sorted((p for p in self.run_root.iterdir() if p.is_dir() and RUN_DIR.fullmatch(p.name)),
                      key=lambda p: run_order(p.name)) if self.run_root.is_dir() else []

    def previous_run(self, current: Path) -> Path | None:
        """The newest EARLIER run that reached a verdict and kept a trx: the plan's timings and reds."""
        for p in reversed(self._runs()):
            if p != current and (p / VERDICT_FILE).exists() and any(p.glob("*.trx")):
                return p
        return None

    def _prune(self, current: Path) -> None:
        for p in self._runs()[:-KEEP_RUNS]:
            if p != current:
                shutil.rmtree(p, ignore_errors=True)

    def _gate(self, out: Outcome, label: str, started: float, run: Path) -> Outcome:
        # ⛔ THE AUDITS AND THE CORPUS FETCH RUN BEFORE THE GATE SLOT IS TAKEN (kb/Work PB2524). They read only the
        # tree — no build output, no test result — so they need nothing the slot rations (concurrent builds and test
        # legs, DESIGN-test-build-ci §3.14.6). Inside the slot they were 35-45 % of its hold (~230 s of serial Python
        # per gate, one core busy) while every other gate queued behind it.
        t = time.monotonic()
        audit_reds = self.host.audits(_no_slot, self.say)  # an unchanged self-test is REUSED (PB2914)
        out.timings["audits_s"] = round(time.monotonic() - t, 1)
        if audit_reds:
            # FAIL FAST ON THE AUDITS (kb/Work PB2523): they need no build and no test result and take seconds, while
            # a red one used to be reported only after the whole ~6-min population — 4 of wave 1034's 8 first-run
            # reds were audit-only.
            out.reasons += audit_reds
            return self._finish(out, run, "RED", f"{'; '.join(audit_reds)} — fail-fast: the audits run before the "
                                f"slot and the build and NO LEG RAN; fix the audit and re-gate (run {run.name})", 1)
        out.reasons += self.host.fetch_corpus(_no_slot, self.say)
        t = time.monotonic()
        try:
            slot = self.host.take_slot(label, self.say)
        except ValueError as e:  # the shared gate settings turned malformed while this gate queued: stop, with the reason
            return self._finish(out, run, NOT_RUN_CAUSES["settings"].verdict, str(e), 2)
        out.timings["slot_wait_s"] = round(time.monotonic() - t, 1)
        out.slot = slot.describe()  # acquire() has printed `gate-slot: took slot k of N …`; the verdict line names it
        try:
            return self._tested(out, run, slot, started)
        finally:
            slot.release()

    def _tested(self, out: Outcome, run: Path, slot: Slot, started: float) -> Outcome:
        """The build, the listings, the plan, the legs and the checks: the part of the gate the slot rations."""
        spawn: Spawn = slot.spawn_kwargs
        t = time.monotonic()
        if not self.host.build(spawn):
            return self._finish(out, run, "BUILD FAILED", "the solution did not build (see above)", 1)
        out.timings["build_s"] = round(time.monotonic() - t, 1)
        try:
            binaries = self.host.binaries()
            (run / "binaries.json").write_text(json.dumps(binaries, indent=1, sort_keys=True), encoding="utf-8", newline="\n")
            # The three listings run CONCURRENTLY, as the legs do: each is a separate `--list-tests` host (~2 s).
            with concurrent.futures.ThreadPoolExecutor(max_workers=len(GATED_ASSEMBLIES)) as pool:
                listed = dict(zip(GATED_ASSEMBLIES, pool.map(lambda a: self.host.listing(a, spawn), GATED_ASSEMBLIES)))
            listings = {}
            for asm, (names, raw) in listed.items():
                listings[asm] = names
                (run / f"listing-{asm}.txt").write_text(raw, encoding="utf-8", newline="\n")
        except PopulationError as e:
            return self._finish(out, run, NOT_RUN_CAUSES["population"].verdict,
                                f"the population could not be listed: {e}", 2)

        # The self-tests that read the BUILT assemblies (kb/Work PB2563) run beside the legs: they only list tests, and
        # the legs are the gate's longest step, so they add no wall time. Joined, and printed, after the legs.
        self._beside(out, "post_build_self_tests", lambda: self.host.built_self_tests(spawn))
        return self._legs_and_checks(out, run, listings, binaries, started, spawn)

    def _legs_and_checks(self, out: Outcome, run: Path, listings: dict[str, Counter], binaries: dict,
                         started: float, spawn: Spawn) -> Outcome:
        plan, digest = self._plan(out, listings, run)
        legs = self._legs(plan)
        to_run = legs
        if out.scope == "leg1" and len(legs) > 1:
            # Batched gating (kb/Work PB2515), the one gate's shape (PB3022): leg 2 is CI's whole-population run.
            # The population check below still measures every planned leg, so each leg-2 case is named NOT RUN.
            to_run = {1: legs[1]}  # two planned legs: leg 1 is one of them
            out.leg1_only = True
            self.say(f"build-local: {LEG1_ONLY} — the scope is leg1"
                     + (f" until {out.scope_until}" if out.scope_until else "")
                     + f"; leg 2 is left to {LEG2_OWNER}")
        legs_started = time.monotonic()
        for leg, assemblies in to_run.items():
            if out.stopped:
                break
            out.legs_invoked[leg] = assemblies
            leg_runs = self._run_leg(out, leg, assemblies, run, plan, digest, spawn, legs_started)
            if any(r.red for r in leg_runs) and leg < max(to_run):
                out.stopped = True  # FAIL FAST: the remainder is named by the population check below
        out.timings["legs_s"] = round(time.monotonic() - legs_started, 1)
        reds = [r for r in out.runs if r.red]
        if reds:
            out.timings["first_red_s"] = round(min(r.finished_s for r in reds), 1)
        self._join_beside(out)

        self._check_populations(out, listings, legs, run)
        self._publish_timings(out, run, listings)
        if plan is not None:
            self._check_identity(out, plan, digest, binaries, listings, run)
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        out.tree_end = self.host.measured_tree()  # a tree edited WHILE it was gated is never reused (`reusable`)
        self._prune(run)
        return self._verdict(out, run, plan, legs)

    def _plan(self, out: Outcome, listings: dict[str, Counter], run: Path) -> tuple[dict | None, str | None]:
        """The order plan. No plan input, a stale one or a broken one gives ONE leg in the plain order (§3.14.1) —
        named, never a skip."""
        try:
            base = self._base()
            previous = self.previous_run(run)
            plan, digest = self.host.plan(listings, base, previous, run / "plan.json")
        except Exception as e:  # noqa: BLE001 — ANY planning failure falls back to the whole population in one leg
            out.plan_note = f"no plan ({type(e).__name__}: {str(e).splitlines()[0] if str(e) else ''})"
            self.say(f"build-local: ⚠ the order plan could not be made — {out.plan_note}; ONE leg in the plain order")
            return None, None
        for line in gate_plan.summary_lines(plan, run / "plan.json"):
            self.say(line)
        out.plan_sha256 = plan["sha256"]
        out.plan_note = (f"plan {plan['sha256'][:12]}: map {(plan['map'] or 'none')[:12]} ({plan['map_state']}), "
                         f"timings {plan['timings']}")
        return plan, digest

    @staticmethod
    def _legs(plan: dict | None) -> dict[int, list[str]]:
        """Which assemblies each leg invokes. An assembly the plan gives no case in a leg is not invoked for it: a
        host handed no case prints no verdict line and exits 0, which the leg report would score RED (§3.14.2)."""
        if plan is None:
            return {1: list(GATED_ASSEMBLIES)}
        legs: dict[int, list[str]] = {}
        for leg in (1, 2):
            names = [a for a in GATED_ASSEMBLIES if leg in plan["assemblies"][a]["legs"]]
            if names:
                legs[leg] = names
        return legs

    def _run_leg(self, out: Outcome, leg: int, assemblies: list[str], run: Path, plan: dict | None,
                 digest: str | None, spawn: Spawn, legs_started: float) -> list[LegRun]:
        self.say(f"build-local: leg {leg} — {', '.join(assemblies)} concurrently")
        handshake = {} if plan is None else {"COBOLNET_GATE_PLAN": str((run / "plan.json").resolve()),
                                             "COBOLNET_GATE_LEG": str(leg), "COBOLNET_GATE_PLAN_SHA256": digest}

        def one(asm: str) -> tuple[str, int, float, float]:
            env = leg_env(handshake, run / f"{leg_stem(leg, asm)}.observations.log")
            out.handshakes[(leg, asm)] = {k: v for k, v in env.items() if k.upper().startswith("COBOLNET_GATE_")}
            t = time.monotonic()
            rc = self.host.run_leg(asm, leg, run, env, spawn)
            return asm, rc, time.monotonic() - t, time.monotonic() - legs_started

        with concurrent.futures.ThreadPoolExecutor(max_workers=len(assemblies)) as pool:
            results = {asm: (rc, wall, fin) for asm, rc, wall, fin in pool.map(one, assemblies)}
        leg_runs = []
        for asm in assemblies:  # reported in a fixed order, whatever order they finished in
            rc, wall, fin = results[asm]
            stem = leg_stem(leg, asm)
            # THE ONE RULE for what a leg prints (kb/Work PB1573): a green leg its verdict line, a red one its
            # COMPLETE output. Its exit code is the leg's verdict: 0 green, anything else red.
            rep = subprocess.run([sys.executable, str(REPO / "scripts" / "test_leg_report.py"), "--name",
                                  f"{asm} leg {leg}", "--log", str(run / f"{stem}.log"), "--rc", str(rc)],
                                 capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
            for line in (rep.stdout + rep.stderr).splitlines():
                self.say(line)
            leg_runs.append(LegRun(asm, leg, rc, rep.returncode != 0, round(wall, 1), round(fin, 1)))
        out.runs += leg_runs
        return leg_runs

    def _check_populations(self, out: Outcome, listings: dict[str, Counter], legs: dict[int, list[str]],
                           run: Path) -> None:
        for asm in GATED_ASSEMBLIES:
            invoked = [leg for leg, names in legs.items() if asm in names]
            ran = [leg for leg in invoked if asm in out.legs_invoked.get(leg, [])]
            trx = [str(run / f"{leg_stem(leg, asm)}.trx") for leg in ran if (run / f"{leg_stem(leg, asm)}.trx").exists()]
            stopped = len(ran) < len(invoked)
            try:
                if not trx and not ran:  # every leg of it was cut off by the stop: its whole population is NOT RUN
                    pop = Population(asm, sum(listings[asm].values()), 0, 0, 0, Counter(listings[asm]), Counter(),
                                     Counter(), stopped_early=True)
                elif not trx:
                    raise PopulationError(f"{asm}: no leg of it wrote a trx")
                else:
                    pop = check_population(asm, listings[asm], trx, stopped_early=stopped)
            except PopulationError as e:
                out.reasons.append(f"{asm} POPULATION UNMEASURED ({e})")
                self.say(f"=== POPULATION {asm}: UNMEASURED — {e} ===")
                continue
            out.populations[asm] = pop
            lines = pop.report()
            if stopped:
                named = [l for l in lines[1:] if l.lstrip().startswith("NOT RUN")]
                out.not_run[asm] = sum(pop.never_ran.values())
                (run / f"not-run-{asm}.txt").write_text("\n".join(named) + "\n", encoding="utf-8", newline="\n")
                lines = [lines[0], *[l for l in lines[1:] if l not in named], *named[:NOT_RUN_SHOWN]]
                if len(named) > NOT_RUN_SHOWN:
                    lines.append(f"  … {len(named) - NOT_RUN_SHOWN:,} more NOT RUN, every one named in "
                                 f"{run / f'not-run-{asm}.txt'}")
            for line in lines:
                self.say(line)

    def _publish_timings(self, out: Outcome, run: Path, listings: dict[str, Counter]) -> None:
        """Every gate that ran a leg, a stopped one included, publishes its measured durations to the shared timings
        store (kb/Work PB2527), so the NEXT gate — in a fresh worktree — plans longest first. The store is a
        hint for ordering: a failed write is reported on the console and in the verdict file, never in the verdict."""
        if not any(run.glob("*.trx")):
            return
        try:
            n, note = self.host.publish_timings(run, listings)
        except Exception as e:  # noqa: BLE001 — see the docstring: ordering degrades, the verdict is unaffected
            out.timings_store = f"NOT updated ({type(e).__name__}: {e})"
            self.say(f"build-local: ⚠ the shared timings store was {out.timings_store}; later plans keep the "
                     "durations already there")
            return
        out.timings_store = f"published {n:,} measured case durations" + (f"; {note}" if note else "")
        self.say(f"build-local: {out.timings_store} to the shared timings store")

    def _check_identity(self, out: Outcome, plan: dict, digest: str, binaries: dict[str, dict[str, str]],
                        listings: dict[str, Counter], run: Path) -> None:
        """Each leg host ran THIS plan on THESE binaries (§3.14.4): its identity record names the digest the driver
        handed it, the plan's content digest, and the hashes step 3 recorded; it was handed the whole discovered
        population and ran only its own leg's keys. A leg run under a handshake that wrote no record did not run the
        gate's framework at all."""
        for leg, assemblies in out.legs_invoked.items():
            for asm in assemblies:
                where = f"leg {leg} {asm}"
                path = run / f"{leg_stem(leg, asm)}.json"
                try:
                    rec = json.loads(path.read_text(encoding="utf-8"))
                except (OSError, ValueError) as e:
                    out.identity.append(f"{where}: no readable identity record ({path.name}: {e})")
                    continue
                problems = []
                if str(rec.get("plan_sha256", "")).lower() != digest.lower():
                    problems.append(f"read plan digest {rec.get('plan_sha256')}, the driver handed {digest}")
                if rec.get("plan_content_sha256") != plan["sha256"]:
                    problems.append("its plan's content digest is not this plan's")
                if rec.get("leg") != leg or rec.get("assembly") != asm:
                    problems.append(f"it says leg {rec.get('leg')} {rec.get('assembly')}")
                built = binaries[asm]
                if rec.get("test_assembly", {}).get("sha256") != built["<test>"]:
                    problems.append("its test assembly is not the one this gate built")
                products = {k: v for k, v in built.items() if k != "<test>"}
                if rec.get("product_assemblies") != products:
                    changed = sorted(set(products) ^ set(rec.get("product_assemblies", {})) | {
                        k for k in products if rec.get("product_assemblies", {}).get(k) not in (None, products[k])})
                    problems.append(f"it ran other product binaries than this gate built ({', '.join(changed)})")
                if rec.get("received") != sum(listings[asm].values()):
                    problems.append(f"it was handed {rec.get('received')} cases of {sum(listings[asm].values())} "
                                    "discovered")
                wrong_leg = [k for k in rec.get("runs", []) if gate_plan.leg_of(plan, asm, k) != leg]
                if wrong_leg:
                    problems.append(f"it ran {len(wrong_leg)} case(s) of the other leg (first: {wrong_leg[0]})")
                out.identity += [f"{where}: {p}" for p in problems]
        if out.identity:
            self.say(f"=== IDENTITY: MISMATCH — {len(out.identity)} ===")
            for p in out.identity:
                self.say(f"  IDENTITY MISMATCH  {p}")
        else:
            self.say(f"=== IDENTITY: every leg host ran plan {plan['sha256'][:12]} on the binaries this gate built ===")

    def _verdict(self, out: Outcome, run: Path, plan: dict | None, legs: dict[int, list[str]]) -> Outcome:
        pops = out.populations
        tally = " · ".join(f"{a} {pops[a].ran:,}/{pops[a].discovered:,}" if a in pops else f"{a} UNMEASURED"
                           for a in GATED_ASSEMBLIES)
        skipped = sum(p.skipped for p in pops.values())
        n_legs = len(legs)
        detail = (f"{tally} cases ran (skipped {skipped:,}) in {len(out.legs_invoked)} of {n_legs} leg(s)"
                  + (f" · {out.slot}" if out.slot else "") + f" ({out.plan_note}; run {run.name})")
        reds = [f"{r.asm} leg {r.leg} RED" for r in out.runs if r.red]
        problems = reds + out.reasons + [f"{a} POPULATION {'NOT EXACT' if not pops[a].stopped_early else 'STOPPED'}"
                                         for a in pops if not pops[a].exact and not pops[a].stopped_early]
        if out.identity:
            problems.append(f"{len(out.identity)} IDENTITY MISMATCH(ES)")
        remainder = " · ".join(f"{a} {n:,}" for a, n in out.not_run.items())
        if out.stopped:
            problems.append(f"STOPPED AFTER LEG 1 — leg 2 NOT RUN: {remainder} (named in {run / 'not-run-*.txt'})")
            return self._finish(out, run, "RED/INCOMPLETE", f"{'; '.join(problems)} — {detail}", 1)
        if out.leg1_only:
            left = (f"leg 2 left to {LEG2_OWNER}: {remainder} (named in "
                    f"{run / 'not-run-*.txt'}; scope leg1" + (f" until {out.scope_until}" if out.scope_until else "")
                    + ")")
            if problems:
                return self._finish(out, run, f"{LEG1_ONLY}: RED", f"{'; '.join(problems)}; {left} — {detail}", 1)
            return self._finish(out, run, f"{LEG1_ONLY}: GREEN", f"{left} — {detail}", 0)
        if problems:
            return self._finish(out, run, "RED", f"{'; '.join(problems)} — {detail}", 1)
        return self._finish(out, run, "GREEN", detail, 0)

    def _record(self, out: Outcome, run: Path) -> dict:
        return {"schema": RECORD_SCHEMA, "verdict": out.verdict, "line": out.line, "exit_code": out.exit_code,
                "head": out.head, "tree": out.tree, "tree_end": out.tree_end, "base": out.base,
                "reused_from": out.reused_from, "reused_detail": out.reused_detail, "reasons": out.reasons,
                "slot": out.slot, "plan_sha256": out.plan_sha256, "plan": out.plan_note, "stopped": out.stopped,
                "scope": out.scope, "scope_until": out.scope_until, "leg1_only": out.leg1_only,
                "legs": {str(leg): names for leg, names in out.legs_invoked.items()},
                "runs": [vars(r) for r in out.runs], "timings": out.timings, "not_run": out.not_run,
                "timings_store": out.timings_store,
                "identity": out.identity,
                "populations": {a: {"discovered": p.discovered, "ran": p.ran, "skipped": p.skipped, "exact": p.exact}
                                for a, p in out.populations.items()}}


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────


class FakeHost(Host):
    """A planted machine: a small population per assembly, a planted plan, and leg hosts that write the trx, the log
    and the identity record the real ones write — each arm plants one defect into them."""

    POPULATION = {
        "Conformance": ["N.Conf.A", "N.Conf.B", "N.Conf.C", "N.Conf.D", "N.Conf.E", "N.Conf.F"],
        "Unit": ["N.Unit.A", "N.Unit.B", "N.Unit.C", "N.Unit.D"],
        "Characterization": ["N.Char.A", "N.Char.B"],
    }
    #: Conformance runs in two legs; Unit's leg 1 is EMPTY (it runs in leg 2 only); Characterization in leg 1 only.
    LEGS = {"Conformance": ([1, 2], ["N.Conf.A", "N.Conf.B"], ["N.Conf.C", "N.Conf.D", "N.Conf.E", "N.Conf.F"]),
            "Unit": ([2], [], ["N.Unit.A", "N.Unit.B", "N.Unit.C", "N.Unit.D"]),
            "Characterization": ([1], ["N.Char.A", "N.Char.B"], [])}

    def __init__(self, root: Path, lock: Path, plant: set[str] = frozenset(), plan_fails: bool = False,
                 scope: str | None = None, trees: list[str | None] | None = None, base: str = "0" * 40):
        """`trees`: the working tree the gate measures, call by call (its start, then its end; the last value
        repeats); `base`: the planted cut point."""
        super().__init__(root)
        self.root, self._lock, self.plant, self.plan_fails, self.scope = root, lock, set(plant), plan_fails, scope
        self.trees, self.base_sha = list(trees or ["tree-1"]), base
        self.slots_taken = 0
        self.audited = self.built = False
        self.events: list[str] = []  # the order the gate drove the host in: audits, fetch, slot, build
        self.envs: dict[tuple[int, str], dict[str, str]] = {}
        self.plan_obj: dict | None = None

    def lock_path(self) -> Path:
        return self._lock

    def take_slot(self, label, say):
        self.events.append("slot")
        if "slot-settings" in self.plant:
            raise ValueError("planted: settings.json turned malformed while the gate queued")
        self.slots_taken += 1
        return Slot(0, 1, 1, 0.0, -1)  # a held slot's shape, no lock: the cap itself is gate_slot.py's self-test

    def implementer_scope(self):
        """The planted scope: None plants NO setting, so the coded default is in force, as `Host.implementer_scope`
        returns it; reading the real shared settings file is gate_slot.py's self-test."""
        if "settings" in self.plant:
            raise ValueError("planted: a malformed settings.json")
        if self.scope is None:
            return DEFAULT_SCOPE, None
        return self.scope, Setting(self.scope, dt.datetime(2999, 1, 1, tzinfo=dt.timezone.utc), "planted", "t", "t")

    def default_base(self) -> str:
        return self.base_sha

    def head(self) -> str | None:
        return "head-1"

    def measured_tree(self) -> str | None:
        return self.trees.pop(0) if len(self.trees) > 1 else self.trees[0]

    def audits(self, spawn, say) -> list[str]:
        self.audited = True
        self.events.append("audits")
        say("PLANTED-AUDIT-OUTPUT")
        return ["RENDERED VIEWS RED"] if "audit" in self.plant else []

    def built_self_tests(self, spawn):
        self.events.append("post-build self-tests")
        if "post-build-crash" in self.plant:
            raise RuntimeError("planted: a defect in the post-build self-tests' runner")
        if "post-build" in self.plant:
            return ["POST-BUILD SELF-TESTS RED"], ["--- post-build self-tests (0.0s) ---", "PLANTED-POST-BUILD-RED"]
        return [], ["--- post-build self-tests (0.0s) ---"]

    def fetch_corpus(self, spawn, say) -> list[str]:
        self.events.append("fetch")
        return []

    def build(self, spawn) -> bool:
        self.built = True
        self.events.append("build")
        return "build" not in self.plant

    def publish_timings(self, run, listings):
        self.events.append("publish")
        if "publish" in self.plant:
            raise OSError("planted: the shared timings store is not writable")
        return len(list(gate_plan.trx_results(run))), ""

    def binaries(self):
        if "crash" in self.plant:
            raise RuntimeError("planted: a defect in the driver's own code")
        return {a: {"<test>": f"test-{a}", "Cobol.Net.Compiler.dll": f"compiler-{a}"} for a in GATED_ASSEMBLIES}

    def listing(self, asm, spawn):
        if "population" in self.plant and asm == "Unit":
            raise PopulationError("Unit: planted: `--list-tests` exited 1 (a test class's static constructor threw)")
        path = synthetic_listing(self.root / f"src-listing-{asm}.txt", self.POPULATION[asm])
        text = Path(path).read_text(encoding="utf-8")
        return Counter(self.POPULATION[asm]), text

    def plan(self, listings, base, previous, out):
        if self.plan_fails:
            raise RuntimeError("planted: the impact analysis crashed")
        plan = {"schema": 1, "base": base, "map": None, "map_state": "none", "every_tier1": ["planted"],
                "constants": {}, "timings": "none", "reds": {},
                "assemblies": {a: {"legs": legs, "leg1": l1, "leg2": l2, "stats": {
                    "cases": len(self.POPULATION[a]), "tiers": {}, "leg1_cases": len(l1), "leg1_seconds": 0,
                    "recorded_seconds": 0, "leg1_floor_seconds": 0, "leg1_floor_collection": None,
                    "one_leg_reason": ""}} for a, (legs, l1, l2) in self.LEGS.items()}}
        plan["sha256"] = hashlib.sha256(gate_plan.canonical(plan)).hexdigest()
        self.plan_obj = plan
        return plan, gate_plan.write_plan(plan, out)

    def run_leg(self, asm, leg, run, env, spawn) -> int:
        self.envs[(leg, asm)] = dict(env)
        planned = env.get("COBOLNET_GATE_LEG") is not None
        names = [n for n in self.POPULATION[asm] if not planned or gate_plan.leg_of(self.plan_obj, asm, n) == leg]
        cases = [(n, ["Passed"]) for n in names]
        red = False
        if asm == "Conformance" and leg == 1 and "red-leg1" in self.plant:
            cases[0] = (cases[0][0], ["Failed"])
            red = True
        if asm == "Conformance" and leg == 2 and "drop" in self.plant:
            cases = cases[1:]
        if asm == "Conformance" and leg == 2 and "duplicate" in self.plant:
            cases.append(("N.Conf.A", ["Passed"]))
        if asm == "Conformance" and "skip" in self.plant and leg == max(self.LEGS[asm][0]):
            cases[-1] = (cases[-1][0], ["NotExecuted"])
        stem = leg_stem(leg, asm)
        synthetic_trx(run / f"{stem}.trx", cases)
        passed = sum(o == ["Passed"] for _, o in cases)
        failed = sum(o == ["Failed"] for _, o in cases)
        log = [f"  Failed {cases[0][0]} [1 s]", "  Error Message:", "   PLANTED-RED-MESSAGE"] if red else []
        log.append(f"{'Failed!' if red else 'Passed!'}  - Failed:     {failed}, Passed:     {passed}, Skipped:     0, "
                   f"Total:     {len(cases)}, Duration: 1 s - Cobol.Net.Tests.{asm}.dll (net10.0)")
        (run / f"{stem}.log").write_text("\n".join(log) + "\n", encoding="utf-8", newline="\n")
        if planned:
            rec = {"schema": 1, "assembly": asm, "leg": leg, "plan_sha256": env["COBOLNET_GATE_PLAN_SHA256"],
                   "plan_content_sha256": self.plan_obj["sha256"],
                   "test_assembly": {"file": f"Cobol.Net.Tests.{asm}.dll", "mvid": "0", "sha256": f"test-{asm}"},
                   "product_assemblies": {"Cobol.Net.Compiler.dll": f"compiler-{asm}"},
                   "received": len(self.POPULATION[asm]), "runs": [gate_plan.name_key(n) for n in names]}
            if "digest" in self.plant and asm == "Conformance" and leg == 2:
                rec["plan_sha256"] = "f" * 64
            if "binary" in self.plant and asm == "Conformance" and leg == 2:
                rec["product_assemblies"] = {"Cobol.Net.Compiler.dll": "rebuilt-between-legs"}
            (run / f"{stem}.json").write_text(json.dumps(rec), encoding="utf-8", newline="\n")
        return 1 if red else 0


def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate  # noqa: PLC0415  (this directory's: the one scratch-repository fixture)
    isolate()  # ⛔ hermetic: the arms below make git repositories (scratch_repo.isolate, kb/Work PB1719)
    results: list[tuple[str, bool, str]] = []
    root = Path(tempfile.mkdtemp(prefix="gate-legs-selftest-"))
    sink: list[str] = []

    def gate(name: str, **fake) -> tuple[Outcome, FakeHost]:
        """The arms of the two-leg mechanics (fail-fast, the population, the identity, …) run under the `whole`
        scope unless they name another; `scope=None` plants no setting, the coded default."""
        d = root / name
        d.mkdir()
        fake.setdefault("scope", "whole")
        host = FakeHost(d, d / "gate.lock", **fake)
        sink.clear()
        return Gate(host, d / "runs", say=sink.append).run(), host

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    def said(text: str) -> bool:
        return any(text in line for line in sink)

    try:
        o, h = gate("green")
        arm("a whole planted population in two legs is GREEN, every assembly EXACT",
            o.verdict == "GREEN" and o.exit_code == 0 and all(p.exact for p in o.populations.values())
            and len(o.populations) == 3 and h.slots_taken == 1, o.line)
        arm("an assembly whose leg 1 is empty is not invoked for leg 1, and its population is still whole",
            "Unit" not in o.legs_invoked.get(1, []) and "Unit" in o.legs_invoked.get(2, [])
            and o.populations["Unit"].exact and ("Characterization" not in o.legs_invoked.get(2, [])), str(o.legs_invoked))
        hs = o.handshakes
        arm("every leg host is handed all three handshake variables, naming its own leg and the plan's digest",
            all(set(v) == set(HANDSHAKE) and v["COBOLNET_GATE_LEG"] == str(leg) for (leg, _), v in hs.items()), str(hs))
        o2 = [l for l in sink if l.startswith(VERDICT)]
        arm("the verdict line is the last line, and counts every assembly's discovered cases",
            sink[-1] == o.line and "Conformance 6/6" in o.line and "Unit 4/4" in o.line
            and "Characterization 2/2" in o.line and len(o2) == 1, o.line)
        runs = sorted(p.name for p in (root / "green" / "runs").iterdir() if p.name != RUNS_LOCK)  # its run numbers' lock
        rec = json.loads((root / "green" / "runs" / runs[0] / VERDICT_FILE).read_text(encoding="utf-8"))
        arm("the run directory is fresh and holds the verdict file, the plan, every trx, log and identity record",
            len(runs) == 1 and RUN_DIR.fullmatch(runs[0]) is not None and rec["verdict"] == "GREEN"
            and (root / "green" / "runs" / runs[0] / "plan.json").exists()
            and len(list((root / "green" / "runs" / runs[0]).glob("leg-*.trx"))) == 4, str(runs))

        o, _ = gate("drop", plant={"drop"})
        arm("a dropped case is NEVER RAN, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].never_ran.values()) == 1
            and said("NEVER RAN"), o.line)
        o, _ = gate("dup", plant={"duplicate"})
        arm("a case run in both legs is RAN TWICE, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].ran_twice.values()) == 1, o.line)
        o, _ = gate("skip", plant={"skip"})
        arm("a skipped case is counted skipped, never as ran — and the gate stays GREEN",
            o.verdict == "GREEN" and o.populations["Conformance"].skipped == 1 and "skipped 1" in o.line, o.line)
        o, _ = gate("digest", plant={"digest"})
        arm("a leg host that read another plan digest is an IDENTITY MISMATCH, and the gate is RED",
            o.verdict == "RED" and any("plan digest" in p for p in o.identity), str(o.identity))
        o, _ = gate("binary", plant={"binary"})
        arm("a binary changed between the legs is an IDENTITY MISMATCH, and the gate is RED",
            o.verdict == "RED" and any("other product binaries" in p for p in o.identity), str(o.identity))

        o, h = gate("red", plant={"red-leg1"})
        arm("a red in leg 1 STOPS the gate: leg 2 is never invoked, the verdict is RED/INCOMPLETE, exit 1",
            o.verdict == "RED/INCOMPLETE" and o.exit_code == 1 and 2 not in o.legs_invoked and o.stopped
            and not any(k[0] == 2 for k in h.envs), o.line)
        arm("the stopped gate NAMES its remainder: the counts on the verdict line, every case in the run directory",
            o.not_run == {"Conformance": 4, "Unit": 4} and "leg 2 NOT RUN: Conformance 4 · Unit 4" in o.line
            and said("NOT RUN") and "first_red_s" in o.timings, f"{o.not_run} {o.line}")
        arm("a red leg's whole output, its failure message included, reaches the console through test_leg_report.py",
            said("PLANTED-RED-MESSAGE") and said("Conformance leg 1: RED"), "\n".join(sink[-20:]))

        o, h = gate("noplan", plan_fails=True)
        arm("no plan (the planner failed): ONE leg in the plain order, every assembly, no handshake — named, GREEN",
            o.verdict == "GREEN" and o.legs_invoked == {1: list(GATED_ASSEMBLIES)}
            and all(not v for v in o.handshakes.values()) and "no plan (RuntimeError" in o.line, o.line)
        stray = {"COBOLNET_GATE_LEG": "2", "VSTestTestCaseFilter": "FullyQualifiedName~Nothing"}
        os.environ.update(stray)
        try:
            observations = Path("run") / "leg-1-Unit.observations.log"
            env = leg_env({}, observations)
            planned = leg_env({"COBOLNET_GATE_PLAN": "p", "COBOLNET_GATE_LEG": "1", "COBOLNET_GATE_PLAN_SHA256": "d"},
                              observations)
            try:
                leg_env({"COBOLNET_GATE_LEG": "1"}, observations)
                partial_refused = False
            except ValueError:
                partial_refused = True
        finally:
            for k in stray:
                os.environ.pop(k, None)
        arm("a leg host's environment is scrubbed of a stray handshake and VSTest property, and a partial handshake "
            "is refused",
            "COBOLNET_GATE_LEG" not in env and "VSTestTestCaseFilter" not in env and planned["COBOLNET_GATE_LEG"] == "1"
            and "VSTestTestCaseFilter" not in planned and partial_refused)
        arm("a leg host writes the harness observation log into the run directory, one file per leg host (PB3108)",
            env.get("COBOLNET_HARNESS_LOG") == str(observations) == planned.get("COBOLNET_HARNESS_LOG"), str(env.get(
                "COBOLNET_HARNESS_LOG")))

        # Batched gating (kb/Work PB2515): the shared implementer scope `leg1`, and with no setting the coded default.
        o, h = gate("default-scope", scope=None)
        arm("no scope set: the coded default is leg1 (batched gating) — leg 1 only, LEG 1 ONLY: GREEN, no expiry",
            o.scope == "leg1" and o.scope_until is None and o.leg1_only and 2 not in o.legs_invoked
            and o.verdict == f"{LEG1_ONLY}: GREEN" and o.exit_code == 0 and "until" not in o.line, o.line)
        o, h = gate("leg1", scope="leg1")
        arm("scope leg1 (batched gating): leg 1 only, every leg-2 case named NOT RUN, the verdict line says "
            "LEG 1 ONLY and is never a plain GREEN",
            o.verdict == f"{LEG1_ONLY}: GREEN" and o.exit_code == 0 and o.leg1_only and not o.stopped
            and 2 not in o.legs_invoked and not any(k[0] == 2 for k in h.envs)
            and o.not_run == {"Conformance": 4, "Unit": 4} and "GATE: GREEN" not in o.line
            and o.line.startswith(f"{VERDICT}{LEG1_ONLY}: GREEN") and "until 2999-01-01" in o.line
            and json.loads(next((root / "leg1" / "runs").iterdir()).joinpath(VERDICT_FILE).read_text(
                encoding="utf-8"))["leg1_only"] is True, o.line)
        o, _ = gate("leg1-red", scope="leg1", plant={"red-leg1"})
        arm("scope leg1: a red in leg 1 is LEG 1 ONLY: RED, exit 1",
            o.verdict == f"{LEG1_ONLY}: RED" and o.exit_code == 1 and "Conformance leg 1 RED" in o.line, o.line)
        o, _ = gate("leg1-noplan", scope="leg1", plan_fails=True)
        arm("scope leg1 with no plan: the one leg IS the whole population, so the verdict is a plain GREEN",
            o.verdict == "GREEN" and not o.leg1_only and all(p.exact for p in o.populations.values()), o.line)
        o, _ = gate("settings", plant={"settings"})
        arm("malformed shared gate settings: the gate is NOT RUN [settings], exit 2, with the reason, and the one "
            "verdict reader classifies it ENVIRONMENT: it never blocks the branch (kb/Work PB3526)",
            o.verdict == "NOT RUN [settings]" and o.exit_code == 2 and "malformed settings.json" in o.line
            and read_verdict(o.line[len(VERDICT):]) == not_run("settings")
            and not read_verdict(o.line[len(VERDICT):]).branch_carried, o.line)
        o, h = gate("population", plant={"population"})
        arm("a population that cannot be listed after a GREEN build is NOT RUN [population], exit 2, no leg run, and "
            "the one verdict reader classifies it BRANCH: the tree's own test projects (review R2-1 of wave 1150)",
            o.verdict == "NOT RUN [population]" and o.exit_code == 2 and h.built and not o.runs
            and "planted: `--list-tests` exited 1" in o.line
            and read_verdict(o.line[len(VERDICT):]).branch_carried, o.line)

        o, h = gate("audit", plant={"audit"})
        arm("a red audit (a tracked rendered view) STOPS the gate before the build: RED, the audit "
            "named, no leg ran",
            o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line and "NO LEG RAN" in o.line
            and h.audited and not h.built and not o.runs and not o.legs_invoked and sink[-1] == o.line, o.line)
        arm("the audits and the corpus fetch run BEFORE the gate slot is taken, and a red audit never queues for one "
            "(kb/Work PB2524)",
            h.slots_taken == 0 and h.events == ["audits"] and o.slot is None
            and gate("order")[1].events == ["audits", "fetch", "slot", "build", "post-build self-tests",
                                                           "publish"],
            str(h.events))
        o, h = gate("post-build", plant={"post-build"})
        arm("a red post-build self-test (one that reads the built assemblies, kb/Work PB2563) runs beside the legs and "
            "makes the gate RED after them, its output printed whole",
            o.verdict == "RED" and o.exit_code == 1 and "POST-BUILD SELF-TESTS RED" in o.line
            and sorted(o.legs_invoked) == [1, 2] and not any(r.red for r in o.runs)
            and said("PLANTED-POST-BUILD-RED"), o.line)
        o, h = gate("publish-whole")
        o2, h2 = gate("publish-stopped", plant={"red-leg1"})
        arm("every gate that ran a leg publishes its measured durations to the shared timings store, a stopped "
            "gate's included (kb/Work PB2527)",
            h.events[-1] == "publish" and o.timings_store.startswith("published 12 ")
            and h2.events[-1] == "publish" and o2.timings_store.startswith("published 4 ")
            and said("to the shared timings store"), f"{o.timings_store} | {o2.timings_store}")
        o, h = gate("publish-fails", plant={"publish"})
        rec = json.loads(next((root / "publish-fails" / "runs").iterdir()).joinpath(VERDICT_FILE).read_text(
            encoding="utf-8"))
        arm("a failed publish to the timings store is reported and recorded, and the verdict is unaffected (GREEN)",
            o.verdict == "GREEN" and said("timings store was NOT updated")
            and rec["timings_store"].startswith("NOT updated (OSError"), o.line)
        o, h = gate("slot-settings", plant={"slot-settings"})
        arm("shared gate settings that turn malformed while the gate queues: NOT RUN [settings] (ENVIRONMENT), exit 2, "
            "with the reason, the audits already run",
            o.verdict == "NOT RUN [settings]" and o.exit_code == 2 and "while the gate queued" in o.line
            and not h.built and not read_verdict(o.line[len(VERDICT):]).branch_carried
            and h.events == ["audits", "fetch", "slot"], o.line)

        class HungSelfTests(Host):  # the real Host.audits over planted exit codes: no audit runs
            def __init__(self):
                super().__init__(root)
                self.commands: list[list[str]] = []

            def audit_prerequisites(self):
                return ()

            def _captured(self, cmd, spawn, live=None):
                self.commands.append(cmd)
                return (SELF_TESTS_HANG_EXIT if cmd[0] == "scripts/self_tests.py" else 0), "", 0.0

        class PrerequisiteFails(HungSelfTests):  # a real prerequisite process that fails: no audit may start
            def audit_prerequisites(self):
                return (("PLANTED PREREQUISITE", [sys.executable, "-c", "print('not ready'); raise SystemExit(3)"]),)
        unready, heard_unready = PrerequisiteFails(), []
        reasons = unready.audits(_no_slot, heard_unready.append)
        arm("a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its "
            "output, and no audit runs (kb/Work PB1957 row 44)",
            reasons == ["PLANTED PREREQUISITE RED"] and unready.commands == [] and "not ready" in heard_unready,
            f"{reasons} {unready.commands} {heard_unready}")
        hung = HungSelfTests()
        reasons = hung.audits(_no_slot, lambda _: None)
        arm("a hung self-test is `SELF-TESTS HANG` in the gate's reasons, never RED, and every gate's self-tests run "
            "with --reuse (kb/Work PB2914, PB3022)",
            reasons == ["SELF-TESTS HANG"] and ["scripts/self_tests.py", SELF_TESTS_REUSE] in hung.commands,
            f"{reasons} {hung.commands}")

        class FirstRedStops(Host):  # the real Host.audits over REAL processes: a slow audit with a child, and a red one
            def __init__(self, mark: Path):
                super().__init__(root)
                self.mark = mark

            def audit_prerequisites(self):
                return ()

            def audit_commands(self):
                slow = ("import pathlib, subprocess, sys; "
                        "p = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(3600)']); "
                        f"pathlib.Path({str(self.mark)!r}).write_text(str(p.pid), encoding='utf-8'); p.wait()")
                red = (f"import pathlib, time\nm = pathlib.Path({str(self.mark)!r})\n"
                       "while not m.exists():\n    time.sleep(0.05)\nraise SystemExit(1)")
                return (("SLOW", ["-c", slow]), ("QUICK-RED", ["-c", red]))
        mark = Path(tempfile.mkdtemp(prefix="first-red-")) / "grandchild.pid"
        heard: list[str] = []
        reasons = FirstRedStops(mark).audits(_no_slot, heard.append)
        grandchild = int(mark.read_text(encoding="utf-8"))
        while _pid_alive(grandchild):  # termination is asynchronous; a tree that never dies is the hang detector's
            time.sleep(0.05)
        arm("the first RED audit stops every audit still running, its whole process tree included, and the stopped one "
            "is reported STOPPED, never RED (kb/Work PB3061)",
            reasons == ["QUICK-RED RED"] and any(s.startswith("--- audit SLOW: STOPPED") for s in heard)
            and any("1 audit(s) still running stopped" in s for s in heard), f"{reasons} {heard}")
        o, h = gate("post-build-crash", plant={"post-build-crash"})
        arm("a defect in work beside the critical path is a named RED with its traceback, and the legs still ran",
            o.verdict == "RED" and o.exit_code == 1 and "POST BUILD SELF TESTS FAILED (RuntimeError" in o.line
            and len(o.runs) == 4 and said("planted: a defect in the post-build self-tests' runner"), o.line)
        o, h = gate("one-shape", scope="leg1", plant={"red-leg1"})
        arm("ONE SHAPE (kb/Work PB3022): every gate takes a slot, runs its audits before it and stops at a red in "
            "leg 1; there is no mode",
            h.slots_taken == 1 and h.events[:3] == ["audits", "fetch", "slot"] and o.verdict == f"{LEG1_ONLY}: RED"
            and "mode" not in o.line and "mode" not in json.loads(next((root / "one-shape" / "runs").iterdir())
                                                                  .joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
            and f"leg 2 left to {LEG2_OWNER}" in o.line, o.line)
        o, _ = gate("crash", plant={"crash"})
        arm("a defect in the driver still ends in ONE verdict line (NOT RUN [driver], exit 2) — the callers block on "
            "it — which the one verdict reader classifies BRANCH: the driver is the tree's own code (review R2-1)",
            o.verdict == "NOT RUN [driver]" and o.exit_code == 2 and sink[-1] == o.line and "RuntimeError" in o.line
            and read_verdict(o.line[len(VERDICT):]) == not_run("driver")
            and said("Traceback"), o.line)
        o, _ = gate("build", plant={"build"})
        arm("a failed build is BUILD FAILED, exit 1, and no leg runs",
            o.verdict == "BUILD FAILED" and o.exit_code == 1 and not o.runs, o.line)

        # Timings and reds come from the newest EARLIER run that reached a verdict; five runs are kept.
        d = root / "history"
        (d / "runs").mkdir(parents=True)
        for i in range(7):
            p = d / "runs" / f"2026010{i}T000000Z-00000{i}"
            p.mkdir()
            (p / VERDICT_FILE).write_text("{}", encoding="utf-8", newline="\n")
            synthetic_trx(p / "leg-1-Unit.trx", [("N.Unit.A", ["Passed"])])
        (d / "runs" / "20260109T000000Z-000009").mkdir()  # newest, but it never reached a verdict
        g = Gate(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append)
        prev = g.previous_run(d / "runs" / "20260110T000000Z-00000a")
        g._prune(d / "runs" / "20260109T000000Z-000009")
        kept = sorted(p.name for p in (d / "runs").iterdir())
        arm("the previous run is the newest one holding a verdict, and only the newest five runs are kept",
            prev is not None and prev.name == "20260106T000000Z-000006" and len(kept) == KEEP_RUNS
            and kept[-1] == "20260109T000000Z-000009", f"{prev} {kept}")

        # A second gate in the SAME worktree is refused at once, naming the holder (a real OS lock, two processes).
        d = root / "second"
        d.mkdir()
        lock, go = d / "gate.lock", d / "go"
        holder = subprocess.Popen([sys.executable, "-c", "\n".join([
            "import os, sys, time, pathlib",
            f"sys.path.insert(0, {str(REPO / 'scripts')!r})",
            "from gate_slot import ExclusiveLock",
            f"l = ExclusiveLock.try_take(pathlib.Path({str(lock)!r}), 'first gate (pid %d)' % os.getpid())",
            "print('held' if l else 'NOT HELD', flush=True)",
            f"g = pathlib.Path({str(go)!r})",
            "t = time.monotonic()",
            "while not g.exists() and time.monotonic() - t < 60: time.sleep(0.02)"])],
            stdout=subprocess.PIPE, text=True, cwd=root.parent)
        try:
            first = holder.stdout.readline().strip()
            host2 = FakeHost(d, lock)
            sink.clear()
            o = Gate(host2, d / "runs", say=sink.append).run()
        finally:
            go.touch()
            holder.wait(timeout=30)
        arm("a second gate in the same worktree is REFUSED at once, naming the holder's pid, and takes no slot: its "
            "verdict is BUSY with its own exit code, which the one verdict reader reads as BUSY, never RED (kb/Work "
            "PB3527)",
            first == "held" and o.verdict == BUSY and o.exit_code == EXIT_BUSY and f"pid {holder.pid}" in o.line
            and host2.slots_taken == 0 and not (d / "runs").exists()
            and read_verdict(o.line[len(VERDICT):]) == BUSY_READING, o.line)
        arm("the one verdict reader: GREEN and LEG 1 ONLY: GREEN pass; RED, RED/INCOMPLETE, LEG 1 ONLY: RED and BUILD "
            "FAILED are RED; NOT RUN (this driver's and the Linux gate's) is NOT RUN with its cause's class, never RED "
            "(kb/Work PB3526), and one naming no known cause is UNCLASSIFIED, BRANCH; a Linux gate's GREEN (legs ...) "
            "passes",
            [read_verdict(v) for v in ("GREEN — x ===", f"{LEG1_ONLY}: GREEN — x", "RED — x", "RED/INCOMPLETE — x",
                                       f"{LEG1_ONLY}: RED — x", "BUILD FAILED — x", "NOT RUN [settings] — x",
                                       "NOT RUN [sdk] (no .NET SDK in this Linux environment) ===",
                                       "NOT RUN [driver] — x", "NOT RUN — x", "NOT RUN [nosuch] — x", "NOT RUNNING",
                                       "GREEN (legs unit) ===", "GREENISH")]
            == [GREEN_READING, GREEN_READING, RED_READING, RED_READING, RED_READING, RED_READING, not_run("settings"),
                not_run("sdk"), not_run("driver"), Reading(NOT_RUN, UNCLASSIFIED), Reading(NOT_RUN, UNCLASSIFIED),
                RED_READING, GREEN_READING, RED_READING]
            and [c.tag for c in NOT_RUN_CAUSES.values() if c.carried == BRANCH]
            == ["driver", "population", "arguments", "verdict", "unslotted", "unclassified"])
        # Every printer of a NOT RUN verdict names its cause (the classification is read, never guessed from prose):
        # the gate scripts' verdict lines and this driver's own.
        printers = {f: (REPO / "scripts" / f).read_text(encoding="utf-8")
                    for f in ("build-local.ps1", "build-local.sh", "linux-gate.sh")}
        untagged, unknown = [], []
        for f, text in printers.items():
            for ln in text.splitlines():
                at = ln.find("GATE: NOT RUN")
                if at < 0 or ln.lstrip().startswith("#"):
                    continue
                tag = _NOT_RUN_TAG.match(ln[at + len("GATE: "):])
                (unknown if tag and tag.group(1) not in NOT_RUN_CAUSES else untagged if not tag else []).append(
                    f"{f}: {ln.strip()[:90]}")
        own = re.findall(r'_finish\(out, \w+, (NOT_RUN[^,]*),', (REPO / "scripts" / "run_gate_legs.py").read_text(
            encoding="utf-8"))
        arm("every NOT RUN verdict a gate prints names a cause NOT_RUN_CAUSES classifies: build-local.ps1, "
            "build-local.sh and linux-gate.sh, and this driver's own (_finish with NOT_RUN_CAUSES[...].verdict)",
            all(printers.values()) and not untagged and not unknown and own
            and all(v.startswith("NOT_RUN_CAUSES[") for v in own), f"untagged={untagged} unknown={unknown} own={own}")

        # The lock is per WORKTREE: a linked worktree's git dir is its own.
        scratch = ScratchRepo.init(root / "repo")
        scratch.commit("self-test", allow_empty=True)
        repo, linked = scratch.path, scratch.worktree(root / "linked", detach=True).path
        a, b = Host(repo).lock_path(), Host(linked).lock_path()
        arm("the gate lock is per worktree: the main checkout and a linked worktree lock different files",
            a != b and a.parent == (repo / ".git").resolve() and b.parent.parent == (repo / ".git" / "worktrees").resolve(),
            f"{a} vs {b}")

        # THE MEASURED TREE (kb/Work PB3536): the working tree's content, through a private index.
        (repo / ".gitignore").write_text("ignored.txt\n", encoding="utf-8", newline="\n")
        (repo / "tracked.txt").write_text("one\n", encoding="utf-8", newline="\n")
        scratch.commit("tracked")
        real = Host(repo)
        index_before = (repo / ".git" / "index").read_bytes()
        t0 = real.measured_tree()
        (repo / "tracked.txt").write_text("two\n", encoding="utf-8", newline="\n")
        t_edit = real.measured_tree()
        (repo / "tracked.txt").write_text("one\n", encoding="utf-8", newline="\n")
        (repo / "untracked.txt").write_text("new\n", encoding="utf-8", newline="\n")
        t_new = real.measured_tree()
        (repo / "untracked.txt").unlink()
        (repo / "ignored.txt").write_text("noise\n", encoding="utf-8", newline="\n")
        t_ignored = real.measured_tree()
        committed = real.git("rev-parse", "HEAD^{tree}")
        arm("the measured tree is the working tree's content: an edit or an untracked file changes it, an ignored file "
            "does not, a clean tree measures as its commit's tree, and the worktree's index is untouched",
            t0 == committed and t_edit not in (None, t0) and t_new not in (None, t0, t_edit) and t_ignored == t0
            and (repo / ".git" / "index").read_bytes() == index_before, f"{t0} {t_edit} {t_new} {t_ignored} {committed}")
        # RACY GIT, pinned rather than left to the clock: the arm above passed alone and failed under a loaded gate,
        # because an edit in the same second as the index's cached entry kept that entry's size and mtime. Here the
        # entry, the index file and the edit all carry one past whole second, so the real index holds the entry
        # racily clean (git re-reads it by content: the porcelain shows the edit) and the private copy must too.
        past = (int(time.time()) - 60) * 1_000_000_000
        tracked, real_index = repo / "tracked.txt", repo / ".git" / "index"
        os.utime(tracked, ns=(past, past))
        scratch.git("add", "--", "tracked.txt")
        os.utime(real_index, ns=(past, past))
        t_racy0 = real.measured_tree()
        tracked.write_text("six\n", encoding="utf-8", newline="\n")      # the same size as "one\n"
        os.utime(tracked, ns=(past, past))
        seen = scratch.git("--no-optional-locks", "status", "--porcelain", "--", "tracked.txt")
        t_racy = real.measured_tree()
        tracked.write_text("one\n", encoding="utf-8", newline="\n")
        arm("an edit in the index's own second (racy git) changes the measured tree: the private index keeps the "
            "real index's mtime, so git re-reads a racily clean entry by content",
            t_racy0 == committed and seen.strip() == "M tracked.txt" and t_racy not in (None, committed),
            f"{t_racy0} {t_racy} {committed} {seen!r}")
        # coord.NEVER_WORK (review R1-2 of wave 1152): every harness worktree carries an edited TRACKED
        # .claude/settings.local.json; with it (edited on disk, then also staged), an untracked STATUS.md and a
        # TestResults/ beside a clean commit, the measured tree is still the commit's own.
        (repo / "ignored.txt").unlink()
        settings = repo / ".claude" / "settings.local.json"
        settings.parent.mkdir()
        settings.write_text("{}\n", encoding="utf-8", newline="\n")
        # forced: a user's global excludes may name it (tracked in the repository all the same, as here)
        scratch.git("add", "-f", "--", ".claude/settings.local.json")
        scratch.git("commit", "-q", "-m", "harness settings")
        committed = real.git("rev-parse", "HEAD^{tree}")
        settings.write_text('{"permissions": {}}\n', encoding="utf-8", newline="\n")
        # STATUS.md is IGNORED, as in this repository (git refuses an `add` pathspec naming an ignored path, even an
        # exclusion: the first form of this exclusion measured no tree at all in a real worktree)
        (repo / ".git" / "info").mkdir(exist_ok=True)
        (repo / ".git" / "info" / "exclude").write_text("STATUS.md\n", encoding="utf-8", newline="\n")
        (repo / "STATUS.md").write_text("STATUS-AT: x\n", encoding="utf-8", newline="\n")
        (repo / "TestResults" / "build-local").mkdir(parents=True)
        (repo / "TestResults" / "build-local" / "gate.log").write_text("x\n", encoding="utf-8", newline="\n")
        t_harness = real.measured_tree()
        scratch.git("add", "--", str(settings))
        t_staged = real.measured_tree()
        (repo / "tracked.txt").write_text("three\n", encoding="utf-8", newline="\n")
        t_work = real.measured_tree()
        scratch.git("reset", "-q", "--hard")
        for p in (repo / "STATUS.md", repo / "TestResults"):
            shutil.rmtree(p) if p.is_dir() else p.unlink()
        arm("the measured tree leaves out coord.NEVER_WORK: a harness-edited tracked settings file (on disk, or staged), "
            "STATUS.md and TestResults/ measure as the commit's own tree, and an edit of real work still changes it",
            t_harness == committed and t_staged == committed and t_work not in (None, committed),
            f"{t_harness} {t_staged} {t_work} {committed}")

        # THE GATE'S REUSE RULE (kb/Work PB3536): a re-gate of an unchanged tree answers from the recorded GREEN.
        def regate(d: Path, **fake) -> tuple[Outcome, FakeHost]:
            fake.setdefault("scope", "leg1")
            host = FakeHost(d, d / "gate.lock", **fake)
            sink.clear()
            return Gate(host, d / "runs", say=sink.append).run(), host

        d = root / "reuse"
        d.mkdir()
        first, _ = regate(d)
        again, h2 = regate(d)
        third, h3 = regate(d)
        # the record of the second gate, read by its own name
        rec2 = read_record(d / "runs" / again.line.rsplit("(run ", 1)[1].split(")", 1)[0])
        # THE ORDER OF RUNS IS THEIR NUMBERS', NEVER THE CLOCK'S (merge-group run 39888287569: on a CI runner three
        # runs started in one microsecond and the stamp-and-nonce names sorted by nonce). The clock here is STOPPED for
        # twenty runs and then TURNED BACK, so a clock-ordered name is red on every host.
        (root / "names").mkdir()
        (root / "names" / "20261022T225440Z-2797b8").mkdir()   # a run named before the numbers
        ticks = iter([dt.datetime(2026, 10, 23, 2, 21, 9, 187360, dt.timezone.utc)] * 20
                     + [dt.datetime(2026, 10, 23, 1, 0, 0, tzinfo=dt.timezone.utc)] * 3)
        ordered = Gate(FakeHost(d, d / "gate.lock"), root / "names", say=sink.append, now=lambda: next(ticks))
        made = [ordered.new_run().name for _ in range(23)]
        arm("runs are ordered by the number each takes at its start, never by the clock: twenty started in one "
            "stopped instant and three more after the clock turned back are the newest, in the order they started, "
            "and a run named before the numbers is the oldest (the plan's newest earlier run, the reuse rule's, the "
            "pruning's)",
            [p.name for p in ordered._runs()] == ["20261022T225440Z-2797b8", *made]
            and [run_order(n)[0] for n in made] == list(range(1, 24)) and all(RUN_DIR.fullmatch(n) for n in made)
            and made[-1].endswith("-20261023T010000Z"), str(made))
        # two gates starting AT ONCE never take one number: eight threads, five runs each, on the stopped clock
        racing = Gate(FakeHost(d, d / "gate.lock"), root / "racing", say=sink.append,
                      now=lambda: dt.datetime(2026, 10, 23, 2, 21, 9, tzinfo=dt.timezone.utc))
        start = threading.Barrier(8)
        took: dict[int, list[int]] = {}

        def race(i: int) -> None:
            start.wait()
            took[i] = [run_order(racing.new_run().name)[0] for _ in range(5)]
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
            list(pool.map(race, range(8)))
        numbers = sorted(n for ns in took.values() for n in ns)
        arm("gates starting at once each take their own number, under the run root's lock, and each one's runs are "
            "ordered as it made them",
            numbers == list(range(1, 41)) and all(ns == sorted(ns) for ns in took.values())
            and len(racing._runs()) == 40, str(took))
        arm("a re-gate of an unchanged tree answers from the recorded GREEN: no audit, slot, build or leg runs, the "
            "verdict line says REUSED and names the run that measured it, exit 0",
            first.verdict == f"{LEG1_ONLY}: GREEN" and again.verdict == first.verdict and again.exit_code == 0
            and h2.events == [] and h2.slots_taken == 0 and not again.runs and "REUSED (kb/Work PB3536)" in again.line
            and again.reused_from is not None and again.reused_from in again.line
            and rec2 is not None and rec2["reused_from"] == again.reused_from and rec2["exit_code"] == 0,
            f"{again.line} {h2.events}")
        arm("a chain of reuses names the run that MEASURED the tree and its own verdict, never a reuse of a reuse",
            third.reused_from == again.reused_from and third.line.count("REUSED") == 1 and h3.events == [], third.line)
        changed, hc = regate(d, trees=["tree-2"])
        arm("a re-gate of a CHANGED tree runs the gate (audits, slot, build, legs)",
            "REUSED" not in changed.line and hc.events[:4] == ["audits", "fetch", "slot", "build"]
            and changed.verdict == f"{LEG1_ONLY}: GREEN", f"{changed.line} {hc.events}")
        d = root / "reuse-red"
        d.mkdir()
        regate(d, plant={"red-leg1"})
        red_again, hr = regate(d)
        arm("a RED is never reused: the same tree is gated again",
            "REUSED" not in red_again.line and "build" in hr.events, red_again.line)
        d = root / "reuse-scope"
        d.mkdir()
        regate(d)
        whole_after_leg1, hw = regate(d, scope="whole")
        leg1_after_whole, hl = regate(d)
        arm("a LEG 1 ONLY GREEN never answers a gate under the scope whole; a plain GREEN answers the scope leg1",
            "REUSED" not in whole_after_leg1.line and "build" in hw.events and whole_after_leg1.verdict == "GREEN"
            and "REUSED" in leg1_after_whole.line and leg1_after_whole.verdict == "GREEN" and hl.events == [],
            f"{whole_after_leg1.line} | {leg1_after_whole.line}")
        d = root / "reuse-moved"
        d.mkdir()
        edited, _ = regate(d, trees=["tree-1", "tree-edited"])
        after_edit, he = regate(d)
        rebased, hb = regate(d, base="1" * 40)
        arm("a gate whose tree changed WHILE it ran is never reused, and another base (a landing's merged main) runs",
            edited.tree == "tree-1" and edited.tree_end == "tree-edited" and "REUSED" not in after_edit.line
            and "build" in he.events and "REUSED" not in rebased.line and "build" in hb.events,
            f"{after_edit.line} | {rebased.line}")

        # THE ONE WAY TO WAIT FOR A GATE (kb/Work PB3536): its run directory's verdict file.
        d = root / "await"
        d.mkdir()
        done = d / "done"
        done.mkdir()
        (done / VERDICT_FILE).write_text(json.dumps({"verdict": "RED", "line": f"{VERDICT}RED — planted ===",
                                                     "exit_code": 1}), encoding="utf-8", newline="\n")
        live = d / "live"
        live.mkdir()
        write_pid(live, os.getpid())  # this process: alive for the whole arm
        dead_proc = subprocess.Popen([sys.executable, "-c", "pass"])
        dead_proc.wait()
        dead = d / "dead"
        dead.mkdir()
        write_pid(dead, dead_proc.pid)
        got = {n: watch(d / n, 0) for n in ("done", "live", "dead", "missing")}
        arm("--await reads the verdict file (its line and exit code), PENDING (its own exit, never BUSY's) while the "
            "gate's process runs, NOT RUN [verdict] (exit 2, BRANCH) when it DIED without a verdict, NOT RUN [run] "
            "(exit 2, ENVIRONMENT) with no run directory",
            got["done"] == (1, f"{VERDICT}RED — planted ===")
            and got["live"][0] == AWAIT_PENDING_EXIT != EXIT_BUSY and got["live"][1].startswith(AWAIT_PENDING)
            and got["dead"][0] == 2 and read_verdict(got["dead"][1][len(VERDICT):]) == not_run("verdict")
            and "DIED" in got["dead"][1]
            and got["missing"][0] == 2 and read_verdict(got["missing"][1][len(VERDICT):]) == not_run("run"), str(got))
        # A handed run's name (review R1-1 of wave 1152): an agent returns `TestResults/build-local/<run>`, relative to
        # ITS worktree, and the wave and the planner read it from the main checkout.
        cwd = Path.cwd()
        os.chdir(live)   # a reader elsewhere: the handed name means the worktree's run, never one of the reader's own
        try:
            from_elsewhere = [watch(resolve_run(Path("await") / "done", w), 0)[0] for w in (root, None)]
            os.chdir(d)
            relative_here = watch(resolve_run("done", None), 0)[0]
        finally:
            os.chdir(cwd)
        arm("a relative run name resolves against the worktree that gated it, wherever the reader runs; an absolute one "
            "is itself; with no worktree, the current directory",
            resolve_run(done, root / "elsewhere") == done and from_elsewhere == [1, 2] and relative_here == 1
            and resolve_run("TestResults/build-local/r", root / "wt") == root / "wt" / "TestResults" / "build-local" / "r",
            f"{from_elsewhere} {relative_here}")
        # a record from before schema 3 carries no exit code: the one verdict reader derives it
        arm("a record with no exit code takes it from its verdict through the one verdict reader",
            [_record_exit({"verdict": v}) for v in (f"{LEG1_ONLY}: GREEN", "GREEN", BUSY, "NOT RUN [driver]", "RED")]
            == [0, 0, EXIT_BUSY, 2, 1])

        # The follower and the detached gate, as real processes: a planted gate that prints, waits for a go file,
        # writes its verdict file and exits 1.
        def planted_gate(run: Path, go: Path | None = None) -> list[str]:
            wait = f"g = pathlib.Path({str(go)!r})\nwhile not g.exists(): time.sleep(0.05)\n" if go else ""
            return [sys.executable, "-c", "import json, pathlib, time\n"
                    f"r = pathlib.Path({str(run)!r})\nprint('PLANTED-GATE-OUTPUT', flush=True)\n{wait}"
                    f"line = {VERDICT!r} + 'LEG 1 ONLY (batched gating, PB2515): RED — planted ==='\n"
                    "(r / 'verdict.json').write_text(json.dumps({'verdict': 'x', 'line': line, 'exit_code': 1}))\n"
                    "print(line, flush=True)\n"]
        d = root / "follow"
        d.mkdir()
        heard: list[str] = []
        runs = iter([d / "r1"])

        def make() -> Path:
            r = next(runs)
            r.mkdir()
            return r
        code = launch_and_follow(make, planted_gate, heard.append)
        arm("build-local's gate runs DETACHED and is followed: its output echoed, the caller's exit code the gate's, "
            "the verdict line last",
            code == 1 and "PLANTED-GATE-OUTPUT" in heard and heard[-1].startswith(f"{VERDICT}LEG 1 ONLY")
            and read_pid(d / "r1") is not None, str(heard))
        refused: list[str] = []

        def unmade() -> Path:
            raise TimeoutError("the run root's lock stayed held")
        code = launch_and_follow(unmade, planted_gate, refused.append)
        arm("a run directory that cannot be made (an unwritable run root, a hung holder of its lock) still ends in a "
            "verdict line, NOT RUN [launch], exit 2, and starts no gate",
            code == 2 and refused[-1].startswith(f"{VERDICT}{NOT_RUN_CAUSES['launch'].verdict} — ")
            and "PLANTED-GATE-OUTPUT" not in refused, str(refused))
        # a follower killed mid-gate (an agent's return kills its background commands' tree) leaves the gate running:
        # its verdict lands in the run directory, where --await reads it
        go, run2 = d / "go", d / "r2"
        follower = subprocess.Popen([sys.executable, "-c", "\n".join([
            "import pathlib, sys",
            f"sys.path.insert(0, {str(REPO / 'scripts')!r})",
            "import run_gate_legs as g",
            f"run = pathlib.Path({str(run2)!r})",
            "def make():",
            "    run.mkdir()",
            "    return run",
            f"sys.exit(g.launch_and_follow(make, lambda r: {planted_gate(run2, go)!r}, print))"])],
            stdout=subprocess.DEVNULL, start_new_session=os.name != "nt")
        t = time.monotonic()
        while read_pid(run2) is None and time.monotonic() - t < 120:  # a hang guard: the arm names what it never saw
            time.sleep(0.05)
        gate_pid = read_pid(run2)
        _stop_tree(follower)
        follower.wait()
        survived = gate_pid is not None and _pid_alive(gate_pid)
        go.touch()
        handed = watch(run2, 120)
        arm("a detached gate OUTLIVES its follower's tree kill and its verdict is read from the run directory",
            survived and handed == (1, f"{VERDICT}LEG 1 ONLY (batched gating, PB2515): RED — planted ==="),
            f"survived {survived}; {handed}")
    finally:
        for dirpath, _, files in os.walk(root):  # git writes read-only objects, which rmtree cannot delete on Windows
            for f in files:
                os.chmod(os.path.join(dirpath, f), 0o666)
        shutil.rmtree(root, ignore_errors=True)

    failed = [n for n, ok, _ in results if not ok]
    print(f"=== run_gate_legs SELF-TEST: {'PASS' if not failed else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(failed)}" if failed else "") + ") ===")
    return 0 if not failed else 1


def print_flush(line: str) -> None:
    print(line, flush=True)


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001 — a stream without reconfigure keeps its encoding; the gate still runs
        pass
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base", help="the change's cut point (default: git merge-base HEAD origin/main)")
    ap.add_argument("--await", dest="await_run", metavar="RUN", help="read a gate's verdict from its run directory: "
                    f"the line and exit code; NOT RUN [verdict] (2) when its process DIED without one, PENDING "
                    f"({AWAIT_PENDING_EXIT}) when --wait-s passes first (kb/Work PB3536)")
    ap.add_argument("--worktree", metavar="DIR", help="--await: the worktree that gated RUN; a relative RUN is its run "
                    "(the form an agent returns), never this checkout's")
    ap.add_argument("--wait-s", type=float, default=AWAIT_WAIT_S, help=f"--await: how long to wait for the verdict "
                    f"(default {AWAIT_WAIT_S}, under a tool call's 10-minute limit)")
    ap.add_argument("--run", metavar="RUN", help="(the detached gate itself, started by build-local; not by hand)")
    ap.add_argument("--self-test", action="store_true", help="fire every arm on planted inputs (DESIGN §3.14.4 (3))")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.await_run:
        code, line = watch(resolve_run(args.await_run, args.worktree), args.wait_s)
        print(line, flush=True)
        return code
    if args.run:
        return Gate(Host(), REPO / RUN_ROOT, base=args.base, run_dir=Path(args.run)).run().exit_code
    gate = Gate(Host(), REPO / RUN_ROOT, base=args.base)
    return launch_and_follow(gate.new_run, lambda run: [sys.executable, str(Path(__file__).resolve()), "--run",
                                                        str(run), *(["--base", args.base] if args.base else [])],
                             print_flush)


if __name__ == "__main__":
    sys.exit(main())
