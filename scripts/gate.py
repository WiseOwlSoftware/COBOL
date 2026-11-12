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
first run of the merged tree, of Linux, of Release and of GitHub's runners (kb/Work/PB4323.md §5.1).

ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; DESIGN-test-build-ci.md §3.17.3), in this order:
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
  * TARGETS (kb/Work PB5207 stage 1, PB5218; scripts/buildgraph.py; DESIGN-test-build-ci.md section 3.14.12 item 5): each
    test assembly is a TARGET with declared inputs (scripts/build_targets.json) and its own KEY, a digest of those inputs'
    blob ids and the facets (platform, configuration, toolchain). A target is UP TO DATE exactly when the one store in the
    git common directory, shared by every worktree, holds a pass at its key: it starts no host, and the verdict line names
    the run and worktree whose pass stands. The others run; a host that ends green with its population EXACT records its
    pass. At a target's key an earlier RED run of this worktree leaves only its unpassed cases to run (kb/Work PB5099:
    WISEOWL_COBOL_GATE_ONLY, the population check composing the rest). The audits are never carried. When every target is
    up to date and the change touches no script, nothing is built (`GREEN — EVERY TARGET UP TO DATE …`).
  * GENERATED TARGETS (kb/Work PB5207 stage 2, PB5231): before any key, every generated target (the conformance suite's
    manifests, `conformance-suite`) is brought up to date at the measured tree (`Gate._generate`): up to date when the
    store records its output digest at its key and its outputs on disk have it; else its generator runs, TRACED, into
    `TestResults/generated/<target>/`. A test target that depends on it is keyed on that OUTPUT DIGEST, not on the
    generator's inputs, so a records-only change whose suite is byte-identical leaves Conformance up to date. The output
    directory is handed to the dependent's host and listing (`WISEOWL_COBOL_GATE_SUITE`, `host_env`), so the host runs
    the suite its key names, and is checked unchanged after the hosts (`_check_generated`). A generation that fails, reads
    outside its inputs, is not deterministic or runs while the tree changes ends the gate RED before any host.

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
import hashlib
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
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT, this_platform  # noqa: E402
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from gate_slot import (ExclusiveLock, GateSlots, Heartbeat, QueueAbandoned, Slot,  # noqa: E402
                       gate_class_from_env)
import process_job  # noqa: E402  — THE one held process tree: an audit's or a test host's whole tree, stopped as one (kb/Work PB5098)
from detached import kill_tree, pid_alive, spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536), THE one tree kill (PB4072), THE one liveness probe (PB4008)
sys.path.append(str(REPO / "scripts" / "orchestrator"))  # appended: no module of it shadows one of scripts/
from coord import NEVER_WORK  # noqa: E402  — THE paths that are never work, so never part of a measured tree
import queue_facts  # noqa: E402  — THE one reader of GitHub's facts: a merge-queue bounce's reds (kb/Work PB3644)
from test_population import (GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError,  # noqa: E402
                             UnreadableTrx, check as check_population, failed_results, list_population, not_passed, passed_cases,
                             parse_listing, red_classes, scrubbed_env, synthetic_listing, synthetic_trx)
from test_population import TRX_NS, trx_root  # noqa: E402


def result_outcomes(path) -> Counter:
    """EVERY RESULT A TRX RECORDS, by outcome (kb/Work PB5205): what a host's own verdict line counts (`Failed: F, Passed:
    P, Skipped: S, Total: T` are results, never definitions: a theory whose rows share a display name is one definition
    with several results). It lives here, beside its one caller (`--rederive`), and reads the file with
    `test_population.trx_root`, so a change to it is no change to what plans the population the compiler tests run. A file
    that cannot be read raises `test_population.UnreadableTrx` naming it."""
    return Counter(r.get("outcome", "") for r in trx_root(path).iter(TRX_NS + "UnitTestResult"))
from standard_streams import ensure_utf8  # noqa: E402
from test_leg_report import verdict_of  # noqa: E402  — THE reader of a host's verdict line

