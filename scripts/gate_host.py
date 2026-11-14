#!/usr/bin/env python3
"""gate_host.py - THE REAL MACHINE THE GATE RUNS ON (kb/Work PB5433; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.5): git, the
gate slot, the audit processes, the build, the listings, the test hosts and their one environment, the bounce results. `Host` is what
`gate_fake_host.FakeHost` plants over; the driver (`gate.py`) holds the one gate that runs on it."""
from __future__ import annotations

import concurrent.futures
import os
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import time
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import external_corpus  # noqa: E402
import gate_growth  # noqa: E402  — THE growth trigger's sample: one per GREEN code-lane gate (kb/Work PB4333)
import lane_classification  # noqa: E402  — THE classification of a change no test reads (kb/Work PB2878)
from self_tests import HANG_EXIT as SELF_TESTS_HANG_EXIT, this_platform  # noqa: E402
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from gate_slot import GateSlots, Slot  # noqa: E402
import toolchain  # noqa: E402  — THE reader of toolchain.env, the versions CI and every host run (kb/Work PB4007)
import process_job  # noqa: E402  — THE one held process tree: an audit's or a test host's whole tree, stopped as one (kb/Work PB5098)
from test_population import GATED_ASSEMBLIES, list_population, scrubbed_env  # noqa: E402
sys.path.append(str(REPO / "scripts" / "orchestrator"))  # appended: no module of it shadows one of scripts/
from coord import NEVER_WORK  # noqa: E402  — THE paths that are never work, so never part of a measured tree
import queue_facts  # noqa: E402  — THE one reader of GitHub's facts: a merge-queue bounce's reds (kb/Work PB3644)
from gate_audits import AUDITS, AUDIT_PREREQUISITES, BUILT_SELF_TESTS, CHANGE_SCOPED, SELF_TESTS_REUSE, TOOLING_ONLY  # noqa: E402


#: ⛔ THE ONE ORDERING CHANNEL TO A TEST HOST (kb/Work PB4328): the path of a file of test class names, one per line, that
#: the host's orderer runs FIRST (tests/_shared/GateLegs.cs `GateOrderer.FirstVariable`, the same name). It orders, it
#: never selects: a class it names that the assembly lacks is ignored, every other class still runs, and the population
#: check proves both. Its prefix is a scrubbed one (test_population.is_scrubbed), so only this gate hands it to a host.
FIRST_CLASSES = "WISEOWL_COBOL_GATE_FIRST"


#: ⛔ THE ONE RETRY CHANNEL TO A TEST HOST (kb/Work PB5099): the path of a file of test display names, one per line, that
#: the host runs INSTEAD of its whole population (tests/_shared/GateLegs.cs `GateOnly`, the same name). Only a re-gate of a
#: tree an earlier run of this worktree already measured sets it (`PassCarry.carry`), for the cases that run did not pass; the
#: population check then composes the passes it carried (test_population.py `check`'s `carried`), so a case neither
#: carried nor run is still a named NEVER RAN. Its prefix is a scrubbed one, so only this gate hands it to a host.
ONLY_CASES = "WISEOWL_COBOL_GATE_ONLY"


#: Where a merge-queue BOUNCE's test results land (kb/Work PB3644): `queue_facts.fetch_bounce_results`, the one GitHub
#: reader, downloads the failed merge-group run's trx files of this branch's pull request here, one subdirectory per
#: artifact, and their red classes go first like the previous run's. No tool WRITES a trx file here, so it is no row.
CI_BOUNCE_RESULTS = Path("TestResults/ci-bounce")


#: The configuration the gate builds and tests (`Host.build`: `dotnet build` of the solution, the SDK's default): a facet of
#: every target's key (kb/Work PB5218), so a pass in it is never one of another configuration.
GATE_CONFIGURATION = "Debug"


LOCK_NAME = "cobol-gate.lock"


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

    def lane(self, base: str | None, tree: str | None) -> lane_classification.Lane:
        """The change's lane: what `tree` (the measured working tree) changes against `base`, classified by THE one
        classification (scripts/lane_classification.py). No base or no tree, or a change git cannot list, is the full lane."""
        if not base or not tree:
            return lane_classification.Lane("full", "no base or no measured tree: the full lane", tooling=True, reads_tooling=True)
        try:
            return lane_classification.CLASSIFICATION.classify(lane_classification.changed(self.repo, base, tree))
        except (subprocess.CalledProcessError, OSError) as e:
            return lane_classification.Lane("full", f"the change could not be listed ({e}): the full lane", tooling=True,
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
        """THIS WORKTREE'S HEAD REFLOG (kb/Work PB5205; `PassCarry.rederive`): (commit, Unix time HEAD moved to it), newest first;
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
        `Generations.handed`), so the listing and the run read one suite."""
        return list_population(self.repo / GATED_ASSEMBLIES[asm], cwd=self.repo, handed=handed, **spawn())

    def fetch_bounce_results(self) -> str:
        """The failed merge-group run's test results of this branch's open pull request, into
        `CI_BOUNCE_RESULTS` (`queue_facts.fetch_bounce_results`, the one GitHub reader); the line it returns says what
        the directory holds. A detached HEAD asks nothing. Never raises: it only orders the run (`Legs.first`)."""
        try:
            branch = self.git("rev-parse", "--abbrev-ref", "HEAD")
        except (OSError, subprocess.SubprocessError):
            branch = ""
        if not branch or branch == "HEAD":
            return "build-local: ci bounce: none (no branch is checked out)"
        return "build-local: " + queue_facts.fetch_bounce_results(branch, self.repo / CI_BOUNCE_RESULTS, self.gh).line

    def run_host(self, asm: str, run: Path, env: dict[str, str], spawn: Spawn) -> int:
        """One assembly's whole population: `dotnet test --no-build` with a trx logger, its trx and its whole output in `run`
        (the run directory, `TestResults/build-local/<run>`, gate_runs.RUN_ROOT), `<asm>.trx` and `<asm>.log`.
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
    on (kb/Work PB5231; `Generations.handed`), so the host runs exactly that suite. The test harness's observation
    log (every retry and non-observation of a child process, tests/_shared/ProcessObservation.cs) goes to `observations`
    in the run directory, one per host, so a non-observation is read beside the host's own log instead of in one temp
    file every gate on the host appends to (kb/Work PB3108)."""
    order = {FIRST_CLASSES: str(first)} if first is not None else {}
    retry = {ONLY_CASES: str(only)} if only is not None else {}
    return {**scrubbed_env(), "DOTNET_CLI_UI_LANGUAGE": "en", **order, **retry, **(handed or {}),
            "WISEOWL_COBOL_HARNESS_LOG": str(observations)}


@dataclass
class HostRun:
    """One assembly's host: its exit code, whether its report was red, its seconds and when it finished."""
    asm: str
    rc: int
    red: bool
    wall_s: float
    finished_s: float  # since the hosts started
