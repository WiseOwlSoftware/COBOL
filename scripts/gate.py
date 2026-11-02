#!/usr/bin/env python3
"""gate.py — THE GATE: the one local gate (kb/Work PB4328, step 5 of PB4323; PB3022;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.1, §3.14.3, §3.14.6, §3.17.3).

    python scripts/gate.py [--base <sha>]          # the gate, DETACHED, its output followed to the verdict
    python scripts/gate.py --await <run> [--worktree W] [--wait-s N]  # a gate's verdict, from its run dir
    python scripts/gate.py --audit-shape        # the ONE SHAPE audit: no live line names a gate mode (kb/Work PB3022)
    python scripts/gate.py --self-test
    (python scripts/gate.py --run <run> [--base <sha>] is the detached gate itself; never run it by hand)

Called by `scripts/build-local.ps1` and `scripts/build-local.sh`, which only set the process priority.

⛔ THE POPULATION IS THE SELECTION (owner decision 1 of PB4323, 2026-10-31; kb/Work PB4328). A change that a test can read
runs the WHOLE Unit, Conformance and Characterization populations on this host: no plan, no tiers, no impact map, no leg
2. The order inside each assembly is the test framework's (tests/_shared/GateLegs.cs `GateOrderer`): the test classes
the previous run of this worktree, or the merge-group run that bounced its pull request, saw red go first, then the
rest longest first by the committed weights file (tests/_shared/test-weights.json). Ordering changes WHEN a case runs,
never WHETHER (owner, 2026-09-28: "order, don't skip"), and the population check proves it. CI's merge group is the
first run of the merged tree, of Linux, of Release and of GitHub's runners (PB4323 §5.1).

ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; §3.17.3), in this order:
  1. the worktree's GATE LOCK (`<worktree git dir>/cobol-gate.lock`): a second gate in the same worktree is REFUSED at
     once, naming the holder — its build would overwrite the binaries under this gate's test hosts — with the verdict
     BUSY (exit 3), never RED: it measured nothing (kb/Work PB3527);
  2. a fresh RUN DIRECTORY `TestResults/build-local/<n>-<UTC stamp>Z/` (n = the run's number in its run root, taken under
     `runs.lock`, so start order never depends on the clock) holding the listings, every trx and log, the first-classes
     file and the verdict file; five are kept;
  3. the toolchain (toolchain.env's versions, kb/Work PB4007), then the change's LANE (scripts/inert_lane.py: inert;
     tooling, a change no compiler test or build reads, kb/Work PB4395; or a code lane with or without TOOLING touched),
     then the audits (the one list below, run CONCURRENTLY; when the change touches tooling one of them is every script
     self-test, found by scripts/self_tests.py — kb/Work PB2563, PB4326), started BESIDE everything after them: the inert
     and the tooling lanes join them at once (they are the whole gate: `_inert`, `_tooling`); a code lane runs
     them beside the corpus fetch, the slot wait, the build and the test hosts, and a red one STOPS the hosts
     (`Host.stop_hosts`) and ends the gate RED (kb/Work PB2523, PB4326). The GnuCOBOL corpus, by the ONE rule
     scripts/external_corpus.py (kb/Work PB2611), is fetched BEFORE the slot (kb/Work PB2524);
  4. the gate slot, always after the lock, so no two gates can wait on each other in a cycle; then the solution build;
  5. the three assemblies' WHOLE populations CONCURRENTLY, each one `dotnet test --no-build` host in a scrubbed
     environment, printed through scripts/test_leg_report.py; beside them each assembly's `--list-tests` (scrubbed), and,
     only when the change touches tooling, the self-test that reads the built assemblies (`self_tests.py --built`; a red
     one makes the gate RED — kb/Work PB4326);
  6. the POPULATION CHECK (scripts/test_population.py) per assembly, its trx against its listing, by name; then the
     verdict line: `=== BUILD-LOCAL GATE: GREEN — Conformance 30,112/30,112 · Unit … cases ran (skipped 0) · … ===`.
It is GREEN only when every host is green and every population is exact.

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
    edit of them: `Host.measured_tree`) at its start and again at its end, the base its lane was decided against and its
    exit code. A gate whose tree and base equal those of an earlier GREEN run in this worktree whose tree did not change
    while it ran answers from that record WITHOUT running anything, and says so in its verdict line (`… GREEN — REUSED:
    …`, naming the run that measured it) — the self-test reuse rule (kb/Work PB2914, PB3022) extended to the gate. A RED
    is never reused.

Exit codes: 0 GREEN (reused or run) · 1 RED (or the build failed) · 2 NOT RUN [<cause>] (the population could not be
listed, the driver failed, the gate's process DIED with no verdict (`--await`, or the follower): BRANCH, the tree's own
code; the toolchain is not toolchain.env's: ENVIRONMENT; NOT_RUN_CAUSES classifies every cause once) · 3 BUSY (another
gate holds this worktree's gate lock: nothing ran and nothing is red; wait for it and re-run, or `--await` its run,
kb/Work PB3527) · 4 PENDING (`--await`: no verdict within `--wait-s`). `read_verdict` is the ONE reader of a verdict
line's text (GREEN, BUSY, NOT RUN with its cause, or RED); enqueue.py's landing reads through it, and so does `--await`
for a record's exit code.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import datetime as dt
import json
import os
import re
import shutil
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
import external_corpus  # noqa: E402
import gate_growth  # noqa: E402  — THE growth trigger's sample: one per GREEN code-lane gate (kb/Work PB4333)
import inert_lane  # noqa: E402  — THE classification of a change no test reads (kb/Work PB2878)
import toolchain  # noqa: E402  — THE reader of toolchain.env, the versions CI and every host run (kb/Work PB4007)
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT  # noqa: E402
from gate_slot import ExclusiveLock, GateSlots, Slot  # noqa: E402
from detached import kill_tree, pid_alive, spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536), THE one tree kill (PB4072), THE one liveness probe (PB4008)
sys.path.append(str(REPO / "scripts" / "orchestrator"))  # appended: no module of it shadows one of scripts/
from coord import NEVER_WORK  # noqa: E402  — THE paths that are never work, so never part of a measured tree
import queue_facts  # noqa: E402  — THE one reader of GitHub's facts: a merge-queue bounce's reds (kb/Work PB3644)
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, list_population, red_classes, scrubbed_env,
                             synthetic_listing, synthetic_trx)
from standard_streams import ensure_utf8  # noqa: E402

VERDICT = "=== BUILD-LOCAL GATE: "
#: ⛔ THE ONE ORDERING CHANNEL TO A TEST HOST (kb/Work PB4328): the path of a file of test class names, one per line, that
#: the host's orderer runs FIRST (tests/_shared/GateLegs.cs `GateOrderer.FirstVariable`, the same name). It orders, it
#: never selects: a class it names that the assembly lacks is ignored, every other class still runs, and the population
#: check proves both. Its prefix is a scrubbed one (test_population.is_scrubbed), so only this gate hands it to a host.
FIRST_CLASSES = "WISEOWL_COBOL_GATE_FIRST"
#: Where a merge-queue BOUNCE's test results land (kb/Work PB3644): `queue_facts.fetch_bounce_results`, the one GitHub
#: reader, downloads the failed merge-group run's trx files of this branch's pull request here, one subdirectory per
#: artifact, and their red classes go first like the previous run's. No tool WRITES a trx file here, so it is no row.
CI_BOUNCE_RESULTS = Path("TestResults/ci-bounce")
RUN_ROOT = Path("TestResults") / "build-local"
#: A run directory's name: `<n>-<UTC start, to the second>Z`, where n is the run's NUMBER in its run root, one more
#: than every run there, taken under the run root's lock (`Gate.new_run`). THE ORDER OF RUNS IS THE ORDER OF THEIR
#: NUMBERS (`run_order`), and "the newest earlier run" reads it: the first classes, the reuse rule, the
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
#: NOT_RUN_CAUSES, which classifies it ONCE (kb/Work PB3526; review R2-1 of wave 1150). The landing's merge of main into
#: the branch (merge_drivers.merge: a clean merge, then abi_pin.settle, a build and a measuring test on the merged tree) is
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
    NotRunCause("merge-blocked", ENVIRONMENT, "the landing's clean merge of main (merge_drivers.merge) could not be "
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


#: A run directory's other fixed names (kb/Work PB3536): the gate process's pid, and its output when it runs detached.
PID_FILE = "gate.pid"
#: ⛔ THE STARTING STATE OF A DETACHED GATE (kb/Work PB3707). A run directory exists before its child process has taken the
#: gate lock and written `gate.pid` (interpreter start, imports), and in that window neither the lock nor the pid file says
#: a gate is coming. The launcher therefore records the child it spawned in `gate.spawn` (the child's pid), and the
#: child's first act under the lock (`write_pid`) replaces it with `gate.pid`. A reader (`gate_phase`) decides a run with
#: no pid by THREE facts: the run is younger than START_WINDOW_S (its name's own stamp), and the spawn record, when it
#: has one, names a process that is alive. A pid alone is never trusted (Windows reuses them, kb/Work PB3662 R1-1): the
#: window bounds a reused one to START_WINDOW_S of a wrong "starting".
SPAWN_FILE = "gate.spawn"
#: The longest a spawned gate may take to reach its lock (interpreter start and imports; measured in single seconds, even
#: with three builds running). Past it, a run with no pid and no verdict is a gate that DIED.
START_WINDOW_S = 60.0
RUNNING, STARTING, DIED = "running", "starting", "died"
#: What a detached gate's lock label says before the run directory it writes (`Gate.run`; `live_gate` reads it back).
RUN_LABEL = ", run "
LOG_FILE = "gate.log"
#: `--await`'s answer while the gate still runs, and its default window (under a tool call's 10-minute limit).
AWAIT_PENDING = "=== GATE AWAIT: PENDING"
AWAIT_PENDING_EXIT = 4
AWAIT_WAIT_S = 540
#: The verdict record's schema: 3 adds the measured head, tree (start and end), base, exit code and reuse (PB3536); 4 adds
#: `head_tree` and `lane`, which the landing requires as evidence (PB3612; PB3660). The Linux half's `linux` field that
#: PB3612 also added is gone (kb/Work PB4327: the Linux half left the per-change gate); no landing reads it, so a record
#: that still carries it is read as before. Schema 5 (kb/Work PB4328): the gate runs the whole population, so a record
#: of schema 4 or below may be a `LEG 1 ONLY` one, and the reuse rule (`Gate.reusable`) never answers from one; it drops
#: the plan's, the scope's, the identity check's and the timings store's fields, which no reader read.
RECORD_SCHEMA = 5
#: The first schema a gate that ran the WHOLE population writes (kb/Work PB4328): the oldest record `Gate.reusable` answers from.
SCHEMA_WHOLE = 5
#: THE NOTE THAT INTRODUCED EACH RECORD SCHEMA, next to the number (kb/Work PB3717): `predates_landing` words "this gate
#: predates kb/Work <note>" from this table, never from a note id typed into the sentence, so the next bump of
#: `RECORD_SCHEMA` cannot leave a refusal naming the previous change. A bump adds its row here (the self-test fails
#: while `RECORD_SCHEMA` has none).
SCHEMA_NOTES = {3: "PB3536", 4: "PB3612", 5: "PB4328"}
#: THE OLDEST RECORD SCHEMA THE LANDING READS AS EVIDENCE, AND THE FIELDS IT REQUIRES (`enqueue.tested_locally`, kb/Work
#: PB3660). The record is written by the BRANCH's driver and read by main's lander, so a field the lander starts to require
#: strands every branch gated before it, and only the record itself can tell that from a dirty tree. Schema 3 was written
#: both before and after PB3612 added `head_tree` (the number was not bumped: PB3660), so for schema 3 the
#: fields themselves are the discriminator; from schema 4 on the number is. Whenever the lander requires a field a record
#: did not carry: raise `RECORD_SCHEMA` and `LANDABLE_SCHEMA` and add the field to `LANDING_FIELDS` (the self-test pins
#: RECORD_SCHEMA >= LANDABLE_SCHEMA, every landing field to a key `Gate._record` writes, and the lander's planted records to
#: the current schema).
LANDABLE_SCHEMA = 3
LANDING_FIELDS = ("head_tree",)
#: A stopped gate's remainder is named in full in the run directory; the console shows this many per assembly.
NOT_RUN_SHOWN = 10

#: ⛔ THE AUDITS, in order — THE list for the gate (build-local.ps1 and .sh used to carry a copy each); battery.sh PHASE
#: -1 and CI's `audits` job run the same scripts. A wrong § is the one defect no test can catch, and each costs about a
#: second. None needs a build or a test result, so they start FIRST and run BESIDE the build and the legs (kb/Work
#: PB4326, step 3 of PB4323: they were a serial valley ahead of the slot): a red one STOPS the test hosts and ends the gate
#: RED, and a full lane that finds one before the slot, the build or a leg has started never starts it (kb/Work PB2523 —
#: four of wave 1034's eight first-run reds were audit-only, each found after a whole ~6-min population). They run
#: OUTSIDE the gate slot (kb/Work PB2524): the slot rations builds and test hosts, not reading the tree.
#: They run CONCURRENTLY, each its own process with its output captured and printed whole (kb/Work PB2563), so the
#: phase costs its slowest audit, not their sum.
#: ⛔ AND THE FIRST RED STOPS THE REST (kb/Work PB3061): the moment one audit exits non-zero, every audit still running
#: is stopped — its whole process tree, so the self-test runner's children go with it — and reported STOPPED, never
#: RED. Wave 1062's agents A and B each had CITATIONS RED at 23 s and their verdict 5–7 min later, after the
#: self-tests. A stopped self-test run loses nothing: every self-test that finished has recorded its PASS, which the
#: re-gate reuses; the rest run then.
#: ⛔ SELF-TESTS is EVERY script self-test, found by discovery (scripts/self_tests.py; kb/Work PB2563) — never a self-test
#: named here by hand: this tuple once named five while eleven orchestrator self-tests ran in no gate at all. The few
#: that read the BUILT assemblies run beside the test hosts of a gate that builds (`self_tests.py --built`,
#: `Host.built_self_tests`). Both halves run ONLY WHEN THE CHANGE TOUCHES A SCRIPT (`TOOLING_ONLY`, `Lane.tooling`; a
#: compiler data file a self-test reads sets only `Lane.reads_tooling`, CI's flag, owner 2026-11-01, kb/Work PB4409).
AUDITS = (
    ("CITATIONS", ["scripts/spec/audit_code_citations.py", "--check"]),
    ("DOC CITATIONS", ["scripts/spec/audit_doc_citations.py", "--check"]),
    ("EVIDENCE SUPERSESSION", ["scripts/spec/audit_evidence_supersession.py", "--check"]),
    ("RENDERED VIEWS", ["scripts/rendered_views.py", "--check"]),
    ("WITNESS LOSS", ["scripts/spec/audit_witness_loss.py", "--check"]),
    ("RULE CATALOG", ["scripts/spec/extract_rule_catalog.py", "--check"]),
    ("SPEC CORRECTIONS", ["scripts/spec/verify_publishable.py"]),
    ("LINE ENDINGS", ["scripts/line_endings.py", "--check"]),
    # kb/Work PB735, PB2878: a byte-level fact about the committed tree, an audit so that no test reads every path.
    ("CONFLICT MARKERS", ["scripts/conflict_markers.py", "--check"]),
    # kb/Work PB2878, PB4395: THE lanes' classification against the tree (every entry names a tracked path; every script
    # the build or a compiler test is seen to read is CODE_READS), a question over the committed tree, so an audit.
    ("INERT LANE", ["scripts/inert_lane.py", "check"]),
    # kb/Work PB3022, PB4395: the gate has ONE shape, so no live line names a mode; a question over the committed tree.
    ("ONE SHAPE", ["scripts/gate.py", "--audit-shape"]),
    # kb/Work PB3575: every diagnostic code is COBOL####; the rename's own rules, asked as a question over the committed
    # tree, an audit for the reason CONFLICT MARKERS is one (PB2878).
    ("DIAGNOSTIC PREFIX", ["scripts/rename_diagnostic_prefix.py", "--check"]),
    # kb/Work PB3837: the code carries the product's name; the rename's own audit over the committed tree, for the same reason.
    ("RETIRED NAMES", ["scripts/rename_retired_names.py", "--check"]),
    ("SELF-TESTS", ["scripts/self_tests.py"]),
    # kb/Work PB1957 row 44, the CI invariant: CI's `audits` job ran these two and the gate did not, so train 1067's
    # PR 9 went RED in CI on a `blocked: true` the work register refuses, behind a GREEN gate.
    ("WORK REGISTER", ["scripts/spec/work.py", "check"]),
    ("FLEET PRACTICES", [".claude/skills/workstream/check_practices.py"]),
    # DESIGN-conformance-suite.md §9 (kb/Work PB3174): the conformance suite's generator refuses a malformed test
    # (its header, its rules, its control, its expected outputs, the portability lint) reading only the tree, so a
    # refused test is red here in a fraction of a second, long before the test hosts end.
    ("CONFORMANCE SUITE", ["conformance-suite/harness/generate.py", "--check"]),
)
#: ⛔ THE AUDITS THAT RUN ONLY WHEN THE CHANGE TOUCHES A SCRIPT (kb/Work PB4326, PB4409; scripts/inert_lane.py
#: `Lane.tooling`): a script, a workflow, a tool, an agent definition, a root manifest the self-tests read or any
#: .py/.ps1/.sh file. A compiler data file a self-test reads (inert_lane.py's TOOLING_READS) starts none here (owner
#: 2026-11-01: "I see no reason why a compiler change should need to run script self-tests"); its first run is CI's.
#: The rest of the population is CI's: the merge group's `audits` job runs every Linux self-test whatever the change (a
#: declared population reader and the DEVLOG/kb corpora are the named residue, inert_lane.py) and `windows-self-tests`
#: the Windows ones whenever the change touches a script or that data (`Lane.reads_tooling`). The recorded-inputs
#: reuse (PB2914) still lets a script change re-run exactly the self-tests that read it.
TOOLING_ONLY = ("SELF-TESTS",)
#: ⛔ WHAT AN AUDIT NEEDS IN THE TREE, made ready before any audit runs, exactly as CI's `audits` job makes it ready:
#: FLEET PRACTICES reads the pinned public skills submodule, which a fresh worktree does not have. A prerequisite that
#: fails is a RED and no audit runs. The self-test maps every command of CI's `audits` job to an AUDITS entry, to one of
#: these, or to its recorded reason for running elsewhere (`ci_audits_not_in_gate`), so the next audit CI gains reaches the gate.
AUDIT_PREREQUISITES = (
    ("PUBLIC SKILLS SUBMODULE", ["git", "submodule", "update", "--init", "tools/claude-skills"]),
)
#: ⛔ A CI AUDIT THAT IS NOT A TREE AUDIT, AND WHERE IT RUNS INSTEAD (kb/Work PB1957 row 44, the CI invariant): train 1067's
#: PR 9 went RED in CI's `audits` job on `work.py check` behind a GREEN gate, because CI ran it and the gate did not and
#: nothing compared the two lists.
CI_AUDITS_ELSEWHERE = {
    "scripts/devlog.py": "a LANDING check, not a tree audit: an implementer's entry stays DEVLOG/pending-*.md until "
                         "enqueue.py stamps it, and `devlog.py check` refuses a pending entry, so it runs where every "
                         "landing passes (`enqueue.py checks`, exit 6) and in CI on a pull request or merge group",
}
#: CI's audits job runs at least this many `python3 <script>` commands, and makes this submodule ready: a reading that
#: finds fewer, or not it, read nothing.
CI_AUDITS_FLOOR, CI_AUDITS_SUBMODULE = 10, "tools/claude-skills"
CI_PYTHON_RE = re.compile(r"\bpython3?\s+(\S+\.py)\b")
CI_SUBMODULE_RE = re.compile(r"\bgit submodule update --init (\S+)")


def ci_audits_not_in_gate(workflow: str, audits=AUDITS, prerequisites=AUDIT_PREREQUISITES,
                          elsewhere: dict[str, str] = CI_AUDITS_ELSEWHERE, floor: int = CI_AUDITS_FLOOR,
                          submodule: str | None = CI_AUDITS_SUBMODULE) -> list[str]:
    """THE CI INVARIANT FOR THE AUDITS (kb/Work PB1957 row 44): every `python3 <script>` a code line of CI's `audits` job
    runs is a script of `audits` or recorded in `elsewhere`, every `git submodule update --init <path>` it runs is one of
    `prerequisites`, and every `elsewhere` entry is still run by the job. A reading under `floor` scripts, or one that
    does not see the job make `submodule` ready, read nothing, and is a finding."""
    from ci_workflow import job  # noqa: PLC0415 — this directory's: the one reader of a CI workflow's shape
    lines = [x.strip() for x in job(workflow, "audits").code_lines]
    scripts = list(dict.fromkeys(m.group(1) for x in lines for m in CI_PYTHON_RE.finditer(x)))
    submodules = list(dict.fromkeys(m.group(1) for x in lines for m in CI_SUBMODULE_RE.finditer(x)))
    gated = {c for _, cmd in audits for c in cmd[:1]}
    prepared = {cmd[-1] for _, cmd in prerequisites if cmd[:4] == ["git", "submodule", "update", "--init"]}
    bad = [f"CI's audits job runs only {len(scripts)} `python3 <script>` commands: the reading is broken"] \
        if len(scripts) < floor else []
    if submodule and submodule not in submodules:
        bad.append(f"CI's audits job no longer makes {submodule} ready: the reading is broken")
    bad += [f"CI's audits job runs {s} and the gate's AUDITS do not" for s in scripts if s not in gated | set(elsewhere)]
    bad += [f"CI's audits job makes the submodule {p} ready and the gate's AUDIT_PREREQUISITES do not"
            for p in submodules if p not in prepared]
    bad += [f"{s} is recorded as running elsewhere (CI_AUDITS_ELSEWHERE) but CI's audits job no longer runs it"
            for s in elsewhere if s not in scripts]
    return bad
#: ⛔ EVERY GATE REUSES A SELF-TEST'S RECORDED PASS (kb/Work PB2914, generalized to the one gate by PB3022;
#: DESIGN-test-build-ci.md §3.14.10, §3.17.3): the gate's audits add this to SELF-TESTS, so a self-test whose inputs
#: are unchanged since a recorded PASS is REUSED, not run, and one that does run records its PASS for the next gate.
#: CI's `audits` job, the Linux gate and a hand run never pass it: they run every self-test.
SELF_TESTS_REUSE = "--reuse"
#: The post-build half of the self-tests: the ones declaring `SELF-TEST-NEEDS: build` (scripts/self_tests.py), run only
#: when the change touches tooling (`TOOLING_ONLY`'s rule) by a gate that builds.
BUILT_SELF_TESTS = ["scripts/self_tests.py", "--built"]
#: ⛔ THE WORK THAT RUNS BESIDE THE GATE'S CRITICAL PATH (`Gate._beside`, kb/Work PB2880, PB4326), each joined once before
#: the verdict: the audits, which read only the tree, beside the build and the test hosts; and, when the change touches tooling,
#: the post-build self-tests, which read the built assemblies, beside the test hosts. A step that needs nothing the critical
#: path is still making belongs here, not in a serial valley ahead of it.
BESIDE = ("audits", "post_build_self_tests")

Spawn = Callable[..., dict]


def _no_slot(**kwargs) -> dict:
    return kwargs


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
            if self.stopping.is_set() and p.poll() is None:
                self.stopped.add(p.pid)
                kill_tree(p.pid)   # its whole tree: each audit leads the process group start_new_session gave it

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
                kill_tree(p.pid)
            return len(running)


# ── the host: everything the driver does to the machine (the self-test plants a fake one) ────────────────────────


class Host:
    """The real machine: git, the build, the listings, the test hosts."""

    def __init__(self, repo: Path = REPO, gh: Callable[[list[str]], tuple[int, str, str]] | None = None):
        self.repo = repo
        self.gh = gh   # the GitHub CLI runner `queue_facts` takes (None: the real one); the self-test plants one
        #: The test hosts this gate runs (`run_host`), registered so a RED audit can stop them (`stop_hosts`, kb/Work
        #: PB4326): the audits run beside the hosts, not ahead of them.
        self.hosts_live = _Live()

    def stop_hosts(self) -> int:
        """Stop every test host running now, process trees and all, and every one that starts from now on; returns how many
        were running. The gate calls it when an audit that ran beside the hosts is RED (kb/Work PB4326)."""
        return self.hosts_live.stop_all()

    def git(self, *args: str) -> str:
        return subprocess.run(["git", *args], cwd=self.repo, check=True, capture_output=True, text=True,
                              encoding="utf-8").stdout.strip()

    def lock_path(self) -> Path:
        """`<worktree git dir>/cobol-gate.lock` — per WORKTREE (a linked worktree's git dir is its own)."""
        return Path(self.git("rev-parse", "--absolute-git-dir")) / LOCK_NAME

    def take_slot(self, label: str, say: Callable[[str], None]) -> Slot:
        """A slot of the GATE pool: the builds and test hosts the cap rations."""
        return GateSlots.for_repo(self.repo).acquire(label, say=say)

    def record_growth(self, run: Path, tests_s: float, conformance: int) -> None:
        """Keep this GREEN code-lane gate's test phase where the growth trigger reads it (scripts/gate_growth.py, kb/Work
        PB4333): the run's verdict.json goes with its worktree, so a week of them is never on disk at once."""
        gate_growth.record(gate_growth.growth_dir(self.repo), run, self.repo.name, tests_s, conformance)

    def default_base(self) -> str:
        return self.git("merge-base", "HEAD", "origin/main")

    def lane(self, base: str | None, tree: str | None) -> inert_lane.Lane:
        """The change's lane: what `tree` (the measured working tree) changes against `base`, classified by THE one
        classification (scripts/inert_lane.py). No base or no tree, or a change git cannot list, is the full lane."""
        if not base or not tree:
            return inert_lane.Lane("full", "no base or no measured tree: the full lane", tooling=True, reads_tooling=True)
        try:
            return inert_lane.CLASSIFICATION.classify(inert_lane.changed(self.repo, base, tree))
        except (subprocess.CalledProcessError, OSError) as e:
            return inert_lane.Lane("full", f"the change could not be listed ({e}): the full lane", tooling=True,
                                   reads_tooling=True)

    def head(self) -> str | None:
        try:
            return self.git("rev-parse", "HEAD")
        except (subprocess.CalledProcessError, OSError):
            return None

    def head_tree(self) -> str | None:
        """The git tree id of HEAD's commit: what the working tree this gate measures must equal for the gate to attest
        the commit (None when git cannot say)."""
        try:
            return self.git("rev-parse", "HEAD^{tree}")
        except (subprocess.CalledProcessError, OSError):
            return None

    def toolchain(self) -> tuple[bool, str]:
        """This host's tools against toolchain.env (kb/Work PB4007; scripts/toolchain.py): (True, the toolchain the gate
        runs under) when every tool the gate runs reports the manifest's version, else (False, each mismatch). CI runs
        exactly those versions, so a gate under any other toolchain measures something CI will not run."""
        try:
            manifest = toolchain.load()
        except (OSError, ValueError) as e:
            return False, f"toolchain.env cannot be read: {e}"
        _, bad = toolchain.compare(manifest, "gate")
        return (not bad), ("; ".join(bad) if bad else toolchain.summary(manifest, "gate"))

    def measured_tree(self) -> str | None:
        """THE TREE A GATE MEASURES (kb/Work PB3536): the git tree id of the WORKING TREE — every tracked file as it is on
        disk, every untracked file git does not ignore, no ignored one — written through a private copy of the index,
        so the worktree's own index is never touched. A path coord.NEVER_WORK names is never work (the harness edits a
        TRACKED `.claude/settings.local.json` in every agent's worktree, and an agent may not commit it): it measures
        at HEAD's content whatever is on disk or staged, so a worktree whose only dirt is the harness's measures as its
        commit's own tree, the tree a GATE-PENDING report's HEAD is checked against (reports.Report.gate_state;
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

    def audit_commands(self, tooling: bool) -> tuple[tuple[str, list[str]], ...]:
        """The audits this gate runs: AUDITS, less the TOOLING_ONLY ones unless the change touches tooling (the first-red
        arm of the self-test plants its own)."""
        return tuple((name, cmd) for name, cmd in AUDITS if tooling or name not in TOOLING_ONLY)

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

    def audits(self, spawn: Spawn, say: Callable[[str], None], tooling: bool) -> list[str]:
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
                    for name, cmd in self.audit_commands(tooling)]
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
        return subprocess.run(["dotnet", "build", "WiseOwl.Cobol.sln", "-v", "quiet"], cwd=self.repo,
                              **spawn()).returncode == 0

    def listing(self, asm: str, spawn: Spawn) -> tuple[Counter, str]:
        return list_population(self.repo / GATED_ASSEMBLIES[asm], cwd=self.repo, **spawn())

    def fetch_bounce_results(self) -> str:
        """The failed merge-group run's test results of this branch's open pull request, into
        `CI_BOUNCE_RESULTS` (`queue_facts.fetch_bounce_results`, the one GitHub reader); the line it returns says what
        the directory holds. A detached HEAD asks nothing. Never raises: it only orders the run (`Gate._first`)."""
        try:
            branch = self.git("rev-parse", "--abbrev-ref", "HEAD")
        except (OSError, subprocess.SubprocessError):
            branch = ""
        if not branch or branch == "HEAD":
            return "build-local: ci bounce: none (no branch is checked out)"
        return "build-local: " + queue_facts.fetch_bounce_results(branch, self.repo / CI_BOUNCE_RESULTS, self.gh).line

    def run_host(self, asm: str, run: Path, env: dict[str, str], spawn: Spawn) -> int:
        """One assembly's whole population: `dotnet test --no-build` with a trx logger, its whole output to `<asm>.log`.
        The host is registered in `hosts_live` while it runs and leads its own process group, so `stop_hosts` ends its
        whole tree (kb/Work PB4326); a host that starts after the stop is killed at once and exits non-zero."""
        cmd = ["dotnet", "test", str(self.repo / GATED_ASSEMBLIES[asm]), "--no-build", "--results-directory",
               str(run), "--logger", f"trx;LogFileName={asm}.trx"]
        with open(run / f"{asm}.log", "wb") as fh:
            p = subprocess.Popen(cmd, cwd=self.repo, stdout=fh, stderr=subprocess.STDOUT,
                                 start_new_session=os.name != "nt", **spawn(env=env))
            self.hosts_live.add(p)
            try:
                return p.wait()
            finally:
                self.hosts_live.discard(p)


def host_env(first: Path | None, observations: Path) -> dict[str, str]:
    """⛔ THE ONLY PLACE A TEST HOST'S ENVIRONMENT IS MADE (§3.14.3): the scrubbed environment — no stray
    WISEOWL_COBOL_GATE_*, WISEOWL_COBOL_SHARD, VSTest* or RunSettingsFilePath, so nothing can narrow the run — in English,
    with the one ordering channel (FIRST_CLASSES) when this gate has classes to run first. The test harness's observation
    log (every retry and non-observation of a child process, tests/_shared/ProcessObservation.cs) goes to `observations`
    in the run directory, one per host, so a non-observation is read beside the host's own log instead of in one temp
    file every gate on the host appends to (kb/Work PB3108)."""
    order = {FIRST_CLASSES: str(first)} if first is not None else {}
    return {**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en", **order, "WISEOWL_COBOL_HARNESS_LOG": str(observations)}


# ── a run directory: the gate's pid, its verdict record, and the one way to wait for it (kb/Work PB3536) ──────────


def write_pid(run: Path, pid: int) -> None:
    """The gate's own pid, written UNDER its gate lock; it replaces the launcher's spawn record (`write_spawn`)."""
    (run / PID_FILE).write_text(json.dumps({"pid": pid}) + "\n", encoding="utf-8", newline="\n")
    try:
        (run / SPAWN_FILE).unlink(missing_ok=True)
    except OSError:  # the launcher has the record open this instant (Windows): it lingers, and the pid file outranks it
        pass


def read_pid(run: Path) -> int | None:
    return _read_pid_file(run / PID_FILE)


def write_spawn(run: Path, pid: int) -> None:
    """The launcher's record of the child it spawned for `run`, until the child's `write_pid` replaces it (PB3707)."""
    (run / SPAWN_FILE).write_text(json.dumps({"pid": pid}) + "\n", encoding="utf-8", newline="\n")


def read_spawn(run: Path) -> int | None:
    return _read_pid_file(run / SPAWN_FILE)


def _read_pid_file(path: Path) -> int | None:
    try:
        return int(json.loads(path.read_text(encoding="utf-8"))["pid"])
    except (OSError, ValueError, KeyError, TypeError):
        return None


_RUN_STAMP = re.compile(r"\d{8}T\d{6}Z")


def run_age_s(run: Path, now: float | None = None) -> float | None:
    """Seconds since the run directory was made, read from the UTC stamp its name carries (`Gate.new_run`; both name
    forms of RUN_DIR have one), or None for a name with none."""
    m = _RUN_STAMP.search(run.name)
    if m is None:
        return None
    try:
        made = dt.datetime.strptime(m.group(), "%Y%m%dT%H%M%SZ").replace(tzinfo=dt.timezone.utc).timestamp()
    except ValueError:  # digits that are no date (a hand-made name): no age, so never "starting"
        return None
    return (time.time() if now is None else now) - made


def gate_starting(run: Path, now: float | None = None) -> bool:
    """A detached gate that has been spawned and has not yet taken its lock (kb/Work PB3707): the run has no `gate.pid`, is
    younger than START_WINDOW_S, and its spawn record, when the launcher has written one, names a live process. A run with
    a pid is past starting; one past the window, or whose spawned child is gone, is a gate that DIED before its lock."""
    if read_pid(run) is not None:
        return False
    age = run_age_s(run, now)
    if age is None or not -START_WINDOW_S < age < START_WINDOW_S:   # a stamp from the future is a clock step, not a start
        return False
    spawned = read_spawn(run)
    return spawned is None or pid_alive(spawned)


def read_record(run: Path) -> dict | None:
    """The run's verdict record, or None while it has none (a verdict file is written whole, once, at the end)."""
    try:
        rec = json.loads((run / VERDICT_FILE).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    return rec if isinstance(rec, dict) and "verdict" in rec and "line" in rec else None


def record_schema(rec: dict) -> int:
    """A record's schema number; a record written before the field existed (or with a malformed one) is schema 0."""
    value = rec.get("schema")
    return value if isinstance(value, int) and not isinstance(value, bool) else 0


def predates_landing(rec: dict) -> str | None:
    """Why a record cannot be evidence to the landing although its verdict may be GREEN (kb/Work PB3660): it was written by
    a gate older than the lander's reading (schema below `LANDABLE_SCHEMA`, or a field of `LANDING_FIELDS` absent), so a
    "missing field" the lander finds is the gate's age, never the tree's state. None when the record is current."""
    have = record_schema(rec)
    missing = [f for f in LANDING_FIELDS if f not in rec]
    if have >= LANDABLE_SCHEMA and not missing:
        return None
    return (f"it was written by a gate older than this lander reads (record schema {have}, "
            f"{'no ' + '/'.join(missing) + ' field' if missing else 'below ' + str(LANDABLE_SCHEMA)}: the branch's "
            f"scripts/gate.py predates kb/Work {SCHEMA_NOTES[RECORD_SCHEMA]})")


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


def lock_of_run(run: Path) -> Path | None:
    """The gate lock of the worktree a run directory belongs to (`<worktree>/TestResults/build-local/<run>`), or None when
    the run is not under a worktree's run root or that worktree is gone: no gate can run there."""
    wt = run.parent.parent.parent
    if run.parent != wt / RUN_ROOT or not wt.is_dir():
        return None
    try:
        return Host(wt).lock_path()
    except (OSError, subprocess.CalledProcessError):
        return None


def gate_phase(run: Path, live: "LiveGate | None") -> str:
    """WHERE A RUN'S GATE IS (kb/Work PB3662, PB3707), from the worktree's `live_gate` reading (None: no gate lock is
    held): RUNNING when the lock is held by a gate that writes this run (or by one whose label names none), never by a
    process id, which the OS reuses; STARTING when no lock is held for it yet but it was spawned moments ago and its
    child is alive (`gate_starting`); else DIED, a gate that ended or never got its lock, with no verdict."""
    if live is not None and (live.run is None or live.run.name == run.name):
        return RUNNING
    return STARTING if gate_starting(run) else DIED


def gate_running(run: Path) -> Callable[[], bool]:
    """THE READER'S TEST OF "THIS RUN'S GATE IS STILL RUNNING" (kb/Work PB3662, review R1-1 of wave 20261104): the gate
    lock (`live_gate`) is held by a gate that writes this run, never a process id, which the OS reuses: a reader that
    arrives after a reboot or a long death (the planner's survey of a handed-over gate, `--await` from the rolling wave)
    must not read a stranger holding the dead gate's pid as the gate. A run whose gate has not taken its lock yet is
    STARTING (`gate_phase`, kb/Work PB3707) and reads as running for START_WINDOW_S at most; a run from before that
    window, or whose spawned child is gone, is a gate that died."""
    return lock_running(run, lock_of_run(run))


def lock_running(run: Path, lock: Path | None) -> Callable[[], bool]:
    """`gate_running` over a named gate lock (None: no gate can run there)."""
    def running() -> bool:
        return gate_phase(run, live_gate(lock) if lock is not None else None) != DIED
    return running


def watch(run: Path, wait_s: float | None, running: Callable[[], bool], echo: Callable[[str], None] | None = None,
          poll_s: float = 0.5) -> tuple[int, str]:
    """THE ONE WAY TO WAIT FOR A GATE (kb/Work PB3536): its run directory's VERDICT FILE, never a log's tail.
    `running()` says whether the gate that writes the run still runs: `gate_running(run)` for every reader of a gate it
    did not start (the lock, never a reused pid), the follower's own child's liveness for the gate it just launched.
    Returns (exit code, verdict line): the record's own once `verdict.json` is there; NOT RUN [verdict] (2) when the
    gate DIED without one; PENDING (AWAIT_PENDING_EXIT) once `wait_s` passes (None: no limit); NOT
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
            while echo is not None and running():
                drain()
                time.sleep(poll_s)
            drain(final=True)
            return _record_exit(rec), str(rec["line"])
        if not running():
            if (rec := read_record(run)) is not None:  # it wrote the record and ended between the two reads
                continue
            drain(final=True)
            return 2, (f"{VERDICT}{NOT_RUN_CAUSES['verdict'].verdict} — the gate "
                       f"{f'process {pid} ' if pid is not None else ''}DIED with no verdict "
                       f"(run {run}; its output: {log}) ===")
        if wait_s is not None and time.monotonic() - t >= wait_s:
            who = (f"the gate process {pid} is running" if pid is not None else
                   f"no {PID_FILE} names its process yet: it was spawned moments ago and has not taken its gate lock "
                   f"(STARTING, kb/Work PB3707; {START_WINDOW_S:.0f} s at most, then it reads as DIED)")
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
    # the gate writes its own gate.pid UNDER its lock (`Gate.run`), so a reader that finds a pid file knows the gate took the
    # lock: the launcher writes none, but it records the child it spawned (`write_spawn`, kb/Work PB3707), so a reader in the
    # window before the lock reads a young run with a live child as STARTING (`gate_phase`), never as a gate that died. The
    # run is young from the moment its directory exists (its name's stamp), so no gap lies between the two. The follower
    # alone has watched its child since the spawn, so its liveness is the child's.
    try:
        write_spawn(run, pid)
    except OSError as e:  # the record only shortens a dead child's STARTING; the gate runs and is followed without it
        say(f"build-local: ⚠ the spawned gate's pid could not be recorded in {run} ({e}); a reader reads the run as "
            f"STARTING for {START_WINDOW_S:.0f} s whether or not it dies")
    say(f"build-local: the gate runs DETACHED (pid {pid}, run directory {run}): it outlives this process; "
        f"`python scripts/gate.py --await {run}` reads its verdict (kb/Work PB3536)")
    code, line = watch(run, None, lambda: pid_alive(pid), echo=say)
    if not (run / LOG_FILE).exists() or line not in (run / LOG_FILE).read_text(encoding="utf-8", errors="replace"):
        say(line)  # a verdict the gate's own output does not carry (DIED): the caller's last line is still the verdict
    return code


# ── the gate ─────────────────────────────────────────────────────────────────────────────────────────────────────


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
    #: The test classes this gate ran first (`Gate._first`): red at the previous run or in the merge queue.
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
    #: The change's lane (scripts/inert_lane.py, kb/Work PB2878): `inert` (no test reads it: the audits, then the
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


def run_order(name: str) -> tuple[int, str]:
    """The order of a run directory's name (RUN_DIR): its number, then the name. A run named before the numbers has
    number 0, so it is older than every numbered run (and such runs are ordered among themselves by their stamps)."""
    m = RUN_DIR.fullmatch(name)
    if m is None:
        raise ValueError(f"{name!r} is not a run directory's name (RUN_DIR)")
    return int(m["n"] or 0), name


def run_dirs(run_root: Path) -> list[Path]:
    """Every run directory in the run root, oldest first (`run_order`): THE listing of a worktree's gate runs, the gate's
    own and the landing's (enqueue.tested_locally) alike."""
    return sorted((p for p in run_root.iterdir() if p.is_dir() and RUN_DIR.fullmatch(p.name)),
                  key=lambda p: run_order(p.name)) if run_root.is_dir() else []


@dataclass(frozen=True)
class LiveGate:
    """A gate that runs in a worktree now: its lock's label (`holder`: the gate's own `gate <repo> (pid N, since T, run R)`)
    and the run it writes (`run`, named in that label; None before the gate has a run directory)."""
    holder: str
    run: Path | None

    @property
    def what(self) -> str:
        """How a message names it: its run, else its lock's label."""
        return f"the gate run {self.run.name}" if self.run is not None else f"a gate ({self.holder or 'no label'})"


def live_gate(lock_path: Path) -> LiveGate | None:
    """THE ONE READER OF "A GATE IS RUNNING HERE" (kb/Work PB3662): the worktree's gate lock is held. `Gate.run` takes it
    before anything else and holds it until its verdict file is written; it is an OS lock the OS drops when its holder
    dies, so it is held exactly while a gate runs: a gate that died without a verdict frees it, and no process id
    (which Windows reuses) is ever trusted. Taken and released at once to read it. None when no gate runs.
    ⛔ THE WINDOW (kb/Work PB3707): a detached gate's run directory exists, and its child is alive, before the child takes
    the lock, so the lock is NOT held for that moment of interpreter start and imports. A reader that must tell a gate
    still starting from one that died asks `gate_phase`, which adds the spawn record and the run's age to this reading."""
    lock = ExclusiveLock.try_take(lock_path, f"live_gate probe (pid {os.getpid()})")
    if lock is not None:
        lock.release()
        return None
    holder = ExclusiveLock.holder(lock_path)
    run = Path(holder.partition(RUN_LABEL)[2].removesuffix(")")) if RUN_LABEL in holder else None
    return LiveGate(holder, run)


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
            self._record_growth(out, run)
        self.say(out.line)
        return out

    def _record_growth(self, out: Outcome, run: Path) -> None:
        """The growth trigger's sample (scripts/gate_growth.py, kb/Work PB4333): a GREEN gate that ran its test hosts
        (`tests_s` is set only then, so the inert lane has none) and measured them itself (a REUSED verdict ran
        nothing). A sample that cannot be written is said and never changes the verdict: the trigger reads fewer gates."""
        conformance = out.populations.get("Conformance")
        if out.verdict != "GREEN" or out.reused_from or "tests_s" not in out.timings or conformance is None:
            return
        try:
            self.host.record_growth(run, out.timings["tests_s"], conformance.discovered)
        except (OSError, ValueError, subprocess.CalledProcessError) as e:
            self.say(f"build-local: the growth sample was not recorded ({type(e).__name__}: {e}); the verdict stands")

    def run(self) -> Outcome:
        out = Outcome()
        started = time.monotonic()
        run = self.run_dir
        label = (f"gate {self.host.repo} (pid {os.getpid()}, since {time.strftime('%Y-%m-%d %H:%M:%S')}"
                 + (f"{RUN_LABEL}{run}" if run is not None else "") + ")")
        lock_path = self.host.lock_path()
        lock = ExclusiveLock.try_take(lock_path, label)
        if lock is None:
            return self._finish(out, run, BUSY, f"another gate holds this worktree's gate lock ({lock_path}): "
                                f"{ExclusiveLock.holder(lock_path) or '(no label)'}; nothing ran, nothing is red: "
                                "re-run once it ends, or `python scripts/gate.py --await <its run>` reads "
                                "its verdict", EXIT_BUSY)
        with lock:
            if run is not None:  # a detached gate's pid goes UNDER its lock: a pid file then proves the lock was taken,
                write_pid(run, os.getpid())   # and the lock (`gate_running`), never the reusable pid, proves it still runs
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
                    out.head_tree = self.host.head_tree()
                    try:
                        out.base = self._base()
                    except Exception:  # noqa: BLE001 — no base: the code lane (`Host.lane`), and nothing is reused
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
        its start and unchanged at its end — against THIS base, written by a gate that runs the whole population (record
        schema 5 on, kb/Work PB4328: an older record may be a `LEG 1 ONLY` one). None when the tree could not be measured or
        no run qualifies: a RED is never reused, and another tree (a landing's merged head) always runs."""
        if not out.tree:
            return None
        for p in reversed(run_dirs(self.run_root)):
            rec = read_record(p) if p != current else None
            if (rec is not None and rec.get("exit_code") == 0 and rec.get("tree") == out.tree
                    and rec.get("tree_end") == out.tree and rec.get("base") == out.base
                    and record_schema(rec) >= SCHEMA_WHOLE):
                return p, rec
        return None

    def _reused(self, out: Outcome, run: Path, started: float, prior: Path, rec: dict) -> Outcome:
        """Answer from a recorded GREEN (`reusable`): no audit, slot, build or leg runs, and the line says so. Along a
        chain of reuses the line names the run that MEASURED the tree and that run's own verdict detail."""
        out.tree_end, out.reused_from = out.tree, rec.get("reused_from") or prior.name
        out.reused_detail = rec.get("reused_detail") or str(rec.get("line", "")).removeprefix(VERDICT).removesuffix(" ===")
        out.lane = rec.get("lane")  # a chain of reuses keeps what the first run measured
        out.tooling = rec.get("tooling")
        out.toolchain = rec.get("toolchain")
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
            runs = run_dirs(self.run_root)
            run = self.run_root / f"{(run_order(runs[-1].name)[0] if runs else 0) + 1:06d}-{self.now():%Y%m%dT%H%M%SZ}"
            run.mkdir()
        return run

    def previous_run(self, current: Path) -> Path | None:
        """The newest EARLIER run that reached a verdict and kept a trx: its red classes run first (`_first`)."""
        for p in reversed(run_dirs(self.run_root)):
            if p != current and (p / VERDICT_FILE).exists() and any(p.glob("*.trx")):
                return p
        return None

    def _prune(self, current: Path) -> None:
        for p in run_dirs(self.run_root)[:-KEEP_RUNS]:
            if p != current:
                shutil.rmtree(p, ignore_errors=True)

    def _gate(self, out: Outcome, label: str, started: float, run: Path) -> Outcome:
        # ⛔ THE AUDITS AND THE CORPUS FETCH NEVER RUN INSIDE THE GATE SLOT (kb/Work PB2524, PB4326). They read only the
        # tree — no build output, no test result — so they need nothing the slot rations (concurrent builds and test
        # hosts, DESIGN-test-build-ci §3.14.6). Inside the slot they were 35-45 % of its hold (~230 s of serial Python
        # per gate, one core busy) while every other gate queued behind it. The fetch runs before the slot is taken; the
        # audits start with the lane and run beside the slot wait, the build and the test hosts.
        # ⛔ FIRST, THE TOOLCHAIN (kb/Work PB4007): CI runs exactly toolchain.env's versions, so a gate under any other
        # measures what CI will not run (#734 was GREEN here on Python 3.14 and RED in CI's 3.12). Nothing runs on it.
        matched, tools = self.host.toolchain()
        if not matched:
            return self._finish(out, run, NOT_RUN_CAUSES["toolchain"].verdict,
                                f"this host's tools are not toolchain.env's: {tools}. Install the manifest's versions "
                                "(winget on the Windows dev box, scripts/wsl/setup-wsl.sh in WSL; `python "
                                f"scripts/toolchain.py check --scope host` lists them); nothing ran (run {run.name})", 2)
        out.toolchain = tools
        self.say(f"build-local: toolchain (toolchain.env) {tools}")
        lane = self.host.lane(out.base, out.tree)
        out.lane, out.tooling = lane.name, lane.tooling
        self.say(f"build-local: {lane.name} lane (scripts/inert_lane.py) — {lane.reason}")
        # ⛔ THE AUDITS START NOW AND RUN BESIDE EVERYTHING AFTER (kb/Work PB4326, step 3 of PB4323). They read only the
        # tree and take about 40 s once the script self-tests are not among them (those run only when the change touches
        # tooling, `TOOLING_ONLY`), so they no longer stand ahead of the slot and the build as a serial valley: a red one
        # STOPS the test hosts (`_audits`), and `_audit_red` ends the gate RED at each point it can be seen before they start
        # (kb/Work PB2523: fail fast; 4 of wave 1034's 8 first-run reds were audit-only).
        self._beside(out, "audits", lambda: self._audits(out, lane.tooling))
        if lane.inert:
            # ⭐ THE INERT LANE (kb/Work PB2878; DESIGN-test-build-ci.md §3.18): every changed path is one NO TEST reads
            # (the classification the CI `changes` job uses, proved by the inotify watch of every Linux test run), so no
            # slot, build or leg can measure anything about it; the audits, which read it, are the gate: joined here.
            # CI's merge group runs
            # the script self-tests on Linux for every landing, the inert ones included (kb/Work PB3646, PB4327, PB4330).
            self._join_beside(out)
            if out.audit_reds:
                return self._audit_red(out, run, "the inert lane runs nothing but the audits, and one is RED: NO TEST RAN")
            return self._inert(out, run, lane)
        if lane.name == "tooling":
            # ⭐ THE TOOLING LANE (kb/Work PB4395; PIPELINE-REVIEW.md §13, owner 2026-11-01 "Build the approved lane"):
            # every changed path is tooling no compiler test and no build reads (CODE_READS, proved by the watch of every
            # Linux test run and the INERT LANE audit), or inert. The audits, the script self-tests among them, are the
            # gate: joined here, with no corpus fetch, slot, build or test host.
            self._join_beside(out)
            if out.audit_reds:
                return self._audit_red(out, run, "the tooling lane runs the audits and the script self-tests, and one is "
                                       "RED: NO TEST RAN")
            return self._tooling(out, run, lane)
        out.reasons += self.host.fetch_corpus(_no_slot, self.say)
        if self._audits_known_red(out):
            return self._audit_red(out, run, "an audit was RED before the slot, the build and any test host: NO TEST RAN")
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

    def _audits(self, out: Outcome, tooling: bool) -> tuple[list[str], list[str]]:
        """The audits as work BESIDE the critical path (`_beside`, kb/Work PB4326): returns (their RED reasons, their output
        lines), printed whole when joined. The reasons are recorded in `out.audit_reds` BEFORE the hosts are stopped, so
        the gate that finds the stop reads why. A RED audit stops the test hosts (`Host.stop_hosts`): the verdict is RED
        whatever they would have found, and every second they ran on was waste (kb/Work PB2523). An audit phase that
        itself RAISES stops them too and is a named RED (`_join_beside`)."""
        lines: list[str] = []
        t = time.monotonic()
        try:
            reds = self.host.audits(_no_slot, lines.append, tooling)  # an unchanged self-test is REUSED (PB2914)
        except Exception:
            out.audit_reds = ["AUDITS FAILED"]
            self.host.stop_hosts()
            raise
        out.timings["audits_s"] = round(time.monotonic() - t, 1)
        out.audit_reds = reds
        if reds:
            self.host.stop_hosts()
        return reds, lines

    @staticmethod
    def _audits_known_red(out: Outcome) -> bool:
        """An audit is RED already (`_audits` sets `audit_reds` the moment the audit phase ends RED, before it stops the
        hosts): the gate looks at each point it can still avoid the work after it, and never waits for the audits there."""
        return bool(out.audit_reds)

    def _audit_red(self, out: Outcome, run: Path, how: str) -> Outcome:
        """End the gate RED on a RED audit (kb/Work PB2523, PB4326): join everything running beside (it prints the audits'
        blocks), then the verdict, naming the audit(s) and `how` it was found."""
        self._join_beside(out)
        return self._finish(out, run, "RED", f"{'; '.join(out.audit_reds)} — fail-fast: {how}; fix the audit "
                            f"and re-gate (run {run.name})", 1)

    def _inert(self, out: Outcome, run: Path, lane: inert_lane.Lane) -> Outcome:
        """THE INERT LANE's end (kb/Work PB2878): no build and no test host, because no test reads the change; the
        audits were joined, GREEN, before this."""
        out.tree_end = self.host.measured_tree()
        return self._finish(out, run, "GREEN", f"INERT LANE (kb/Work PB2878): {lane.reason}; the audits are GREEN, "
                            f"and no build or test host ran · toolchain {out.toolchain} (run {run.name})", 0)

    def _tooling(self, out: Outcome, run: Path, lane: inert_lane.Lane) -> Outcome:
        """THE TOOLING LANE's end (kb/Work PB4395): no build and no test host, because no compiler test reads the change;
        the audits, the script self-tests among them, were joined, GREEN, before this."""
        out.tree_end = self.host.measured_tree()
        return self._finish(out, run, "GREEN", f"TOOLING LANE (kb/Work PB4395): {lane.reason}; the audits and the script "
                            f"self-tests are GREEN, and no build or test host ran · toolchain {out.toolchain} "
                            f"(run {run.name})", 0)

    def _tested(self, out: Outcome, run: Path, slot: Slot, started: float) -> Outcome:
        """The build, the three whole populations, the checks: the part of the gate the slot rations."""
        spawn: Spawn = slot.spawn_kwargs
        if self._audits_known_red(out):   # one found while the gate queued for the slot: build nothing
            return self._audit_red(out, run, "an audit was RED while the gate queued for the slot: NO TEST RAN")
        t = time.monotonic()
        if not self.host.build(spawn):
            return self._finish(out, run, "BUILD FAILED", "the solution did not build (see above)", 1)
        out.timings["build_s"] = round(time.monotonic() - t, 1)
        if self._audits_known_red(out):
            return self._audit_red(out, run, "an audit was RED before any test host started: NO TEST RAN")
        # The listings (the population each host's trx is checked against) and, when the change touches tooling, the
        # self-tests that read the BUILT assemblies (kb/Work PB2563, PB4326) run BESIDE the hosts: each only lists tests,
        # and the hosts are the gate's longest step.
        listed = self._pool_listings(spawn)
        if out.tooling:
            self._beside(out, "post_build_self_tests", lambda: self.host.built_self_tests(spawn))
        first = self._first(out, run)
        hosts_started = time.monotonic()
        self._run_hosts(out, run, first, spawn, hosts_started)
        out.timings["tests_s"] = round(time.monotonic() - hosts_started, 1)
        reds = [r for r in out.runs if r.red]
        if reds:
            out.timings["first_red_s"] = round(min(r.finished_s for r in reds), 1)
        self._join_beside(out)
        if self.host.hosts_live.stopping.is_set():
            # A RED audit stopped the hosts (`_audits`): what they wrote is a partial run, no population to measure.
            return self._audit_red(out, run, "an audit that ran beside the test hosts was RED and STOPPED them: their "
                                   "results are partial and NOT measured")
        try:
            listings = self._listings(listed, run)
        except PopulationError as e:
            return self._finish(out, run, NOT_RUN_CAUSES["population"].verdict,
                                f"the population could not be listed: {e}", 2)
        self._check_populations(out, listings, run)
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        out.tree_end = self.host.measured_tree()  # a tree edited WHILE it was gated is never reused (`reusable`)
        self._prune(run)
        return self._verdict(out, run)

    def _pool_listings(self, spawn: Spawn) -> dict[str, concurrent.futures.Future]:
        """Start the three `--list-tests` hosts (scrubbed, ~2 s each) beside the test hosts; `_listings` joins them."""
        pool = concurrent.futures.ThreadPoolExecutor(max_workers=len(GATED_ASSEMBLIES), thread_name_prefix="listing")
        try:
            return {asm: pool.submit(self.host.listing, asm, spawn) for asm in GATED_ASSEMBLIES}
        finally:
            pool.shutdown(wait=False)

    @staticmethod
    def _listings(listed: dict[str, concurrent.futures.Future], run: Path) -> dict[str, Counter]:
        """Join the listings (`_pool_listings`) and keep each in the run directory; raises PopulationError when one could
        not be made."""
        listings = {}
        for asm, future in listed.items():
            names, raw = future.result()
            listings[asm] = names
            (run / f"listing-{asm}.txt").write_text(raw, encoding="utf-8", newline="\n")
        return listings

    def _first(self, out: Outcome, run: Path) -> Path | None:
        """THE CLASSES THAT RUN FIRST (kb/Work PB4328): every test class with a red case in this worktree's previous run
        (`previous_run`), its newest hand-run Linux gate (`LINUX_GATE_RESULTS`) or the merge-group run that bounced its
        pull request (`CI_BOUNCE_RESULTS`, fetched first, kb/Work PB3644) — every source test_population.TRX_WRITERS
        names — written to
        `<run>/first-classes.txt` for the hosts' orderer (FIRST_CLASSES); None when there is none. A source it cannot read is named on the console and kept in the run directory (kb/Work PB3661), and the
        gate goes on: this orders the run, it never selects it."""
        self.say(self.host.fetch_bounce_results())
        sources = [p for p in (self.previous_run(run), self.host.repo / LINUX_GATE_RESULTS,
                               self.host.repo / CI_BOUNCE_RESULTS) if p is not None]
        classes: set[str] = set()
        for source in sources:
            try:
                classes |= red_classes(sorted(source.rglob("*.trx")))
            except UnreadableTrx as e:
                self.say(f"build-local: ⚠ a trx the first classes are read from is unreadable ({e}; "
                         f"{self._keep_unreadable(run, e.path)}); the order goes on without it")
        out.first = sorted(classes)
        if not classes:
            return None
        path = run / "first-classes.txt"
        path.write_text("".join(f"{c}\n" for c in out.first), encoding="utf-8", newline="\n")
        self.say(f"build-local: {len(classes)} class(es) red at the previous run or the merge queue run first: "
                 + ", ".join(out.first[:5]) + (" …" if len(classes) > 5 else ""))
        return path

    @staticmethod
    def _keep_unreadable(run: Path, path: Path) -> str:
        """Copy a trx that could not be read into the run directory as `unreadable-<its directory>-<its name>.bad`
        (never a `.trx`: later readers glob a run directory's trx files and must not meet it), and say where it is, or
        why it could not be kept."""
        kept = run / f"unreadable-{path.parent.name}-{path.name}.bad"
        try:
            shutil.copy2(path, kept)
        except OSError as e:
            return f"not kept: {e}"
        return f"kept as {kept.name}"

    def _run_hosts(self, out: Outcome, run: Path, first: Path | None, spawn: Spawn, hosts_started: float) -> None:
        """The three assemblies' whole populations CONCURRENTLY, one host each, each printed through the one leg
        report (scripts/test_leg_report.py) in a fixed order whatever order they finished in."""
        self.say(f"build-local: the whole population — {', '.join(GATED_ASSEMBLIES)} concurrently")

        def one(asm: str) -> tuple[str, int, float, float]:
            env = host_env(first, run / f"{asm}.observations.log")
            t = time.monotonic()
            rc = self.host.run_host(asm, run, env, spawn)
            return asm, rc, time.monotonic() - t, time.monotonic() - hosts_started

        with concurrent.futures.ThreadPoolExecutor(max_workers=len(GATED_ASSEMBLIES)) as pool:
            results = {asm: (rc, wall, fin) for asm, rc, wall, fin in pool.map(one, GATED_ASSEMBLIES)}
        if self.host.hosts_live.stopping.is_set():
            return   # stopped by a RED audit (`_audits`): a killed host's log is no report, `_audit_red` ends the gate
        for asm in GATED_ASSEMBLIES:
            rc, wall, fin = results[asm]
            # THE ONE RULE for what a host prints (kb/Work PB1573): a green host its verdict line, a red one its
            # COMPLETE output. Its exit code is the host's verdict: 0 green, anything else red.
            rep = subprocess.run([sys.executable, str(REPO / "scripts" / "test_leg_report.py"), "--name", asm,
                                  "--log", str(run / f"{asm}.log"), "--rc", str(rc)],
                                 capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
            for line in (rep.stdout + rep.stderr).splitlines():
                self.say(line)
            out.runs.append(HostRun(asm, rc, rep.returncode != 0, round(wall, 1), round(fin, 1)))

    def _check_populations(self, out: Outcome, listings: dict[str, Counter], run: Path) -> None:
        """Ran equals discovered, by name, per assembly (scripts/test_population.py `check`): its trx against its
        listing. A host that wrote no trx is UNMEASURED, a RED."""
        for asm in GATED_ASSEMBLIES:
            trx = run / f"{asm}.trx"
            try:
                if not trx.exists():
                    raise PopulationError(f"{asm}: its host wrote no trx")
                pop = check_population(asm, listings[asm], [str(trx)])
            except PopulationError as e:
                out.reasons.append(f"{asm} POPULATION UNMEASURED ({e})")
                self.say(f"=== POPULATION {asm}: UNMEASURED — {e} ===")
                continue
            out.populations[asm] = pop
            for line in pop.report():
                self.say(line)

    def _verdict(self, out: Outcome, run: Path) -> Outcome:
        pops = out.populations
        tally = " · ".join(f"{a} {pops[a].ran:,}/{pops[a].discovered:,}" if a in pops else f"{a} UNMEASURED"
                           for a in GATED_ASSEMBLIES)
        skipped = sum(p.skipped for p in pops.values())
        first = f"{len(out.first)} class(es) first" if out.first else "no class first"
        detail = (f"{tally} cases ran (skipped {skipped:,})" + (f" · {out.slot}" if out.slot else "")
                  + (f" · toolchain {out.toolchain}" if out.toolchain else "") + f" ({first}; run {run.name})")
        reds = [f"{r.asm} RED" for r in out.runs if r.red]
        problems = reds + out.reasons + [f"{a} POPULATION NOT EXACT" for a in pops if pops[a].defective]
        if problems:
            return self._finish(out, run, "RED", f"{'; '.join(problems)} — {detail}", 1)
        return self._finish(out, run, "GREEN", detail, 0)

    @staticmethod
    def _assembly_walls(out: Outcome) -> dict[str, float]:
        """Per assembly, the seconds its host ran (kb/Work PB4326): what explains a slow test phase, the one the growth
        trigger of PB4323 §3.4 judges (`_record_growth`, scripts/gate_growth.py), with `_concurrency`."""
        return {r.asm: r.wall_s for r in out.runs}

    @staticmethod
    def _concurrency(out: Outcome) -> float | None:
        """EFFECTIVE CONCURRENCY of the hosts (kb/Work PB4326): their wall seconds summed over the span they took. 3.0
        means the three assemblies ran flat out side by side; near 1.0, one assembly's tail ran alone. None when no host
        ran."""
        span = out.timings.get("tests_s", 0.0)
        return round(sum(r.wall_s for r in out.runs) / span, 2) if out.runs and span > 0 else None

    def _record(self, out: Outcome, run: Path) -> dict:
        return {"schema": RECORD_SCHEMA, "verdict": out.verdict, "line": out.line, "exit_code": out.exit_code,
                "head": out.head, "tree": out.tree, "tree_end": out.tree_end, "base": out.base,
                "head_tree": out.head_tree, "toolchain": out.toolchain,
                "reused_from": out.reused_from, "reused_detail": out.reused_detail, "reasons": out.reasons,
                "slot": out.slot, "lane": out.lane, "tooling": out.tooling, "first": out.first,
                "runs": [vars(r) for r in out.runs], "timings": out.timings,
                "assembly_wall_s": self._assembly_walls(out), "effective_concurrency": self._concurrency(out),
                "populations": {a: {"discovered": p.discovered, "ran": p.ran, "skipped": p.skipped, "exact": p.exact}
                                for a, p in out.populations.items()}}


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────


class FakeHost(Host):
    """A planted machine: a small population per assembly, and test hosts that write the trx and the log the real ones
    write — each arm plants one defect into them."""

    POPULATION = {
        "Conformance": ["N.Conf.A", "N.Conf.B", "N.Conf.C", "N.Conf.D", "N.Conf.E", "N.Conf.F"],
        "Unit": ["N.Unit.A", "N.Unit.B", "N.Unit.C", "N.Unit.D"],
        "Characterization": ["N.Char.A", "N.Char.B"],
    }

    def __init__(self, root: Path, lock: Path, plant: set[str] = frozenset(),
                 trees: list[str | None] | None = None, base: str = "0" * 40):
        """`trees`: the working tree the gate measures, call by call (its start, then its end; the last value
        repeats); `base`: the planted cut point."""
        super().__init__(root)
        self.root, self._lock, self.plant = root, lock, set(plant)
        self.trees, self.base_sha = list(trees or ["tree-1"]), base
        self.committed_tree = self.trees[0]  # HEAD's tree: a gate of a clean working tree measures it
        self.slots_taken = 0
        self.audited = self.built = self.toolchain_checked = False
        self.tooling_seen: bool | None = None   # what `audits` was told about the change (kb/Work PB4326)
        #: Set when the gate stops the hosts for a RED audit; and when a test host starts (the "audit-late" arm's order).
        self.stopped_by_audit, self.host_started = threading.Event(), threading.Event()
        self.slot_taken = threading.Event()   # the "audit-slot" arm's ordering: the audit is red once the slot is held
        #: The order the MAIN thread drove the host in: fetch, slot, build. The audits run beside it, in a thread of their
        #: own, so they are `audited`, never an event of this order.
        self.events: list[str] = []
        self.envs: dict[str, dict[str, str]] = {}
        self.growth: list[tuple[str, float, int]] = []   # the growth samples taken: (run name, tests_s, Conformance cases)

    def lock_path(self) -> Path:
        return self._lock

    def record_growth(self, run, tests_s, conformance):
        if "growth-unwritable" in self.plant:
            raise OSError("planted: the growth directory is read-only")
        self.growth.append((run.name, tests_s, conformance))

    def toolchain(self) -> tuple[bool, str]:
        """The planted host's tools: toolchain.env's, unless the arm plants `toolchain` (a JDK of another version)."""
        self.toolchain_checked = True
        if "toolchain" in self.plant:
            return False, "jdk 23.0.2 (toolchain.env 27.0.1)"
        return True, "dotnet-sdk 10.0.403, python 3.15.2"

    def fetch_bounce_results(self) -> str:
        return "build-local: ci bounce: none (a planted host asks GitHub nothing)"

    def take_slot(self, label, say):
        self.events.append("slot")
        if "slot-settings" in self.plant:
            raise ValueError("planted: settings.json turned malformed while the gate queued")
        self.slots_taken += 1
        if "audit-slot" in self.plant:   # the audit turns RED while the gate holds the slot, before it builds
            self.slot_taken.set()
            self.stopped_by_audit.wait(60)
        return Slot(0, 1, 1, 0.0, -1)  # a held slot's shape, no lock: the cap itself is gate_slot.py's self-test

    def default_base(self) -> str:
        return self.base_sha

    def lane(self, base, tree):
        """The planted lane: `inert` plants a change no test reads, `tooling-lane` one no compiler test reads; the
        classification itself is inert_lane.py's self-test."""
        if "inert" in self.plant:
            return inert_lane.Lane("inert", "planted: all 2 changed path(s) are inert", tooling=False, reads_tooling=False)
        if "tooling-lane" in self.plant:
            return inert_lane.Lane("tooling", "planted: 1 of 1 changed path(s) are tooling no compiler test reads", tooling=True,
                                   reads_tooling=True)
        return inert_lane.Lane("full", "planted: the full lane", tooling="tooling" in self.plant,
                               reads_tooling="tooling" in self.plant)

    def head(self) -> str | None:
        return "head-1"

    def head_tree(self) -> str | None:
        return self.committed_tree

    def measured_tree(self) -> str | None:
        return self.trees.pop(0) if len(self.trees) > 1 else self.trees[0]

    def audits(self, spawn, say, tooling) -> list[str]:
        self.audited, self.tooling_seen = True, tooling
        say("PLANTED-AUDIT-OUTPUT")
        if "audit-late" in self.plant:   # red only once a test host is running, so the hosts have to be STOPPED
            self.host_started.wait(60)
            return ["RENDERED VIEWS RED"]
        if "audit-slot" in self.plant:   # red only once the slot is held: the gate must build nothing
            self.slot_taken.wait(60)
            return ["RENDERED VIEWS RED"]
        return ["RENDERED VIEWS RED"] if "audit" in self.plant else []

    def stop_hosts(self) -> int:
        n = super().stop_hosts()
        self.stopped_by_audit.set()
        return n

    def built_self_tests(self, spawn):
        self.events.append("post-build self-tests")
        if "post-build-crash" in self.plant:
            raise RuntimeError("planted: a defect in the post-build self-tests' runner")
        if "post-build" in self.plant:
            return ["POST-BUILD SELF-TESTS RED"], ["--- post-build self-tests (0.0s) ---", "PLANTED-POST-BUILD-RED"]
        return [], ["--- post-build self-tests (0.0s) ---"]

    def fetch_corpus(self, spawn, say) -> list[str]:
        if "audit" in self.plant:   # the real fetch takes longer than the audits: the red is known before the slot
            self.stopped_by_audit.wait(60)
        self.events.append("fetch")
        return []

    def build(self, spawn) -> bool:
        self.built = True
        self.events.append("build")
        if "crash" in self.plant:
            raise RuntimeError("planted: a defect in the driver's own code")
        return "build" not in self.plant

    def listing(self, asm, spawn):
        if "population" in self.plant and asm == "Unit":
            raise PopulationError("Unit: planted: `--list-tests` exited 1 (a test class's static constructor threw)")
        path = synthetic_listing(self.root / f"src-listing-{asm}.txt", self.POPULATION[asm])
        text = Path(path).read_text(encoding="utf-8")
        return Counter(self.POPULATION[asm]), text

    def run_host(self, asm, run, env, spawn) -> int:
        self.envs[asm] = dict(env)
        if "audit-late" in self.plant:   # a host that runs until the gate stops it (Host.stop_hosts), writing nothing
            self.host_started.set()
            self.hosts_live.stopping.wait(60)
            return 1
        cases = [(n, ["Passed"]) for n in self.POPULATION[asm]]
        red = asm == "Conformance" and "red" in self.plant
        if red:
            cases[0] = (cases[0][0], ["Failed"])
        if asm == "Conformance" and "drop" in self.plant:
            cases = cases[1:]
        if asm == "Conformance" and "duplicate" in self.plant:
            cases.append(("N.Conf.A", ["Passed"]))
        if asm == "Conformance" and "foreign" in self.plant:
            cases.append(("N.Conf.NeverListed", ["Passed"]))
        if asm == "Conformance" and "skip" in self.plant:
            cases[-1] = (cases[-1][0], ["NotExecuted"])
        if not (asm == "Unit" and "no-trx" in self.plant):
            synthetic_trx(run / f"{asm}.trx", cases)
        passed = sum(o == ["Passed"] for _, o in cases)
        failed = sum(o == ["Failed"] for _, o in cases)
        log = [f"  Failed {cases[0][0]} [1 s]", "  Error Message:", "   PLANTED-RED-MESSAGE"] if red else []
        log.append(f"{'Failed!' if red else 'Passed!'}  - Failed:     {failed}, Passed:     {passed}, Skipped:     0, "
                   f"Total:     {len(cases)}, Duration: 1 s - WiseOwl.Cobol.Tests.{asm}.dll (net10.0)")
        (run / f"{asm}.log").write_text("\n".join(log) + "\n", encoding="utf-8", newline="\n")
        return 1 if red else 0


def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    isolate()  # ⛔ hermetic: the arms below make git repositories (scratch_repo.isolate, kb/Work PB1719)
    results: list[tuple[str, bool, str]] = []
    root = scratch_dir("gate-legs-selftest-")
    sink: list[str] = []

    def gate(name: str, **fake) -> tuple[Outcome, FakeHost]:
        d = root / name
        d.mkdir()
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
        arm("a whole planted population is GREEN, every assembly EXACT, one slot taken",
            o.verdict == "GREEN" and o.exit_code == 0 and all(p.exact for p in o.populations.values())
            and len(o.populations) == 3 and h.slots_taken == 1, o.line)
        arm("every assembly runs its WHOLE population in one host, handed no narrowing and no first classes",
            sorted(h.envs) == sorted(GATED_ASSEMBLIES) and sorted(r.asm for r in o.runs) == sorted(GATED_ASSEMBLIES)
            and all(FIRST_CLASSES not in v for v in h.envs.values()) and o.first == [], str(h.envs.keys()))
        o2 = [l for l in sink if l.startswith(VERDICT)]
        arm("the verdict line is the last line, and counts every assembly's discovered cases",
            sink[-1] == o.line and "Conformance 6/6" in o.line and "Unit 4/4" in o.line
            and "Characterization 2/2" in o.line and len(o2) == 1 and "LEG" not in o.line, o.line)
        runs = sorted(p.name for p in (root / "green" / "runs").iterdir() if p.name != RUNS_LOCK)  # its run numbers' lock
        rec = json.loads((root / "green" / "runs" / runs[0] / VERDICT_FILE).read_text(encoding="utf-8"))
        arm("the run directory is fresh and holds the verdict file, every listing, trx and log, and the record is "
            f"schema {RECORD_SCHEMA}",
            len(runs) == 1 and RUN_DIR.fullmatch(runs[0]) is not None and rec["verdict"] == "GREEN"
            and rec["schema"] == RECORD_SCHEMA
            and sorted(p.name for p in (root / "green" / "runs" / runs[0]).glob("*.trx"))
            == sorted(f"{a}.trx" for a in GATED_ASSEMBLIES)
            and len(list((root / "green" / "runs" / runs[0]).glob("listing-*.txt"))) == 3, str(runs))
        arm("a GREEN code-lane gate leaves ONE growth sample (kb/Work PB4333): its run, the tests_s its record holds and "
            "the Conformance cases it discovered",
            h.growth == [(runs[0], rec["timings"]["tests_s"], 6)] and "tests_s" in rec["timings"], str(h.growth))

        o, hg = gate("growth-unwritable", plant={"growth-unwritable"})
        arm("a growth sample that cannot be written is said and never changes a GREEN verdict",
            o.verdict == "GREEN" and o.exit_code == 0 and hg.growth == [] and said("the growth sample was not recorded"),
            o.line)

        o, _ = gate("drop", plant={"drop"})
        arm("a dropped case is NEVER RAN, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].never_ran.values()) == 1
            and said("NEVER RAN"), o.line)
        o, _ = gate("dup", plant={"duplicate"})
        arm("a case run twice is RAN TWICE, and the gate is RED",
            o.verdict == "RED" and sum(o.populations["Conformance"].ran_twice.values()) == 1, o.line)
        o, _ = gate("foreign", plant={"foreign"})
        arm("a trx definition the listing never printed is NOT IN THE POPULATION, and the gate is RED",
            o.verdict == "RED" and "Conformance POPULATION NOT EXACT" in o.line
            and o.populations["Conformance"].not_listed == Counter({"N.Conf.NeverListed": 1}), o.line)
        o, _ = gate("skip", plant={"skip"})
        arm("a skipped case is counted skipped, never as ran — and the gate stays GREEN",
            o.verdict == "GREEN" and o.populations["Conformance"].skipped == 1 and "skipped 1" in o.line, o.line)
        o, _ = gate("no-trx", plant={"no-trx"})
        arm("a host that wrote no trx is UNMEASURED, and the gate is RED",
            o.verdict == "RED" and "Unit POPULATION UNMEASURED" in o.line, o.line)

        o, h = gate("red", plant={"red"})
        arm("a red host makes the gate RED, exit 1, and every other host still ran its whole population",
            o.verdict == "RED" and o.exit_code == 1 and "Conformance RED" in o.line and len(o.runs) == 3
            and o.populations["Unit"].exact and "first_red_s" in o.timings and h.growth == [], o.line)
        arm("a red host's whole output, its failure message included, reaches the console through test_leg_report.py",
            said("PLANTED-RED-MESSAGE") and said("Conformance: RED"), "\n".join(sink[-20:]))

        # THE FIRST CLASSES (kb/Work PB4328): the classes red at this worktree's previous run, or in the merge-queue run
        # that bounced its pull request, are handed to every host to run first; the population is still whole.
        d = root / "first"
        d.mkdir()
        first_host = FakeHost(d, d / "gate.lock", plant={"red"}, trees=["tree-red"])
        Gate(first_host, d / "runs", say=sink.append).run()
        bounce = d / CI_BOUNCE_RESULTS / "test-results-Linux-shard-2"
        bounce.mkdir(parents=True)
        synthetic_trx(bounce / "Unit.trx", [("N.Bounced.Case", ["Failed"]), ("N.Unit.A", ["Passed"])])
        again_host = FakeHost(d, d / "gate.lock", trees=["tree-fixed"])
        sink.clear()
        again = Gate(again_host, d / "runs", say=sink.append).run()
        named = Path(again_host.envs["Unit"].get(FIRST_CLASSES, ""))
        arm("a re-gate hands every host the classes red at the previous run and in the bounce (one file, FIRST_CLASSES), "
            "says so, and still runs and checks the whole population",
            again.verdict == "GREEN" and again.first == ["N.Bounced", "N.Conf"]
            and all(v.get(FIRST_CLASSES) == str(named) for v in again_host.envs.values())
            and named.read_text(encoding="utf-8") == "N.Bounced\nN.Conf\n"
            and said("2 class(es) red at the previous run or the merge queue run first")
            and "2 class(es) first" in again.line and all(p.exact for p in again.populations.values()),
            f"{again.line} {again.first}")
        (bounce / "Unit.trx").write_text("<TestRun><Results><UnitTestResult outcome=\"" + chr(1) + "\"/>",
                                         encoding="utf-8", newline="\n")
        torn_host = FakeHost(d, d / "gate.lock", trees=["tree-torn"])
        sink.clear()
        torn = Gate(torn_host, d / "runs", say=sink.append).run()
        kept = list(run_dirs(d / "runs")[-1].glob("unreadable-*"))
        arm("an unreadable trx among the first classes' sources is named and KEPT in the run directory (never as a .trx a "
            "later reader would glob), and the gate goes on GREEN without it (kb/Work PB3661)",
            torn.verdict == "GREEN" and said(str(bounce / "Unit.trx")) and len(kept) == 1
            and kept[0].name == "unreadable-test-results-Linux-shard-2-Unit.trx.bad", f"{torn.line} {kept}")

        stray = {"WISEOWL_COBOL_GATE_FIRST": "elsewhere", "WISEOWL_COBOL_SHARD": "1/2",
                 "VSTestTestCaseFilter": "FullyQualifiedName~Nothing"}
        os.environ.update(stray)
        try:
            observations = Path("run") / "Unit.observations.log"
            env = host_env(None, observations)
            ordered = host_env(Path("run") / "first-classes.txt", observations)
        finally:
            for k in stray:
                os.environ.pop(k, None)
        arm("a test host's environment is scrubbed of a stray ordering file, a shard and a VSTest property, and carries "
            "this gate's own first classes only when it has some",
            not set(stray) & set(env) and ordered[FIRST_CLASSES] == str(Path("run") / "first-classes.txt")
            and "WISEOWL_COBOL_SHARD" not in ordered and "VSTestTestCaseFilter" not in ordered)
        arm("a test host writes the harness observation log into the run directory, one file per host (PB3108)",
            env.get("WISEOWL_COBOL_HARNESS_LOG") == str(observations) == ordered.get("WISEOWL_COBOL_HARNESS_LOG"),
            str(env.get("WISEOWL_COBOL_HARNESS_LOG")))

        o, h = gate("population", plant={"population"})
        arm("a population that cannot be listed is NOT RUN [population], exit 2, and the one verdict reader classifies "
            "it BRANCH: the tree's own test projects (review R2-1 of wave 1150)",
            o.verdict == "NOT RUN [population]" and o.exit_code == 2 and h.built
            and "planted: `--list-tests` exited 1" in o.line
            and read_verdict(o.line[len(VERDICT):]).branch_carried, o.line)

        o, h = gate("audit", plant={"audit"})
        arm("a red audit (a tracked rendered view) that is known before the slot STOPS the gate before the build: RED, the "
            "audit named, no test ran",
            o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line and "NO TEST RAN" in o.line
            and h.audited and not h.built and not o.runs and not h.envs and sink[-1] == o.line
            and said("PLANTED-AUDIT-OUTPUT"), o.line)
        arm("the corpus fetch runs BEFORE the gate slot is taken, a red audit never queues for one (kb/Work PB2524), and "
            "the audits are no event of the main thread's order: they run beside it (kb/Work PB4326)",
            h.slots_taken == 0 and h.events == ["fetch"] and o.slot is None
            and gate("order")[1].events == ["fetch", "slot", "build"],
            str(h.events))
        o, h = gate("audit-slot", plant={"audit-slot"})
        arm("an audit that turns RED while the gate queues for the slot ends the gate before the build: RED, the audit "
            "named, the slot released, nothing built (kb/Work PB4326)",
            o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line
            and "while the gate queued for the slot" in o.line and h.slots_taken == 1 and not h.built and not o.runs
            and h.events == ["fetch", "slot"], f"{o.line} {h.events}")
        o, h = gate("audit-late", plant={"audit-late"})
        arm("an audit that is RED only after the test hosts have started STOPS them: the verdict is RED, the audit "
            "named, the hosts' partial results are NOT measured (no population) (kb/Work PB4326)",
            o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line and "STOPPED them" in o.line
            and h.built and not o.runs and not o.populations and h.stopped_by_audit.is_set()
            and sink[-1] == o.line and said("PLANTED-AUDIT-OUTPUT"), f"{o.line} {h.events}")
        o, h = gate("tooling-no", plant=set())
        o2, h2 = gate("tooling-yes", plant={"tooling"})
        rec = json.loads(run_dirs(root / "tooling-yes" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
        rec_no = json.loads(run_dirs(root / "tooling-no" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
        arm("the script self-tests (the audits in TOOLING_ONLY, and the post-build ones) run ONLY when the change touches "
            "tooling: the audits are told so, the post-build half runs beside the hosts only then, and the record says "
            "which (kb/Work PB4326)",
            h.tooling_seen is False and h2.tooling_seen is True and "post-build self-tests" not in h.events
            and "post-build self-tests" in h2.events and rec["tooling"] is True and rec_no["tooling"] is False,
            f"{h.events} {h2.events} {rec['tooling']} {rec_no['tooling']}")
        arm("the audits a change that touches no tooling runs are AUDITS less TOOLING_ONLY (SELF-TESTS), in order; with "
            "tooling they are all of AUDITS (the real host; kb/Work PB4326)",
            [n for n, _ in Host(root).audit_commands(False)] == [n for n, _ in AUDITS if n not in TOOLING_ONLY]
            and "SELF-TESTS" not in [n for n, _ in Host(root).audit_commands(False)]
            and Host(root).audit_commands(True) == AUDITS and TOOLING_ONLY == ("SELF-TESTS",)
            and "SELF-TESTS" in [n for n, _ in AUDITS], str(TOOLING_ONLY))
        two = Outcome(runs=[HostRun("Unit", 0, False, 30.0, 30.0), HostRun("Conformance", 0, False, 60.0, 60.0)],
                      timings={"tests_s": 60.0})
        arm("the verdict file records, per assembly, the seconds its host ran and the hosts' effective concurrency "
            "(their summed wall seconds over the span they took; kb/Work PB4326)",
            sorted(rec_no["assembly_wall_s"]) == sorted(GATED_ASSEMBLIES)
            and all(isinstance(v, float) for v in rec_no["assembly_wall_s"].values())
            and isinstance(rec_no["effective_concurrency"], float)
            and Gate._concurrency(two) == 1.5 and Gate._assembly_walls(two) == {"Conformance": 60.0, "Unit": 30.0}
            and Gate._concurrency(Outcome()) is None and Gate._assembly_walls(Outcome()) == {},
            f"{rec_no['assembly_wall_s']} {rec_no['effective_concurrency']}")
        o, h = gate("toolchain", plant={"toolchain"})
        arm("a host whose tools are not toolchain.env's is NOT RUN [toolchain] before anything runs, naming the tool, "
            "exit 2, an ENVIRONMENT cause (kb/Work PB4007)",
            o.verdict == NOT_RUN_CAUSES["toolchain"].verdict and o.exit_code == 2 and h.toolchain_checked
            and "jdk 23.0.2 (toolchain.env 27.0.1)" in o.line and h.events == [] and not o.runs
            and not read_verdict(o.line[len(VERDICT):]).branch_carried, o.line)
        o, h = gate("toolchain-named")
        arm("a gate names the toolchain it ran under, in its verdict line and its record (kb/Work PB4007)",
            "toolchain dotnet-sdk 10.0.403, python 3.15.2" in o.line and o.toolchain == "dotnet-sdk 10.0.403, python 3.15.2",
            o.line)
        o, h = gate("inert", plant={"inert"})
        arm("a change in the INERT LANE (kb/Work PB2878) runs the audits and then ends GREEN, with no fetch, slot, build "
            "or test host, the lane recorded",
            o.verdict == "GREEN" and o.exit_code == 0 and "INERT LANE (kb/Work PB2878)" in o.line
            and h.audited and h.events == [] and not h.built and not o.runs and o.lane == "inert"
            and o.tooling is False and h.tooling_seen is False and h.growth == []
            and read_verdict(o.line[len(VERDICT):]) == GREEN_READING, f"{o.line} {h.events}")
        o, h = gate("inert-audit", plant={"inert", "audit"})
        arm("a red audit is RED in the inert lane too: the audits ARE the inert lane's gate",
            o.verdict == "RED" and o.exit_code == 1 and h.audited and h.events == [] and o.lane == "inert"
            and "RENDERED VIEWS RED" in o.line, o.line)
        o, h = gate("tooling-lane", plant={"tooling-lane"})
        rec = json.loads(run_dirs(root / "tooling-lane" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))
        arm("a change in the TOOLING LANE (kb/Work PB4395) runs the audits WITH the script self-tests and then ends "
            "GREEN, with no fetch, slot, build or test host, its record saying `lane: tooling`",
            o.verdict == "GREEN" and o.exit_code == 0 and "TOOLING LANE (kb/Work PB4395)" in o.line
            and h.audited and h.tooling_seen is True and h.events == [] and not h.built and not o.runs
            and o.lane == "tooling" and rec["lane"] == "tooling" and rec["tooling"] is True and h.growth == []
            and read_verdict(o.line[len(VERDICT):]) == GREEN_READING, f"{o.line} {h.events}")
        o, h = gate("tooling-lane-audit", plant={"tooling-lane", "audit"})
        arm("a red audit is RED in the tooling lane: the audits and the self-tests ARE its gate",
            o.verdict == "RED" and o.exit_code == 1 and h.audited and h.events == [] and not h.built
            and o.lane == "tooling" and "RENDERED VIEWS RED" in o.line, o.line)
        o, h = gate("post-build", plant={"post-build", "tooling"})
        arm("a red post-build self-test (one that reads the built assemblies, kb/Work PB2563) runs beside the hosts and "
            "makes the gate RED after them, its output printed whole",
            o.verdict == "RED" and o.exit_code == 1 and "POST-BUILD SELF-TESTS RED" in o.line
            and len(o.runs) == 3 and not any(r.red for r in o.runs)
            and said("PLANTED-POST-BUILD-RED"), o.line)
        o, h = gate("slot-settings", plant={"slot-settings"})
        arm("shared gate settings that turn malformed while the gate queues: NOT RUN [settings] (ENVIRONMENT), exit 2, "
            "with the reason, the audits already run",
            o.verdict == "NOT RUN [settings]" and o.exit_code == 2 and "while the gate queued" in o.line
            and not h.built and not read_verdict(o.line[len(VERDICT):]).branch_carried
            and h.events == ["fetch", "slot"], o.line)

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
        reasons = unready.audits(_no_slot, heard_unready.append, True)
        arm("a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its "
            "output, and no audit runs (kb/Work PB1957 row 44)",
            reasons == ["PLANTED PREREQUISITE RED"] and unready.commands == [] and "not ready" in heard_unready,
            f"{reasons} {unready.commands} {heard_unready}")
        hung = HungSelfTests()
        reasons = hung.audits(_no_slot, lambda _: None, True)
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

            def audit_commands(self, tooling):
                slow = ("import pathlib, subprocess, sys; "
                        "p = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(3600)']); "
                        f"pathlib.Path({str(self.mark)!r}).write_text(str(p.pid), encoding='utf-8'); p.wait()")
                red = (f"import pathlib, time\nm = pathlib.Path({str(self.mark)!r})\n"
                       "while not m.exists():\n    time.sleep(0.05)\nraise SystemExit(1)")
                return (("SLOW", ["-c", slow]), ("QUICK-RED", ["-c", red]))
        mark = scratch_dir("first-red-") / "grandchild.pid"
        heard: list[str] = []
        reasons = FirstRedStops(mark).audits(_no_slot, heard.append, False)
        grandchild = int(mark.read_text(encoding="utf-8"))
        while pid_alive(grandchild):  # termination is asynchronous; a tree that never dies is the hang detector's
            time.sleep(0.05)
        arm("the first RED audit stops every audit still running, its whole process tree included, and the stopped one "
            "is reported STOPPED, never RED (kb/Work PB3061)",
            reasons == ["QUICK-RED RED"] and any(s.startswith("--- audit SLOW: STOPPED") for s in heard)
            and any("1 audit(s) still running stopped" in s for s in heard), f"{reasons} {heard}")
        o, h = gate("post-build-crash", plant={"post-build-crash", "tooling"})
        arm("a defect in work beside the critical path is a named RED with its traceback, and the hosts still ran",
            o.verdict == "RED" and o.exit_code == 1 and "POST BUILD SELF TESTS FAILED (RuntimeError" in o.line
            and len(o.runs) == 3 and said("planted: a defect in the post-build self-tests' runner"), o.line)
        o, h = gate("one-shape", plant={"red"})
        arm("ONE SHAPE (kb/Work PB3022): every gate takes a slot, runs its audits beside the build and the hosts and runs "
            "the whole population; there is no mode and no scope",
            h.slots_taken == 1 and h.events[:3] == ["fetch", "slot", "build"] and o.verdict == "RED"
            and not {"mode", "scope", "leg1_only", "plan"} & set(json.loads(
                run_dirs(root / "one-shape" / "runs")[-1].joinpath(VERDICT_FILE).read_text(encoding="utf-8"))), o.line)
        o, _ = gate("crash", plant={"crash"})
        arm("a defect in the driver still ends in ONE verdict line (NOT RUN [driver], exit 2) — the callers block on "
            "it — which the one verdict reader classifies BRANCH: the driver is the tree's own code (review R2-1)",
            o.verdict == "NOT RUN [driver]" and o.exit_code == 2 and sink[-1] == o.line and "RuntimeError" in o.line
            and read_verdict(o.line[len(VERDICT):]) == not_run("driver")
            and said("Traceback"), o.line)
        o, _ = gate("build", plant={"build"})
        arm("a failed build is BUILD FAILED, exit 1, and no test host runs",
            o.verdict == "BUILD FAILED" and o.exit_code == 1 and not o.runs, o.line)

        # The first classes come from the newest EARLIER run that reached a verdict; five runs are kept.
        d = root / "history"
        (d / "runs").mkdir(parents=True)
        for i in range(7):
            p = d / "runs" / f"2026010{i}T000000Z-00000{i}"
            p.mkdir()
            (p / VERDICT_FILE).write_text("{}", encoding="utf-8", newline="\n")
            synthetic_trx(p / "Unit.trx", [("N.Unit.A", ["Passed"])])
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
        old = "LEG 1 ONLY (batched gating, PB2515)"   # the line of a record a gate before PB4328 wrote
        arm("the one verdict reader: GREEN and an older gate's LEG 1 ONLY: GREEN pass; RED, RED/INCOMPLETE, LEG 1 ONLY: "
            "RED and BUILD FAILED are RED; NOT RUN (this driver's and a hand-run Linux gate's) is NOT RUN with its cause's class, never RED "
            "(kb/Work PB3526), and one naming no known cause is UNCLASSIFIED, BRANCH; a Linux gate's GREEN (legs ...) "
            "passes",
            [read_verdict(v) for v in ("GREEN — x ===", f"{old}: GREEN — x", "RED — x", "RED/INCOMPLETE — x",
                                       f"{old}: RED — x", "BUILD FAILED — x", "NOT RUN [settings] — x",
                                       "NOT RUN [sdk] (no .NET SDK in this Linux environment) ===",
                                       "NOT RUN [disk] (777216 free inodes on the filesystem holding /x) ===",
                                       "NOT RUN [driver] — x", "NOT RUN — x", "NOT RUN [nosuch] — x", "NOT RUNNING",
                                       "GREEN (legs unit) ===", "GREENISH")]
            == [GREEN_READING, GREEN_READING, RED_READING, RED_READING, RED_READING, RED_READING, not_run("settings"),
                not_run("sdk"), not_run("disk"), not_run("driver"), Reading(NOT_RUN, UNCLASSIFIED), Reading(NOT_RUN, UNCLASSIFIED),
                RED_READING, GREEN_READING, RED_READING]
            and NOT_RUN_CAUSES["disk"].carried == ENVIRONMENT
            and NOT_RUN_CAUSES["merge-blocked"].carried == ENVIRONMENT
            and [c.tag for c in NOT_RUN_CAUSES.values() if c.carried == BRANCH]
            == ["driver", "merge-failed", "population", "arguments", "verdict", "unclassified"])
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
        own = re.findall(r'_finish\(out, \w+, (NOT_RUN[^,]*),', (REPO / "scripts" / "gate.py").read_text(
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

        # A MERGE-QUEUE BOUNCE'S REDS (kb/Work PB3644): the real Host fetches the failed merge-group run's test results of
        # the checked-out branch's pull request into the worktree, where `Gate._first` reads their red classes.
        # The collaborators are planted; `queue_facts` has its own arms for the fetch.
        trx_head = ('<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/'
                    'TeamTest/2010"><Results>')
        asked: list[list[str]] = []

        def bounce_gh(args: list[str]) -> tuple[int, str, str]:
            asked.append(list(args))
            if args[:2] == ["repo", "view"]:
                return 0, "o/r\n", ""
            if args[:2] == ["pr", "list"]:
                return 0, "547\n", ""
            if args[0] == "api":
                return 0, json.dumps({"workflow_runs": [{"id": 77, "head_branch": "gh-readonly-queue/main/pr-547-abc123",
                                                         "conclusion": "failure", "created_at": "2026-10-24T10:00:00Z"}]}), ""
            if args[:2] == ["run", "download"]:
                art = Path(args[args.index("--dir") + 1]) / "test-results-Linux-unit"
                art.mkdir(parents=True)
                (art / "unit.trx").write_text(
                    trx_head + '<UnitTestResult testId="t1" testName="N.Bounced.Case" duration="00:00:01" outcome="Failed" />'
                    '</Results><TestDefinitions><UnitTest name="N.Bounced.Case" id="t1"><TestMethod className="N.Bounced" '
                    'name="Case" /></UnitTest></TestDefinitions></TestRun>', encoding="utf-8", newline="\n")
                return 0, "", ""
            return 1, "", "unrouted"
        with_pr = Host(repo, gh=bounce_gh)
        note = with_pr.fetch_bounce_results()
        bounce_reds = red_classes(sorted((repo / CI_BOUNCE_RESULTS).rglob("*.trx")))
        arm("the real Host fetches the failed merge-group run of the checked-out branch's pull request into the worktree, "
            "where its red classes are read to run first (kb/Work PB3644, PB4328)",
            "failed merge-group run 77 of pull request #547" in note and bounce_reds == {"N.Bounced"}
            and ["pr", "list", "--head", "main"] == asked[1][:4], f"{note} {bounce_reds} {asked}")
        Host(repo, gh=lambda args: (1, "", "HTTP 502")).fetch_bounce_results()
        kept = red_classes(sorted((repo / CI_BOUNCE_RESULTS).rglob("*.trx")))
        arm("a GitHub that cannot be asked leaves the fetched results as they were (a non-observation changes nothing)",
            kept == {"N.Bounced"}, str(kept))
        scratch.git("checkout", "--quiet", "--detach")
        asked.clear()
        detached = Host(repo, gh=bounce_gh).fetch_bounce_results()
        scratch.git("checkout", "--quiet", "main")
        arm("a detached HEAD has no pull request: nothing is asked of GitHub",
            not asked and "none (no branch is checked out)" in detached, f"{detached} {asked}")
        shutil.rmtree(repo / "TestResults")   # (a real worktree ignores it; this scratch repo does not, and measures it)

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
            "and a run named before the numbers is the oldest (the first classes' newest earlier run, the reuse rule's, the "
            "pruning's)",
            [p.name for p in run_dirs(ordered.run_root)] == ["20261022T225440Z-2797b8", *made]
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
            and len(run_dirs(racing.run_root)) == 40, str(took))
        # A GATE THAT IS STILL RUNNING OWES A VERDICT (kb/Work PB3662): the one reader the landing and the lane share.
        # The reader is the gate lock itself (review R1-1: a pid file's process id is reused by Windows, an OS lock is not).
        livelock = d / "live-gate.lock"
        none_yet = live_gate(livelock)
        dead_run = Gate(FakeHost(d, livelock), root / "live", say=sink.append).new_run()
        write_pid(dead_run, os.getpid())   # a gate that DIED with no verdict, its pid since reused by a live process
        reused = live_gate(livelock)
        with ExclusiveLock.try_take(livelock, f"gate {d} (pid 7, since T{RUN_LABEL}{dead_run})"):
            seen = live_gate(livelock)
        with ExclusiveLock.try_take(livelock, f"gate {d} (pid 8, since T)"):
            starting = live_gate(livelock)
        arm("live_gate: a gate runs exactly while its gate lock is held (the holder's label names the run it writes, "
            "else the label is all there is); a free lock is none, even beside a run with no verdict whose gate.pid "
            "names a live process (a gate that died, its pid reused), and a lock the OS dropped reads free again",
            none_yet is None and reused is None and seen is not None and seen.run == dead_run
            and starting is not None and starting.run is None and "pid 8" in starting.what
            and live_gate(livelock) is None, f"{none_yet} {reused} {seen} {starting}")
        # the pid goes UNDER the lock (`gate_running` reads a pid file as proof the gate took its lock): a detached gate
        # that finds the lock held ends BUSY and writes none
        busy_run = root / "busy-run"
        busy_run.mkdir()
        with ExclusiveLock.try_take(livelock, f"gate {d} (pid 9, since T)"):
            busy = Gate(FakeHost(d, livelock), root / "busy", say=sink.append, run_dir=busy_run).run()
        arm("a detached gate that finds the gate lock held ends BUSY without a pid file: a gate.pid proves its writer "
            "took the lock",
            busy.verdict == BUSY and busy.exit_code == EXIT_BUSY and read_pid(busy_run) is None
            and read_record(busy_run) is not None, f"{busy.line} {read_pid(busy_run)}")
        arm("a re-gate of an unchanged tree answers from the recorded GREEN: no audit, slot, build or host runs, the "
            "verdict line says REUSED and names the run that measured it, exit 0",
            first.verdict == "GREEN" and again.verdict == first.verdict and again.exit_code == 0
            and h2.events == [] and h2.slots_taken == 0 and h2.growth == [] and not again.runs and "REUSED (kb/Work PB3536)" in again.line
            and again.reused_from is not None and again.reused_from in again.line
            and rec2 is not None and rec2["reused_from"] == again.reused_from and rec2["exit_code"] == 0,
            f"{again.line} {h2.events}")
        arm("a chain of reuses names the run that MEASURED the tree and its own verdict, never a reuse of a reuse",
            third.reused_from == again.reused_from and third.line.count("REUSED") == 1 and h3.events == [], third.line)
        changed, hc = regate(d, trees=["tree-2"])
        arm("a re-gate of a CHANGED tree runs the gate (fetch, slot, build, hosts)",
            "REUSED" not in changed.line and hc.events[:3] == ["fetch", "slot", "build"]
            and changed.verdict == "GREEN", f"{changed.line} {hc.events}")
        d = root / "reuse-red"
        d.mkdir()
        regate(d, plant={"red"})
        red_again, hr = regate(d)
        arm("a RED is never reused: the same tree is gated again",
            "REUSED" not in red_again.line and "build" in hr.events, red_again.line)
        d = root / "reuse-old"
        d.mkdir()
        older, _ = regate(d)
        older_run = run_dirs(d / "runs")[-1]
        rec_old = read_record(older_run)
        (older_run / VERDICT_FILE).write_text(json.dumps({**rec_old, "schema": SCHEMA_WHOLE - 1, "leg1_only": True}),
                                              encoding="utf-8", newline="\n")
        after_old, ho = regate(d)
        arm("a GREEN a gate before PB4328 wrote (record schema below SCHEMA_WHOLE: it may be LEG 1 ONLY) never answers a "
            "re-gate of the same tree: the whole population runs",
            older.verdict == "GREEN" and "REUSED" not in after_old.line and "build" in ho.events
            and after_old.verdict == "GREEN", after_old.line)
        d = root / "reuse-moved"
        d.mkdir()
        edited, _ = regate(d, trees=["tree-1", "tree-edited"])
        after_edit, he = regate(d)
        rebased, hb = regate(d, base="1" * 40)
        arm("a gate whose tree changed WHILE it ran is never reused, and another base (a landing's merged main) runs",
            edited.tree == "tree-1" and edited.tree_end == "tree-edited" and "REUSED" not in after_edit.line
            and "build" in he.events and "REUSED" not in rebased.line and "build" in hb.events,
            f"{after_edit.line} | {rebased.line}")

        # THE GATE HAS NO LINUX HALF (kb/Work PB4327; owner 2026-10-31, PIPELINE-REVIEW.md section 7): the Linux half of
        # the targeted leg re-ran on WSL the leg Windows had just run, and every merge group runs the whole Linux
        # population; a hand run of linux-gate.sh is the tool that reproduces a Linux CI red. So the gate runs nothing
        # on Linux, the record carries no `linux` key, and the verdict line names no Linux half.
        d = root / "record"
        d.mkdir()
        lin, hl = regate(d)
        lrec = read_record(run_dirs(d / "runs")[-1])
        arm("the gate runs nothing on Linux: one slot, no Linux call, no Linux key in the record and no Linux half in "
            "the verdict line (kb/Work PB4327)",
            lin.verdict == "GREEN" and lin.exit_code == 0 and hl.slots_taken == 1
            and not any("linux" in e for e in hl.events) and "linux" not in lrec and "Linux half" not in lin.line
            and lrec["head_tree"] == "tree-1", f"{lin.line} {hl.events} {sorted(lrec)}")
        # kb/Work PB3660: the record this driver writes is current for the lander that reads it: its schema is the
        # current one and at least the oldest the lander accepts, it carries every field the lander requires, and a
        # record without them (or of an older schema) reads as a gate older than the lander, not as a dirty tree.
        old_rec = {k: v for k, v in lrec.items() if k not in LANDING_FIELDS}
        arm("the record the gate writes is current for the landing (schema, every landing field, not 'older than the "
            "lander'); one lacking a landing field or of a schema below the landable one is, and names which",
            record_schema(lrec) == RECORD_SCHEMA >= LANDABLE_SCHEMA and all(f in lrec for f in LANDING_FIELDS)
            and predates_landing(lrec) is None
            and "no head_tree field" in (predates_landing(old_rec) or "")
            and "below 3" in (predates_landing({**lrec, "schema": 2}) or "")
            and predates_landing({**lrec, "schema": 3}) is None and record_schema({"schema": True}) == 0
            and record_schema({}) == 0, f"{lrec.get('schema')} {predates_landing(old_rec)}")
        # kb/Work PB3717: the sentence's note id is DERIVED from the schema (SCHEMA_NOTES), so a bump of RECORD_SCHEMA
        # that adds its row names the new note, and one that forgets the row is this arm's red (a drift test).
        globals_before = (globals()["RECORD_SCHEMA"], dict(SCHEMA_NOTES))
        try:
            globals()["RECORD_SCHEMA"] = RECORD_SCHEMA + 1
            SCHEMA_NOTES[RECORD_SCHEMA] = "PB9999"
            bumped = predates_landing(old_rec) or ""
        finally:
            globals()["RECORD_SCHEMA"] = globals_before[0]
            SCHEMA_NOTES.clear()
            SCHEMA_NOTES.update(globals_before[1])
        arm("the 'predates kb/Work <note>' sentence is derived from SCHEMA_NOTES: every schema from the oldest landable "
            "one to the current has a row, the sentence names the current schema's note, and a bump names its own",
            all(n in SCHEMA_NOTES for n in range(LANDABLE_SCHEMA, RECORD_SCHEMA + 1))
            and f"predates kb/Work {SCHEMA_NOTES[RECORD_SCHEMA]})" in (predates_landing(old_rec) or "")
            and "predates kb/Work PB9999)" in bumped,
            f"{RECORD_SCHEMA} {SCHEMA_NOTES} {predates_landing(old_rec)} | {bumped}")
        # THE ONE WAY TO WAIT FOR A GATE (kb/Work PB3536): its run directory's verdict file.
        d = root / "await"
        d.mkdir()
        done = d / "done"
        done.mkdir()
        (done / VERDICT_FILE).write_text(json.dumps({"verdict": "RED", "line": f"{VERDICT}RED — planted ===",
                                                     "exit_code": 1}), encoding="utf-8", newline="\n")
        live = d / "live"
        live.mkdir()
        write_pid(live, os.getpid())  # a pid that is ALIVE for the whole arm (this process)
        dead = d / "dead"             # a gate that died with no verdict: its pid since taken by a live process (this
        dead.mkdir()                  # one), the very case Windows and Linux both produce when they reuse a pid
        write_pid(dead, os.getpid())
        stamp_now = f"{dt.datetime.now(dt.timezone.utc):%Y%m%dT%H%M%SZ}"
        fresh = d / f"000090-{stamp_now}"   # a gate that has not taken its lock yet, so wrote no pid (kb/Work PB3707)
        fresh.mkdir()
        spawned = d / f"000091-{stamp_now}"  # ... and one whose launcher recorded the live child it spawned
        spawned.mkdir()
        write_spawn(spawned, os.getpid())
        child = subprocess.Popen([sys.executable, "-c", "pass"])
        child.wait()
        orphan = d / f"000092-{stamp_now}"   # ... a young run whose spawned child is gone: died before its lock
        orphan.mkdir()
        write_spawn(orphan, child.pid)
        aged = d / "000093-20200101T000000Z"  # ... a run with no pid, no verdict, long past the start-up window
        aged.mkdir()
        bogus = d / "000094-20261399T000000Z"  # ... and one whose name's stamp is no date at all: no age, never starting
        bogus.mkdir()
        wlock = d / "worktree.lock"
        with ExclusiveLock.try_take(wlock, f"gate {d} (pid 7, since T{RUN_LABEL}{live})"):   # the gate that runs `live`
            got = {n: watch(d / n, 0, lock_running(d / n, wlock))
                   for n in ("done", "live", "dead", fresh.name, spawned.name, orphan.name, aged.name, "missing")}
            other = watch(dead, 0, lock_running(dead, wlock))   # the lock is held, but by another run's gate
        phases = {n: gate_phase(d / n, None) for n in (fresh.name, spawned.name, orphan.name, aged.name, bogus.name)}
        arm("--await reads the verdict file (its line and exit code), PENDING (its own exit, never BUSY's) while the "
            "run's gate holds the gate lock (or is STARTING: spawned moments ago, its child alive, no lock yet), NOT RUN "
            "[verdict] (exit 2, BRANCH) when it DIED without a verdict EVEN WHEN gate.pid names a live process (a "
            "reused pid) or another run's gate holds the lock, and when a run with no pid is past the start-up window or "
            "its spawned child is gone, NOT RUN [run] (exit 2, ENVIRONMENT) with no run directory",
            got["done"] == (1, f"{VERDICT}RED — planted ===")
            and got["live"][0] == got[fresh.name][0] == got[spawned.name][0] == AWAIT_PENDING_EXIT != EXIT_BUSY
            and got["live"][1].startswith(AWAIT_PENDING) and "STARTING" in got[fresh.name][1]
            and phases == {fresh.name: STARTING, spawned.name: STARTING, orphan.name: DIED, aged.name: DIED, bogus.name: DIED}
            and got[orphan.name][0] == got[aged.name][0] == 2 and "DIED" in got[orphan.name][1]
            and "DIED" in got[aged.name][1]
            and got["dead"][0] == other[0] == 2 and read_verdict(got["dead"][1][len(VERDICT):]) == not_run("verdict")
            and "DIED" in got["dead"][1] and "DIED" in other[1]
            and got["missing"][0] == 2 and read_verdict(got["missing"][1][len(VERDICT):]) == not_run("run"), str(got))
        arm("a run directory outside any worktree's run root, or of a worktree that is gone, has no gate lock to ask (no "
            "gate can run there); a run of the repository's own checkout asks that checkout's gate lock",
            lock_of_run(d / "dead") is None and lock_of_run(root / "gone" / RUN_ROOT / "r") is None
            and lock_of_run(REPO / RUN_ROOT / "r") == Host(REPO).lock_path(),
            f"{lock_of_run(d / 'dead')} {lock_of_run(root / 'gone' / RUN_ROOT / 'r')}")
        # A handed run's name (review R1-1 of wave 1152): an agent returns `TestResults/build-local/<run>`, relative to
        # ITS worktree, and the wave and the planner read it from the main checkout.
        cwd = Path.cwd()
        os.chdir(live)   # a reader elsewhere: the handed name means the worktree's run, never one of the reader's own
        try:
            from_elsewhere = [watch(r, 0, gate_running(r))[0]
                              for r in (resolve_run(Path("await") / "done", w) for w in (root, None))]
            os.chdir(d)
            relative_here = watch(r := resolve_run("done", None), 0, gate_running(r))[0]
        finally:
            os.chdir(cwd)
        arm("a relative run name resolves against the worktree that gated it, wherever the reader runs; an absolute one "
            "is itself; with no worktree, the current directory",
            resolve_run(done, root / "elsewhere") == done and from_elsewhere == [1, 2] and relative_here == 1
            and resolve_run("TestResults/build-local/r", root / "wt") == root / "wt" / "TestResults" / "build-local" / "r",
            f"{from_elsewhere} {relative_here}")
        # a record from before schema 3 carries no exit code: the one verdict reader derives it
        arm("a record with no exit code takes it from its verdict through the one verdict reader",
            [_record_exit({"verdict": v}) for v in ("LEG 1 ONLY (batched gating, PB2515): GREEN", "GREEN", BUSY, "NOT RUN [driver]", "RED")]
            == [0, 0, EXIT_BUSY, 2, 1])

        # The follower and the detached gate, as real processes: a planted gate that prints, waits for a go file,
        # writes its verdict file and exits 1. Like `Gate.run` it takes the worktree's gate lock first, then writes its pid
        # under it, and holds the lock until the verdict file is written.
        plock = root / "planted-gate.lock"

        def planted_gate(run: Path, go: Path | None = None) -> list[str]:
            wait = f"gof = pathlib.Path({str(go)!r})\nwhile not gof.exists(): time.sleep(0.05)\n" if go else ""
            return [sys.executable, "-c", "import json, os, pathlib, sys, time\n"
                    f"sys.path.insert(0, {str(REPO / 'scripts')!r})\nimport gate as g\n"
                    f"r = pathlib.Path({str(run)!r})\n"
                    f"lock = g.ExclusiveLock.try_take(pathlib.Path({str(plock)!r}), "
                    f"'gate x (pid %d, since T{RUN_LABEL}%s)' % (os.getpid(), r))\n"
                    "g.write_pid(r, os.getpid())\nprint('PLANTED-GATE-OUTPUT', flush=True)\n"
                    f"{wait}"
                    f"line = {VERDICT!r} + 'RED — planted ==='\n"
                    "(r / 'verdict.json').write_text(json.dumps({'verdict': 'x', 'line': line, 'exit_code': 1}))\n"
                    "lock.release()\nprint(line, flush=True)\n"]
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
            code == 1 and "PLANTED-GATE-OUTPUT" in heard and heard[-1].startswith(f"{VERDICT}RED — planted")
            and read_pid(d / "r1") is not None, str(heard))
        # kb/Work PB3707: the launcher records the child it spawned (the only fact a reader has before the child takes the
        # gate lock), and a child that exits before its lock leaves a run that reads DIED at once, not after START_WINDOW_S.
        stillborn = d / f"000002-{dt.datetime.now(dt.timezone.utc):%Y%m%dT%H%M%SZ}"

        def make_stillborn() -> Path:
            stillborn.mkdir()
            return stillborn
        said: list[str] = []
        code = launch_and_follow(make_stillborn, lambda r: [sys.executable, "-c", "pass"], said.append)
        named = re.search(r"\(pid (\d+), run directory", " ".join(said))
        arm("the launcher records the child it spawned, and a spawned child that ends before taking the gate lock is a "
            "gate that DIED (NOT RUN [verdict], exit 2) the moment it ends, however young the run is",
            code == 2 and named is not None and read_spawn(stillborn) == int(named.group(1))
            and gate_phase(stillborn, None) == DIED and "DIED" in said[-1], f"{code} {said} {read_spawn(stillborn)}")
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
            "import gate as g",
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
        kill_tree(follower.pid)
        follower.wait()
        survived = gate_pid is not None and pid_alive(gate_pid)
        go.touch()
        handed = watch(run2, 120, lock_running(run2, plock))
        arm("a detached gate OUTLIVES its follower's tree kill and its verdict is read from the run directory",
            survived and handed == (1, f"{VERDICT}RED — planted ==="),
            f"survived {survived}; {handed}")
    finally:
        remove_tree(root)

    shape = audit_shape_arms()
    arm("ONE SHAPE (kb/Work PB3022): the audit names every line of the live trees that tells anyone to run a gate mode, "
        "skips history and frozen evidence, and finds the tree it walks", shape == "", shape)
    planted_ci = ("jobs:\n  audits:\n    runs-on: ubuntu-latest\n    steps:\n      - run: git submodule update --init sub/a\n"
                  "      - run: |\n          python3 scripts/a.py --check\n          python3 scripts/devlog.py check\n"
                  "      # python3 scripts/commented.py\n      - run: python3 scripts/b.py\n")
    planted_gate_audits = (("A", ["scripts/a.py", "--check"]),)
    planted_elsewhere = {"scripts/devlog.py": "x", "scripts/gone.py": "x"}
    planted_audits = ci_audits_not_in_gate(planted_ci, planted_gate_audits, (), planted_elsewhere, 3, "sub/a")
    arm("the CI invariant (kb/Work PB1957 row 44): a CI audit the gate does not run, a submodule it does not make ready "
        "and a recorded exception CI no longer runs are each named; a recorded exception CI runs and a commented-out "
        "command are not", planted_audits == [
            "CI's audits job runs scripts/b.py and the gate's AUDITS do not",
            "CI's audits job makes the submodule sub/a ready and the gate's AUDIT_PREREQUISITES do not",
            "scripts/gone.py is recorded as running elsewhere (CI_AUDITS_ELSEWHERE) but CI's audits job no longer runs it"],
        str(planted_audits))
    blind = ci_audits_not_in_gate(planted_ci, planted_gate_audits, (), planted_elsewhere, 4, "sub/z")[:2]
    arm("a reading of CI's audits job that saw too few commands, or not the submodule it must make ready, is a finding, "
        "never a clean answer over nothing", blind == [
            "CI's audits job runs only 3 `python3 <script>` commands: the reading is broken",
            "CI's audits job no longer makes sub/z ready: the reading is broken"], str(blind))
    real_ci = ci_audits_not_in_gate((REPO / ".github" / "workflows" / "build-and-test.yml").read_text(encoding="utf-8"))
    arm("THE REAL TREE: every command of CI's audits job is a gate audit, an audit prerequisite or a recorded exception "
        "(the next audit CI gains reaches the gate; kb/Work PB4395 moved this from a compiler test)", real_ci == [],
        str(real_ci))
    failed = [n for n, ok, _ in results if not ok]
    print(f"=== gate SELF-TEST: {'PASS' if not failed else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(failed)}" if failed else "") + ") ===")
    return 0 if not failed else 1


#: ⛔ ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; DESIGN-test-build-ci.md §3.17.3): the gate takes no mode,
#: so no brief, skill, script, doc or test may tell anyone to run one: a caller left naming the deleted lander mode would
#: fail at the parameter binder, and one naming the implementer mode would teach a distinction that no longer exists.
#: History (the DEVLOG, the kb/Work notes, frozen evidence) keeps its words. The `--audit-shape` audit (AUDITS' ONE SHAPE)
#: reads the tracked files of these trees and the root's Markdown files; until kb/Work PB4395 it was a compiler test
#: (GateLegDriftTests) that walked them all, which made every script, skill and workflow a path a compiler test reads.
SHAPE_MODE_RE = re.compile(r"(?:-Mode|--mode)\s+(?:" + "implementer" + "|" + "lander" + r")\b")
SHAPE_TREES = (".claude/", ".github/", "scripts/", "docs/", "tests/")
SHAPE_EXTENSIONS = (".md", ".py", ".ps1", ".sh", ".js", ".cs", ".yml", ".json")
SHAPE_SKIPPED = ("bin", "obj", "TestResults", "claude-skills", "evidence", "Work", "worktrees")


def audit_shape(root: Path, paths: list[str]) -> list[str]:
    """ONE SHAPE's offenders among the tracked `paths` (`SHAPE_MODE_RE`): `path:line: text` for each line that names a
    gate mode, in the shape trees and the root's Markdown files, history and frozen evidence left out."""
    out: list[str] = []
    for rel in paths:
        parts = rel.split("/")
        in_tree = rel.startswith(SHAPE_TREES) and rel.endswith(SHAPE_EXTENSIONS)
        if not (in_tree and not any(p in SHAPE_SKIPPED for p in parts[:-1])) and not (len(parts) == 1 and rel.endswith(".md")):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        out += [f"{rel}:{i}: {line.strip()}" for i, line in enumerate(text.splitlines(), 1) if SHAPE_MODE_RE.search(line)]
    return out


def audit_shape_main(root: Path = REPO) -> int:
    paths = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    tracked = [p for p in paths.decode("utf-8").split("\0") if p]
    if ".claude/skills/workstream/templates/MANDATORY-PRACTICES.md" not in tracked:
        print("=== ONE SHAPE: NOT RUN — the tracked tree has no MANDATORY-PRACTICES.md: the walk is broken ===")
        return 2
    bad = audit_shape(root, tracked)
    for b in bad:
        print(f"    {b}")
    print("=== ONE SHAPE: " + (f"RED — {len(bad)} line(s) tell someone to run a gate mode; the gate has ONE shape, "
                                "`pwsh scripts/build-local.ps1 -Priority BelowNormal` (kb/Work PB3022) ===" if bad
                                else "GREEN — no live line names a gate mode ==="))
    return 1 if bad else 0


def audit_shape_arms() -> str:
    """The audit on a planted tree: '' when every arm holds, else what failed."""
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        planted = {"scripts/a.ps1": "pwsh build-local.ps1 -Mode " + "lander\n", "docs/b.md": "ok\nrun --mode " + "implementer now\n",
                   "README.md": "-Mode " + "implementer\n", "DEVLOG/1.md": "-Mode " + "lander\n",
                   "docs/rearchitecture/evidence/x.md": "-Mode " + "lander\n", "tests/T.cs": "-Modest lander\n",
                   "src/a.cs": "-Mode " + "lander\n"}
        for rel, text in planted.items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(text, encoding="utf-8", newline="\n")
        got = [b.split(":", 2)[0] + ":" + b.split(":", 2)[1] for b in audit_shape(root, sorted(planted))]
        want = ["README.md:1", "docs/b.md:2", "scripts/a.ps1:1"]
        return "" if got == want else f"got {got}, want {want}"


def print_flush(line: str) -> None:
    print(line, flush=True)


def main() -> int:
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
    ap.add_argument("--audit-shape", action="store_true", help="the ONE SHAPE audit: no live line names a gate mode "
                    "(kb/Work PB3022)")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.audit_shape:
        return audit_shape_main()
    if args.await_run:
        run = resolve_run(args.await_run, args.worktree)
        code, line = watch(run, args.wait_s, gate_running(run))
        print(line, flush=True)
        return code
    if args.run:
        return Gate(Host(), REPO / RUN_ROOT, base=args.base, run_dir=Path(args.run)).run().exit_code
    gate = Gate(Host(), REPO / RUN_ROOT, base=args.base)
    return launch_and_follow(gate.new_run, lambda run: [sys.executable, str(Path(__file__).resolve()), "--run",
                                                        str(run), *(["--base", args.base] if args.base else [])],
                             print_flush)


if __name__ == "__main__":
    ensure_utf8(line_buffering=True)  # the gate log is tailed while the gate runs
    sys.exit(main())
