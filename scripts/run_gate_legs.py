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
  7. ⭐ THE LINUX HALF OF THE TARGETED LEG (kb/Work PB3612; owner 2026-10-23, DESIGN §3.17.3), on a Windows host only
     and only after a green Windows half: the SAME plan's legs the Windows half ran, in a WSL clone of the committed
     HEAD, by `bash scripts/linux-gate.sh --legs targeted --plan <this run's plan.json>` (the clone is built there and
     runs `--plan-leg`: no audits, no build, no slot of its own — this gate still holds ITS slot). A red Linux half
     makes the gate RED and names the host; one that measured nothing (no WSL, no SDK, a full disk) ends the gate NOT RUN
     with the Linux gate's own cause. The verdict file records it (`linux`: verdict, the committed `tree` it measured),
     beside `head_tree`: that record is the landing's evidence that the branch was "tested locally" (enqueue.py
     `tested_locally`), and a reused answer needs it. A Linux host's own legs are the Linux half; the inert lane's is the script self-tests alone (PB3646).
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
import inert_lane  # noqa: E402  — THE classification of a change no test reads (kb/Work PB2878)
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT  # noqa: E402
from gate_slot import _pid_alive  # noqa: E402  — the first-red arm checks that a stopped audit's tree is gone
from gate_slot import (DEFAULT_SCOPE, ExclusiveLock, GateSettings, GateSlots, Setting, Slot,  # noqa: E402
                       git_common_dir, slot_dir)
from detached import spawn_detached  # noqa: E402  — THE one launch that outlives its caller (kb/Work PB3536)
sys.path.append(str(REPO / "scripts" / "orchestrator"))  # appended: no module of it shadows one of scripts/
from coord import NEVER_WORK  # noqa: E402  — THE paths that are never work, so never part of a measured tree
from test_population import (GATED_ASSEMBLIES, HANDSHAKE, Population, PopulationError, UnreadableTrx,  # noqa: E402
                             check as check_population, list_population, scrubbed_env, synthetic_listing,
                             synthetic_trx)

VERDICT = "=== BUILD-LOCAL GATE: "
#: The Linux half's verdict prefix (scripts/linux-gate.sh), which `read_verdict` reads like this driver's own.
LINUX_VERDICT = "=== LINUX GATE: "
#: The verdict line of `--plan-leg` (`plan_leg`: an existing plan's legs, run in a Linux clone), which linux-gate.sh turns
#: into its own `=== LINUX GATE: ` line.
PLAN_LEG_VERDICT = "=== PLAN LEG: "
LINUX_LEG_LOG = "linux-leg.log"
#: How long `Host.linux_gate_holder` waits for `linux-gate.sh --probe` (a WSL cold start is the slow part; the probe reads
#: a few files): a hung hand-off is read as no holder, never waited on.
LINUX_PROBE_TIMEOUT_S = 120
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
    NotRunCause("disk", ENVIRONMENT, "the Linux filesystem the gate clones onto has too few free inodes or too little "
                                     "free space, after the gate removed every ended gate's leftovers (linux-gate.sh "
                                     "check_disk, kb/Work PB3194)"),
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


@dataclass(frozen=True)
class LinuxLeg:
    """The Linux half of the targeted leg (kb/Work PB3612): the Linux gate's reading of its own verdict line, that line,
    and the log it wrote in the run directory."""
    reading: Reading
    line: str
    log: str


