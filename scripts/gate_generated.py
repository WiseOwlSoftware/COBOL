#!/usr/bin/env python3
"""gate_generated.py - THE GENERATED TARGETS A GATE BRINGS UP TO DATE (kb/Work PB5411, PB5231; docs/rearchitecture/DESIGN-orchestrator-loop.md section
16.5, step 2): brought up to date at the measured tree, handed to their dependents' hosts and checked again after them. `Generations` returns
what it found, as a value (`GenerationFailed`, `Ending`), and never calls back into `Gate`."""
from __future__ import annotations

import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from gate_verdict import Ending, Outcome  # noqa: E402
from gate_host import Host  # noqa: E402


@dataclass
class GenerationFailed:
    """A generated target that could not be brought up to date, or a malformed graph (`Generations.generate`): the exception's type
    and text, which `Gate._gate` ends RED with."""
    kind: str
    text: str


class Generations:
    """The generated targets one gate brought up to date, by name (kb/Work PB5231)."""

    def __init__(self, host: Host, say: Callable[[str], None]):
        self.host, self.say = host, say
        #: The generated targets this gate brought up to date (`generate`), by name: what it hands the hosts of their
        #: dependents (`handed`) and checks again after them (`check_generated`).
        self._generations: dict[str, buildgraph.Generation] = {}

    def outputs(self) -> dict[str, str]:
        """Each generated target's output digest, by name: what a pass recorded in the store was keyed on (`PassCarry.record_passes`)."""
        return {n: g.outputs for n, g in self._generations.items()}

    def generate(self, out: Outcome, facets: dict[str, str]) -> GenerationFailed | None:
        """THE GENERATED TARGETS, THEN THE KEYS (kb/Work PB5231). Every generated target is brought up to date at the
        measured tree (`Host.generate`: up to date when the store records its output digest at its key AND its outputs on
        disk have it, else it runs, traced), and each test target's key takes its dependencies' output digests
        (`Host.target_keys`): so a records-only change whose generated suite is byte-identical leaves Conformance's key,
        and Conformance, up to date. Sets `out.keys` and `out.generated`; returns, as a value (`GenerationFailed`, which the
        gate ends RED with), how it failed when a generated target could not be brought up to date (it failed, read outside
        its inputs, wrote an undeclared output, was not deterministic, or ran while the tree changed) or the graph is
        malformed: no test host can run on a suite that is not the key's. No measured tree: no generation and no key (nothing is up to date or recorded)."""
        if not out.tree:
            out.keys = {}
            return None
        tree = out.tree
        try:
            self._generations = self.host.generate(tree, facets, lambda: self.host.measured_tree() == tree)
            out.generated = {n: g.record() for n, g in self._generations.items()}
            for g in self._generations.values():
                self.say(f"build-local: {g.describe()} — {g.why} (kb/Work PB5231)")
            out.keys = self.host.target_keys(tree, facets, {n: g.outputs for n, g in self._generations.items()}) or {}
        except (buildgraph.BuildError, buildgraph.GraphError) as e:
            return GenerationFailed(type(e).__name__, str(e))
        return None

    def handed(self, asm: str) -> dict[str, str]:
        """What `asm`'s host (and its listing) is handed (kb/Work PB5231; `host_env`): for each generated target its target
        depends on, that target's variable naming the output directory this gate generated and keyed it on."""
        graph = self.host.graph()
        target = self.host.targets().get(asm)
        deps = target.depends if target is not None else ()
        return {graph.targets[d].env: str(self._generations[d].directory) for d in deps if d in self._generations}

    def check_generated(self, out: Outcome, run: Path, ran: list[str]) -> set[str]:
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

    def all_up_to_date(self, out: Outcome, run: Path, started: float) -> Ending:
        """EVERY TARGET IS UP TO DATE (kb/Work PB5207, PB5218): the store holds a pass at each target's key, so no corpus
        fetch, slot, build or test host runs, and the change touches no script, so no post-build self-test is owed. The
        audits, which read THIS tree, ran now and were joined GREEN. The line names, per target, the run and worktree
        whose pass stands, and what ran now. Returns the gate's verdict as a value (`Ending`); `Gate._finish` writes it."""
        out.tree_end = self.host.measured_tree()
        out.timings["wall_s"] = round(time.monotonic() - started, 1)
        stands = "; ".join(f"{asm} (run {u['run']} of {Path(str(u['worktree'])).name}, "
                           f"{int((u.get('counts') or {}).get('ran', 0)):,} cases)" for asm, u in out.up_to_date.items())
        return Ending("GREEN", f"EVERY TARGET UP TO DATE (kb/Work PB5207, PB5218): the store holds a pass "
                            f"at each target's key: {stands}{self.detail()}; what ran now: the audits, GREEN; no "
                            f"corpus fetch, build or test host ran · toolchain {out.toolchain} (run {run.name})", 0)

    def detail(self) -> str:
        """The verdict line's entry for the generated targets (kb/Work PB5231): `· conformance-suite generated in 13.1 s
        (output 1a2b3c4d5e6f, 5 file(s))`, or `… up to date …`."""
        return "".join(f" · {g.describe()}" for g in self._generations.values())
