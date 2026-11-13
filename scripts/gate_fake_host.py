"""gate_fake_host.py — A PLANTED MACHINE FOR THE GATE (kb/Work PB5432; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.2):
a `gate.Host` whose populations are small and whose test hosts write the trx and the log the real ones write, each case
planting one defect into them. An ordinary module, no case and no expectation: the gate's case files
(`scripts/selftests/gate/`) and `scripts/arch/perf_baseline.py`'s self-test both plant `gate.Gate` over it."""

from collections import Counter
from gate_slot import QueueAbandoned, Slot
from pathlib import Path
from test_population import GATED_ASSEMBLIES, PopulationError, synthetic_listing, synthetic_trx
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
import lane_classification  # noqa: E402  — THE classification of a change no test reads (kb/Work PB2878)
import threading
import time

from gate import FIRST_CLASSES, Host, ONLY_CASES


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
        classification itself is scripts/selftests/inert_lane/'s."""
        if "inert" in self.plant:
            return lane_classification.Lane("inert", "planted: all 2 changed path(s) are inert", tooling=False, reads_tooling=False)
        if "tooling-lane" in self.plant:
            return lane_classification.Lane("tooling", "planted: 1 of 1 changed path(s) are tooling no compiler test reads", tooling=True,
                                   reads_tooling=True)
        return lane_classification.Lane("full", "planted: the full lane", tooling="tooling" in self.plant,
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
