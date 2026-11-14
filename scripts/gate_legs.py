#!/usr/bin/env python3
"""gate_legs.py - THE TEST POPULATIONS A GATE RUNS (kb/Work PB5411, PB4328, PB5099; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.5,
step 2): the plan of the retry, the listings, the classes that run first, the hosts and their heartbeat, the population check and the walls
and concurrency the record reports. `Legs` is built over the `Generations` it hands the hosts, and never calls back into `Gate`."""
from __future__ import annotations

import concurrent.futures
import shutil
import subprocess
import sys
import threading
import time
from collections import Counter
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from gate_slot import Heartbeat  # noqa: E402
from test_population import GATED_ASSEMBLIES, LINUX_GATE_RESULTS, PopulationError, UnreadableTrx, check as check_population, not_passed, red_classes  # noqa: E402
from gate_verdict import HostRun, Outcome  # noqa: E402
from gate_host import CI_BOUNCE_RESULTS, Host, Spawn, host_env  # noqa: E402
from gate_reuse import Carry  # noqa: E402
from gate_generated import Generations  # noqa: E402


class Legs:
    """One gate's test populations over a host (kb/Work PB4328, PB5099, PB5218)."""

    def __init__(self, host: Host, say: Callable[[str], None], generations: Generations,
                 heartbeat: Callable[[], Heartbeat] = Heartbeat, heartbeat_poll_s: float = 1.0):
        """`heartbeat`: makes the clock of the line the test phase writes while the hosts run (`run_hosts`), polled every
        `heartbeat_poll_s`; the self-test steps it."""
        self.host, self.say, self.generations = host, say, generations
        self.heartbeat, self.heartbeat_poll_s = heartbeat, heartbeat_poll_s

    def plan(self, out: Outcome, run: Path, listings: dict[str, Counter], carry: Carry,
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

    def pool_listings(self, spawn: Spawn, todo: list[str]) -> dict[str, concurrent.futures.Future]:
        """Start the `--list-tests` hosts (scrubbed, ~2 s each) of the assemblies in `todo` beside the test hosts;
        `listings` joins them."""
        pool = concurrent.futures.ThreadPoolExecutor(max_workers=max(len(todo), 1), thread_name_prefix="listing")
        try:
            return {asm: pool.submit(self.host.listing, asm, spawn, self.generations.handed(asm)) for asm in todo}
        finally:
            pool.shutdown(wait=False)

    @staticmethod
    def listings(listed: dict[str, concurrent.futures.Future], run: Path) -> dict[str, Counter]:
        """Join the listings (`pool_listings`) and keep each in the run directory; raises PopulationError when one could
        not be made."""
        listings = {}
        for asm, future in listed.items():
            names, raw = future.result()
            listings[asm] = names
            (run / f"listing-{asm}.txt").write_text(raw, encoding="utf-8", newline="\n")
        return listings

    def first(self, out: Outcome, run: Path, previous: Path | None) -> Path | None:
        """THE CLASSES THAT RUN FIRST (kb/Work PB4328): every test class with a red case in this worktree's previous run
        (`previous`: the run `Gate.previous_run` found), its newest hand-run Linux gate (`LINUX_GATE_RESULTS`) or the merge-group run that bounced its
        pull request (`CI_BOUNCE_RESULTS`, fetched first, kb/Work PB3644) — every source test_population.TRX_WRITERS
        names — written to
        `<run>/first-classes.txt` for the hosts' orderer (FIRST_CLASSES); None when there is none. A source it cannot read is named on the console and kept in the run directory (kb/Work PB3661), and the
        gate goes on: this orders the run, it never selects it."""
        self.say(self.host.fetch_bounce_results())
        sources = [p for p in (previous, self.host.repo / LINUX_GATE_RESULTS,
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

    def run_hosts(self, out: Outcome, run: Path, first: Path | None, spawn: Spawn, hosts_started: float,
                   plan: dict[str, Path | None]) -> None:
        """The planned assemblies CONCURRENTLY, one host each, each printed through the one leg report
        (scripts/test_leg_report.py) in a fixed order whatever order they finished in. `plan` (`plan`, kb/Work PB5099,
        PB5218) names each assembly that runs: None for its whole population, a retry file for its cases not yet passed;
        an assembly it does not name (up to date, or every case carried) starts no host."""
        asms = [a for a in GATED_ASSEMBLIES if a in plan]
        self.say("build-local: " + (", ".join(f"{a} ({'whole population' if plan[a] is None else 'cases not yet passed'})"
                                              for a in asms) or "no test host: nothing to run")
                 + (" concurrently" if len(asms) > 1 else ""))

        stop_beat, finished = threading.Event(), []   # `finished`: appended by each host thread (list.append is atomic)

        def one(asm: str) -> tuple[str, int, float, float]:
            env = host_env(first, buildgraph.observation_log(run, asm), plan[asm], self.generations.handed(asm))
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

    def check_populations(self, out: Outcome, listings: dict[str, Counter], run: Path, carry: Carry | None,
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

    @staticmethod
    def assembly_walls(out: Outcome) -> dict[str, float]:
        """Per assembly, the seconds its host ran (kb/Work PB4326): what explains a slow test phase, the one the growth
        trigger of kb/Work/PB4323.md §3.4 judges (`Gate._record_growth`, scripts/gate_growth.py), with `_concurrency`."""
        return {r.asm: r.wall_s for r in out.runs}

    @staticmethod
    def concurrency(out: Outcome) -> float | None:
        """EFFECTIVE CONCURRENCY of the hosts (kb/Work PB4326): their wall seconds summed over the span they took. 3.0
        means the assemblies ran flat out side by side; near 1.0, one assembly's tail ran alone. None when no host
        ran."""
        span = out.timings.get("tests_s", 0.0)
        return round(sum(r.wall_s for r in out.runs) / span, 2) if out.runs and span > 0 else None