VERDICT = "=== BUILD-LOCAL GATE: "
#: ⛔ THE ONE ORDERING CHANNEL TO A TEST HOST (kb/Work PB4328): the path of a file of test class names, one per line, that
#: the host's orderer runs FIRST (tests/_shared/GateLegs.cs `GateOrderer.FirstVariable`, the same name). It orders, it
#: never selects: a class it names that the assembly lacks is ignored, every other class still runs, and the population
#: check proves both. Its prefix is a scrubbed one (test_population.is_scrubbed), so only this gate hands it to a host.
FIRST_CLASSES = "WISEOWL_COBOL_GATE_FIRST"
#: ⛔ THE ONE RETRY CHANNEL TO A TEST HOST (kb/Work PB5099): the path of a file of test display names, one per line, that
#: the host runs INSTEAD of its whole population (tests/_shared/GateLegs.cs `GateOnly`, the same name). Only a re-gate of a
#: tree an earlier run of this worktree already measured sets it (`Gate.carry`), for the cases that run did not pass; the
#: population check then composes the passes it carried (test_population.py `check`'s `carried`), so a case neither
#: carried nor run is still a named NEVER RAN. Its prefix is a scrubbed one, so only this gate hands it to a host.
ONLY_CASES = "WISEOWL_COBOL_GATE_ONLY"
#: Where a merge-queue BOUNCE's test results land (kb/Work PB3644): `queue_facts.fetch_bounce_results`, the one GitHub
#: reader, downloads the failed merge-group run's trx files of this branch's pull request here, one subdirectory per
#: artifact, and their red classes go first like the previous run's. No tool WRITES a trx file here, so it is no row.
CI_BOUNCE_RESULTS = Path("TestResults/ci-bounce")
RUN_ROOT = Path("TestResults") / "build-local"
#: The configuration the gate builds and tests (`Host.build`: `dotnet build` of the solution, the SDK's default): a facet of
#: every target's key (kb/Work PB5218), so a pass in it is never one of another configuration.
GATE_CONFIGURATION = "Debug"
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
    # kb/Work PB4886, PB5256: every HELD edition's catalog (inventory_schema.HELD, the schema's list, so a newly held
    # edition needs no line here) is this run's, and a reconciled edition's is complete (no finding), so a repair of a
    # transcription that is not regenerated cannot leave a stale denominator; an unreconciled edition's findings are its
    # catalog's, and a change of them is stale too.
    ("RULE CATALOGS", ["scripts/spec/extract_rule_catalog.py", "--held", "--check"]),
    # kb/Work PB5256: every older held edition's candidate pairing with its next edition is this run's, so the
    # adjudication of its change records starts from the catalogs as they are.
    ("LINEAGE CANDIDATES", ["scripts/spec/lineage.py", "--candidates", "--check"]),
    # kb/Work PB689, PB5256: the independent count critic, every held ISO edition: each rule block holds exactly the
    # ordinals its transcription prints, but where the catalog records a finding that explains the difference.
    ("CATALOG COVERAGE", ["scripts/spec/audit_catalog_coverage.py", "--held", "--check"]),
    # kb/Work PB4992: each held edition's inventory is a VIEW (its catalog joined with its store of adjudications), and
    # what the view refuses (an adjudication of a dropped rule, a copied catalog field or state, an adjudication decided
    # against other text) and an older edition's illegal change record are refused here, in every lane.
    ("INVENTORY VIEW", ["scripts/spec/inventory_view.py", "--check"]),
    # kb/Work PB4996: every edition gate of the compiler (a construct catalogue window, a grammar version predicate) rests on
    # an edge the rule lineage proves: a start on a NEW or CHANGED rule, an end on a REMOVED or CHANGED one.
    ("EDITION GATES", ["scripts/spec/edition_gates.py", "--check"]),
    ("SPEC CORRECTIONS", ["scripts/spec/verify_publishable.py"]),
    # kb/Work PB4850: the 1985 transcription is publishable and agrees with its reconciliation ledger (LEDGER-1985.json).
    ("SPEC CORRECTIONS 1985", ["scripts/spec/verify_publishable.py", "--edition", "1985"]),
    ("LINE ENDINGS", ["scripts/line_endings.py", "--check"]),
    # kb/Work PB735, PB2878: a byte-level fact about the committed tree, an audit so that no test reads every path.
    ("CONFLICT MARKERS", ["scripts/conflict_markers.py", "--check"]),
    # kb/Work PB4453: no compiler test reads a Markdown file (owner 2026-11-02), so what a document says about the compiler
    # or about another document is checked HERE, in every lane: the documentation audits (scripts/doc_audits.py), and the two
    # register audits that were Unit tests running these scripts to read docs/CONFORMANCE.md.
    ("DOC AUDITS", ["scripts/doc_audits.py", "--check"]),
    ("ARCH SUBSYSTEMS", ["scripts/arch/r2_subsystems.py", "--check"]),
    ("ANNEX A.1 REGISTER", ["scripts/spec/audit_annex_a1.py", "--check"]),
    ("DERIVATION REGISTER", ["scripts/spec/audit_derivations.py", "--check"]),
    # kb/Work PB5052: every 1985 figure verdict names the block of the transcription its page and ordinal say; it reads no
    # PDF, so the gate runs it where `sweep_figures.py --edition 1985 --check` (which measures the scan) cannot.
    ("FIGURE VERDICTS 1985", ["scripts/spec/sweep_figures.py", "--edition", "1985", "--identity"]),
    # kb/Work PB2878, PB4395: THE lanes' classification against the tree (every entry names a tracked path; every script
    # the build or a compiler test is seen to read is CODE_READS), a question over the committed tree, so an audit.
    ("INERT LANE", ["scripts/inert_lane.py", "check"]),
    # kb/Work PB5218: every repository read a test assembly's sources spell lies in its target's declared inputs
    # (scripts/build_targets.json), so a target found up to date at its key cannot have read a path its key left out.
    ("BUILD TARGETS", ["scripts/buildgraph.py", "check"]),
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
#: ⛔ THE AUDITS THAT READ ONLY WHAT THE CHANGE CHANGES (kb/Work PB5225): each is handed the change's cut point and its
#: measured tree (`--base <base> --tree <tree>`), the pair the lane is classified from. LINE ENDINGS read all 35,241
#: tracked files in every gate (13.2 s) although a file already on main was read when it landed; the full sweep is CI's
#: Linux `audits` job's, once per merge group, the backstop for a commit made outside git's attributes. A gate that has
#: no base or no measured tree (the full lane's fallback, `Host.lane`) runs them over everything.
CHANGE_SCOPED = ("LINE ENDINGS",)
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
    its registration; `stopped` holds the pids stopped, so their exits are reported STOPPED, never RED. Each is a
    ProcessJob (scripts/process_job.py, kb/Work PB5098), so a stop kills its WHOLE tree, an orphaned grandchild included,
    which `taskkill /T` cannot reach."""

    def __init__(self) -> None:
        self.lock = threading.Lock()
        self.stopping = threading.Event()
        self.procs: set[process_job.ProcessJob] = set()
        self.stopped: set[int] = set()

    def add(self, p: process_job.ProcessJob) -> None:
        with self.lock:
            self.procs.add(p)
            if self.stopping.is_set() and p.popen.poll() is None:
                self.stopped.add(p.pid)
                p.kill()   # its whole tree, orphans included (kb/Work PB5098)

    def discard(self, p: process_job.ProcessJob) -> None:
        with self.lock:
            self.procs.discard(p)

    def stop_all(self) -> int:
        """Stop every registered process still running, and every one registered from now on; returns how many."""
        with self.lock:
            self.stopping.set()
            running = [p for p in self.procs if p.popen.poll() is None]
            for p in running:
                self.stopped.add(p.pid)
                p.kill()
            return len(running)

    def stopped_any(self) -> bool:
        """Whether a stop KILLED a process (kb/Work PB5205): one was still running when `stop_all` ran, or was registered
        after it. A stop that found every process already exited cut nothing short, so their results are whole."""
        with self.lock:
            return bool(self.stopped)


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

    def take_slot(self, label: str, say: Callable[[str], None], abandon: Callable[[], bool]) -> Slot:
        """A slot of the GATE pool: the builds and test hosts the cap rations. `abandon` is polled while the gate queues:
        true withdraws its ticket and raises `QueueAbandoned` (kb/Work PB5322)."""
        return GateSlots.for_repo(self.repo).acquire(label, say=say, abandon=abandon)

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

    def graph(self) -> buildgraph.Graph:
        """THE BUILD'S GRAPH (scripts/build_targets.json, checked by `buildgraph.load_targets`): raises GraphError on a
        malformed one (a cycle, an unknown dependency)."""
        return buildgraph.load_targets()

    def targets(self) -> dict[str, buildgraph.TestTarget]:
        """The test targets of the graph, by ASSEMBLY."""
        return self.graph().by_assembly()

    def platform(self) -> str:
        """The platform facet of a target's key: this host's (`self_tests.this_platform`)."""
        return this_platform()

    def target_keys(self, tree: str, facets: dict[str, str], outputs: dict[str, str] | None = None,
                    refused: dict[str, str] | None = None) -> dict[str, str] | None:
        """EACH TEST TARGET'S KEY at the measured `tree` (kb/Work PB5218, PB5231; `buildgraph.keys`), by assembly: one `git
        ls-tree`, no file read. A target that depends on a generated one takes that target's OUTPUT DIGEST: from
        `outputs` (what this gate's generations made, `generate`), else from the store's record at its key (a re-derivation
        of an old run). None when git cannot say (then nothing is up to date and nothing is carried); raises
        buildgraph.Unresolved, naming the dependency, when a digest is in neither, unless `refused` is given: then that
        assembly is left out and its reason kept there, by assembly (`buildgraph.keys`)."""
        try:
            entries = buildgraph.tree_entries(self.repo, tree)
        except (subprocess.CalledProcessError, OSError):
            return None
        graph = self.graph()
        by_name: dict[str, str] | None = {} if refused is not None else None
        got = buildgraph.keys(graph, entries, facets,
                              outputs if outputs is not None else buildgraph.stored_outputs(self.store()), by_name)
        if refused is not None:
            refused.update({asm: by_name[t.name] for asm, t in graph.by_assembly().items() if t.name in by_name})
        return {asm: got[t.name] for asm, t in graph.by_assembly().items() if t.name in got}

    def generate(self, tree: str, facets: dict[str, str],
                 stable: Callable[[], bool]) -> dict[str, buildgraph.Generation]:
        """BRING EVERY GENERATED TARGET UP TO DATE at the measured `tree` (kb/Work PB5231; `buildgraph.bring_up_to_date`):
        each one whose store record at its key is matched by its outputs on disk runs nothing; the others run, traced,
        into `<worktree>/TestResults/generated/<target>/`, and their output digest is recorded. `stable` says whether the
        working tree is still `tree` (a generation that ran while it changed records nothing). Raises buildgraph.BuildError
        naming the target and what stopped it."""
        return buildgraph.bring_up_to_date(self.repo, self.graph(), buildgraph.tree_entries(self.repo, tree), facets,
                                           self.store(), stable=stable)

    def output_digest(self, generation: buildgraph.Generation) -> str | None:
        """The digest of `generation`'s output directory NOW (None when it is no longer the declared outputs): what the
        gate compares with the digest it keyed the dependents on, after their hosts ran."""
        try:
            return buildgraph.output_digest(generation.directory, self.graph().generated()[generation.target])[0]
        except buildgraph.Unresolved:
            return None

    def store(self) -> buildgraph.Store:
        """THE ONE STORE of the targets' passes, in the git common directory: every worktree's."""
        return buildgraph.Store.for_repo(self.repo)

    def head_reflog(self) -> list[tuple[str, int]] | None:
        """THIS WORKTREE'S HEAD REFLOG (kb/Work PB5205; `Gate.rederive`): (commit, Unix time HEAD moved to it), newest first;
        None when git cannot say. A linked worktree's HEAD reflog is its own."""
        try:
            text = self.git("log", "-g", "--date=unix", "--format=%H %gd", "HEAD")
        except (subprocess.CalledProcessError, OSError):
            return None
        moves = []
        for line in text.splitlines():
            m = re.fullmatch(r"([0-9a-f]{40}) HEAD@\{(\d+)\}", line.strip())
            if m is None:
                return None
            moves.append((m.group(1), int(m.group(2))))
        return moves

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
        # ⛔ A FILE, NEVER A PIPE (kb/Work PB5098): a pipe an orphaned grandchild still holds blocks its reader forever
        with tempfile.TemporaryFile("w+", encoding="utf-8", errors="replace", newline="\n") as fh:
            with process_job.ProcessJob.start([sys.executable, *cmd], cwd=self.repo, stdout=fh, stderr=subprocess.STDOUT,
                                              **spawn()) as p:
                if live is not None:
                    live.add(p)
                try:
                    rc = p.wait()
                finally:
                    if live is not None:
                        live.discard(p)
            fh.seek(0)
            out = fh.read()
        if live is not None and p.pid in live.stopped:
            return None, out, time.monotonic() - t
        return rc, out, time.monotonic() - t

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

    @staticmethod
    def gate_form(name: str, cmd: list[str], change: tuple[str, str] | None) -> list[str]:
        """An audit's command as THE GATE runs it: the self-tests reuse recorded PASSes (SELF_TESTS_REUSE), and a
        CHANGE_SCOPED audit reads only the `change` (base, measured tree) when the gate knows one."""
        if name == "SELF-TESTS":
            return [*cmd, SELF_TESTS_REUSE]
        if name in CHANGE_SCOPED and change is not None:
            return [*cmd, "--base", change[0], "--tree", change[1]]
        return cmd

    def audits(self, spawn: Spawn, say: Callable[[str], None], tooling: bool,
               change: tuple[str, str] | None) -> list[str]:
        """The audits (`audit_commands`), every one of them, CONCURRENTLY, once their prerequisites
        (`audit_prerequisites`, run first and in order) are ready, each in its gate form (`gate_form`: the self-tests
        reuse recorded PASSes, a CHANGE_SCOPED audit reads only `change`, the (base, measured tree) pair or None).
        Each audit's whole output is printed as one block when it finishes, so no two interleave.
        ⛔ THE FIRST RED STOPS THE REST (kb/Work PB3061): the audits still running are stopped, process trees and all
        (`_Live.stop_all`), and reported STOPPED — never RED — so a red found in 23 s is the verdict in 23 s and not
        after the slowest audit (the self-tests: 5–9 min in every first gate of wave 1062). Returns the RED reasons in
        AUDITS order; a hung self-test is `SELF-TESTS HANG`, never RED (kb/Work PB2914)."""
        if not_ready := self._prepared(spawn, say):
            return not_ready
        reds: dict[str, str] = {}
        live = _Live()
        commands = [(name, self.gate_form(name, cmd, change)) for name, cmd in self.audit_commands(tooling)]
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

    def listing(self, asm: str, spawn: Spawn, handed: dict[str, str] | None = None) -> tuple[Counter, str]:
        """`asm`'s population (`--list-tests`), its discovery handed the generated outputs its host is handed (`handed`,
        `Gate._handed`), so the listing and the run read one suite."""
        return list_population(self.repo / GATED_ASSEMBLIES[asm], cwd=self.repo, handed=handed, **spawn())

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
        The host is registered in `hosts_live` while it runs as the root of a ProcessJob, so `stop_hosts` ends its
        whole tree, orphans included (kb/Work PB4326, PB5098); a host that starts after the stop is killed at once and
        exits non-zero."""
        cmd = ["dotnet", "test", str(self.repo / GATED_ASSEMBLIES[asm]), "--no-build", "--results-directory",
               str(run), "--logger", f"trx;LogFileName={asm}.trx"]
        with open(run / f"{asm}.log", "wb") as fh:
            with process_job.ProcessJob.start(cmd, cwd=self.repo, stdout=fh, stderr=subprocess.STDOUT,
                                              **spawn(env=env)) as p:
                self.hosts_live.add(p)
                try:
                    return p.wait()
                finally:
                    self.hosts_live.discard(p)


def host_env(first: Path | None, observations: Path, only: Path | None = None,
             handed: dict[str, str] | None = None) -> dict[str, str]:
    """⛔ THE ONLY PLACE A TEST HOST'S ENVIRONMENT IS MADE (DESIGN-test-build-ci.md §3.14.3): the scrubbed environment — no stray
    WISEOWL_COBOL_GATE_*, WISEOWL_COBOL_SHARD, VSTest* or RunSettingsFilePath, so nothing can narrow the run — in English,
    with the one ordering channel (FIRST_CLASSES) when this gate has classes to run first, and the one retry channel
    (ONLY_CASES) when this host runs only the cases an earlier run of the tree did not pass. `handed`: each generated
    target the host's target depends on, its variable naming the output directory the gate generated and keyed the target
    on (kb/Work PB5231; `Gate._handed`), so the host runs exactly that suite. The test harness's observation
    log (every retry and non-observation of a child process, tests/_shared/ProcessObservation.cs) goes to `observations`
    in the run directory, one per host, so a non-observation is read beside the host's own log instead of in one temp
    file every gate on the host appends to (kb/Work PB3108)."""
    order = {FIRST_CLASSES: str(first)} if first is not None else {}
    retry = {ONLY_CASES: str(only)} if only is not None else {}
    return {**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en", **order, **retry, **(handed or {}),
            "WISEOWL_COBOL_HARNESS_LOG": str(observations)}


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
    gate_class: str | None = None  # the class the slot was queued under (gate_slot.GATE_CLASSES, kb/Work PB4888)
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
    #: THE RUNS THIS GATE COMPOSES WITH (kb/Work PB5099; `Gate.carry`): earlier runs of this worktree at the same code
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


#: THE ORIGINAL RECORD OF A RE-DERIVED RUN (kb/Work PB5205; `Gate.rederive`): the verdict file as its gate wrote it, kept
#: beside the re-derived one and never overwritten.
ORIGINAL_VERDICT_FILE = "verdict.original.json"
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


def run_started(run: Path) -> float | None:
    """WHEN A RUN'S GATE STARTED, by the filesystem's clock (kb/Work PB5205): the modification time of its `gate.pid`, which
    the gate writes under its lock BEFORE it measures the tree (`Gate.run`). The filesystem's clock, never the run name's
    stamp (the driver's clock, which a self-test moves): the window it opens is compared with the hosts' file times and
    HEAD's reflog, which are the same real clock. None when the run has no pid file."""
    try:
        return (run / PID_FILE).stat().st_mtime
    except OSError:
        return None


@dataclass
class Rederived:
    """WHAT A RUN'S OWN ON-DISK EVIDENCE PROVES (kb/Work PB5205; `Gate.rederive`): the record it re-derives (None when
    refused), what it found, and every reason it refused, each naming what is missing."""
    record: dict | None
    found: list[str]
    refusals: list[str]
    level: str = ""   # "stored" (every assembly proven EXACT), "passes" (the next gate lists), or "stored and passes"
    stored: dict[str, dict] = field(default_factory=dict)   # the assemblies proven EXACT, with their counts


@dataclass
class Carry:
    """WHAT EARLIER RUNS OF THIS WORKTREE LEFT AT EACH TARGET'S KEY (kb/Work PB5099, PB5218; `Gate.carry`): the runs they
    are, newest first, and each assembly's passes there."""
    runs: list[str]
    passed: dict[str, Counter]
    red_hosts: set[str] = field(default_factory=set)   # assemblies whose host was RED in a run whose passes are carried


def measuring_runs(run: Path) -> list[Path]:
    """THE RUN DIRECTORIES WHOSE TRX FILES ARE THE EVIDENCE OF `run`'s VERDICT (kb/Work PB5099, PB5218): the run that
    measured it (itself, or the one a REUSED record names), the runs its verdict composed with (`Outcome.composed_from`)
    and the run of each target's pass the store showed up to date (`Outcome.up_to_date`, any worktree's), those that
    still exist. The ONE resolution a reader of a gate's test results (the landing lane's classification of a merge
    queue red) goes through, so a composed GREEN is read by the runs that make it GREEN."""
    rec = read_record(run) or {}
    measured = run.parent / str(rec["reused_from"]) if rec.get("reused_from") else run
    rec = read_record(measured) or {}
    names = [measured.name, *(n for n in rec.get("composed_from") or [] if isinstance(n, str))]
    paths = [run.parent / n for n in names]
    # A target the store showed up to date (kb/Work PB5218): its pass's run, in whichever worktree recorded it.
    paths += [Path(u["evidence"]) for u in (rec.get("up_to_date") or {}).values()
              if isinstance(u, dict) and isinstance(u.get("evidence"), str)]
    return [p for p in dict.fromkeys(paths) if p.is_dir()]


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
                 now: Callable[[], dt.datetime] = lambda: dt.datetime.now(dt.timezone.utc),
                 heartbeat: Callable[[], Heartbeat] = Heartbeat, heartbeat_poll_s: float = 1.0):
        """`run_dir`: the run directory a detached gate's launcher made (`--run`); None makes a fresh one. `now`: the
        UTC clock a new run's name shows, never its order (`new_run`); the self-test stops it and turns it back.
        `heartbeat`: makes the clock of the line the test phase writes while the hosts run (`_run_hosts`), polled every
        `heartbeat_poll_s`; the self-test steps it."""
        self.host, self.run_root, self.base, self.say, self.run_dir = host, run_root, base, say, run_dir
        self.heartbeat, self.heartbeat_poll_s = heartbeat, heartbeat_poll_s
        self.now = now
        self._pool: concurrent.futures.ThreadPoolExecutor | None = None  # `run` owns it for the work `_beside` starts
        self._base_sha: str | None = None
        #: The generated targets this gate brought up to date (`_generate`), by name: what it hands the hosts of their
        #: dependents (`_handed`) and checks again after them (`_check_generated`).
        self._generations: dict[str, buildgraph.Generation] = {}

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
        nothing, and a COMPOSED one ran only the part of the population its earlier runs had not passed, kb/Work PB5099; nor one
        that found a target up to date, kb/Work PB5218). A sample that cannot be written is said and never changes the verdict: the trigger reads fewer gates."""
        conformance = out.populations.get("Conformance")
        if (out.verdict != "GREEN" or out.reused_from or out.composed_from or out.up_to_date
                or "tests_s" not in out.timings or conformance is None):
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
        its start and unchanged at its end — against THIS base, under THIS host's toolchain (kb/Work PB5218), written by a gate that runs the whole population (record
        schema 5 on, kb/Work PB4328: an older record may be a `LEG 1 ONLY` one). None when the tree could not be measured or
        no run qualifies: a RED is never reused, and another tree (a landing's merged head) always runs."""
        if not out.tree:
            return None
        tools: str | None = None
        for p in reversed(run_dirs(self.run_root)):
            rec = read_record(p) if p != current else None
            if (rec is not None and rec.get("exit_code") == 0 and rec.get("tree") == out.tree
                    and rec.get("tree_end") == out.tree and rec.get("base") == out.base
                    and record_schema(rec) >= SCHEMA_WHOLE):
                # ⛔ AND UNDER THIS HOST'S TOOLCHAIN (kb/Work PB5218): a GREEN measured under other tools is no answer here,
                # the rule a target's key has (its toolchain facet). Asked once, only when a record qualifies.
                if tools is None:
                    matched, tools = self.host.toolchain()
                    tools = tools if matched else ""
                if tools and rec.get("toolchain") == tools:
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

    def facets(self, toolchain: str) -> dict[str, str]:
        """The facets of every target's key for this gate: the host's platform, the configuration it builds and runs
        (GATE_CONFIGURATION) and the toolchain it runs under."""
        return {"platform": self.host.platform(), "configuration": GATE_CONFIGURATION, "toolchain": toolchain}

    def carry(self, current: Path, out: Outcome, todo: list[str]) -> Carry | None:
        """THE GATE'S COMPOSITION RULE, PER TARGET (kb/Work PB5099, owner 2026-11-09: "if only one test failed, any retry
        should test only that one case"; kb/Work PB5218): for each assembly in `todo` (a target the store does not show up
        to date), what the earlier runs of THIS WORKTREE passed at ITS key. An earlier run counts when it is a full-lane
        run that measured its own tree (a RED one included, its trx are its results; a REUSED record ran nothing) and its
        tree did not change while it ran; its trx of an assembly count when it recorded that assembly's key as this one.
        `_tested` then runs only the listed cases those passes do not cover. The per-case evidence stays in this worktree's
        run directories in stage 1 (kb/Work PB5218): the store holds whole-target passes only. None when no run
        qualifies."""
        if not out.tree or not out.keys:
            return None
        runs: list[str] = []
        red_hosts: set[str] = set()
        passed = {asm: Counter() for asm in GATED_ASSEMBLIES}
        for p in reversed(run_dirs(self.run_root)):
            rec = read_record(p) if p != current else None
            if (rec is None or rec.get("reused_from") or rec.get("lane") != "full" or record_schema(rec) < SCHEMA_WHOLE
                    or not rec.get("tree") or rec.get("tree_end") != rec["tree"]):
                continue
            keys = rec.get("keys") if isinstance(rec.get("keys"), dict) else {}
            found = {}
            try:
                for asm in todo:
                    if keys.get(asm) == out.keys.get(asm) and (p / f"{asm}.trx").is_file():
                        found[asm] = passed_cases([p / f"{asm}.trx"])
            except UnreadableTrx as e:
                self.say(f"build-local: ⚠ run {p.name}'s passes are not carried ({e})")
                continue
            if found:
                runs.append(p.name)
                for asm, names in found.items():
                    passed[asm] |= names
                red_hosts |= {r["asm"] for r in rec.get("runs") or []
                              if isinstance(r, dict) and r.get("red") and r.get("asm") in found
                              and not self._red_explained(p, r["asm"])}
        return Carry(runs, passed, red_hosts=red_hosts) if runs else None

    def record_passes(self, out: Outcome, run: Path, assemblies: list[str]) -> None:
        """RECORD WHAT WAS PROVEN (kb/Work PB5218): each assembly whose host was green (or that ran no host because its
        carried passes covered it) and whose population check was EXACT, at a tree that did not change while the gate
        ran, gets its pass in the store at its key. A red, a partial or an unmeasured population records nothing."""
        if not out.tree or out.tree_end != out.tree:
            return
        store = self.host.store()
        for asm in assemblies:
            pop, k = out.populations.get(asm), out.keys.get(asm)
            if pop is None or not pop.exact or k is None or any(r.asm == asm and r.red for r in out.runs):
                continue
            target = self.host.targets()[asm]
            store.record(target.name, k, {
                "assembly": asm, "facets": self.facets(str(out.toolchain)), "worktree": str(self.host.repo),
                "run": run.name, "evidence": str(run), "head": out.head, "tree": out.tree, "when": f"{self.now():%Y-%m-%dT%H:%M:%SZ}",
                "counts": {"discovered": pop.discovered, "ran": pop.ran, "skipped": pop.skipped, "carried": pop.carried},
                "composed_from": out.composed_from,
                # the output digest of each generated target it was keyed on and ran (kb/Work PB5231)
                "depends": {d: self._generations[d].outputs for d in target.depends if d in self._generations}})
            out.recorded.append(asm)

    def rederive(self, run: Path) -> Rederived:
        """RE-DERIVE A RUN'S POPULATION EVIDENCE FROM ITS OWN FILES (kb/Work PB5205; owner 2026-11-10: "We better not be
        running all those tests again, when only the self tests failed to run"). A gate before PB5205, or one an audit
        stopped, may have left complete, green test results under a record that says `populations: {}` and
        `tree_end: null`, which `carry` cannot use. This proves from the run directory and the worktree what such a record
        could not say, and refuses, naming what is missing, unless ALL of it holds:
          * the record is a full-lane, schema-5-on record of a run that measured its own tree (never a REUSED one);
          * THE TOOLCHAIN it recorded is the one this host reports now (`Host.toolchain`);
          * EVERY HOST FINISHED GREEN: the record lists each gated assembly's host with exit 0, its log ends in the host's
            own `Passed!  - Failed: 0, Passed: P, Skipped: S, Total: T` line, and its trx is COMPLETE against that line (T
            results, P Passed, S NotExecuted and nothing else: `result_outcomes`, the results that line counts). A host
            that never finished has no such line; a short trx, too few results;
          * EVERY REASON IS AN AUDIT'S (`audit_reasons`): a host-level or population reason is never re-derived;
          * THE TREE DID NOT MOVE while it ran: it measured HEAD's own tree (`tree` equals `head_tree`, so nothing
            uncommitted), HEAD's reflog puts HEAD at the recorded head when the run started (`run_started`: its
            `gate.pid`, written before the tree was measured) and records no move from then to the last host's last
            write. THE RESIDUE (kb/Work PB5205 review R1-2), named in the record: an uncommitted edit made while the run
            ran and undone at ANY time before this re-derivation is in no reflog, where the live gate, which measures
            `tree_end` after its hosts, would have refused it;
          * each target's key is computed from its RECORDED tree and toolchain (`Host.target_keys`), a dependent one on the
            generated output the RUN recorded it was handed (`_rederived_keys`, kb/Work PB5231 review R2-2): refused when
            the store now records another digest there, and that assembly REFUSED (never stored or carried) when the run
            predates the hand-off.
        Then the record gains `tree_end` (its tree), `keys`, `audit_reds` and `rederived` (when, the evidence, the
        residue). Its verdict, line and exit code are unchanged: it stays RED. An assembly for which a run of this worktree
        at the SAME KEY kept the listing (`listing-<asm>.txt`, the expected population) is checked EXACT against it by the
        live check (`check_population`) and, exact, its pass is recorded in the store ("stored": the next gate finds that
        target up to date); a listing it is not exact against is a refusal. An assembly with no such listing is "passes":
        `carry` takes its trx passes, and the next gate builds, lists, runs only the cases they do not cover (none, for a
        complete run), checks EXACT and records the pass then. Neither can make a partial run a pass."""
        found: list[str] = []
        refusals: list[str] = []
        rec = read_record(run)
        if rec is None:
            return Rederived(None, found, [f"{run} holds no readable verdict record"])
        if rec.get("reused_from") or rec.get("lane") != "full" or record_schema(rec) < SCHEMA_WHOLE or not rec.get("tree"):
            return Rederived(None, found, ["it is not a full-lane record (schema 5 on) of a run that measured its own tree"])
        if rec.get("rederived"):
            return Rederived(None, found, [f"it was re-derived already ({rec['rederived'].get('at')})"])
        if rec.get("tree_end") == rec["tree"] and rec.get("keys"):
            return Rederived(None, found, ["nothing to re-derive: the record already says its tree_end and its keys"])
        matched, tools = self.host.toolchain()
        if not matched or tools != rec.get("toolchain"):
            refusals.append(f"the toolchain it recorded ({rec.get('toolchain')}) is not this host's ({tools})")
        hosts = {r.get("asm"): r for r in rec.get("runs") or [] if isinstance(r, dict)}
        ends: list[float] = []
        for asm in GATED_ASSEMBLIES:
            r, log, trx = hosts.get(asm), run / f"{asm}.log", run / f"{asm}.trx"
            if r is None or r.get("rc") != 0 or r.get("red"):
                refusals.append(f"{asm}: the record does not show its host exiting green ({r})")
                continue
            try:
                line = verdict_of(log.read_text(encoding="utf-8", errors="replace").splitlines()) or ""
                ends += [log.stat().st_mtime, trx.stat().st_mtime]
            except OSError as e:
                refusals.append(f"{asm}: its host log or trx cannot be read ({e})")
                continue
            m = HOST_PASSED.search(line)
            if m is None:
                refusals.append(f"{asm}: its host log ends in no `Passed!` verdict line (it never finished): {line!r}")
                continue
            passed_n, skipped_n, total = (int(g) for g in m.groups())
            try:
                outcomes = result_outcomes(trx)
            except UnreadableTrx as e:
                refusals.append(f"{asm}: its trx cannot be read ({e})")
                continue
            got = (sum(outcomes.values()), outcomes["Passed"], outcomes["NotExecuted"])
            if got != (total, passed_n, skipped_n):
                refusals.append(f"{asm}: its trx is not complete against its host's verdict line (results, Passed, "
                                f"NotExecuted {got}; the host said {(total, passed_n, skipped_n)})")
                continue
            found.append(f"{asm}: host exit 0, `Passed!` {passed_n:,} of {total:,}, trx complete")
        audit, other = audit_reasons([str(x) for x in rec.get("reasons") or []])
        if other:
            refusals.append(f"reasons no audit gave: {other}")
        started = run_started(run)
        if rec.get("head_tree") != rec["tree"] or not rec.get("head"):
            refusals.append("it did not measure its HEAD's own tree (uncommitted changes, or no head recorded): no reflog "
                            "can show that tree stood")
        elif started is None or not ends:
            refusals.append("its start (its gate.pid's time) or its end (the hosts' last writes) is unknown")
        else:
            moves = self.host.head_reflog()
            end = max(ends)
            if moves is None:
                refusals.append("HEAD's reflog cannot be read")
            else:
                before = [c for c, t in moves if t < started]
                during = [(c, t) for c, t in moves if started <= t <= end]
                if not before or before[0] != rec["head"]:
                    refusals.append(f"HEAD's reflog does not put HEAD at {rec['head'][:12]} when the run started "
                                    f"({before[0][:12] if before else 'no entry before it'})")
                if during:
                    refusals.append("HEAD moved while the run ran: " + ", ".join(
                        f"{c[:12]} at {dt.datetime.fromtimestamp(t, dt.timezone.utc):%H:%M:%SZ}" for c, t in during))
                if not refusals:
                    found.append(f"HEAD stood at {rec['head'][:12]} (tree {rec['tree'][:12]}) from the run's start "
                                 f"{dt.datetime.fromtimestamp(started, dt.timezone.utc):%Y-%m-%dT%H:%M:%SZ} to its hosts' "
                                 f"last write {dt.datetime.fromtimestamp(end, dt.timezone.utc):%Y-%m-%dT%H:%M:%SZ}")
        keys, refused = self._rederived_keys(rec, refusals)
        if refusals or keys is None:
            return Rederived(None, found, refusals)
        new = {**rec, "tree_end": rec["tree"], "keys": keys, "audit_reds": audit}
        levels: dict[str, str] = {asm: f"refused: {why}" for asm, why in refused.items()}
        stored: dict[str, dict] = {}
        for asm in GATED_ASSEMBLIES:
            if asm in refused:
                found.append(f"{asm}: REFUSED, never stored or carried: {refused[asm]}")
                continue
            listing = self._listing_at(asm, keys[asm], run)
            if listing is None:
                levels[asm] = "passes"
                continue
            where, listed = listing
            pop = check_population(asm, listed, [str(run / f"{asm}.trx")])
            if not pop.exact:
                return Rederived(None, found, [f"{asm}: not EXACT against the listing of run {where} at its key: "
                                               f"{pop.verdict_line()}"])
            stored[asm] = {"discovered": pop.discovered, "ran": pop.ran, "skipped": pop.skipped, "carried": 0}
            found.append(f"{asm}: EXACT against the listing run {where} kept at its key")
            levels[asm] = "stored"
        new["rederived"] = {
            "at": f"{self.now():%Y-%m-%dT%H:%M:%SZ}", "by": "gate.py --rederive (kb/Work PB5205, PB5218)", "levels": levels,
            "evidence": found,
            "residue": "an uncommitted edit made while the run ran and undone at any time before this re-derivation is in "
                       "no reflog (the live gate, measuring tree_end after its hosts, would have refused it)"
                       + ("; an assembly with no listing of its key on disk is left to the next gate, which lists its "
                          "population and checks the carried passes EXACT against it" if "passes" in levels.values() else "")}
        new["populations"] = {**(rec.get("populations") or {}), **{a: {**c, "exact": True} for a, c in stored.items()}}
        kept = [v for a, v in levels.items() if a not in refused]
        level = ("stored" if all(v == "stored" for v in kept) else
                 "passes" if all(v == "passes" for v in kept) else "stored and passes") \
            + (f" ({', '.join(sorted(refused))} refused)" if refused else "")
        return Rederived(new, found, [], level, stored)

    def _rederived_keys(self, rec: dict, refusals: list[str]) -> tuple[dict[str, str] | None, dict[str, str]]:
        """THE KEYS A RE-DERIVED RUN STANDS AT (kb/Work PB5231 review R2-2): each assembly's key at the run's RECORDED
        tree and toolchain, a dependent one keyed on the output digest the RUN ITSELF recorded (`generated`: the suite
        its host was handed and ran), never the digest the store holds now. Where the store's record at that generated
        key holds another digest, the run is refused (`refusals`), naming both. A run that recorded no generation of a
        dependency (it predates the hand-off, and its host generated its own suite) has that dependent assembly REFUSED:
        returned in the second map, never stored or carried. Returns (the keys, None when git cannot say; the refused
        assemblies with why)."""
        facets = self.facets(str(rec.get("toolchain") or ""))
        generated = {n: g for n, g in (rec.get("generated") or {}).items() if isinstance(g, dict) and g.get("outputs")}
        store = self.host.store()
        for name, g in sorted(generated.items()):
            now = (store.lookup(name, str(g.get("key") or "")) or {}).get("outputs")
            if now is not None and now != g["outputs"]:
                refusals.append(f"{name}: the run was handed output {g['outputs'][:12]} at key {str(g.get('key'))[:12]}, "
                                f"and the store now records {now[:12]} there: its dependents' passes are not that run's")
        refused: dict[str, str] = {}
        keys = self.host.target_keys(rec["tree"], facets, {n: g["outputs"] for n, g in generated.items()}, refused)
        refused = {asm: f"its run recorded no generation it was handed ({why}; it predates the hand-off of kb/Work "
                        "PB5231, so its host generated its own suite)" for asm, why in refused.items()}
        if keys is None:
            refusals.append(f"the targets' keys of its tree {rec['tree'][:12]} cannot be computed")
        return keys, refused

    def _listing_at(self, asm: str, key: str, run: Path) -> tuple[str, Counter] | None:
        """The expected population of `asm` at its `key`: the listing (`listing-<asm>.txt`) a run of this worktree kept
        whose key of `asm` (recorded, or computed from its recorded tree and toolchain) is `key`; `run` first."""
        for p in [run, *reversed(run_dirs(self.run_root))]:
            f = p / f"listing-{asm}.txt"
            rec = read_record(p) or {}
            if not f.is_file() or not rec.get("tree"):
                continue
            recorded = rec.get("keys") if isinstance(rec.get("keys"), dict) else {}
            try:
                k = recorded.get(asm) or (self.host.target_keys(rec["tree"], self.facets(str(rec.get("toolchain") or "")))
                                          or {}).get(asm)
            except buildgraph.Unresolved:
                continue   # its key cannot be resolved (kb/Work PB5231), so its listing is of no key we know
            if k == key:
                try:
                    return p.name, parse_listing(f.read_text(encoding="utf-8"), str(f))
                except (OSError, PopulationError):
                    continue
        return None

    def write_rederived(self, run: Path, rd: Rederived) -> None:
        """Keep the record its gate wrote as ORIGINAL_VERDICT_FILE (never overwritten), write the re-derived one, and record
        in the store the pass of every assembly it proved EXACT (kb/Work PB5218)."""
        assert rd.record is not None
        rec = rd.record
        store = self.host.store()
        for asm, counts in rd.stored.items():
            store.record(self.host.targets()[asm].name, rec["keys"][asm], {
                "assembly": asm, "facets": self.facets(str(rec.get("toolchain") or "")), "worktree": str(self.host.repo),
                "run": run.name, "evidence": str(run), "head": rec.get("head"), "tree": rec.get("tree"),
                "when": f"{self.now():%Y-%m-%dT%H:%M:%SZ}", "counts": counts, "rederived": True})
        original = run / ORIGINAL_VERDICT_FILE
        if not original.exists():
            shutil.copy2(run / VERDICT_FILE, original)
        (run / VERDICT_FILE).write_text(json.dumps(rd.record, indent=1, sort_keys=True) + "\n", encoding="utf-8",
                                        newline="\n")

    @staticmethod
    def _red_explained(run: Path, asm: str) -> bool:
        """WHETHER A RED HOST'S RED IS THE CASES ITS TRX NAMES (kb/Work PB5099, review R1-1 and R2-1): the host printed a
        `Failed!` verdict whose Failed count equals the trx's Failed results, and there is at least one. Then a retry of
        those cases tests everything the red was about. Any other red (no verdict line, a count the trx does not
        account for, a crash after the last result, a cleanup failure) has a host-level cause no case carries, and the
        assembly runs whole again."""
        try:
            verdict = verdict_of((run / f"{asm}.log").read_text(encoding="utf-8", errors="replace").splitlines())
            failed = failed_results(run / f"{asm}.trx")
        except (OSError, UnreadableTrx):
            return False
        m = re.match(r"Failed!\s+-\s+Failed:\s+(\d+)", verdict or "")
        return bool(m) and failed > 0 and int(m.group(1)) == failed

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

    def _prune(self, current: Path, keep: tuple[str, ...] | list[str] = ()) -> None:
        """Delete the oldest runs past KEEP_RUNS, never `current` and never a run the verdict composes with (`keep`, the
        record's `composed_from`: DESIGN-test-build-ci §3.14.12 item 5, the record names runs a reader must find)."""
        for p in run_dirs(self.run_root)[:-KEEP_RUNS]:
            if p != current and p.name not in keep:
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
        # ⭐ THE TARGETS (kb/Work PB5207 stages 1 and 2, PB5218, PB5231; scripts/buildgraph.py): first every GENERATED target
        # is brought up to date (the conformance suite's manifests), because a test target that depends on one is keyed on
        # its OUTPUT; then each assembly's key at this tree; the store, shared by every worktree, answers which are UP TO
        # DATE (a pass at their key), and those start no host. The rest run, less what an earlier RED run of this worktree
        # passed at their key (`carry`, kb/Work PB5099).
        if (generated_red := self._generate(out, run, tools)) is not None:
            return generated_red
        store = self.host.store()
        for asm in GATED_ASSEMBLIES:
            k = out.keys.get(asm)
            rec = store.lookup(self.host.targets()[asm].name, k) if k else None
            if rec is not None:
                out.up_to_date[asm] = {"key": k, "worktree": rec.get("worktree"), "run": rec.get("run"),
                                       "evidence": rec.get("evidence"), "counts": rec.get("counts") or {}}
        todo = [asm for asm in GATED_ASSEMBLIES if asm not in out.up_to_date]
        for asm, u in out.up_to_date.items():
            self.say(f"build-local: {asm} is UP TO DATE: the store holds a pass at its key {u['key'][:12]} "
                     f"(run {u['run']} of {u['worktree']})")
        if not todo and not out.tooling:
            self._join_beside(out)
            if out.audit_reds:
                return self._audit_red(out, run, "the audits are the only work this gate has, and one is RED: NO TEST RAN")
            return self._all_up_to_date(out, run, started)
        carry = self.carry(run, out, todo)
        if todo:   # the GnuCOBOL corpus is a test host's: a gate that runs none fetches nothing
            out.reasons += self.host.fetch_corpus(_no_slot, self.say)
        if self._audits_known_red(out):
            return self._audit_red(out, run, "an audit was RED before the slot, the build and any test host: NO TEST RAN")
        t = time.monotonic()
        try:
            slot = self.host.take_slot(label, self.say, lambda: self._audits_known_red(out))
        except QueueAbandoned:
            # ⛔ THE VERDICT IS KNOWN, SO THE TICKET LEAVES (kb/Work PB5322): an audit went RED while the gate queued, so the
            # slot is never taken, the tickets behind this one are served, and the worker reads the verdict now.
            out.timings["slot_wait_s"] = round(time.monotonic() - t, 1)
            return self._audit_red(out, run, "an audit was RED while the gate queued for the slot; its ticket was "
                                   "withdrawn and the slot never taken: NO TEST RAN")
        except ValueError as e:  # the shared gate settings turned malformed while this gate queued: stop, with the reason
            return self._finish(out, run, NOT_RUN_CAUSES["settings"].verdict, str(e), 2)
        out.timings["slot_wait_s"] = round(time.monotonic() - t, 1)
        out.timings["slot_taken_s"] = round(time.monotonic() - started, 1)  # WHEN the slot was taken, from the gate's start (PB4888)
        out.gate_class = gate_class_from_env()
        out.slot = slot.describe()  # acquire() has printed `gate-slot: took slot k of N …`; the verdict line names it
        try:
            return self._tested(out, run, slot, started, carry, todo)
        finally:
            slot.release()

    def _generate(self, out: Outcome, run: Path, tools: str) -> Outcome | None:
        """THE GENERATED TARGETS, THEN THE KEYS (kb/Work PB5231). Every generated target is brought up to date at the
        measured tree (`Host.generate`: up to date when the store records its output digest at its key AND its outputs on
        disk have it, else it runs, traced), and each test target's key takes its dependencies' output digests
        (`Host.target_keys`): so a records-only change whose generated suite is byte-identical leaves Conformance's key,
        and Conformance, up to date. Sets `out.keys` and `out.generated`; returns the gate's RED end when a generated
        target could not be brought up to date (it failed, read outside its inputs, wrote an undeclared output, was not
        deterministic, or ran while the tree changed) or the graph is malformed: no test host can run on a suite that
        is not the key's. No measured tree: no generation and no key (nothing is up to date or recorded)."""
        if not out.tree:
            out.keys = {}
            return None
        facets = self.facets(tools)
        tree = out.tree
        try:
            self._generations = self.host.generate(tree, facets, lambda: self.host.measured_tree() == tree)
            out.generated = {n: g.record() for n, g in self._generations.items()}
            for g in self._generations.values():
                self.say(f"build-local: {g.describe()} — {g.why} (kb/Work PB5231)")
            out.keys = self.host.target_keys(tree, facets, {n: g.outputs for n, g in self._generations.items()}) or {}
        except (buildgraph.BuildError, buildgraph.GraphError) as e:
            self._join_beside(out)
            return self._finish(out, run, "RED", f"GENERATED TARGET RED — {type(e).__name__}: {e} — no test host ran: a "
                                f"dependent's key would not be the suite it runs (kb/Work PB5231; run {run.name})", 1)
        return None

    def _handed(self, asm: str) -> dict[str, str]:
        """What `asm`'s host (and its listing) is handed (kb/Work PB5231; `host_env`): for each generated target its target
        depends on, that target's variable naming the output directory this gate generated and keyed it on."""
        graph = self.host.graph()
        target = self.host.targets().get(asm)
        deps = target.depends if target is not None else ()
        return {graph.targets[d].env: str(self._generations[d].directory) for d in deps if d in self._generations}

    def _check_generated(self, out: Outcome, run: Path, ran: list[str]) -> set[str]:
        """THE HOSTS RAN THE KEYED OUTPUTS (kb/Work PB5231, review R1-2): after the hosts, each generated output a host of
        `ran` was handed must still have the digest its dependents were keyed on, and the host's observation log
        (`<run>/<asm>.observations.log`) must not record that it generated its own (`GeneratedTarget.own_mark`). Either
        failure is a RED reason; returns the assemblies concerned, whose passes are not recorded."""
        stale: set[str] = set()
        graph = self.host.graph()

        def red(asm: str, reason: str) -> None:
            stale.add(asm)
            if reason not in out.reasons:
                out.reasons.append(reason)
                self.say(f"=== {reason} ===")

        for asm in ran:
            target = self.host.targets().get(asm)
            for d in (target.depends if target is not None else ()):
                g = self._generations.get(d)
                if g is None:
                    continue
                now = self.host.output_digest(g)
                if now != g.outputs:
                    red(asm, f"GENERATED OUTPUT CHANGED: {d}'s output in {g.directory} is {str(now)[:12]} after the "
                             f"{asm} host, not the {g.outputs[:12]} {asm} was keyed on")
                # ⛔ INSIDE THE GATE THE HOST RUNS THE HANDED SUITE (kb/Work PB5231 R1-2): a host that generated its own
                # (its hand-off site records `own_mark` when no output was handed) ran a suite its key does not name.
                if graph.targets[d].recorded_own(run_log := buildgraph.observation_log(run, asm)):
                    red(asm, f"GENERATED BY THE HOST: the {asm} host generated its own {d} instead of the one the gate "
                             f"handed it ({run_log}), so its key is not what it ran")
        return stale

    def _audits(self, out: Outcome, tooling: bool) -> tuple[list[str], list[str]]:
        """The audits as work BESIDE the critical path (`_beside`, kb/Work PB4326): returns (their RED reasons, their output
        lines), printed whole when joined. The reasons are recorded in `out.audit_reds` BEFORE the hosts are stopped, so
        the gate that finds the stop reads why. A RED audit stops the test hosts (`Host.stop_hosts`): the verdict is RED
        whatever they would have found, and every second they ran on was waste (kb/Work PB2523). An audit phase that
        itself RAISES stops them too and is a named RED (`_join_beside`)."""
        lines: list[str] = []
        t = time.monotonic()
        try:
            # an unchanged self-test is REUSED (PB2914); a CHANGE_SCOPED audit reads only the change (PB5225)
            change = (out.base, out.tree) if out.base and out.tree else None
            reds = self.host.audits(_no_slot, lines.append, tooling, change)
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

    def _all_up_to_date(self, out: Outcome, run: Path, started: float) -> Outcome:
        """EVERY TARGET IS UP TO DATE (kb/Work PB5207, PB5218): the store holds a pass at each target's key, so no corpus
        fetch, slot, build or test host runs, and the change touches no script, so no post-build self-test is owed. The
        audits, which read THIS tree, ran now and were joined GREEN. The line names, per target, the run and worktree
        whose pass stands, and what ran now."""
        out.tree_end = self.host.measured_tree()
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        stands = "; ".join(f"{asm} (run {u['run']} of {Path(str(u['worktree'])).name}, "
                           f"{int((u.get('counts') or {}).get('ran', 0)):,} cases)" for asm, u in out.up_to_date.items())
        return self._finish(out, run, "GREEN", f"EVERY TARGET UP TO DATE (kb/Work PB5207, PB5218): the store holds a pass "
                            f"at each target's key: {stands}{self._generated_detail()}; what ran now: the audits, GREEN; no "
                            f"corpus fetch, build or test host ran · toolchain {out.toolchain} (run {run.name})", 0)

    def _generated_detail(self) -> str:
        """The verdict line's entry for the generated targets (kb/Work PB5231): `· conformance-suite generated in 13.1 s
        (output 1a2b3c4d5e6f, 5 file(s))`, or `… up to date …`."""
        return "".join(f" · {g.describe()}" for g in self._generations.values())

    def _plan(self, out: Outcome, run: Path, listings: dict[str, Counter], carry: Carry,
              todo: list[str]) -> dict[str, Path | None]:
        """THE RETRY AT EACH TARGET'S KEY (kb/Work PB5099, PB5218): per assembly of `todo`, the listed cases the carried
        passes (`carry`) do not cover, written one name per line to `<run>/only-<assembly>.txt` for the host's retry hook
        (ONLY_CASES). Returns, per assembly that has a case left, its retry file (None: its whole population, when the
        carried passes cover none of it); an assembly with none left has no host. Records the runs this gate composes
        with and how many cases it carries."""
        lefts = {asm: not_passed(listings[asm], carry.passed[asm]) for asm in todo}
        for asm in carry.red_hosts & set(todo):
            # ⛔ A HOST WHOSE RED THE CASES DO NOT EXPLAIN (`_red_explained`: a class or collection cleanup failure, a crash
            # after the last result, no verdict line, a failure count beside a failed case that the trx does not name)
            # has a cause no retry of cases reaches, and composing around it would turn a deterministic host failure
            # GREEN: the host runs the whole assembly again.
            carry.passed[asm] = Counter()
            lefts[asm] = dict(listings[asm])
        to_run = {asm: sum(left.values()) for asm, left in lefts.items()}
        carried = {asm: sum(listings[asm].values()) - to_run[asm] for asm in todo}
        plan: dict[str, Path | None] = {}
        for asm, left in lefts.items():
            if not left:
                continue
            if not carried[asm]:
                plan[asm] = None
                continue
            plan[asm] = run / f"only-{asm}.txt"
            plan[asm].write_text("".join(f"{n}\n" for n in left), encoding="utf-8", newline="\n")
        if any(carried.values()):
            out.carried, out.composed_from = {a: c for a, c in carried.items() if c}, carry.runs
            self.say(f"build-local: earlier runs of this worktree passed cases at these targets' keys (runs "
                     f"{', '.join(carry.runs)}), so only what they did not pass runs: "
                     + ", ".join(f"{a} {carried[a]:,} passed / {to_run[a]:,} to run" for a in todo)
                     + " (kb/Work PB5099, PB5218)")
        return plan

    def _tested(self, out: Outcome, run: Path, slot: Slot, started: float, carry: Carry | None,
                todo: list[str]) -> Outcome:
        """The build, the populations of the targets in `todo` (less what `carry` already passed), the post-build
        self-tests when the change touches a script, the checks, and the store records of what was proven: the part of
        the gate the slot rations."""
        spawn: Spawn = slot.spawn_kwargs
        if self._audits_known_red(out):   # one found in the instant between the queue's last look and the slot: build nothing
            return self._audit_red(out, run, "an audit was RED as the gate took the slot: NO TEST RAN")
        t = time.monotonic()
        if not self.host.build(spawn):
            return self._finish(out, run, "BUILD FAILED", "the solution did not build (see above)", 1)
        out.timings["build_s"] = round(time.monotonic() - t, 1)
        if self._audits_known_red(out):
            return self._audit_red(out, run, "an audit was RED before any test host started: NO TEST RAN")
        # The listings (the population each host's trx is checked against) and, when the change touches tooling, the
        # self-tests that read the BUILT assemblies (kb/Work PB2563, PB4326) run BESIDE the hosts: each only lists tests,
        # and the hosts are the gate's longest step.
        listed = self._pool_listings(spawn, todo)
        listings: dict[str, Counter] | None = None
        plan: dict[str, Path | None] = {asm: None for asm in todo}
        if carry is not None:
            # A RETRY needs the population first (what the carried passes leave is the listing less them), where a whole
            # run joins the listings after the hosts: ~2 s each, which the retry's saved minutes repay.
            try:
                listings = self._listings(listed, run)
            except PopulationError as e:
                return self._finish(out, run, NOT_RUN_CAUSES["population"].verdict,
                                    f"the population could not be listed: {e}", 2)
            plan = self._plan(out, run, listings, carry, todo)
        if out.tooling:
            self._beside(out, "post_build_self_tests", lambda: self.host.built_self_tests(spawn))
        first = self._first(out, run)
        hosts_started = time.monotonic()
        self._run_hosts(out, run, first, spawn, hosts_started, plan)
        out.timings["tests_s"] = round(time.monotonic() - hosts_started, 1)
        reds = [r for r in out.runs if r.red]
        if reds:
            out.timings["first_red_s"] = round(min(r.finished_s for r in reds), 1)
        self._join_beside(out)
        if self.host.hosts_live.stopped_any():
            # A RED audit stopped the hosts (`_audits`): what they wrote is a partial run, no population to measure. ⛔ A RED
            # audit that came after every host had exited (a script self-test that hung past the last host, kb/Work
            # PB5205) stopped none: their results are whole, so the populations are measured, each EXACT green one's pass
            # is recorded in the store, and the verdict is RED for the audit alone.
            return self._audit_red(out, run, "an audit that ran beside the test hosts was RED and STOPPED them: their "
                                   "results are partial and NOT measured")
        try:
            listings = listings or self._listings(listed, run)
        except PopulationError as e:
            return self._finish(out, run, NOT_RUN_CAUSES["population"].verdict,
                                f"the population could not be listed: {e}", 2)
        self._check_populations(out, listings, run, carry, todo)
        stale = self._check_generated(out, run, [r.asm for r in out.runs])
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        out.tree_end = self.host.measured_tree()  # a tree edited WHILE it was gated is never reused (`reusable`)
        self.record_passes(out, run, [a for a in todo if a not in stale])
        self._prune(run, out.composed_from)
        return self._verdict(out, run)

    def _pool_listings(self, spawn: Spawn, todo: list[str]) -> dict[str, concurrent.futures.Future]:
        """Start the `--list-tests` hosts (scrubbed, ~2 s each) of the assemblies in `todo` beside the test hosts;
        `_listings` joins them."""
        pool = concurrent.futures.ThreadPoolExecutor(max_workers=max(len(todo), 1), thread_name_prefix="listing")
        try:
            return {asm: pool.submit(self.host.listing, asm, spawn, self._handed(asm)) for asm in todo}
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

    def _run_hosts(self, out: Outcome, run: Path, first: Path | None, spawn: Spawn, hosts_started: float,
                   plan: dict[str, Path | None]) -> None:
        """The planned assemblies CONCURRENTLY, one host each, each printed through the one leg report
        (scripts/test_leg_report.py) in a fixed order whatever order they finished in. `plan` (`_plan`, kb/Work PB5099,
        PB5218) names each assembly that runs: None for its whole population, a retry file for its cases not yet passed;
        an assembly it does not name (up to date, or every case carried) starts no host."""
        asms = [a for a in GATED_ASSEMBLIES if a in plan]
        self.say("build-local: " + (", ".join(f"{a} ({'whole population' if plan[a] is None else 'cases not yet passed'})"
                                              for a in asms) or "no test host: nothing to run")
                 + (" concurrently" if len(asms) > 1 else ""))

        def one(asm: str) -> tuple[str, int, float, float]:
            env = host_env(first, buildgraph.observation_log(run, asm), plan[asm], self._handed(asm))
            t = time.monotonic()
            rc = self.host.run_host(asm, run, env, spawn)
            return asm, rc, time.monotonic() - t, time.monotonic() - hosts_started

        # THE HEARTBEAT (kb/Work PB5097): the hosts write only their own logs, so the gate's console log (the one the
        # loop's progress report reads as the group's act) is silent from the line above to the verdict, 20 minutes and
        # more of a whole-population run. A line at every HEARTBEAT_S says the gate is alive and in its test phase.
        stop_beat, finished = threading.Event(), []

        def beat() -> None:
            hb = self.heartbeat()
            while not stop_beat.wait(self.heartbeat_poll_s):
                if hb.due():
                    self.say(f"build-local: tests running, {len(finished)} of {len(asms)} test host(s) finished, "
                             f"{time.monotonic() - hosts_started:.0f} s since the hosts started")

        beater = threading.Thread(target=beat, name="heartbeat", daemon=True)
        beater.start()
        try:
            with concurrent.futures.ThreadPoolExecutor(max_workers=max(len(asms), 1)) as pool:
                results = {}
                for asm, rc, wall, fin in pool.map(one, asms):
                    results[asm] = (rc, wall, fin)
                    finished.append(asm)
        finally:
            stop_beat.set()
            beater.join()
        if self.host.hosts_live.stopped_any():
            return   # stopped by a RED audit (`_audits`): a killed host's log is no report, `_audit_red` ends the gate
        for asm in asms:
            rc, wall, fin = results[asm]
            # THE ONE RULE for what a host prints (kb/Work PB1573): a green host its verdict line, a red one its
            # COMPLETE output. Its exit code is the host's verdict: 0 green, anything else red.
            rep = subprocess.run([sys.executable, str(REPO / "scripts" / "test_leg_report.py"), "--name", asm,
                                  "--log", str(run / f"{asm}.log"), "--rc", str(rc)],
                                 capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
            for line in (rep.stdout + rep.stderr).splitlines():
                self.say(line)
            out.runs.append(HostRun(asm, rc, rep.returncode != 0, round(wall, 1), round(fin, 1)))

    def _check_populations(self, out: Outcome, listings: dict[str, Counter], run: Path, carry: Carry | None,
                           todo: list[str]) -> None:
        """Ran equals discovered, by name, per assembly of `todo` (scripts/test_population.py `check`): its trx against
        its listing, with the passes `carry` brings (a RETRY, kb/Work PB5099: an assembly it ran nothing in has no trx and
        is all carried). A host that ran and wrote no trx is UNMEASURED, a RED. A target up to date is not checked here:
        its pass was proven exact when it was recorded."""
        for asm in todo:
            trx = run / f"{asm}.trx"
            try:
                if not trx.exists() and (carry is None or any(r.asm == asm for r in out.runs)):
                    raise PopulationError(f"{asm}: its host wrote no trx")
                pop = check_population(asm, listings[asm], [str(trx)] if trx.exists() else [],
                                       carried=carry.passed[asm] if carry is not None else None)
            except PopulationError as e:
                out.reasons.append(f"{asm} POPULATION UNMEASURED ({e})")
                self.say(f"=== POPULATION {asm}: UNMEASURED — {e} ===")
                continue
            out.populations[asm] = pop
            for line in pop.report():
                self.say(line)

    def _verdict(self, out: Outcome, run: Path) -> Outcome:
        pops = out.populations

        def one(a: str) -> str:
            if a in out.up_to_date:
                u = out.up_to_date[a]
                return (f"{a} up to date (run {u['run']} of {Path(str(u['worktree'])).name}, "
                        f"{int((u.get('counts') or {}).get('ran', 0)):,} cases)")
            return f"{a} {pops[a].ran:,}/{pops[a].discovered:,} ran" if a in pops else f"{a} UNMEASURED"

        tally = " · ".join(one(a) for a in GATED_ASSEMBLIES)
        skipped = sum(p.skipped for p in pops.values())
        first = f"{len(out.first)} class(es) first" if out.first else "no class first"
        carried = sum(p.carried for p in pops.values())
        detail = (f"{tally} (skipped {skipped:,})" + self._generated_detail()
                  + (f" · passes recorded: {', '.join(out.recorded)}" if out.recorded else "")
                  + (f" · {carried:,} carried from run(s) {', '.join(out.composed_from)} (kb/Work PB5099)" if carried else "")
                  + (f" · {out.slot}" if out.slot else "")
                  + (f" · toolchain {out.toolchain}" if out.toolchain else "") + f" ({first}; run {run.name})")
        reds = [f"{r.asm} RED" for r in out.runs if r.red]
        problems = reds + out.reasons + [f"{a} POPULATION NOT EXACT" for a in pops if pops[a].defective]
        if problems:
            return self._finish(out, run, "RED", f"{'; '.join(problems)} — {detail}", 1)
        return self._finish(out, run, "GREEN", detail, 0)

    @staticmethod
    def _assembly_walls(out: Outcome) -> dict[str, float]:
        """Per assembly, the seconds its host ran (kb/Work PB4326): what explains a slow test phase, the one the growth
        trigger of kb/Work/PB4323.md §3.4 judges (`_record_growth`, scripts/gate_growth.py), with `_concurrency`."""
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
                "composed_from": out.composed_from, "carried": out.carried,
                "keys": out.keys, "up_to_date": out.up_to_date, "recorded": out.recorded, "audit_reds": out.audit_reds,
                "generated": out.generated,
                "slot": out.slot, "gate_class": out.gate_class, "lane": out.lane, "tooling": out.tooling, "first": out.first,
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
                 trees: list[str | None] | None = None, base: str = "0" * 40, keys: dict[str, object] | None = None,
                 tools: str = "dotnet-sdk 10.0.403, python 3.15.2", store_dir: Path | None = None,
                 platform: str = "windows", graph: buildgraph.Graph | None = None, repo: Path | None = None):
        """`trees`: the working tree the gate measures, call by call (its start, then its end; the last value
        repeats); `base`: the planted cut point; `keys`: each planted tree's inputs (`target_keys`, kb/Work PB5218): a
        string stands for every target (two trees with one string differ only in paths no target reads), a dict names it
        per assembly (an assembly it does not name is the tree's own); a tree not named is its own for every target;
        `tools` and `platform`: the toolchain and platform the host reports; `store_dir`: the store (default: one per
        planted worktree; two hosts handed one directory are two worktrees of one repository). `graph` and `repo`: a
        planted graph over a REAL scratch repository (kb/Work PB5231): the generated targets really run, traced, and the
        keys are the real ones (`Host.generate`, `Host.target_keys`); `trees` then names that repository's real trees.
        Without them the graph is the real targets file, nothing is generated and the keys are planted."""
        super().__init__(root)
        self.planted_graph = graph
        if repo is not None:
            self.repo = repo
        self.root, self._lock, self.plant, self.keys, self.tools = root, lock, set(plant), dict(keys or {}), tools
        self.store_dir, self.platform_name = store_dir or root / "store", platform
        self.trees, self.base_sha = list(trees or ["tree-1"]), base
        self.committed_tree = self.trees[0]  # HEAD's tree: a gate of a clean working tree measures it
        self.slots_taken = 0
        self.audited = self.built = self.toolchain_checked = False
        self.tooling_seen: bool | None = None   # what `audits` was told about the change (kb/Work PB4326)
        self.change_seen: tuple[str, str] | None = None   # the (base, measured tree) `audits` was handed (PB5225)
        #: Set when the gate stops the hosts for a RED audit; and when a test host starts (the "audit-late" arm's order).
        self.stopped_by_audit, self.host_started = threading.Event(), threading.Event()
        #: Set when every planted test host has returned: the "audit-after-hosts" arm's audit turns RED only then.
        self.hosts_done, self._hosts_returned = threading.Event(), 0
        #: Set by the "slow-host" arm's console once the gate has written a heartbeat line (kb/Work PB5097): its planted
        #: hosts stay in their test phase until then.
        self.beat_seen = threading.Event()
        self.reflog: list[tuple[str, int]] = [("head-1", 0)]   # `head_reflog`: (commit, Unix time HEAD moved to it)
        self.slot_taken = threading.Event()   # the "audit-slot" arm's ordering: the audit is red once the gate is queued
        self.ticket_live = False              # the "audit-slot" arm: the gate's ticket is in the planted queue
        #: The order the MAIN thread drove the host in: fetch, slot, build. The audits run beside it, in a thread of their
        #: own, so they are `audited`, never an event of this order.
        self.events: list[str] = []
        self.envs: dict[str, dict[str, str]] = {}
        self.listing_handed: dict[str, dict[str, str]] = {}   # what each listing was handed (`listing`, kb/Work PB5231)
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
        return True, self.tools

    def fetch_bounce_results(self) -> str:
        return "build-local: ci bounce: none (a planted host asks GitHub nothing)"

    def take_slot(self, label, say, abandon):
        self.events.append("slot")
        if "slot-settings" in self.plant:
            raise ValueError("planted: settings.json turned malformed while the gate queued")
        if "audit-slot" in self.plant:   # every slot is held: the gate queues, and the audit turns RED meanwhile
            self.slot_taken.set()   # the gate is in the queue
            self.ticket_live = True
            deadline = time.monotonic() + 60
            try:
                while not abandon():
                    if time.monotonic() > deadline:
                        raise AssertionError("planted: the queued gate never saw the RED audit (it would have waited on)")
                    time.sleep(0.01)
            finally:
                self.ticket_live = False   # `acquire` drops the ticket on any exit
            raise QueueAbandoned("planted")
        self.slots_taken += 1
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

    def platform(self) -> str:
        return self.platform_name

    def store(self) -> buildgraph.Store:
        return buildgraph.Store(self.store_dir)

    def graph(self) -> buildgraph.Graph:
        return self.planted_graph if self.planted_graph is not None else super().graph()

    def generate(self, tree, facets, stable):
        """The real generations over the planted graph's scratch repository (`graph`, kb/Work PB5231); none without one,
        once the graph has loaded (a malformed one raises here, as the real host's does)."""
        self.graph()
        return super().generate(tree, facets, stable) if self.planted_graph is not None else {}

    def target_keys(self, tree, facets, outputs=None, refused=None):
        """The planted keys: per assembly, the tree's `keys` value (else the tree itself) with the facets; the real ones
        over a planted graph (`graph`). The real key's digest over a real tree is buildgraph.py's self-test and the
        real-key arm here."""
        if self.planted_graph is not None:
            return super().target_keys(tree, facets, outputs, refused)
        planted = self.keys.get(tree, tree)
        out = {}
        for asm in GATED_ASSEMBLIES:
            value = planted.get(asm, tree) if isinstance(planted, dict) else planted
            out[asm] = f"{asm}:{value}|{facets['platform']}|{facets['configuration']}|{facets['toolchain']}"
        return out

    def head(self) -> str | None:
        return "head-1"

    def head_reflog(self):
        """The planted reflog (`reflog`, newest first): HEAD came to head-1 long before any run, unless an arm plants a move."""
        return list(self.reflog)

    def head_tree(self) -> str | None:
        return self.committed_tree

    def measured_tree(self) -> str | None:
        return self.trees.pop(0) if len(self.trees) > 1 else self.trees[0]

    def audits(self, spawn, say, tooling, change) -> list[str]:
        self.audited, self.tooling_seen, self.change_seen = True, tooling, change
        say("PLANTED-AUDIT-OUTPUT")
        if "audit-late" in self.plant:   # red only once a test host is running, so the hosts have to be STOPPED
            self.host_started.wait(60)
            return ["RENDERED VIEWS RED"]
        if "audit-slot" in self.plant:   # red only once the gate is queued for the slot: it must withdraw and build nothing
            self.slot_taken.wait(60)
            return ["RENDERED VIEWS RED"]
        if "audit-after-hosts" in self.plant:   # a self-test that hangs past the last host (kb/Work PB5205): no host to stop
            self.hosts_done.wait(60)
            return ["SELF-TESTS HANG"]
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

    def listing(self, asm, spawn, handed=None):
        self.listing_handed[asm] = dict(handed or {})
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
            with self.hosts_live.lock:
                self.hosts_live.stopped.add(-1)   # what `_Live.stop_all` records for a real host it kills
            return 1
        try:
            return self._planted_host(asm, run, env)
        finally:
            with self.hosts_live.lock:
                self._hosts_returned += 1
                if self._hosts_returned == len(GATED_ASSEMBLIES):
                    self.hosts_done.set()

    def _planted_host(self, asm, run, env) -> int:
        if "tamper-output" in self.plant:   # a host that writes into the generated output it was handed (kb/Work PB5231)
            for name, directory in env.items():
                if name.startswith("WISEOWL_COBOL_GATE_") and name not in (ONLY_CASES, FIRST_CLASSES):
                    for manifest in Path(directory).rglob("*.json"):
                        manifest.write_text("{}\n", encoding="utf-8", newline="\n")
        if "own-generation" in self.plant and asm == "Conformance":   # a host that ignored the hand-off (PB5231 R1-2)
            with open(env["WISEOWL_COBOL_HARNESS_LOG"], "a", encoding="utf-8", newline="\n") as log:
                log.write(f"2026-11-11T00:00:00Z\t1\t{buildgraph.OWN_GENERATION}  suite :: planted\n")
        if "slow-host" in self.plant:   # a host in its test phase, silent, until the gate's heartbeat line has been seen
            self.beat_seen.wait(60)
        only = env.get(ONLY_CASES)   # a RETRY (kb/Work PB5099): the host runs exactly the cases its file names
        names = Path(only).read_text(encoding="utf-8").splitlines() if only else self.POPULATION[asm]
        cases = [(n, ["Passed"]) for n in self.POPULATION[asm] if n in names]
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
        if asm == "Conformance" and "log-miscount" in self.plant:   # a red the trx does not account for: the log counts one more
            failed += 1
        log.append(f"{'Failed!' if red else 'Passed!'}  - Failed:     {failed}, Passed:     {passed}, Skipped:     0, "
                   f"Total:     {len(cases)}, Duration: 1 s - WiseOwl.Cobol.Tests.{asm}.dll (net10.0)")
        (run / f"{asm}.log").write_text("\n".join(log) + "\n", encoding="utf-8", newline="\n")
        return 1 if red or (asm == "Conformance" and "cleanup-red" in self.plant) else 0   # cleanup-red: all Passed, exit 1


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

        stray = {"WISEOWL_COBOL_GATE_FIRST": "elsewhere", "WISEOWL_COBOL_GATE_ONLY": "elsewhere", "WISEOWL_COBOL_SHARD": "1/2",
                 "VSTestTestCaseFilter": "FullyQualifiedName~Nothing"}
        os.environ.update(stray)
        try:
            observations = Path("run") / "Unit.observations.log"
            env = host_env(None, observations)
            ordered = host_env(Path("run") / "first-classes.txt", observations)
            retried = host_env(None, observations, Path("run") / "only-Unit.txt")
        finally:
            for k in stray:
                os.environ.pop(k, None)
        arm("a test host's environment is scrubbed of a stray ordering file, retry file, shard and VSTest property, and "
            "carries this gate's own first classes and retry file only when it has them",
            not set(stray) & set(env) and ordered[FIRST_CLASSES] == str(Path("run") / "first-classes.txt")
            and "WISEOWL_COBOL_SHARD" not in ordered and "VSTestTestCaseFilter" not in ordered
            and ONLY_CASES not in ordered and retried[ONLY_CASES] == str(Path("run") / "only-Unit.txt")
            and FIRST_CLASSES not in retried)
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
        arm("an audit that turns RED while the gate queues for the slot ends the gate AT ONCE (kb/Work PB5322): RED, the "
            "audit named, the ticket withdrawn, the slot never taken, nothing built, the verdict written and its line "
            "printed; a planted queue that never lets the gate in returns in seconds, not at the slot",
            o.verdict == "RED" and o.exit_code == 1 and "RENDERED VIEWS RED" in o.line
            and "ticket was withdrawn and the slot never taken" in o.line and h.slots_taken == 0 and not h.ticket_live
            and o.slot is None and not h.built and not o.runs and h.events == ["fetch", "slot"]
            and sink[-1] == o.line and run_dirs(root / "audit-slot" / "runs")[-1].joinpath(VERDICT_FILE).exists()
            and o.timings.get("slot_wait_s", 99) < 30, f"{o.line} {h.events}")
        d = root / "slow-host"
        d.mkdir()
        slow = FakeHost(d, d / "gate.lock", plant={"slow-host"})
        beats: list[str] = []

        def console(line: str) -> None:
            beats.append(line)
            if "tests running" in line:
                slow.beat_seen.set()

        steps = iter(range(0, 10**9, 100))   # the arm's own clock: 100 s a reading, a beat every 300 s
        o = Gate(slow, d / "runs", say=console, heartbeat=lambda: Heartbeat(300.0, lambda: float(next(steps))),
                 heartbeat_poll_s=0.01).run()
        arm("a gate in its test phase writes a heartbeat line to its console while its hosts run and say nothing "
            "(kb/Work PB5097), and the line names the hosts finished and the seconds since they started; the clock is "
            "the arm's own, so no wall-clock limit decides it",
            o.verdict == "GREEN" and any(re.search(r"build-local: tests running, 0 of \d+ test host\(s\) finished, \d+ s since the "
                                                   r"hosts started", b) for b in beats), f"{o.line} {beats[-5:]}")
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
        arm("the gate hands its audits the change it classified its lane from: (the base, the measured tree) (kb/Work "
            "PB5225)", h.change_seen == ("0" * 40, "tree-1") and (o.base, o.tree) == h.change_seen,
            f"{h.change_seen} {o.base} {o.tree}")
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
        reasons = unready.audits(_no_slot, heard_unready.append, True, None)
        arm("a failed audit prerequisite (CI's `git submodule update --init tools/claude-skills`) is its RED, with its "
            "output, and no audit runs (kb/Work PB1957 row 44)",
            reasons == ["PLANTED PREREQUISITE RED"] and unready.commands == [] and "not ready" in heard_unready,
            f"{reasons} {unready.commands} {heard_unready}")
        hung = HungSelfTests()
        reasons = hung.audits(_no_slot, lambda _: None, True, ("base-1", "tree-1"))
        arm("a hung self-test is `SELF-TESTS HANG` in the gate's reasons, never RED, and every gate's self-tests run "
            "with --reuse (kb/Work PB2914, PB3022)",
            reasons == ["SELF-TESTS HANG"] and ["scripts/self_tests.py", SELF_TESTS_REUSE] in hung.commands,
            f"{reasons} {hung.commands}")
        whole = HungSelfTests()
        whole.audits(_no_slot, lambda _: None, False, None)
        arm("a CHANGE_SCOPED audit (LINE ENDINGS) reads only the change the gate hands it, `--base <base> --tree "
            "<measured tree>`, and the whole tree when the gate has no change; no other audit is told (kb/Work PB5225)",
            CHANGE_SCOPED == ("LINE ENDINGS",)
            and ["scripts/line_endings.py", "--check", "--base", "base-1", "--tree", "tree-1"] in hung.commands
            and ["scripts/line_endings.py", "--check"] in whole.commands
            and sum("--base" in c for c in hung.commands) == 1 and not any("--base" in c for c in whole.commands),
            f"{hung.commands} {whole.commands}")

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
        reasons = FirstRedStops(mark).audits(_no_slot, heard.append, False, None)
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
        retooled, hrt = regate(d, tools="dotnet-sdk 9.0.100, python 3.12.0")
        arm("a recorded GREEN measured under another toolchain is never REUSED (kb/Work PB5218): the gate runs",
            "REUSED" not in retooled.line and "build" in hrt.events and retooled.verdict == "GREEN", retooled.line)
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
        remove_tree(d / "store")   # a gate before PB5218 recorded nothing in the store
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
        arm("a gate whose tree changed WHILE it ran is never reused, and another base is no answer to reuse (the reuse rule "
            "reads the base its lane was decided against); a run whose tree moved records no pass, and the same tree under "
            "another base finds every target up to date at its keys (kb/Work PB5218)",
            edited.tree == "tree-1" and edited.tree_end == "tree-edited" and "REUSED" not in after_edit.line
            and not edited.recorded and "build" in he.events and "REUSED" not in rebased.line
            and "EVERY TARGET UP TO DATE" in rebased.line and hb.events == [],
            f"{after_edit.line} | {rebased.line}")

        # THE COMPOSITION RULE (kb/Work PB5099; owner 2026-11-09: "if only one test failed, any retry should test only
        # that one case"): at a target's key, a re-gate runs only what an earlier run did not pass.
        d = root / "compose"
        d.mkdir()
        red1, _ = regate(d, plant={"red"})
        again_red, ha = regate(d, plant={"red"})
        arm("a retry that is still RED is RED (the carried passes hide nothing, the case runs again alone)",
            again_red.verdict == "RED" and again_red.populations["Conformance"].carried == 5
            and sorted(ha.envs) == ["Conformance"] and "Conformance RED" in again_red.line, again_red.line)
        fix, hf = regate(d)
        fix_rec = read_record(run_dirs(d / "runs")[-1])
        only_text = (run_dirs(d / "runs")[-1] / "only-Conformance.txt").read_text(encoding="utf-8")
        arm("a re-gate of a tree whose earlier run was RED runs ONLY that run's red case: one host, one retry file, the "
            "other assemblies up to date in the store, and the verdict is GREEN with Conformance EXACT",
            red1.verdict == "RED" and fix.verdict == "GREEN" and sorted(hf.envs) == ["Conformance"]
            and only_text == "N.Conf.A\n" and hf.envs["Conformance"].get(ONLY_CASES) is not None
            and fix.populations["Conformance"].exact and sorted(fix.up_to_date) == ["Characterization", "Unit"]
            and fix.carried == {"Conformance": 5} and fix.recorded == ["Conformance"]
            and "Conformance 6/6" in fix.line and "5 carried from run(s)" in fix.line and "build" in hf.events,
            f"{fix.line} {sorted(hf.envs)} {only_text!r}")
        arm("the composed record names the runs it composes with and what it carried, writes no growth sample, and "
            "`measuring_runs` resolves it to the runs that hold its evidence",
            fix_rec["composed_from"] == [run_dirs(d / "runs")[1].name, run_dirs(d / "runs")[0].name]
            and fix_rec["carried"] == {"Conformance": 5} and hf.growth == [] and [p.name for p in measuring_runs(run_dirs(d / "runs")[-1])]
            == [run_dirs(d / "runs")[-1].name, run_dirs(d / "runs")[1].name, run_dirs(d / "runs")[0].name],
            str(fix_rec["composed_from"]))
        d = root / "compose-cleanup"
        d.mkdir()
        cl1, _ = regate(d, plant={"cleanup-red"})
        cl2, hcl = regate(d, plant={"cleanup-red"})
        arm("a host that went RED with every case Passed (a cleanup failure) leaves no case to retry, so the retry runs the "
            "whole assembly again and a deterministic host failure stays RED (review R1-1)",
            cl1.verdict == "RED" and cl2.verdict == "RED" and sorted(hcl.envs) == ["Conformance"]
            and not (run_dirs(d / "runs")[-1] / "only-Conformance.txt").exists() and ONLY_CASES not in hcl.envs["Conformance"]
            and cl2.populations["Conformance"].carried == 0 and "Conformance RED" in cl2.line, cl2.line)
        d = root / "compose-miscount"
        d.mkdir()
        mc1, _ = regate(d, plant={"red", "log-miscount"})
        mc2, hmc = regate(d, plant={"red", "log-miscount"})
        arm("a red host whose verdict counts a failure its trx does not name (a host-level fault beside a failed case) "
            "runs the whole assembly again, and the carried passes do not turn it GREEN (review R2-1)",
            mc1.verdict == "RED" and mc2.verdict == "RED" and sorted(hmc.envs) == ["Conformance"]
            and mc2.populations["Conformance"].carried == 0, mc2.line)
        d = root / "compose-prune"
        d.mkdir()
        regate(d, plant={"red"})
        for i in range(4):   # four runs that measured nothing (killed, refused) between the RED run and its retry
            (d / "runs" / f"2026012{i}T000000Z-00000{i + 2}").mkdir()
        fixed, _ = regate(d)
        names = [p.name for p in run_dirs(d / "runs")]
        arm("the retry's prune never deletes a run its verdict composes with (review R1-2)",
            fixed.verdict == "GREEN" and bool(fixed.composed_from) and all(n in names for n in fixed.composed_from)
            and len(measuring_runs(run_dirs(d / "runs")[-1])) == 1 + len(fixed.composed_from), f"{fixed.composed_from} {names}")
        d = root / "compose-code"
        d.mkdir()
        regate(d, plant={"red"})
        code, hc2 = regate(d, trees=["tree-2"])
        arm("a re-gate of a tree that differs in a path a test may read runs the WHOLE population: no retry file, "
            "nothing carried, no composition across two keys",
            code.verdict == "GREEN" and sorted(hc2.envs) == sorted(GATED_ASSEMBLIES) and not code.composed_from
            and all(ONLY_CASES not in v for v in hc2.envs.values()) and not any(code.carried.values()), code.line)
        d = root / "compose-toolchain"
        d.mkdir()
        regate(d, plant={"red"})
        tool, ht = regate(d, tools="dotnet-sdk 9.0.100, python 3.12.0")
        arm("a run under another toolchain is never composed with: the whole population runs (the toolchain is a facet "
            "of every target's key, kb/Work PB5218)",
            tool.verdict == "GREEN" and sorted(ht.envs) == sorted(GATED_ASSEMBLIES) and not tool.composed_from, tool.line)
        d = root / "compose-moved"
        d.mkdir()
        regate(d, plant={"red"}, trees=["tree-1", "tree-edited"])
        moved, hm = regate(d)
        arm("a run whose tree changed WHILE it ran contributes no pass: the whole population runs",
            moved.verdict == "GREEN" and sorted(hm.envs) == sorted(GATED_ASSEMBLIES) and not moved.composed_from, moved.line)
        d = root / "compose-inert"
        d.mkdir()
        green1, _ = regate(d)
        inert, hi = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"})
        inert_rec = read_record(run_dirs(d / "runs")[-1])
        arm("a GREEN code-lane run, then a commit that changes only inert paths, is GREEN with NO fetch, slot, build or "
            "host: the audits ran, the line says EVERY TARGET UP TO DATE and names the run whose passes stand, and the "
            "record names them",
            green1.verdict == "GREEN" and inert.verdict == "GREEN" and inert.exit_code == 0 and hi.events == []
            and hi.slots_taken == 0 and hi.audited and not inert.runs and hi.envs == {} and hi.growth == []
            and "EVERY TARGET UP TO DATE (kb/Work PB5207, PB5218)" in inert.line
            and f"run {run_dirs(d / 'runs')[0].name}" in inert.line
            and {u["run"] for u in inert_rec["up_to_date"].values()} == {run_dirs(d / "runs")[0].name}
            and inert_rec["tree"] == "tree-2" and inert_rec["lane"] == "full", f"{inert.line} {hi.events}")
        reused_inert, hri = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"})
        arm("a composed GREEN is itself the answer for a re-gate of its tree (REUSED), and `measuring_runs` still reaches "
            "the run that measured the tests",
            "REUSED" in reused_inert.line and hri.events == []
            and [p.name for p in measuring_runs(run_dirs(d / "runs")[-1])] == [run_dirs(d / "runs")[1].name,
                                                                              run_dirs(d / "runs")[0].name],
            reused_inert.line)
        code_after, hca = regate(d, trees=["tree-3"], keys={"tree-3": "tree-1"})
        arm("an inert-only commit on top of an up-to-date run is up to date through the run that measured the tests, "
            "never through the up-to-date one's own (absent) trx",
            "EVERY TARGET UP TO DATE" in code_after.line and hca.events == []
            and [p.name for p in measuring_runs(run_dirs(d / "runs")[-1])]
            == [run_dirs(d / "runs")[-1].name, run_dirs(d / "runs")[0].name], code_after.line)
        d = root / "compose-code-after-green"
        d.mkdir()
        regate(d)
        code_green, hcg = regate(d, trees=["tree-2"])
        arm("a GREEN code-lane run, then a commit that touches one path a test may read, runs EVERYTHING",
            code_green.verdict == "GREEN" and hcg.events[:3] == ["fetch", "slot", "build"]
            and sorted(hcg.envs) == sorted(GATED_ASSEMBLIES) and not code_green.composed_from, code_green.line)
        d = root / "compose-inert-audit"
        d.mkdir()
        regate(d)
        inert_red, hir = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"}, plant={"audit"})
        arm("a composed gate whose audits are RED is RED with nothing run (the audits are the only work it has)",
            inert_red.verdict == "RED" and hir.events == [] and hir.envs == {} and "RENDERED VIEWS RED" in inert_red.line,
            inert_red.line)
        d = root / "compose-inert-red"
        d.mkdir()
        regate(d, plant={"red"})
        inert_retry, hrr = regate(d, trees=["tree-2"], keys={"tree-2": "tree-1"})
        arm("a RED run, then an inert-only commit, retries only the red case: the inert path costs no case",
            inert_retry.verdict == "GREEN" and sorted(hrr.envs) == ["Conformance"]
            and inert_retry.carried == {"Conformance": 5} and sorted(inert_retry.up_to_date) == ["Characterization", "Unit"],
            inert_retry.line)

        # THE TARGETS (kb/Work PB5207 stage 1, PB5218; owner 2026-11-10: "This is precisely what the Unix make and Windows
        # MSBuild utilities are for. They rerun only what is required due to changes."): each assembly is a target with its
        # own key, and a pass is recorded once in ONE store every worktree shares. records-85's run 000001 passed all 64,465
        # cases and was RED only because the orchestrate self-tests hung; its re-gate ran all 64,465 again (kb/Work PB5205).
        same = {"tree-2": "tree-1"}   # tree-2 differs from tree-1 only in paths no target reads
        d = root / "target-hang"
        d.mkdir()
        hang, _ = regate(d, plant={"tooling", "audit-after-hosts"})
        hang_rec = read_record(run_dirs(d / "runs")[-1]) or {}
        arm("an audit that turns RED after every test host has exited (a self-test that hung past the last host) stops no "
            "host: the run is RED for the audit alone, its populations are measured EXACT and each target's pass is "
            "recorded in the store at its key",
            hang.verdict == "RED" and "SELF-TESTS HANG" in hang.line and len(hang.populations) == 3
            and all(pp.exact for pp in hang.populations.values()) and hang_rec.get("tree_end") == "tree-1"
            and hang_rec.get("audit_reds") == ["SELF-TESTS HANG"] and sorted(hang.recorded) == sorted(GATED_ASSEMBLIES)
            and all(FakeHost(d, d / "gate.lock").store().up_to_date(t, hang.keys[a])
                    for a, t in (("Unit", "unit"), ("Conformance", "conformance"), ("Characterization", "characterization"))),
            hang.line)
        tool_hang, hth = regate(d, trees=["tree-2"], keys=same, plant={"tooling"})
        arm("a run RED only for a hung self-test, then a tooling-only difference (every key equal): every target is up to "
            "date, so no corpus fetch and no test host; the audits run with the self-tests, and the change touches a script, "
            "so the post-build self-tests are built and run",
            tool_hang.verdict == "GREEN" and hth.envs == {} and not tool_hang.runs and "fetch" not in hth.events
            and hth.built and hth.audited and hth.tooling_seen is True
            and sorted(tool_hang.up_to_date) == sorted(GATED_ASSEMBLIES)
            and f"run {run_dirs(d / 'runs')[0].name}" in tool_hang.line, f"{tool_hang.line} {hth.events}")
        d = root / "target-hang-code"
        d.mkdir()
        regate(d, plant={"tooling", "audit-after-hosts"})
        hang_code, hhc = regate(d, trees=["tree-2"], plant={"tooling"})
        arm("a run RED only for a hung self-test, then a difference in a path every target reads (every key differs): "
            "nothing is up to date or carried and everything runs",
            hang_code.verdict == "GREEN" and hhc.events[:3] == ["fetch", "slot", "build"]
            and sorted(hhc.envs) == sorted(GATED_ASSEMBLIES) and not hang_code.composed_from and not hang_code.up_to_date
            and not any(hang_code.carried.values()), f"{hang_code.line} {hhc.events}")
        d = root / "target-green-docs"
        d.mkdir()
        regate(d)
        docs, hdo = regate(d, trees=["tree-2"], keys=same)
        arm("a GREEN run, then a change no target reads and that touches no script: EVERY TARGET UP TO DATE, the audits "
            "alone run (no corpus fetch, slot, build or host), and the line names each target's pass",
            docs.verdict == "GREEN" and hdo.events == [] and hdo.slots_taken == 0 and hdo.audited and not hdo.built
            and "EVERY TARGET UP TO DATE" in docs.line and all(a in docs.line for a in GATED_ASSEMBLIES), docs.line)
        d = root / "target-unit-only"
        d.mkdir()
        regate(d)
        unit_only, huo = regate(d, trees=["tree-2"], keys={"tree-2": {"Conformance": "tree-1", "Characterization": "tree-1"}})
        arm("equal conformance and characterization keys with a changed unit key (a records-only change) run Unit ONLY: "
            "one host, the others up to date, and Unit's new pass is recorded",
            unit_only.verdict == "GREEN" and sorted(huo.envs) == ["Unit"] and "build" in huo.events
            and sorted(unit_only.up_to_date) == ["Characterization", "Conformance"] and unit_only.recorded == ["Unit"]
            and "Conformance up to date" in unit_only.line and "Unit 4/4 ran" in unit_only.line, unit_only.line)
        d = root / "target-conformance-input"
        d.mkdir()
        regate(d)
        conf_in, hci = regate(d, trees=["tree-2"], keys={"tree-2": {"Unit": "tree-1", "Characterization": "tree-1"}})
        arm("a changed input of conformance (its key differs) runs Conformance, and only it",
            conf_in.verdict == "GREEN" and sorted(hci.envs) == ["Conformance"] and conf_in.recorded == ["Conformance"],
            conf_in.line)
        shared = root / "shared-store"
        wa, wb = root / "worktree-a", root / "worktree-b"
        wa.mkdir()
        wb.mkdir()
        in_a = Gate(FakeHost(wa, wa / "gate.lock", store_dir=shared), wa / "runs", say=sink.append).run()
        hb = FakeHost(wb, wb / "gate.lock", store_dir=shared)
        sink.clear()
        in_b = Gate(hb, wb / "runs", say=sink.append).run()
        arm("a pass recorded from worktree A is UP TO DATE in worktree B at the same content (one store, the git common "
            "directory's): B starts no slot, build or host, and names A's run",
            in_a.verdict == "GREEN" and in_b.verdict == "GREEN" and hb.events == [] and hb.envs == {}
            and all(u["worktree"] == str(wa) for u in in_b.up_to_date.values()) and "worktree-a" in in_b.line,
            f"{in_b.line} {hb.events}")
        d = root / "target-facets"
        d.mkdir()
        regate(d)
        other_tools, hot = regate(d, tools="dotnet-sdk 9.0.100, python 3.12.0", trees=["tree-2"], keys=same)
        other_plat, hop = regate(d, platform="linux", trees=["tree-3"], keys={"tree-3": "tree-1"})
        arm("another toolchain or another platform is another key: every target runs",
            sorted(hot.envs) == sorted(GATED_ASSEMBLIES) and not other_tools.up_to_date
            and sorted(hop.envs) == sorted(GATED_ASSEMBLIES) and not other_plat.up_to_date, f"{other_tools.line} | {other_plat.line}")
        d = root / "target-not-exact"
        d.mkdir()
        dropped, _ = regate(d, plant={"drop"})
        d_red = root / "target-red"
        d_red.mkdir()
        red, _ = regate(d_red, plant={"red"})
        red_store = FakeHost(d_red, d_red / "gate.lock").store()
        arm("a population that is not EXACT, or a red host, records nothing for that target (the others' passes are "
            "recorded)",
            dropped.verdict == "RED" and "Conformance" not in dropped.recorded and "Unit" in dropped.recorded
            and not FakeHost(d, d / "gate.lock").store().up_to_date("conformance", dropped.keys["Conformance"])
            and red.verdict == "RED" and "Conformance" not in red.recorded
            and not red_store.up_to_date("conformance", red.keys["Conformance"]), f"{dropped.line} | {red.line}")
        d = root / "target-audit-and-postbuild"
        d.mkdir()
        both, _ = regate(d, plant={"tooling", "audit-after-hosts", "post-build"})
        after_both, hab = regate(d, trees=["tree-2"], keys=same, plant={"tooling"})
        arm("a run RED for an audit AND the post-build self-tests, every host green (kb/Work PB5205 review R1-1): its "
            "targets' passes stand, and the next gate at their keys starts no host but builds and runs the post-build "
            "self-tests again",
            both.verdict == "RED" and "POST-BUILD SELF-TESTS RED" in both.line and sorted(both.recorded) == sorted(GATED_ASSEMBLIES)
            and after_both.verdict == "GREEN" and hab.built and hab.envs == {} and "post-build self-tests" in hab.events,
            f"{both.line} | {after_both.line} {hab.events}")
        d = root / "target-carry"
        d.mkdir()
        regate(d, plant={"red"})
        carried_retry, hcr = regate(d, trees=["tree-2"], keys=same)
        arm("at a target's key, an earlier RED run of this worktree leaves only its unpassed case to run (kb/Work PB5099); "
            "the targets that passed are up to date",
            carried_retry.verdict == "GREEN" and sorted(hcr.envs) == ["Conformance"]
            and carried_retry.carried == {"Conformance": 5} and sorted(carried_retry.up_to_date) == ["Characterization", "Unit"]
            and carried_retry.recorded == ["Conformance"], carried_retry.line)

        # THE REAL KEYS (`Host.target_keys`, `buildgraph.key`) over a real repository with the real targets file.
        keyrepo = ScratchRepo.init(root / "keyrepo")
        planted_tool, code_tool = "scripts/orchestrator/planted_tool.py", "scripts/standard_streams.py"
        files = {"src/A.cs": "class A {}\n", "docs/n.md": "# n\n", "kb/Work/X.md": "x\n", planted_tool: "x = 1\n",
                 code_tool: "y = 1\n", "tests/version-matrix/inventory/r.json": "{}\n",
                 "tests/WiseOwl.Cobol.Tests.Unit/U.cs": "class U {}\n", "tests/characterization/positive/p.cob": "p\n"}
        keyhost = Host(keyrepo.path)
        kfacets = {"platform": "windows", "configuration": GATE_CONFIGURATION, "toolchain": "t1"}

        suite_out = {"conformance-suite": "d" * 64}   # the generated suite's output digest, as a gate's generation gives it

        def keys_after(edit: dict[str, str], facets: dict[str, str] = kfacets,
                       outputs: dict[str, str] = suite_out) -> dict[str, str]:
            keyrepo.commit("edit", {**files, **edit}, allow_empty=True)
            return keyhost.target_keys(keyhost.head_tree() or "", facets, outputs) or {}

        k0 = keys_after({})
        ks = {"docs": keys_after({"docs/n.md": "# edited\n", "kb/Work/X.md": "y\n", planted_tool: "x = 2\n"}),
              "unit-test": keys_after({"tests/WiseOwl.Cobol.Tests.Unit/U.cs": "class U { int x; }\n"}),
              "char": keys_after({"tests/characterization/positive/p.cob": "q\n"}),
              "inventory": keys_after({"tests/version-matrix/inventory/r.json": '{"v": 2}\n'}),
              "suite-output": keys_after({}, outputs={"conformance-suite": "e" * 64}),
              "src": keys_after({"src/A.cs": "class A { int b; }\n"}),
              "toolchain": keys_after({}, {**kfacets, "toolchain": "t2"})}
        try:
            unresolved = str(keyhost.target_keys(keyhost.head_tree() or "", kfacets))
        except buildgraph.Unresolved as e:
            unresolved = f"Unresolved: {e}"
        arm("the real keys: a docs, kb or tooling-lane edit leaves every key; a Unit test source changes Unit's key only, a "
            "characterization program Unit's and Characterization's only; an inventory edit Unit's only (Conformance takes "
            "the generated suite's digest, kb/Work PB5231), and another suite digest Conformance's only; src/ and the "
            "toolchain change every key; with no record of the suite in the store Conformance's key is UNRESOLVED",
            len(k0) == 3 and ks["docs"] == k0
            and ks["unit-test"]["Unit"] != k0["Unit"] and ks["unit-test"]["Conformance"] == k0["Conformance"]
            and ks["unit-test"]["Characterization"] == k0["Characterization"]
            and ks["char"]["Characterization"] != k0["Characterization"] and ks["char"]["Conformance"] == k0["Conformance"]
            and ks["inventory"]["Unit"] != k0["Unit"] and ks["inventory"]["Conformance"] == k0["Conformance"]
            and ks["inventory"]["Characterization"] == k0["Characterization"]
            and ks["suite-output"]["Conformance"] != k0["Conformance"]
            and ks["suite-output"]["Unit"] == k0["Unit"] and ks["suite-output"]["Characterization"] == k0["Characterization"]
            and all(ks["src"][a] != k0[a] and ks["toolchain"][a] != k0[a] for a in GATED_ASSEMBLIES)
            and unresolved.startswith("Unresolved: conformance's key needs the output digest of conformance-suite"),
            f"{ks} {unresolved}")

        # THE GENERATED TARGET (kb/Work PB5231): a planted graph over a REAL scratch repository, its generator really run
        # (traced) by the gate, its output digest really keying Conformance; only the test hosts are planted.
        grepo = ScratchRepo.init(root / "genrepo")
        grepo.commit("base", {".gitignore": "TestResults/\n__pycache__/\n", "gen/make.py": buildgraph.PLANTED_GENERATOR,
                              "inv/r1.json": '{"id": "r1", "verdict": "GAP"}\n', "src/A.cs": "class A {}\n",
                              "tests/C/T.cs": "class T {}\n", "tests/U/U.cs": "class U {}\n",
                              "tests/H/H.cs": "class H {}\n"})
        every = ("platform", "configuration", "toolchain")
        ggraph = buildgraph.Graph.of({
            "suite": buildgraph.GeneratedTarget(
                name="suite", inputs=("gen/", "inv/"), exclude=("@inert",), facets=("toolchain",),
                command=("gen/make.py", "--out", buildgraph.OUT), outputs=("suite/manifest.json",),
                env="WISEOWL_COBOL_GATE_SUITE"),
            "unit": buildgraph.TestTarget(name="unit", assembly="Unit", project="tests/U/", inputs=("src/", "tests/U/", "inv/"),
                                          exclude=("@inert",), facets=every),
            "conformance": buildgraph.TestTarget(name="conformance", assembly="Conformance", project="tests/C/",
                                                 inputs=("src/", "tests/C/"), exclude=("@inert",), facets=every,
                                                 depends=("suite",)),
            "characterization": buildgraph.TestTarget(name="characterization", assembly="Characterization",
                                                      project="tests/H/", inputs=("src/", "tests/H/"), exclude=("@inert",),
                                                      facets=every)}, every)
        gstore, gd = root / "gen-store", root / "gen-gates"
        gd.mkdir()
        suite_dir = str(grepo.path / buildgraph.GENERATED_ROOT / "suite")

        def ggate(edit: dict[str, str | None] | None = None, **fake) -> tuple[Outcome, FakeHost]:
            if edit:
                grepo.commit("edit", edit)
            host = FakeHost(gd, gd / "gate.lock", graph=ggraph, repo=grepo.path, store_dir=gstore,
                            trees=[grepo.git("rev-parse", "HEAD^{tree}")], **fake)
            sink.clear()
            return Gate(host, gd / "runs", say=sink.append).run(), host

        g0, hg0 = ggate()
        arm("a gate brings the generated target up to date first (its generator traced and run) and hands its output "
            "directory to Conformance's host and listing, and to no other assembly's; the line names the generation",
            g0.verdict == "GREEN" and g0.generated["suite"]["ran"] and sorted(g0.recorded) == sorted(GATED_ASSEMBLIES)
            and hg0.envs["Conformance"].get("WISEOWL_COBOL_GATE_SUITE") == suite_dir
            and hg0.listing_handed["Conformance"] == {"WISEOWL_COBOL_GATE_SUITE": suite_dir}
            and "WISEOWL_COBOL_GATE_SUITE" not in hg0.envs["Unit"] and "suite generated in" in g0.line,
            f"{g0.line} {hg0.envs.get('Conformance', {}).get('WISEOWL_COBOL_GATE_SUITE')}")
        g1, hg1 = ggate({"inv/r1.json": '{"id": "r1", "verdict": "PASS"}\n'})
        arm("an inventory edit that leaves the generator's output unchanged: the generator re-runs, Conformance's key is "
            "unchanged and Conformance is UP TO DATE (no host); Unit, which reads the inventory, runs",
            g1.verdict == "GREEN" and g1.generated["suite"]["ran"] and g1.keys["Conformance"] == g0.keys["Conformance"]
            and sorted(hg1.envs) == ["Unit"] and "Conformance up to date" in g1.line
            and g1.generated["suite"]["outputs"] == g0.generated["suite"]["outputs"], g1.line)
        g2, hg2 = ggate({"inv/r2.json": '{"id": "r2", "verdict": "GAP"}\n'})
        arm("an inventory edit that changes the generator's output changes Conformance's key: Conformance runs on the new "
            "suite",
            g2.verdict == "GREEN" and g2.keys["Conformance"] != g1.keys["Conformance"] and "Conformance" in hg2.envs
            and "Conformance" in g2.recorded and g2.generated["suite"]["outputs"] != g1.generated["suite"]["outputs"],
            g2.line)
        g3, hg3 = ggate({"gen/make.py": buildgraph.PLANTED_GENERATOR + "# edited\n"})
        arm("a generator source edit re-runs the generator; its output unchanged, every test target is UP TO DATE and the "
            "line says the suite was generated",
            g3.verdict == "GREEN" and g3.generated["suite"]["ran"] and g3.generated["suite"]["key"] != g2.generated["suite"]["key"]
            and hg3.envs == {} and "EVERY TARGET UP TO DATE" in g3.line and "suite generated in" in g3.line, g3.line)
        g4, hg4 = ggate({"docs/n.md": "# a path no target reads: another tree, so no REUSED record answers\n"})
        arm("its inputs unchanged: the generated target is up to date (its record and its outputs on disk agree), nothing runs",
            g4.verdict == "GREEN" and not g4.generated["suite"]["ran"] and "suite up to date" in g4.line and hg4.envs == {},
            g4.line)
        rec_file = buildgraph.Store(gstore)._path("suite", g4.generated["suite"]["key"])
        rec_file.unlink()
        g5, hg5 = ggate({"docs/n.md": "# again\n"})
        arm("a missing dependency record is not up to date: with its outputs still on disk the generator runs again "
            "(no record at its key), and only then is Conformance, at its unchanged key, up to date",
            g5.verdict == "GREEN" and g5.generated["suite"]["ran"] and g5.generated["suite"]["why"] == "no record at its key"
            and rec_file.is_file() and g5.keys["Conformance"] == g4.keys["Conformance"] and hg5.envs == {}, g5.line)
        g6, hg6 = ggate({"tests/C/T.cs": "class T { int x; }\n"}, plant={"tamper-output"})
        arm("a host that changes the generated output it was handed makes the gate RED and records no pass of its target",
            g6.verdict == "RED" and "GENERATED OUTPUT CHANGED" in g6.line and "Conformance" not in g6.recorded
            and not buildgraph.Store(gstore).up_to_date("conformance", g6.keys["Conformance"]), g6.line)
        g6b, _ = ggate({"tests/C/T.cs": "class T { int y; }\n"}, plant={"own-generation"})
        arm("a gate whose Conformance host generated its own suite instead of the handed one is RED and records no pass "
            "of its target (kb/Work PB5231 R1-2)",
            g6b.verdict == "RED" and "GENERATED BY THE HOST" in g6b.line and "Conformance" not in g6b.recorded
            and not buildgraph.Store(gstore).up_to_date("conformance", g6b.keys["Conformance"]), g6b.line)
        g7, _ = ggate({"gen/NONDET": "x\n"})
        remove_tree(grepo.path / buildgraph.GENERATED_ROOT / "suite")
        g8, hg8 = ggate({"docs/n.md": "# the generator's inputs as at g7\n"})
        arm("a nondeterministic generator (planted: the time) is REFUSED when its outputs are generated again at a key: "
            "the gate is RED naming it, and no test host runs",
            g7.verdict == "GREEN" and g8.verdict == "RED" and "GENERATED TARGET RED — Nondeterministic" in g8.line
            and hg8.envs == {} and not hg8.built, g8.line)
        # A RE-DERIVATION KEYS ON THE DIGEST THE RUN WAS HANDED (kb/Work PB5231 review R2-2), from g7's real record.
        g7_rec = next((r for r in map(read_record, run_dirs(gd / "runs")) if r and r.get("line") == g7.line), {})
        rgate = Gate(FakeHost(gd, gd / "gate.lock", graph=ggraph, repo=grepo.path, store_dir=gstore), gd / "runs",
                     say=sink.append)
        own_refusals: list[str] = []
        own_keys, own_refused = rgate._rederived_keys(g7_rec, own_refusals)
        suite_rec_path = buildgraph.Store(gstore)._path("suite", g7_rec["generated"]["suite"]["key"])
        kept_text = suite_rec_path.read_text(encoding="utf-8")
        suite_rec_path.write_text(kept_text.replace(g7_rec["generated"]["suite"]["outputs"], "f" * 64), encoding="utf-8",
                                  newline="\n")
        moved_refusals: list[str] = []
        rgate._rederived_keys(g7_rec, moved_refusals)
        suite_rec_path.write_text(kept_text, encoding="utf-8", newline="\n")
        old_refusals: list[str] = []
        old_keys, old_refused = rgate._rederived_keys({k: v for k, v in g7_rec.items() if k != "generated"}, old_refusals)
        arm("a re-derivation keys Conformance on the digest the run itself was handed; one whose digest the store now "
            "contradicts is refused naming both; a run from before the hand-off has Conformance REFUSED, never stored",
            own_refusals == [] and own_refused == {} and own_keys == g7_rec["keys"]
            and len(moved_refusals) == 1 and g7_rec["generated"]["suite"]["outputs"][:12] in moved_refusals[0]
            and "f" * 12 in moved_refusals[0]
            and old_refusals == [] and sorted(old_refused) == ["Conformance"] and "Conformance" not in (old_keys or {})
            and (old_keys or {}).get("Unit") == g7_rec["keys"]["Unit"],
            f"{own_refusals} {own_refused} | {moved_refusals} | {old_refusals} {old_refused}")
        cyclic = root / "cyclic-targets.json"
        cyclic.write_text(json.dumps({"facets": ["toolchain"], "build": {"inputs": {}}, "targets": {
            "a": {"kind": "generated", "command": ["a.py", "{out}"], "outputs": {"x": ""}, "env": "WISEOWL_COBOL_GATE_A",
                  "inputs": {"a.py": ""}, "depends": ["b"]},
            "b": {"kind": "generated", "command": ["b.py", "{out}"], "outputs": {"x": ""}, "env": "WISEOWL_COBOL_GATE_B",
                  "inputs": {"b.py": ""}, "depends": ["a"]}}}), encoding="utf-8", newline="\n")
        cd = root / "gate-cycle"
        cd.mkdir()
        chost = FakeHost(cd, cd / "gate.lock")
        chost.graph = lambda: buildgraph.load_targets(cyclic)   # the targets file has a cycle
        sink.clear()
        cyc = Gate(chost, cd / "runs", say=sink.append).run()
        arm("a dependency cycle in the targets file is refused: the gate is RED naming the cycle, and nothing is built or run",
            cyc.verdict == "RED" and "a dependency cycle: a -> b -> a" in cyc.line and not chost.built and chost.envs == {},
            cyc.line)

        # RE-DERIVED EVIDENCE (kb/Work PB5205, PB5218; `Gate.rederive`): a gate before the fix, stopped by a hung self-test
        # after every host had exited, wrote `populations: {}` and `tree_end: null` over complete green results and
        # recorded nothing in the store (records-85's run 000001). Each arm plants that over a real planted run.
        def old_stopped(d: Path, keep_listings: bool = False) -> Path:
            regate(d)
            r = run_dirs(d / "runs")[-1]
            rec = read_record(r) or {}
            for k in ("keys", "audit_reds", "up_to_date", "recorded"):
                rec.pop(k, None)
            rec.update(verdict="RED", exit_code=1, reasons=["SELF-TESTS HANG"], populations={}, tree_end=None,
                       line=f"{VERDICT}RED — SELF-TESTS HANG — fail-fast: an audit that ran beside the test hosts was RED "
                            "and STOPPED them: their results are partial and NOT measured ===")
            (r / VERDICT_FILE).write_text(json.dumps(rec), encoding="utf-8", newline="\n")
            remove_tree(d / "store")   # the old gate recorded nothing in the store
            if not keep_listings:
                for f in r.glob("listing-*.txt"):
                    f.unlink()
            return r

        def rederive_in(d: Path, r: Path, **fake) -> tuple[Rederived, FakeHost]:
            host = FakeHost(d, d / "gate.lock", **fake)
            return Gate(host, d / "runs", say=sink.append).rederive(r), host

        d = root / "rederive-passes"
        d.mkdir()
        r = old_stopped(d)
        rd, _ = rederive_in(d, r)
        Gate(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append).write_rederived(r, rd) if rd.record else None
        written, original = read_record(r) or {}, json.loads((r / ORIGINAL_VERDICT_FILE).read_text(encoding="utf-8")) \
            if (r / ORIGINAL_VERDICT_FILE).exists() else {}
        after, hpa = regate(d, trees=["tree-2"], keys=same)
        arm("a stopped run with complete green trx and no listing is RECOVERED from its own files (passes): its record "
            "gains tree_end, keys and `rederived` (with the residue: an edit undone at ANY time before the re-derivation), "
            "stays RED, keeps the original beside it, stores nothing, and the next gate at its keys builds and lists but "
            "starts no test host, then records the passes",
            rd.record is not None and rd.level == "passes" and written.get("tree_end") == "tree-1"
            and len(written.get("keys") or {}) == 3 and written.get("exit_code") == 1 and original.get("tree_end") is None
            and "undone at any time before this re-derivation" in written.get("rederived", {}).get("residue", "")
            and not rd.stored and after.verdict == "GREEN" and "build" in hpa.events and hpa.envs == {}
            and sum(after.carried.values()) == 12 and sorted(after.recorded) == sorted(GATED_ASSEMBLIES),
            f"{rd.refusals} {after.line} {hpa.events}")
        d = root / "rederive-green"
        d.mkdir()
        r = old_stopped(d, keep_listings=True)
        rd, _ = rederive_in(d, r)
        Gate(FakeHost(d, d / "gate.lock"), d / "runs", say=sink.append).write_rederived(r, rd) if rd.record else None
        after, hpg = regate(d, trees=["tree-2"], keys=same)
        arm("a stopped run with complete green trx and its keys' listings is RECOVERED EXACT: the store holds each "
            "target's pass, so the next gate at those keys finds every target up to date with no build and no test host",
            rd.record is not None and rd.level == "stored" and sorted(rd.stored) == sorted(GATED_ASSEMBLIES)
            and after.verdict == "GREEN" and hpg.events == [] and hpg.envs == {} and "EVERY TARGET UP TO DATE" in after.line,
            f"{rd.refusals} {after.line}")
        d = root / "rederive-unfinished"
        d.mkdir()
        r = old_stopped(d)
        (r / "Unit.log").write_text("  Passed N.Unit.A [1 s]\n", encoding="utf-8", newline="\n")   # killed: no verdict line
        unfinished, _ = rederive_in(d, r)
        d = root / "rederive-short"
        d.mkdir()
        r = old_stopped(d)
        synthetic_trx(r / "Conformance.trx", [(n, ["Passed"]) for n in FakeHost.POPULATION["Conformance"][:-1]])
        short, _ = rederive_in(d, r)
        arm("a run with a host that never finished (no `Passed!` line) or a SHORT trx (fewer results than its host's line) "
            "is REFUSED, naming the assembly and what is missing, and nothing is written",
            unfinished.record is None and any("Unit" in x and "never finished" in x for x in unfinished.refusals)
            and short.record is None and any("Conformance" in x and "not complete" in x for x in short.refusals)
            and read_record(r).get("rederived") is None, f"{unfinished.refusals} | {short.refusals}")
        d = root / "rederive-moved"
        d.mkdir()
        r = old_stopped(d)
        moved_at = int(run_started(r) or 0) + 1   # a second after the gate started, by the hosts' own clock
        os.utime(r / "Conformance.log", (moved_at + 1, moved_at + 1))   # the hosts' last write: after the move
        host = FakeHost(d, d / "gate.lock")
        host.reflog = [("head-2", moved_at), ("head-1", 0)]
        moved = Gate(host, d / "runs", say=sink.append).rederive(r)
        arm("a run whose HEAD moved while it ran (a reflog entry between its start and its hosts' last write) is REFUSED",
            moved.record is None and any("HEAD moved while the run ran" in x for x in moved.refusals), str(moved.refusals))
        d = root / "rederive-toolchain"
        d.mkdir()
        r = old_stopped(d)
        other, _ = rederive_in(d, r, tools="dotnet-sdk 9.0.100, python 3.12.0")
        d = root / "rederive-reason"
        d.mkdir()
        r = old_stopped(d)
        rec = read_record(r) or {}
        (r / VERDICT_FILE).write_text(json.dumps({**rec, "reasons": ["SELF-TESTS HANG", "Unit POPULATION UNMEASURED (x)"]}),
                                      encoding="utf-8", newline="\n")
        reason, _ = rederive_in(d, r)
        arm("a run recorded under another toolchain, or RED for a reason no audit gave, is REFUSED",
            other.record is None and any("toolchain" in x for x in other.refusals)
            and reason.record is None and any("reasons no audit gave" in x for x in reason.refusals),
            f"{other.refusals} | {reason.refusals}")

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


