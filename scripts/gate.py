#!/usr/bin/env python3
"""gate.py — THE GATE: the one local gate (kb/Work PB4328, step 5 of PB4323; PB3022;
docs/rearchitecture/DESIGN-test-build-ci.md §3.14.1, §3.14.3, §3.14.6, §3.17.3).

    python scripts/gate.py [--base <sha>]          # the gate, DETACHED, its output followed to the verdict
    python scripts/gate.py --await <run> [--worktree W] [--wait-s N]  # a gate's verdict, from its run dir
    python scripts/gate.py --audit-shape        # the ONE SHAPE audit: no live line names a gate mode (kb/Work PB3022)
    (python scripts/gate.py --run <run> [--base <sha>] is the detached gate itself; never run it by hand)

Called by `scripts/build-local.ps1` and `scripts/build-local.sh`, which only set the process priority.

THE MODULES (kb/Work PB5433, PB5411; DESIGN-orchestrator-loop.md section 16.5). This file is the driver: `Gate` (its stages in order, the
audits and lanes, the work beside them, THE one place a verdict and record are written) and the CLI. `gate_verdict.py` is the verdict
vocabulary and `Outcome`, `gate_audits.py` the audits, `gate_host.py` the real machine, `gate_runs.py` the run store and the await;
`Gate` holds `gate_reuse.PassCarry` (what a recorded run lets this gate reuse), `gate_generated.Generations` and `gate_legs.Legs` (the
test populations), each returning what it found and never calling back.

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
    manifests, `conformance-suite`) is brought up to date at the measured tree (`Generations.generate`): up to date when the
    store records its output digest at its key and its outputs on disk have it; else its generator runs, TRACED, into
    `TestResults/generated/<target>/`. A test target that depends on it is keyed on that OUTPUT DIGEST, not on the
    generator's inputs, so a records-only change whose suite is byte-identical leaves Conformance up to date. The output
    directory is handed to the dependent's host and listing (`WISEOWL_COBOL_GATE_SUITE`, `host_env`), so the host runs
    the suite its key names, and is checked unchanged after the hosts (`_check_generated`). A generation that fails, reads
    outside its inputs, is not deterministic or runs while the tree changes ends the gate RED before any host.

Exit codes: 0 GREEN (reused or run) · 1 RED (or the build failed) · 2 NOT RUN [<cause>] (the population could not be
listed, the driver failed, the gate's process DIED with no verdict (`--await`, or the follower): BRANCH, the tree's own
code; the toolchain is not toolchain.env's, or a tool's version could not be read: ENVIRONMENT; NOT_RUN_CAUSES
classifies every cause once) · 3 BUSY (another
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
import shutil
import subprocess
import sys
import time
import traceback
from collections import Counter
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import lane_classification  # noqa: E402  — THE classification of a change no test reads (kb/Work PB2878)
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from gate_slot import ExclusiveLock, Heartbeat, QueueAbandoned, Slot, gate_class_from_env  # noqa: E402
from test_population import GATED_ASSEMBLIES, PopulationError  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402
from gate_audits import audit_shape_main  # noqa: E402
from gate_verdict import BUSY, EXIT_BUSY, NOT_RUN_CAUSES, VERDICT, Outcome  # noqa: E402
from gate_host import BESIDE, Host, Spawn, _no_slot  # noqa: E402
from gate_runs import AWAIT_PENDING_EXIT, AWAIT_WAIT_S, KEEP_RUNS, RECORD_SCHEMA, RUNS_LOCK, RUNS_LOCK_WAIT_S, RUN_LABEL, RUN_ROOT, VERDICT_FILE, gate_running, launch_and_follow, read_record, resolve_run, run_dirs, run_order, watch, write_pid  # noqa: E402
from gate_reuse import Carry, PassCarry  # noqa: E402
from gate_generated import Generations  # noqa: E402
from gate_legs import Legs  # noqa: E402


class Gate:
    def __init__(self, host: Host, run_root: Path, base: str | None = None,
                 say: Callable[[str], None] = lambda s: print(s, flush=True), run_dir: Path | None = None,
                 now: Callable[[], dt.datetime] = lambda: dt.datetime.now(dt.timezone.utc),
                 heartbeat: Callable[[], Heartbeat] = Heartbeat, heartbeat_poll_s: float = 1.0):
        """`run_dir`: the run directory a detached gate's launcher made (`--run`); None makes a fresh one. `now`: the
        UTC clock a new run's name shows, never its order (`new_run`); the self-test stops it and turns it back.
        `heartbeat`: makes the clock of the line the test phase writes while the hosts run (`Legs.run_hosts`), polled every
        `heartbeat_poll_s`; the self-test steps it."""
        self.host, self.run_root, self.base, self.say, self.run_dir = host, run_root, base, say, run_dir
        self.now = now
        self._pool: concurrent.futures.ThreadPoolExecutor | None = None  # `run` owns it for the work `_beside` starts
        self._base_sha: str | None = None
        # The three collaborators (kb/Work PB5411): each returns what it found and never calls back into this class.
        self.passes = PassCarry(host, run_root, say, now)
        self.generations = Generations(host, say)
        self.legs = Legs(host, say, self.generations, heartbeat, heartbeat_poll_s)

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
                    if (prior := self.passes.reusable(run, out)) is not None:
                        return self._reused(out, run, started, *prior)
                    return self._gate(out, label, started, run)
                except Exception as e:  # noqa: BLE001 — a defect in the driver still ends in a verdict line, never
                    # silence: every caller BLOCKS on that line (MANDATORY-PRACTICES P2), so a traceback alone would
                    # hang it.
                    self.say(traceback.format_exc().rstrip())
                    return self._finish(out, run, NOT_RUN_CAUSES["driver"].verdict,
                                        f"the gate driver failed: {type(e).__name__}: {e}", 2)

    def _reused(self, out: Outcome, run: Path, started: float, prior: Path, rec: dict) -> Outcome:
        """Answer from a recorded GREEN (`PassCarry.reusable`): no audit, slot, build or leg runs, and the line says so. Along a
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
        # A tool whose version the host could not read just now (it did not answer, could not start, or answered no
        # version) is its own cause, never "absent": the gate must not tell the operator to install a tool that is
        # installed, and the line keeps what happened so a recurrence is root-caused from its record (kb/Work PB5616).
        host_tools = self.host.toolchain()
        cause, tools = host_tools.cause, host_tools.tools
        if cause == "toolchain":
            return self._finish(out, run, NOT_RUN_CAUSES["toolchain"].verdict,
                                f"this host's tools are not toolchain.env's: {tools}. Install the manifest's versions "
                                "(winget on the Windows dev box, scripts/wsl/setup-wsl.sh in WSL; `python "
                                f"scripts/toolchain.py check --scope host` lists them); nothing ran (run {run.name})", 2)
        if cause == "toolchain-unread":
            return self._finish(out, run, NOT_RUN_CAUSES["toolchain-unread"].verdict,
                                f"this host could not read a tool's version just now: {tools}. The tool is NOT shown "
                                "to be missing or of another version, so install nothing: this line is the record of "
                                "what happened, and a recurrence is root-caused from it (kb/Work PB5616); nothing ran "
                                f"(run {run.name})", 2)
        if cause is not None:
            raise ValueError(f"Host.toolchain answered the cause {cause!r}, which the gate does not stop on")
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
        if (failed := self.generations.generate(out, self.passes.facets(tools))) is not None:
            self._join_beside(out)   # a generated target RED still reports the work beside it
            return self._finish(out, run, "RED", f"GENERATED TARGET RED — {failed.kind}: {failed.text} — no test host ran: a "
                                f"dependent's key would not be the suite it runs (kb/Work PB5231; run {run.name})", 1)
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
            return self._finish(out, run, *self.generations.all_up_to_date(out, run, started))
        carry = self.passes.carry(run, out, todo)
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
        listed = self.legs.pool_listings(spawn, todo)
        listings: dict[str, Counter] | None = None
        plan: dict[str, Path | None] = {asm: None for asm in todo}
        if carry is not None:
            # A RETRY needs the population first (what the carried passes leave is the listing less them), where a whole
            # run joins the listings after the hosts: ~2 s each, which the retry's saved minutes repay.
            try:
                listings = self.legs.listings(listed, run)
            except PopulationError as e:
                return self._finish(out, run, NOT_RUN_CAUSES["population"].verdict,
                                    f"the population could not be listed: {e}", 2)
            plan = self.legs.plan(out, run, listings, carry, todo)
        if out.tooling:
            self._beside(out, "post_build_self_tests", lambda: self.host.built_self_tests(spawn))
        first = self.legs.first(out, run, self.previous_run(run))
        hosts_started = time.monotonic()
        self.legs.run_hosts(out, run, first, spawn, hosts_started, plan)
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
            listings = listings or self.legs.listings(listed, run)
        except PopulationError as e:
            return self._finish(out, run, NOT_RUN_CAUSES["population"].verdict,
                                f"the population could not be listed: {e}", 2)
        self.legs.check_populations(out, listings, run, carry, todo)
        stale = self.generations.check_generated(out, run, [r.asm for r in out.runs])
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        out.tree_end = self.host.measured_tree()  # a tree edited WHILE it was gated is never reused (`PassCarry.reusable`)
        self.passes.record_passes(out, run, [a for a in todo if a not in stale], self.generations.outputs())
        self._prune(run, out.composed_from)
        return self._verdict(out, run)

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
        detail = (f"{tally} (skipped {skipped:,})" + self.generations.detail()
                  + (f" · passes recorded: {', '.join(out.recorded)}" if out.recorded else "")
                  + (f" · {carried:,} carried from run(s) {', '.join(out.composed_from)} (kb/Work PB5099)" if carried else "")
                  + (f" · {out.slot}" if out.slot else "")
                  + (f" · toolchain {out.toolchain}" if out.toolchain else "") + f" ({first}; run {run.name})")
        reds = [f"{r.asm} RED" for r in out.runs if r.red]
        problems = reds + out.reasons + [f"{a} POPULATION NOT EXACT" for a in pops if pops[a].defective]
        if problems:
            return self._finish(out, run, "RED", f"{'; '.join(problems)} — {detail}", 1)
        return self._finish(out, run, "GREEN", detail, 0)

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
                "assembly_wall_s": Legs.assembly_walls(out), "effective_concurrency": Legs.concurrency(out),
                "populations": {a: {"discovered": p.discovered, "ran": p.ran, "skipped": p.skipped, "exact": p.exact}
                                for a, p in out.populations.items()}}


def print_flush(line: str) -> None:
    print(line, flush=True)


def rederive_main(run: Path, dry_run: bool) -> int:
    """`--rederive RUN [--dry-run]` (kb/Work PB5205, PB5218; `PassCarry.rederive`): what the run's own evidence proves, each
    target's key at its recorded tree and at its worktree's HEAD tree now, and which targets a gate at HEAD would run. 0
    when it is proved (and, without --dry-run, the record rewritten and the passes it proved exact stored), 1 when
    refused, 2 when RUN is no run of a worktree."""
    wt = run.parent.parent.parent
    if run.parent != wt / RUN_ROOT or not run.is_dir():
        print(f"{VERDICT}NOT RUN — {run} is not a run directory of a worktree ({RUN_ROOT}/<run>) ===")
        return 2
    host = Host(wt)
    passes = PassCarry(host, wt / RUN_ROOT, say=print)
    rd = passes.rederive(run)
    for line in rd.found:
        print(f"  found    {line}")
    for line in rd.refusals:
        print(f"  REFUSED  {line}")
    rec = read_record(run) or {}
    facets = passes.facets(str(rec.get("toolchain") or ""))

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
        passes.write_rederived(run, rd)
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
