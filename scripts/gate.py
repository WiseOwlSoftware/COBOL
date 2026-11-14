#!/usr/bin/env python3
"""gate.py — THE GATE: the one local gate (kb/Work PB4328, step 5 of PB4323; PB3022;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.1, §3.14.3, §3.14.6, §3.17.3).

    python scripts/gate.py [--base <sha>]          # the gate, DETACHED, its output followed to the verdict
    python scripts/gate.py --await <run> [--worktree W] [--wait-s N]  # a gate's verdict, from its run dir
    python scripts/gate.py --audit-shape        # the ONE SHAPE audit: no live line names a gate mode (kb/Work PB3022)
    (python scripts/gate.py --run <run> [--base <sha>] is the detached gate itself; never run it by hand)

Called by `scripts/build-local.ps1` and `scripts/build-local.sh`, which only set the process priority.

⛔ THE POPULATION IS THE SELECTION (owner decision 1 of PB4323, 2026-10-31; kb/Work PB4328). A change that a test can read
runs the WHOLE population of every test target (scripts/build_targets.json, test_population.GATED_ASSEMBLIES) on this host: no plan, no tiers, no impact map, no leg
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
  3. the toolchain (toolchain.env's versions, kb/Work PB4007), then the change's LANE (scripts/lane_classification.py: inert;
     tooling, a change no compiler test or build reads, kb/Work PB4395; or a code lane with or without TOOLING touched),
     then the audits (the one list below, run CONCURRENTLY; when the change touches tooling one of them is every script
     self-test, found by scripts/self_tests.py — kb/Work PB2563, PB4326), started BESIDE everything after them: the inert
     and the tooling lanes join them at once (they are the whole gate: `_inert`, `_tooling`); a code lane runs
     them beside the corpus fetch, the slot wait, the build and the test hosts, and a red one STOPS the hosts
     (`Host.stop_hosts`) and ends the gate RED (kb/Work PB2523, PB4326). The GnuCOBOL corpus, by the ONE rule
     scripts/external_corpus.py (kb/Work PB2611), is fetched BEFORE the slot (kb/Work PB2524);
  4. the gate slot, always after the lock, so no two gates can wait on each other in a cycle; then the solution build;
  5. the gated assemblies' WHOLE populations CONCURRENTLY, each one `dotnet test --no-build` host in a scrubbed
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

import argparse
import concurrent.futures
import datetime as dt
import json
import os
import re
import shutil
import subprocess
import sys
import threading
import time
import traceback
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import lane_classification  # noqa: E402  — THE classification of a change no test reads (kb/Work PB2878)
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from gate_slot import ExclusiveLock, Heartbeat, QueueAbandoned, Slot, gate_class_from_env  # noqa: E402
from test_population import GATED_ASSEMBLIES, LINUX_GATE_RESULTS, Population, PopulationError, UnreadableTrx, check as check_population, failed_results, not_passed, passed_cases, parse_listing, red_classes  # noqa: E402
from test_leg_report import verdict_of  # noqa: E402  — THE reader of a host's verdict line
from standard_streams import ensure_utf8  # noqa: E402
from gate_audits import audit_shape_main  # noqa: E402
from gate_verdict import BUSY, EXIT_BUSY, HOST_PASSED, NOT_RUN, NOT_RUN_CAUSES, VERDICT, audit_reasons, result_outcomes  # noqa: E402
from gate_host import BESIDE, CI_BOUNCE_RESULTS, GATE_CONFIGURATION, Host, HostRun, Spawn, _no_slot, host_env  # noqa: E402
from gate_runs import AWAIT_PENDING_EXIT, AWAIT_WAIT_S, KEEP_RUNS, ORIGINAL_VERDICT_FILE, RECORD_SCHEMA, RUNS_LOCK, RUNS_LOCK_WAIT_S, RUN_LABEL, RUN_ROOT, SCHEMA_WHOLE, VERDICT_FILE, gate_running, launch_and_follow, read_record, record_schema, resolve_run, run_dirs, run_order, run_started, watch, write_pid  # noqa: E402


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
        self.say(f"build-local: {lane.name} lane (scripts/lane_classification.py) — {lane.reason}")
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

    def _inert(self, out: Outcome, run: Path, lane: lane_classification.Lane) -> Outcome:
        """THE INERT LANE's end (kb/Work PB2878): no build and no test host, because no test reads the change; the
        audits were joined, GREEN, before this."""
        out.tree_end = self.host.measured_tree()
        return self._finish(out, run, "GREEN", f"INERT LANE (kb/Work PB2878): {lane.reason}; the audits are GREEN, "
                            f"and no build or test host ran · toolchain {out.toolchain} (run {run.name})", 0)

    def _tooling(self, out: Outcome, run: Path, lane: lane_classification.Lane) -> Outcome:
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

        stop_beat, finished = threading.Event(), []   # `finished`: appended by each host thread (list.append is atomic)

        def one(asm: str) -> tuple[str, int, float, float]:
            env = host_env(first, buildgraph.observation_log(run, asm), plan[asm], self._handed(asm))
            t = time.monotonic()
            rc = self.host.run_host(asm, run, env, spawn)
            finished.append(asm)   # at the host's own return, not at pool.map's submission-order yield (R1-2)
            return asm, rc, time.monotonic() - t, time.monotonic() - hosts_started

        # THE HEARTBEAT (kb/Work PB5097): the hosts write only their own logs, so the gate's console log (the one the
        # loop's progress report reads as the group's act) is silent from the line above to the verdict, 20 minutes and
        # more of a whole-population run. A line at every HEARTBEAT_S says the gate is alive and in its test phase.
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
        means the assemblies ran flat out side by side; near 1.0, one assembly's tail ran alone. None when no host
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
    ap.add_argument("--audit-shape", action="store_true", help="the ONE SHAPE audit: no live line names a gate mode "
                    "(kb/Work PB3022)")
    args = ap.parse_args()
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