def rederive_main(run: Path, dry_run: bool) -> int:
    """`--rederive RUN [--dry-run]` (kb/Work PB5205, PB5218; `Gate.rederive`): what the run's own evidence proves, each
    target's key at its recorded tree and at its worktree's HEAD tree now, and which targets a gate at HEAD would run. 0
    when it is proved (and, without --dry-run, the record rewritten and the passes it proved exact stored), 1 when
    refused, 2 when RUN is no run of a worktree."""
    wt = run.parent.parent.parent
    if run.parent != wt / RUN_ROOT or not run.is_dir():
        print(f"{VERDICT}NOT RUN — {run} is not a run directory of a worktree ({RUN_ROOT}/<run>) ===")
        return 2
    host = Host(wt)
    gate = Gate(host, wt / RUN_ROOT, say=print)
    rd = gate.rederive(run)
    for line in rd.found:
        print(f"  found    {line}")
    for line in rd.refusals:
        print(f"  REFUSED  {line}")
    rec = read_record(run) or {}
    facets = gate.facets(str(rec.get("toolchain") or ""))

    def keys_at(tree: str | None) -> dict[str, str] | None:
        try:
            return host.target_keys(tree, facets) if tree else None
        except buildgraph.Unresolved as e:   # a generated dependency with no record at its key (kb/Work PB5231)
            print(f"  UNRESOLVED  the keys at {tree[:12]}: {e}")
            return None

    then = keys_at(str(rec.get("tree")) if rec.get("tree") else None)
    head_tree = host.head_tree()
    now = keys_at(head_tree)
    for asm in GATED_ASSEMBLIES:
        print(f"  {asm}: key at the run's tree {str(rec.get('tree'))[:12]} {(then or {}).get(asm)}; at HEAD's tree "
              f"{str(head_tree)[:12]} (head {str(host.head())[:12]}) {(now or {}).get(asm)}")
    same = [a for a in GATED_ASSEMBLIES if then and now and then.get(a) == now.get(a)]
    how = (f"at HEAD the run's evidence stands for {', '.join(same) or 'no target'}; a gate there runs "
           f"{', '.join(a for a in GATED_ASSEMBLIES if a not in same) or 'no test host'}") if rd.record is not None \
        else "nothing is carried"
    if rd.record is None:
        print(f"=== REDERIVE {run.name}: REFUSED — {len(rd.refusals)} reason(s); {how} ===")
        return 1
    if not dry_run:
        gate.write_rederived(run, rd)
    print(f"=== REDERIVE {run.name}: PROVED ({rd.level}){' — dry run, nothing written' if dry_run else ''}; {how} ===")
    return 0


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
    ap.add_argument("--rederive", metavar="RUN", help="re-derive a run's population evidence from its own trx, host logs "
                    "and HEAD's reflog, and rewrite its record (the original kept beside it) when it is proved (kb/Work "
                    "PB5205); a relative RUN is --worktree's")
    ap.add_argument("--dry-run", action="store_true", help="--rederive: say what it proves and write nothing")
    ap.add_argument("--self-test", action="store_true", help="fire every arm on planted inputs (DESIGN §3.14.4 (3))")
    ap.add_argument("--audit-shape", action="store_true", help="the ONE SHAPE audit: no live line names a gate mode "
                    "(kb/Work PB3022)")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.audit_shape:
        return audit_shape_main()
    if args.rederive:
        return rederive_main(resolve_run(args.rederive, args.worktree), args.dry_run)
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