#: A run directory's other fixed names (kb/Work PB3536): the gate process's pid, and its output when it runs detached.
PID_FILE = "gate.pid"
#: What a detached gate's lock label says before the run directory it writes (`Gate.run`; `live_gate` reads it back).
RUN_LABEL = ", run "
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
    # kb/Work PB735, PB2878: a byte-level fact about the committed tree, an audit so that no test reads every path.
    ("CONFLICT MARKERS", ["scripts/conflict_markers.py", "--check"]),
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

    def lane(self, base: str | None, tree: str | None) -> inert_lane.Lane:
        """The change's lane: what `tree` (the measured working tree) changes against `base`, classified by THE one
        classification (scripts/inert_lane.py). No base or no tree, or a change git cannot list, is the full lane."""
        if not base or not tree:
            return inert_lane.Lane(False, "no base or no measured tree: the full lane")
        try:
            return inert_lane.CLASSIFICATION.classify(inert_lane.changed(self.repo, base, tree))
        except (subprocess.CalledProcessError, OSError) as e:
            return inert_lane.Lane(False, f"the change could not be listed ({e}): the full lane")

    def head(self) -> str | None:
        try:
            return self.git("rev-parse", "HEAD")
        except (subprocess.CalledProcessError, OSError):
            return None

    def head_tree(self) -> str | None:
        """The git tree id of HEAD's commit: what the Linux half measures (it clones the COMMITTED head), and what the
        working tree the Windows half measures must equal for the gate to attest the commit (None when git cannot say)."""
        try:
            return self.git("rev-parse", "HEAD^{tree}")
        except (subprocess.CalledProcessError, OSError):
            return None

    def runs_linux_leg(self) -> bool:
        """Whether this host runs the LINUX HALF of the targeted leg itself, in a WSL clone of HEAD (kb/Work PB3612; owner
        2026-10-23): a Windows host does. On a Linux host this gate's own legs are the Linux half."""
        return os.name == "nt"

    def linux_leg(self, run: Path, plan: Path | None, legs: list[int], spawn: Spawn, inert: bool = False) -> LinuxLeg:
        """The Linux half: the SAME plan's `legs`, run in a WSL clone of the committed HEAD by `scripts/linux-gate.sh
        --legs targeted` (the clone is built there and runs this driver's `--plan-leg`), the whole output to `run`'s
        LINUX_LEG_LOG. Its verdict line is read by `read_verdict`, like every gate's. No plan (`plan` None) is the whole
        population in one leg, as this gate runs it. `inert`: the INERT LANE's Linux half (kb/Work PB3646), `--legs
        inert`: the script self-tests alone in the clone, with no plan and no leg."""
        if inert:
            cmd = ["bash", "scripts/linux-gate.sh", "--legs", "inert", "--nice"]
        else:
            where = "none" if plan is None else Path(os.path.relpath(plan, self.repo)).as_posix()
            cmd = ["bash", "scripts/linux-gate.sh", "--legs", "targeted", "--plan", where, "--plan-legs",
                   ",".join(map(str, legs)), "--nice"]
        log = run / LINUX_LEG_LOG
        with open(log, "wb") as fh:
            rc = subprocess.run(cmd, cwd=self.repo, stdout=fh, stderr=subprocess.STDOUT, **spawn()).returncode
        lines = [ln.strip() for ln in log.read_bytes().decode("utf-8", errors="replace").splitlines()
                 if ln.strip().startswith(LINUX_VERDICT)]
        if not lines:
            return LinuxLeg(not_run("verdict"), f"no `{LINUX_VERDICT.strip()}` line (exit {rc}; {log}): the Linux half "
                                                "measured nothing", log.name)
        reading = read_verdict(lines[-1][len(LINUX_VERDICT):])
        return LinuxLeg(RED_READING if reading == GREEN_READING and rc != 0 else reading, lines[-1], log.name)

    def linux_gate_holder(self, run=subprocess.run, which=shutil.which,
                          say: Callable[[str], None] = lambda m: print(m, file=sys.stderr)) -> str | None:
        """The Linux gate of this worktree that runs NOW (kb/Work PB3649), as `scripts/linux-gate.sh --probe` words it (its
        BUSY line: pid and start time), or None. A caller that waits for a landing's exit 12 asks it beside the gate lock:
        exit 12 has two sources, and a Linux gate does not hold the Windows lock (a hand run of linux-gate.sh does not
        take it; the driver's own Linux half runs under it). None is also the answer where no Linux gate can run (a
        Windows host with no WSL) and where the probe itself could not answer (a hand-off that failed): a retry costs
        one landing attempt that touches nothing when it meets BUSY again, while a wait that never ends costs the
        branch, so an unanswerable probe is never read as a busy one; it is SAID (`say`, stderr by default) instead."""
        if os.name == "nt" and which("wsl.exe") is None:
            return None
        try:
            probe = run(["bash", "scripts/linux-gate.sh", "--probe"], cwd=self.repo, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", timeout=LINUX_PROBE_TIMEOUT_S)
        except (OSError, subprocess.SubprocessError) as e:
            say(f"run_gate_legs: the Linux gate probe of {self.repo} was not answered ({type(e).__name__}: {e}); read as "
                "no Linux gate running")
            return None
        if probe.returncode not in (0, EXIT_BUSY):
            say(f"run_gate_legs: the Linux gate probe of {self.repo} was not answered (exit {probe.returncode}: "
                f"{probe.stdout.strip().splitlines()[-1][:160] if probe.stdout.strip() else 'no output'}); read as no "
                "Linux gate running")
        if probe.returncode != EXIT_BUSY:
            return None
        lines = [ln.strip() for ln in probe.stdout.splitlines() if ln.strip().startswith(LINUX_VERDICT)]
        return lines[-1][len(LINUX_VERDICT):].removesuffix(" ===") if lines else "a Linux gate of this tree (no detail)"

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


def gate_running(run: Path) -> Callable[[], bool]:
    """THE READER'S TEST OF "THIS RUN'S GATE IS STILL RUNNING" (kb/Work PB3662, review R1-1 of wave 20261104): the gate
    lock (`live_gate`) is held by a gate that writes this run, never a process id, which the OS reuses: a reader that
    arrives after a reboot or a long death (the planner's survey of a handed-over gate, `--await` from the rolling wave)
    must not read a stranger holding the dead gate's pid as the gate. A run with no `gate.pid` is a gate that has not
    taken its lock yet (`Gate.run` writes the pid UNDER the lock) or one from before kb/Work PB3536: the reader cannot
    tell it from a gate not started, so it reads as running, and `watch` words it as such."""
    return lock_running(run, lock_of_run(run))


def lock_running(run: Path, lock: Path | None) -> Callable[[], bool]:
    """`gate_running` over a named gate lock (None: no gate can run there)."""
    def running() -> bool:
        if read_pid(run) is None:
            return True
        live = live_gate(lock) if lock is not None else None
        return live is not None and (live.run is None or live.run.name == run.name)
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
    # the gate writes its own gate.pid UNDER its lock (`Gate.run`), so a reader that finds a pid file knows the gate took the
    # lock: the launcher writes none (a pid written at the spawn would read a gate still starting as a dead one). The
    # follower alone has watched its child since the spawn, so its liveness is the child's.
    say(f"build-local: the gate runs DETACHED (pid {pid}, run directory {run}): it outlives this process; "
        f"`python scripts/run_gate_legs.py --await {run}` reads its verdict (kb/Work PB3536)")
    code, line = watch(run, None, lambda: _pid_alive(pid), echo=say)
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
    #: The change's lane (scripts/inert_lane.py, kb/Work PB2878): `inert` (no test reads it: the audits, then the
    #: verdict) or `full`; None until the audits passed.
    lane: str | None = None
    #: The Linux half of the targeted leg (kb/Work PB3612), when this host ran it: {verdict, cause, line, log, legs, tree}
    #: where `tree` is the committed HEAD's tree the Linux clone measured. None: it ran none (a Linux host, the inert
    #: lane, or a Windows half that was red first).
    linux: dict | None = None
    head_tree: str | None = None


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
    (which Windows reuses) is ever trusted. Taken and released at once to read it. None when no gate runs."""
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
        self.say(out.line)
        return out

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
                                "re-run once it ends, or `python scripts/run_gate_legs.py --await <its run>` reads "
                                "its verdict", EXIT_BUSY)
        with lock:
            if run is not None:  # a detached gate's pid goes UNDER its lock: a pid file then proves the lock was taken,
                write_pid(run, os.getpid())   # and the lock (`gate_running`), never the reusable pid, proves it still runs
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
                    out.head_tree = self.host.head_tree()
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
        for p in reversed(run_dirs(self.run_root)):
            rec = read_record(p) if p != current else None
            if (rec is not None and rec.get("exit_code") == 0 and rec.get("tree") == out.tree
                    and rec.get("tree_end") == out.tree and rec.get("base") == out.base
                    and (not rec.get("leg1_only") or out.scope == "leg1") and self._linux_attested(rec, out)):
                return p, rec
        return None

    def _linux_attested(self, rec: dict, out: Outcome) -> bool:
        """A recorded GREEN answers for this gate only if it carries what this gate measures (kb/Work PB3612): on a host
        that runs the Linux half, a GREEN Linux half of the COMMITTED tree this head has (the Linux clone measures HEAD,
        the Windows half the working tree). The inert lane's is the Linux self-tests (kb/Work PB3646), so it is
        required there too."""
        if not self.host.runs_linux_leg():
            return True
        linux = rec.get("linux") or {}
        return linux.get("verdict") == "GREEN" and out.head_tree is not None and linux.get("tree") == out.head_tree

    def _reused(self, out: Outcome, run: Path, started: float, prior: Path, rec: dict) -> Outcome:
        """Answer from a recorded GREEN (`reusable`): no audit, slot, build or leg runs, and the line says so. Along a
        chain of reuses the line names the run that MEASURED the tree and that run's own verdict detail."""
        out.tree_end, out.reused_from = out.tree, rec.get("reused_from") or prior.name
        out.reused_detail = rec.get("reused_detail") or str(rec.get("line", "")).removeprefix(VERDICT).removesuffix(" ===")
        out.scope_until, out.leg1_only, out.plan_note = rec.get("scope_until"), bool(rec.get("leg1_only")), rec.get("plan", "")
        out.lane, out.linux = rec.get("lane"), rec.get("linux")  # a chain of reuses keeps what the first run measured
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
        """The newest EARLIER run that reached a verdict and kept a trx: the plan's timings and reds."""
        for p in reversed(run_dirs(self.run_root)):
            if p != current and (p / VERDICT_FILE).exists() and any(p.glob("*.trx")):
                return p
        return None

    def _prune(self, current: Path) -> None:
        for p in run_dirs(self.run_root)[:-KEEP_RUNS]:
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
        lane = self.host.lane(out.base, out.tree)
        out.lane = lane.name
        self.say(f"build-local: {lane.name} lane (scripts/inert_lane.py) — {lane.reason}")
        if lane.inert:
            # ⭐ THE INERT LANE (kb/Work PB2878; DESIGN-test-build-ci.md §3.18): every changed path is one NO TEST reads
            # (the classification the CI `changes` job and the Linux gate use, proved by the inotify watch of every
            # Linux test run), so no slot, build or leg can measure anything about it; the audits above, which read it,
            # were the gate. GREEN under either scope: there is no leg 2 left unrun, so the record covers both.
            # ⭐ ... and one thing more (kb/Work PB3646): CI's `audits` job also runs the script self-tests on LINUX on every
            # event, the inert ones included, and no test-leg watch covers them (they are audits that read the inert
            # paths by design). So a Windows host runs them in a WSL clone of HEAD too, under a slot: the Linux half of
            # the inert lane, and `tested_locally` requires it as it requires the full lane's.
            return self._inert(out, label, run, lane)
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

    def _inert(self, out: Outcome, label: str, run: Path, lane: inert_lane.Lane) -> Outcome:
        """THE INERT LANE's end (kb/Work PB2878, PB3646): no build and no test leg, because no test reads the change; the
        audits ran before. A host that runs the Linux half (`Host.runs_linux_leg`) also runs the script self-tests in a WSL
        clone of HEAD, under a gate slot (the clone and its interpreters are the load the cap rations), exactly the job
        CI's `audits` runs on Linux on every event. A Linux host's audits ARE that run. GREEN under either scope: there is no
        leg 2 left unrun, so the record covers both."""
        linux = ""
        if self.host.runs_linux_leg():
            t = time.monotonic()
            try:
                slot = self.host.take_slot(label, self.say)
            except ValueError as e:  # the shared gate settings turned malformed while this gate queued
                return self._finish(out, run, NOT_RUN_CAUSES["settings"].verdict, str(e), 2)
            out.timings["slot_wait_s"] = round(time.monotonic() - t, 1)
            out.slot = slot.describe()
            try:
                stop = self._linux_leg(out, run, None, [], slot.spawn_kwargs, inert=True)
            finally:
                slot.release()
            if stop is not None:
                return self._linux_stopped(out, run, stop, "the audits ran and no test leg was due (inert lane)")
            if out.reasons:
                out.tree_end = self.host.measured_tree()
                return self._finish(out, run, "RED", f"{'; '.join(out.reasons)} — the inert lane's Linux self-tests "
                                    f"(kb/Work PB3646) are not GREEN: {out.linux['line']} (run {run.name})", 1)
            linux = " and the Linux self-tests (WSL clone of HEAD) are GREEN"
        out.tree_end = self.host.measured_tree()
        verdict = f"{LEG1_ONLY}: GREEN" if out.scope == "leg1" else "GREEN"
        return self._finish(out, run, verdict, f"INERT LANE (kb/Work PB2878): {lane.reason}; the audits are GREEN{linux}, "
                            f"and no build or test leg ran (run {run.name})", 0)

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
        if not (reds or out.reasons or out.identity or out.stopped) and self.host.runs_linux_leg():
            # FAIL FAST, Windows first (kb/Work PB3022 item 10, PB3612): the Linux half runs only on a Windows half that
            # is green, under the slot this gate still holds.
            if (stop := self._linux_leg(out, run, plan, sorted(to_run), spawn)) is not None:
                return self._linux_stopped(out, run, stop, f"the Windows half ran leg(s) {', '.join(map(str, sorted(to_run)))}")
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        out.tree_end = self.host.measured_tree()  # a tree edited WHILE it was gated is never reused (`reusable`)
        self._prune(run)
        return self._verdict(out, run, plan, legs)

    def _linux_stopped(self, out: Outcome, run: Path, stop: LinuxLeg, ran: str) -> Outcome:
        """The gate's end when its Linux half measured nothing (`_linux_leg` returned `stop`): BUSY (another Linux gate of
        the worktree runs; never a red) or NOT RUN with the Linux gate's own cause. `ran` says what the Windows half did."""
        out.tree_end = self.host.measured_tree()
        if stop.reading == BUSY_READING:
            return self._finish(
                out, run, BUSY, f"the Linux half of the targeted leg is BUSY ({stop.line}): another Linux "
                "gate of this worktree runs, so the Linux half measured nothing and nothing is red: re-run "
                f"once it ends; {ran} (run {run.name})", EXIT_BUSY)
        return self._finish(out, run, NOT_RUN_CAUSES[stop.reading.cause.tag].verdict,
                            f"the Linux half of the targeted leg was NOT RUN ({stop.line}); {ran} (run {run.name})", 2)

    def _linux_leg(self, out: Outcome, run: Path, plan: dict | None, legs: list[int], spawn: Spawn,
                   inert: bool = False) -> LinuxLeg | None:
        """THE LINUX HALF OF THE TARGETED LEG (kb/Work PB3612; DESIGN-test-build-ci.md §3.17.3): the plan's `legs`, the
        legs the Windows half just ran, in a WSL clone of the committed HEAD (`Host.linux_leg`), printed here and recorded
        in `out.linux`. A red is a reason (the verdict is RED and names the host); a NOT RUN, which measured nothing, is
        returned so the gate ends NOT RUN with the Linux gate's own cause; a BUSY (another Linux gate of this worktree
        runs: linux-gate.sh's one gate per tree, kb/Work PB3310) is returned so the gate ends BUSY, never a red.
        `inert`: the INERT LANE's Linux half (kb/Work PB3646): the script self-tests alone, no plan and no leg."""
        t = time.monotonic()
        if inert:
            self.say("build-local: the Linux half of the INERT lane — the script self-tests in a WSL clone of HEAD "
                     "(scripts/linux-gate.sh --legs inert; kb/Work PB3646), under this gate's slot")
        else:
            self.say(f"build-local: the Linux half — leg(s) {', '.join(map(str, legs))} of the same plan in a WSL clone of "
                     "HEAD (scripts/linux-gate.sh --legs targeted; kb/Work PB3612), under this gate's slot")
        leg = self.host.linux_leg(run, run / "plan.json" if plan is not None else None, legs, spawn, inert=inert)
        out.timings["linux_leg_s"] = round(time.monotonic() - t, 1)
        log = run / leg.log
        if log.is_file():
            for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
                self.say(line)
        cause = leg.reading.cause
        out.linux = {"verdict": leg.reading.word, "cause": cause.tag if cause else None, "line": leg.line,
                     "log": leg.log, "legs": legs, "tree": out.head_tree}
        if leg.reading == GREEN_READING:
            return None
        if leg.reading == BUSY_READING:
            return leg
        if leg.reading.word == NOT_RUN:
            return leg if cause else LinuxLeg(not_run("unclassified"), leg.line, leg.log)
        out.reasons.append("LINUX HALF RED")
        return None

    def _plan(self, out: Outcome, listings: dict[str, Counter], run: Path) -> tuple[dict | None, str | None]:
        """The order plan. No plan input, a stale one or a broken one gives ONE leg in the plain order (§3.14.1) —
        named, never a skip."""
        try:
            base = self._base()
            previous = self.previous_run(run)
            plan, digest = self.host.plan(listings, base, previous, run / "plan.json")
        except Exception as e:  # noqa: BLE001 — ANY planning failure falls back to the whole population in one leg
            if isinstance(e, UnreadableTrx):
                # The plan's reds and timings come from the previous gates' trx files; one it cannot read is named and
                # KEPT in this run (kb/Work PB3661): the Linux half empties TestResults/linux-gate/ before it writes, so
                # the file would be gone by the verdict. The whole-population fallback stays: with a source of reds
                # unreadable the gate cannot know what was red, and leg 2 is not run locally (PB2515), so dropping that
                # source's tier 0a could hide a red.
                out.plan_note = f"no plan ({e}; {self._keep_unreadable(run, e.path)})"
            else:
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
    def _keep_unreadable(run: Path, path: Path) -> str:
        """Copy a trx the plan could not read into the run directory as `unreadable-<its directory>-<its name>.bad`
        (never a `.trx`: later readers glob a run directory's trx files and must not meet it), and say where it is, or
        why it could not be kept."""
        kept = run / f"unreadable-{path.parent.name}-{path.name}.bad"
        try:
            shutil.copy2(path, kept)
        except OSError as e:
            return f"not kept: {e}"
        return f"kept as {kept.name}"

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
                  + (f" · Linux half (WSL clone of HEAD) {out.linux['verdict']}" if out.linux else "")
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
                "head_tree": out.head_tree, "linux": out.linux,
                "reused_from": out.reused_from, "reused_detail": out.reused_detail, "reasons": out.reasons,
                "slot": out.slot, "plan_sha256": out.plan_sha256, "plan": out.plan_note, "stopped": out.stopped,
                "scope": out.scope, "scope_until": out.scope_until, "leg1_only": out.leg1_only, "lane": out.lane,
                "legs": {str(leg): names for leg, names in out.legs_invoked.items()},
                "runs": [vars(r) for r in out.runs], "timings": out.timings, "not_run": out.not_run,
                "timings_store": out.timings_store,
                "identity": out.identity,
                "populations": {a: {"discovered": p.discovered, "ran": p.ran, "skipped": p.skipped, "exact": p.exact}
                                for a, p in out.populations.items()}}


def plan_leg(host: Host, plan_arg: str, legs: list[int], results: Path, say: Callable[[str], None] = print) -> int:
    """THE LINUX HALF OF THE TARGETED LEG (kb/Work PB3612; DESIGN-test-build-ci.md §3.17.3): the legs `legs` of an
    EXISTING plan (`plan_arg`: the Windows half's plan.json, or `none` for the whole population in one leg), run
    against the solution already built in `host.repo` (scripts/linux-gate.sh builds its clone once, then calls this).
    No audits, no build, no slot, no timings publish: the Windows half ran them in the gate that holds the slot. What
    it keeps is what makes the Windows half sound, with the same code: each leg through the one handshake
    (`Gate._run_leg`, `leg_env`), the population check and the identity check against THIS clone's listing and
    binaries (a case the plan never heard of runs in leg 1; a planned case this host lacks is not a drop). Results go
    to `results`. Prints `=== PLAN LEG: GREEN|RED — … ===`; exit 0 only on GREEN."""
    results.mkdir(parents=True, exist_ok=True)
    plan = digest = None
    if plan_arg != "none":
        copy = results / "plan.json"
        shutil.copyfile(plan_arg, copy)  # the identity records land beside the plan: the Windows run's stay untouched
        digest, plan = _sha256(copy), json.loads(copy.read_text(encoding="utf-8"))
    out = Outcome()
    gate = Gate(host, results, say=say)
    try:
        binaries = host.binaries()
        with concurrent.futures.ThreadPoolExecutor(max_workers=len(GATED_ASSEMBLIES)) as pool:
            listed = dict(zip(GATED_ASSEMBLIES, pool.map(lambda a: host.listing(a, _no_slot), GATED_ASSEMBLIES)))
    except PopulationError as e:
        say(f"{PLAN_LEG_VERDICT}RED — the population could not be listed: {e} ===")
        return 1
    listings = {asm: names for asm, (names, _) in listed.items()}
    planned = gate._legs(plan)
    to_run = {leg: planned[leg] for leg in legs if leg in planned}
    started = time.monotonic()
    for leg, assemblies in to_run.items():
        out.legs_invoked[leg] = assemblies
        gate._run_leg(out, leg, assemblies, results, plan, digest, _no_slot, started)
    gate._check_populations(out, listings, planned, results)
    if plan is not None:
        gate._check_identity(out, plan, digest, binaries, listings, results)
    problems = ([f"{r.asm} leg {r.leg} RED" for r in out.runs if r.red] + out.reasons
                + [f"{a} POPULATION NOT EXACT" for a, p in out.populations.items() if not p.exact and not p.stopped_early]
                + ([f"{len(out.identity)} IDENTITY MISMATCH(ES)"] if out.identity else []))
    if not to_run:
        problems.append(f"the plan has none of the leg(s) {', '.join(map(str, legs))}")
    tally = " · ".join(f"{a} {out.populations[a].ran:,}/{out.populations[a].discovered:,}" if a in out.populations
                       else f"{a} UNMEASURED" for a in GATED_ASSEMBLIES)
    what = (f"leg(s) {', '.join(map(str, to_run))} of plan {plan['sha256'][:12]}" if plan is not None
            else "the whole population in one leg, no plan")
    say(f"{PLAN_LEG_VERDICT}{'RED — ' + '; '.join(problems) + ' — ' if problems else 'GREEN — '}{what}: {tally} cases ran "
        "===")
    return 1 if problems else 0


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
        self.committed_tree = self.trees[0]  # HEAD's tree: a gate of a clean working tree measures it
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

    def lane(self, base, tree):
        """The planted lane: `inert` plants a change no test reads; the classification itself is inert_lane.py's
        self-test."""
        return (inert_lane.Lane(True, "planted: all 2 changed path(s) are inert") if "inert" in self.plant
                else inert_lane.Lane(False, "planted: the full lane"))

    def head(self) -> str | None:
        return "head-1"

    def head_tree(self) -> str | None:
        return self.committed_tree

    def runs_linux_leg(self) -> bool:
        """The planted Linux half: a plant naming `linux` (`linux`, `linux-red`, `linux-sdk`, `linux-silent`,
        `linux-busy`) makes this a Windows host that runs it; none plants a Linux host (its own legs are the Linux
        half)."""
        return any(p.startswith("linux") for p in self.plant)

    def linux_leg(self, run, plan, legs, spawn, inert=False) -> LinuxLeg:
        self.events.append("linux inert" if inert
                           else f"linux {plan.name if plan else 'none'} legs {','.join(map(str, legs))}")
        if "linux-silent" in self.plant:
            (run / LINUX_LEG_LOG).write_text("PLANTED-LINUX-NOISE\n", encoding="utf-8", newline="\n")
            return LinuxLeg(not_run("verdict"), "no `=== LINUX GATE: ` line", LINUX_LEG_LOG)
        if "linux-sdk" in self.plant:
            line = f"{LINUX_VERDICT}NOT RUN [sdk] (no .NET SDK in this Linux environment) ==="
        elif "linux-busy" in self.plant:
            line = (f"{LINUX_VERDICT}BUSY — another Linux gate of this tree is running (pid 4242); this run touched "
                    "nothing ===")
        elif "linux-red" in self.plant:
            line = f"{LINUX_VERDICT}RED (failed or not run: unit; HEAD head-1; logs in TestResults/linux-gate/) ==="
        elif inert:
            line = f"{LINUX_VERDICT}GREEN (inert lane, kb/Work PB2878, PB3646: planted; legs selftests; HEAD head-1) ==="
        else:
            line = f"{LINUX_VERDICT}GREEN (targeted legs {','.join(map(str, legs))}; HEAD head-1) ==="
        (run / LINUX_LEG_LOG).write_text(f"PLANTED-LINUX-LEG-OUTPUT\n{line}\n", encoding="utf-8", newline="\n")
        return LinuxLeg(read_verdict(line[len(LINUX_VERDICT):].removesuffix(" ===")), line, LINUX_LEG_LOG)

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
        if "plan-torn" in self.plant:   # the Linux gate's newest run left a torn trx (kb/Work PB3661)
            torn = self.root / "linux-gate" / "unit.trx"
            torn.parent.mkdir(exist_ok=True)
            torn.write_text("<TestRun><Results><UnitTestResult outcome=\"" + chr(1) + "\"/>", encoding="utf-8", newline="\n")
            gate_plan.read_reds(torn.parent)
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
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    isolate()  # ⛔ hermetic: the arms below make git repositories (scratch_repo.isolate, kb/Work PB1719)
    results: list[tuple[str, bool, str]] = []
    root = scratch_dir("gate-legs-selftest-")
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
        o, h = gate("plan-torn", plant={"plan-torn"})
        run_dirs_torn = sorted((h.root / "runs").iterdir())
        kept = [p for r in run_dirs_torn if r.is_dir() for p in r.glob("unreadable-*")]
        arm("an unreadable previous-gate trx is NAMED by its path in the plan note, KEPT in the run directory (never as "
            "a .trx a later reader would glob) and still costs the plan: ONE leg in the plain order, GREEN (kb/Work "
            "PB3661)",
            o.verdict == "GREEN" and o.legs_invoked == {1: list(GATED_ASSEMBLIES)}
            and str(h.root / "linux-gate" / "unit.trx") in o.line and "unreadable-linux-gate-unit.trx.bad" in o.line
            and len(kept) == 1 and kept[0].name == "unreadable-linux-gate-unit.trx.bad"
            and kept[0].read_bytes() == (h.root / "linux-gate" / "unit.trx").read_bytes(), o.line)
        # THE LINUX GATE'S HOLDER (kb/Work PB3649): `linux-gate.sh --probe` through the real Host, with stand-ins for the
        # process (no WSL is started) and for the lookup of wsl.exe.
        class _Probed:
            def __init__(self, rc: int, out: str):
                self.returncode, self.stdout = rc, out

        busy_text = (LINUX_VERDICT + "BUSY — another Linux gate of this tree is running (pid 4242, started Fri Oct 24 "
                     "11:00:00 2026); this run touched nothing ===\n")
        calls: list[list[str]] = []

        def probing(rc: int, out: str = "", raises: BaseException | None = None):
            def run(cmd, **kw):
                calls.append(cmd)
                if raises is not None:
                    raise raises
                return _Probed(rc, out)
            return run

        real = Host(root)
        held = real.linux_gate_holder(run=probing(EXIT_BUSY, "wsl noise\n" + busy_text), which=lambda _: "wsl.exe")
        arm("the Linux gate's holder is the --probe's BUSY line (pid and start time), asked as `linux-gate.sh --probe`",
            held is not None and held.startswith("BUSY — another Linux gate of this tree is running (pid 4242, started")
            and not held.endswith("===") and calls == [["bash", "scripts/linux-gate.sh", "--probe"]], f"{held} {calls}")
        said_probe: list[str] = []
        free_ok = real.linux_gate_holder(run=probing(0, "linux-gate probe: FREE — no Linux gate of this tree runs\n"),
                                         which=lambda _: "wsl.exe", say=said_probe.append) is None and not said_probe
        unanswered = [real.linux_gate_holder(run=probing(2, LINUX_VERDICT + "NOT RUN [tree] (x) ===\n"),
                                             which=lambda _: "wsl.exe", say=said_probe.append),
                      real.linux_gate_holder(run=probing(0, raises=subprocess.TimeoutExpired("bash", 1)),
                                             which=lambda _: "wsl.exe", say=said_probe.append),
                      real.linux_gate_holder(run=probing(0, raises=FileNotFoundError("bash")),
                                             which=lambda _: "wsl.exe", say=said_probe.append)]
        arm("no holder when the probe says FREE (said nothing), and when it could not answer (exit 2), hangs or cannot "
            "start (each SAID, never silent): a retry that meets BUSY again touches nothing, a wait that never ends costs "
            "the branch",
            free_ok and unanswered == [None, None, None] and len(said_probe) == 3
            and all("was not answered" in m and "read as no Linux gate running" in m for m in said_probe), str(said_probe))
        if os.name == "nt":
            calls.clear()
            arm("a Windows host with no WSL has no Linux gate to hold anything, and is not asked",
                real.linux_gate_holder(run=probing(EXIT_BUSY, busy_text), which=lambda _: None) is None and not calls)
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
        o, h = gate("inert", plant={"inert"}, scope=None)
        arm("a change in the INERT LANE (kb/Work PB2878) runs the audits and then ends GREEN under the leg1 scope, with "
            "no fetch, slot, build or leg, the lane recorded",
            o.verdict == f"{LEG1_ONLY}: GREEN" and o.exit_code == 0 and "INERT LANE (kb/Work PB2878)" in o.line
            and h.events == ["audits"] and not h.built and not o.runs and o.lane == "inert" and not o.leg1_only
            and read_verdict(o.line[len(VERDICT):]) == GREEN_READING, f"{o.line} {h.events}")
        o, h = gate("inert-whole", plant={"inert"})
        arm("the inert lane under the `whole` scope is a plain GREEN: no leg 2 is left unrun",
            o.verdict == "GREEN" and o.exit_code == 0 and h.events == ["audits"], o.line)
        o, h = gate("inert-audit", plant={"inert", "audit"})
        arm("a red audit is RED in the inert lane too: the audits ARE the inert lane's gate",
            o.verdict == "RED" and o.exit_code == 1 and h.events == ["audits"] and o.lane is None, o.line)
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
        mark = scratch_dir("first-red-") / "grandchild.pid"
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
                                       "NOT RUN [disk] (777216 free inodes on the filesystem holding /x) ===",
                                       "NOT RUN [driver] — x", "NOT RUN — x", "NOT RUN [nosuch] — x", "NOT RUNNING",
                                       "GREEN (legs unit) ===", "GREENISH")]
            == [GREEN_READING, GREEN_READING, RED_READING, RED_READING, RED_READING, RED_READING, not_run("settings"),
                not_run("sdk"), not_run("disk"), not_run("driver"), Reading(NOT_RUN, UNCLASSIFIED), Reading(NOT_RUN, UNCLASSIFIED),
                RED_READING, GREEN_READING, RED_READING]
            and NOT_RUN_CAUSES["disk"].carried == ENVIRONMENT
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

        # THE LINUX HALF OF THE TARGETED LEG (kb/Work PB3612): on a host that runs it, the same plan's legs in a WSL
        # clone of HEAD, after a green Windows half and under the slot the gate holds.
        d = root / "linux-half"
        d.mkdir()
        lin, hl = regate(d, plant={"linux"})
        def newest(dd: Path) -> Path:
            return run_dirs(dd / "runs")[-1]
        lrec = read_record(newest(d))
        arm("the Linux half runs the SAME plan's legs after the Windows half's, inside the slot, prints its output, and "
            "is recorded with the committed tree it measured (the evidence enqueue.py reads)",
            lin.verdict == f"{LEG1_ONLY}: GREEN" and lin.exit_code == 0
            and hl.events[-2:] == ["publish", "linux plan.json legs 1"] and hl.slots_taken == 1
            and "Linux half (WSL clone of HEAD) GREEN" in lin.line and any("PLANTED-LINUX-LEG-OUTPUT" in l for l in sink)
            and lrec["linux"]["verdict"] == "GREEN" and lrec["linux"]["tree"] == lrec["head_tree"] == "tree-1"
            and lrec["linux"]["legs"] == [1], lin.line)
        again_l, h_again = regate(d, plant={"linux"})
        windows_only, h_win = regate(d)
        no_half = root / "linux-half-old"
        no_half.mkdir()
        regate(no_half)                      # a record from a host that ran no Linux half (or before this rule)
        old_rec, h_old = regate(no_half, plant={"linux"})
        arm("a recorded GREEN answers a re-gate only when it carries the Linux half this gate measures: a record with "
            "one is reused (the Linux half not run again), one without is not; a Linux host reuses a record as before",
            "REUSED" in again_l.line and "linux" not in " ".join(h_again.events) and h_again.events == []
            and "REUSED" in windows_only.line and "REUSED" not in old_rec.line and "build" in h_old.events
            and old_rec.linux and old_rec.linux["verdict"] == "GREEN", f"{again_l.line} | {old_rec.line}")
        d2 = root / "linux-half-stale"
        d2.mkdir()
        first_l, _ = regate(d2, plant={"linux"})
        stale_rec = newest(d2)
        tampered = {**read_record(stale_rec), "linux": {**first_l.linux, "tree": "tree-of-another-head"}}
        (stale_rec / VERDICT_FILE).write_text(json.dumps(tampered), encoding="utf-8", newline="\n")
        stale, h_stale = regate(d2, plant={"linux"})
        arm("a recorded Linux half is reused only for the committed tree the re-gate has (the Linux clone measures "
            "HEAD, the Windows half the working tree): a record naming another HEAD tree is run again",
            "REUSED" not in stale.line and any(e.startswith("linux ") for e in h_stale.events), stale.line)
        o, h = gate("linux-win-red", plant={"linux", "red-leg1"})
        arm("FAIL FAST, Windows first: a red Windows half never starts the Linux clone",
            o.exit_code == 1 and o.linux is None and not any(e.startswith("linux ") for e in h.events), o.line)
        o, h = gate("linux-red", plant={"linux-red"})
        arm("a red Linux half is a RED gate that names the host, exit 1, with the Windows half's tally in the line",
            o.exit_code == 1 and "LINUX HALF RED" in o.line and o.linux["verdict"] == "RED"
            and o.verdict == "RED" and "Conformance 6/6" in o.line, o.line)
        o, h = gate("linux-sdk", plant={"linux-sdk"})
        o_silent, _ = gate("linux-silent", plant={"linux-silent"})
        arm("a Linux half that measured nothing ends the gate NOT RUN with the Linux gate's own cause (exit 2), never "
            "GREEN and never a red of the change: its NOT RUN [sdk]; a Linux gate that printed no verdict line is "
            "NOT RUN [verdict]",
            o.exit_code == 2 and o.verdict == "NOT RUN [sdk]" and o.linux["verdict"] == NOT_RUN
            and o_silent.exit_code == 2 and o_silent.verdict == "NOT RUN [verdict]", f"{o.line} | {o_silent.line}")
        o, h = gate("linux-busy", plant={"linux-busy"})
        arm("a BUSY Linux half (another Linux gate of this worktree runs: linux-gate.sh's one gate per tree, kb/Work "
            "PB3310) ends the gate BUSY with BUSY's own exit, never RED: the one verdict reader reads its line as BUSY",
            o.exit_code == EXIT_BUSY and o.verdict == BUSY and o.linux["verdict"] == BUSY
            and "LINUX HALF RED" not in o.line and read_verdict(o.line[len(VERDICT):]) == BUSY_READING, o.line)
        o, h = gate("linux-inert", plant={"linux", "inert"}, scope=None)
        arm("the inert lane runs no build and no test leg, but its Linux half is the script self-tests alone (kb/Work "
            "PB3646: CI's audits job runs them on Linux on every event), run after the audits under a gate slot, "
            "recorded, and named in the verdict",
            o.exit_code == 0 and o.lane == "inert" and o.linux is not None and o.linux["verdict"] == "GREEN"
            and h.events == ["audits", "slot", "linux inert"] and not h.built and h.slots_taken == 1
            and "Linux self-tests (WSL clone of HEAD) are GREEN" in o.line, f"{o.line} {h.events}")
        o_red, _ = gate("linux-inert-red", plant={"linux-red", "inert"}, scope=None)
        o_sdk, _ = gate("linux-inert-sdk", plant={"linux-sdk", "inert"}, scope=None)
        o_busy, _ = gate("linux-inert-busy", plant={"linux-busy", "inert"}, scope=None)
        arm("a red Linux self-test run in the inert lane is a RED gate (exit 1) that names the host; one that measured "
            "nothing is NOT RUN with the Linux gate's cause and a BUSY one is BUSY, exactly as in the full lane",
            o_red.exit_code == 1 and o_red.verdict == "RED" and "Linux self-tests" in o_red.line
            and o_sdk.exit_code == 2 and o_sdk.verdict == "NOT RUN [sdk]"
            and o_busy.exit_code == EXIT_BUSY and o_busy.verdict == BUSY,
            f"{o_red.line} | {o_sdk.line} | {o_busy.line}")
        d_inert = root / "linux-inert-records"
        d_inert.mkdir()
        regate(d_inert, plant={"inert"})     # a Linux host's inert record: its audits are the Linux run, it has no Linux half
        on_windows, h_inert = regate(d_inert, plant={"inert", "linux"})
        reused_inert, h_reused = regate(d_inert, plant={"inert", "linux"})
        arm("an inert-lane record with no Linux half is no evidence on a host that runs one (the gate runs the Linux "
            "self-tests); once it carries one, a re-gate reuses it",
            "REUSED" not in on_windows.line and "linux inert" in h_inert.events
            and "REUSED" in reused_inert.line and h_reused.events == [], f"{on_windows.line} | {reused_inert.line}")
        o, h = gate("linux-leg1-of-whole", plant={"linux"})
        arm("under the `whole` scope the Linux half runs both legs the Windows half ran",
            o.exit_code == 0 and h.events[-1] == "linux plan.json legs 1,2" and o.linux["legs"] == [1, 2], str(h.events))

        # plan_leg, the Linux clone's command: the legs of an EXISTING plan against the built tree, the same code
        # (`Gate._run_leg`, the population and identity checks) as the Windows half.
        d = root / "plan-leg"
        d.mkdir()
        ph = FakeHost(d, d / "gate.lock")
        pl, digest = ph.plan({a: Counter(ph.POPULATION[a]) for a in GATED_ASSEMBLIES}, "0" * 40, None, d / "plan.json")
        sink.clear()
        rc1 = plan_leg(ph, str(d / "plan.json"), [1], d / "out1", sink.append)
        v1 = [l for l in sink if l.startswith(PLAN_LEG_VERDICT)]
        handed = ph.envs.get((1, "Conformance"), {})
        sink.clear()
        h12 = FakeHost(d, d / "gate.lock")
        h12.plan_obj = pl
        rc12 = plan_leg(h12, str(d / "plan.json"), [1, 2], d / "out2", sink.append)
        v12 = [l for l in sink if l.startswith(PLAN_LEG_VERDICT)]
        sink.clear()
        bad_host = FakeHost(d, d / "gate.lock", plant={"digest"})
        bad_host.plan_obj = pl
        rc_bad = plan_leg(bad_host, str(d / "plan.json"), [1, 2], d / "out3", sink.append)
        arm("plan_leg runs the named legs of an existing plan: leg 1 alone is GREEN with the rest named NOT RUN (the "
            "population stopped early), legs 1 and 2 are GREEN and exact, the plan is copied beside the results, every "
            "leg host gets the handshake naming the COPY, and an identity mismatch is RED",
            rc1 == 0 and len(v1) == 1 and "GREEN" in v1[0] and "leg(s) 1 of plan" in v1[0]
            and rc12 == 0 and "GREEN" in v12[0] and "Conformance 6/6" in v12[0]
            and (d / "out1" / "plan.json").read_bytes() == (d / "plan.json").read_bytes()
            and handed.get("COBOLNET_GATE_PLAN") == str((d / "out1" / "plan.json").resolve())
            and handed.get("COBOLNET_GATE_PLAN_SHA256") == digest and rc_bad == 1, f"{v1} {v12} {rc_bad}")
        sink.clear()
        rc_none = plan_leg(FakeHost(d, d / "gate.lock"), "none", [1], d / "out4", sink.append)
        arm("plan_leg without a plan runs the whole population in its one leg with no handshake",
            rc_none == 0 and any("no plan" in l and "GREEN" in l for l in sink if l.startswith(PLAN_LEG_VERDICT)),
            str(sink[-1:]))

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
        fresh = d / "fresh"           # a gate that has not taken its lock yet, so wrote no pid
        fresh.mkdir()
        wlock = d / "worktree.lock"
        with ExclusiveLock.try_take(wlock, f"gate {d} (pid 7, since T{RUN_LABEL}{live})"):   # the gate that runs `live`
            got = {n: watch(d / n, 0, lock_running(d / n, wlock)) for n in ("done", "live", "dead", "fresh", "missing")}
            other = watch(dead, 0, lock_running(dead, wlock))   # the lock is held, but by another run's gate
        arm("--await reads the verdict file (its line and exit code), PENDING (its own exit, never BUSY's) while the "
            "run's gate holds the gate lock (or has not taken it yet), NOT RUN [verdict] (exit 2, BRANCH) when it DIED "
            "without a verdict EVEN WHEN gate.pid names a live process (a reused pid) or another run's gate holds the "
            "lock, NOT RUN [run] (exit 2, ENVIRONMENT) with no run directory",
            got["done"] == (1, f"{VERDICT}RED — planted ===")
            and got["live"][0] == got["fresh"][0] == AWAIT_PENDING_EXIT != EXIT_BUSY
            and got["live"][1].startswith(AWAIT_PENDING)
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
            [_record_exit({"verdict": v}) for v in (f"{LEG1_ONLY}: GREEN", "GREEN", BUSY, "NOT RUN [driver]", "RED")]
            == [0, 0, EXIT_BUSY, 2, 1])

        # The follower and the detached gate, as real processes: a planted gate that prints, waits for a go file,
        # writes its verdict file and exits 1. Like `Gate.run` it takes the worktree's gate lock first, then writes its pid
        # under it, and holds the lock until the verdict file is written.
        plock = root / "planted-gate.lock"

        def planted_gate(run: Path, go: Path | None = None) -> list[str]:
            wait = f"gof = pathlib.Path({str(go)!r})\nwhile not gof.exists(): time.sleep(0.05)\n" if go else ""
            return [sys.executable, "-c", "import json, os, pathlib, sys, time\n"
                    f"sys.path.insert(0, {str(REPO / 'scripts')!r})\nimport run_gate_legs as g\n"
                    f"r = pathlib.Path({str(run)!r})\n"
                    f"lock = g.ExclusiveLock.try_take(pathlib.Path({str(plock)!r}), "
                    f"'gate x (pid %d, since T{RUN_LABEL}%s)' % (os.getpid(), r))\n"
                    "g.write_pid(r, os.getpid())\nprint('PLANTED-GATE-OUTPUT', flush=True)\n"
                    f"{wait}"
                    f"line = {VERDICT!r} + 'LEG 1 ONLY (batched gating, PB2515): RED — planted ==='\n"
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
        handed = watch(run2, 120, lock_running(run2, plock))
        arm("a detached gate OUTLIVES its follower's tree kill and its verdict is read from the run directory",
            survived and handed == (1, f"{VERDICT}LEG 1 ONLY (batched gating, PB2515): RED — planted ==="),
            f"survived {survived}; {handed}")
    finally:
        remove_tree(root)

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
    ap.add_argument("--plan-leg", metavar="PLAN", help="(the Linux half of the targeted leg, run by scripts/linux-gate.sh in "
                    "its built clone; kb/Work PB3612) run the --plan-legs of an existing plan file, or `none` for the "
                    "whole population in one leg, against the solution already built here: no audits, build or slot")
    ap.add_argument("--plan-legs", default="1", metavar="N[,N]", help="--plan-leg: which legs of the plan (default 1)")
    ap.add_argument("--results", metavar="DIR", help="--plan-leg: where the legs' trx, logs and identity records go")
    ap.add_argument("--self-test", action="store_true", help="fire every arm on planted inputs (DESIGN §3.14.4 (3))")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.plan_leg:
        if not args.results:
            ap.error("--plan-leg needs --results")
        return plan_leg(Host(), args.plan_leg, [int(n) for n in args.plan_legs.split(",")], Path(args.results),
                        print_flush)
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
    sys.exit(main())
