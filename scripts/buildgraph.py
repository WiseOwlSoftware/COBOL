#!/usr/bin/env python3
"""buildgraph.py — THE BUILD'S GRAPH: ITS TARGETS, THEIR KEYS, THEIR DEPENDENCIES AND THE ONE STORE OF WHAT EACH PROVED
(kb/Work PB5207 stages 1 and 2, PB5218, PB5231).

    python scripts/buildgraph.py keys --toolchain S [--tree T] [--platform P] [--configuration C]
                                                # each target's key; a generated dependency's output from the store
    python scripts/buildgraph.py digest --target NAME DIR   # the output digest of a generated target's output directory
    python scripts/buildgraph.py check          # the BUILD TARGETS audit: every repository read a test source names
                                                # lies in its assembly's declared inputs (or is its dependency's run)
    python scripts/buildgraph.py publish [--remote R]   # send this host's passes to R's refs/cobol-build-store/passes
                                                # (the landing's push; CI's plan, scripts/ci_plan.py, reads them)
    python scripts/buildgraph.py --self-test

THE MODEL (owner 2026-11-10: "This is precisely what the Unix make and Windows MSBuild utilities are for. They rerun only
what is required due to changes."). A TARGET is a named step with DECLARED INPUTS (path patterns,
scripts/build_targets.json), the FACETS its result depends on (of platform, configuration and toolchain) and the targets
it DEPENDS on. There are two KINDS:
  * a TEST target (`TestTarget`) is a test assembly; what it records is a PASS (a gated population, EXACT, green);
  * a GENERATED target (`GeneratedTarget`) is a Python command that writes declared OUTPUTS into a directory of the
    worktree's own (`GENERATED_ROOT/<name>`); what it records is the DIGEST of those outputs (`output_digest`).
Its KEY is a digest of its name, its facets, the OUTPUT DIGEST of each target it depends on (never that target's inputs)
and the `git ls-tree -r` entries (mode, object id, path: no file is read) of every tracked path its inputs match and its
exclusions do not. A dependency therefore contributes what it MADE, not what it read: an edit to the dependency's inputs
that leaves its output byte-identical leaves every dependent's key unchanged (Bazel's cut-off on an unchanged output).
The graph is checked when it is loaded (`load_targets`): every dependency names a generated target of the file, there is
no cycle, and `Graph.order` is a topological order.

A target is UP TO DATE exactly when the STORE holds its record at its key (`Store.up_to_date`), and for a generated
target ALSO when its outputs on disk have the recorded digest (`generate`: otherwise it is generated again, and a
generation at a key whose recorded digest it does not reproduce is NOT DETERMINISTIC and refused). The store is one
directory in the git common directory, `cobol-build-store/<target>/<key>.json`, so every worktree of the repository
shares it: a record made in worktree A is up to date in worktree B at the same content. It holds only what was proven: a
red, a partial or an unmeasured run records nothing; a generation that failed, read outside its inputs, wrote an output it
does not declare or ran while the working tree changed records nothing.

AN INPUT OR AN OUTPUT THAT CANNOT BE RESOLVED IS A FAILURE NAMING IT, never a wider key (owner 2026-11-10: "If it cannot
be resolved, there is a problem/bug"): a dependency whose output digest is unknown (`Unresolved`), a declared output the
command did not write or a file it wrote that is not declared, a facet the caller did not give.

A GENERATED TARGET'S RUN IS TRACED (`TRACER`): its command runs under a Python audit hook that records every file it
opens for reading, every directory it lists and every process it starts. Its bytecode cache is a fresh empty directory
of its own (`sys.pycache_prefix`), so every Python source it executes is OPENED, whatever loader runs it (an import, or
`spec_from_file_location` + `exec_module`, which never enters `sys.modules`): a warm `__pycache__` in the worktree is never
read (kb/Work PB5231 review R1-1, R2-1). At its end it lists the modules it loaded that are not Python sources (a
compiled extension, whose loading raises no open event). A tracked path it opened that its inputs do not match, compiled
code opened or loaded from inside the repository (no tracked source the key could cover), a tracked directory it listed
that no input lies in, or any process it started (whose reads no hook sees) refuses the generation by name
(`undeclared`): its declared inputs are verified at every run, not trusted. THE TRACE'S
RESIDUE, named: a call that asks whether a path exists or stats it (`Path.exists`, `is_file`, `is_dir`, `os.stat`, …)
raises no audit event, so no run sees it. The STATIC half covers it, before any run (`static_check`, part of `check`):
the program's import closure (every import, a lazy one included) lies in its inputs, and every existence or stat call in
it and its closure probes a path under its inputs, decided from literals and the module's static names; a call that
cannot be decided is a finding until the target's `judged` map states why its path is an input (review R1-3).

THE INPUT PATTERNS are matched by the one matcher of the classification, `inert_lane.under`: `dir/` is every path below
it; a pattern with `*` or `?` is a listing pattern (within one directory and case-insensitive, as a .NET directory
listing matches, so a key changes for every file a test's listing reads); anything else is that path or, when it names a
directory, every path below it. `@build` is the shared build inputs of the targets file;
`@population-inputs` is every path a compiler test or the build may read (`inert_lane.Classification.is_population_input`).
An EXCLUSION is a class, `@inert` (no test reads it, proven by the inert lane's watch) or `@tooling-lane` (no compiler
test or build reads it, proven by the same watch and the INERT LANE audit), or a path pattern: a path a dependency reads
for the target and the target does not read itself (the conformance suite's generator, for `conformance`).

THE DECLARATIONS ARE CHECKED, NOT TRUSTED (`check`, `scan`). The audit reads every test source of an assembly and
the `tests/_shared` files it uses (by the types it names, transitively) and finds each repository read it spells: a
`TestRepo` helper with its leading literal segments, `Path.Combine(TestRepo.Root, "…")`, and a string literal rooted at a
top-level tracked directory. A read outside the assembly's declared inputs is a finding naming its file and line, unless
it is the PROGRAM of a target the assembly depends on, read at that dependency's ONE HAND-OFF SITE: the source that names
the variable the gate hands the output in (`GeneratedTarget.env`) and the mark its host records when it was handed none
and generated its own (`GeneratedTarget.own_mark`, which the gate turns RED). There the read is reached through the
generator and belongs to its target; anywhere else running or reading the program reads every input of the generator,
none in the assembly's key, and is a finding (review R1-2). A dependent assembly with no such site is a finding. In a target declared narrowly (no `@population-inputs`), a read the audit
cannot resolve, a bare repository root that no STATEMENT judgment covers, is a finding too, and a judgment nothing matches is
stale (kb/Work PB5230 review R1-1; `reads_in`, `_root_use`, `findings_of`). A read is judged at its RESOLVED path (its
`.` and `..` segments resolved, `_resolved`); one that climbs out of the repository is a finding by name. A read that
names no tracked path is DROPPED only when it is provably no input: a build output (a `bin` or `obj` segment) or a path
git ignores (`git check-ignore --no-index`: a cache, a scratch or a generated location); anything else names no file the
key could cover, a typo, a stale path or a file not yet made, and in a narrow target is a finding (review R2-1).

THE ONE-HOME RULE, EXTENDED (review R2-2). A test source reaches the repository only through TestRepo, the one locator
(kb/Work PB3814's `TestRepoDriftTests`, whose `Anchors` are the base-directory anchors a hand-rolled locator starts from),
and scratch space only through ScratchDirectory (`ScratchDirectoryDriftTests`). In a narrowly declared target the audit
refuses every statement that reaches the file system another way (`bypasses_in`): one of those ANCHORS (read from the
rule's own list, `root_anchors`, so an anchor added there is refused here too), `Environment.CurrentDirectory`, an
assembly's location, an environment variable read on a line that forms a path, a string literal handed to a System.IO
call as its path, or a rooted literal handed to `Path.Combine`/`Join`; unless the statement carries a hand judgment with
its reason.

⛔ THIS AUDIT IS THE EARLY WARNING, NOT THE NET. It reads source text, so it sees a path a source SPELLS and nothing
else. The SOUND net is the dynamic proof (kb/Work PB5235: a CI shard fails when a test opens a tracked path outside its
target's declared inputs); only it sees a path computed in a CHILD PROCESS (a script or a compiled program a test runs,
whose reads no source of the test spells), in NATIVE code, through REFLECTION, or any value formed at run time (a path
read from a file, built in a loop, passed through a helper the audit does not follow), and `repo-drift`'s bare roots,
which this audit counts but does not refuse.
"""
from __future__ import annotations

import argparse
import ast
import datetime as dt
import fnmatch
import hashlib
import json
import os
import posixpath
import re
import shutil
import subprocess
import sys
import tempfile
import time
from collections.abc import Callable, Iterable, Mapping
from dataclasses import dataclass, replace
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import ClassVar

sys.path.insert(0, str(Path(__file__).resolve().parent))
import csharp_lexer  # noqa: E402  — THE one lexer of a C# source, shared with inert_lane (kb/Work PB5230 review R2-1)
import inert_lane  # noqa: E402  — THE classification: what is inert, what is tooling no compiler test reads
from standard_streams import ensure_utf8  # noqa: E402
from test_population import is_scrubbed  # noqa: E402  — THE scrub rule a test host's environment is made by

REPO = Path(__file__).resolve().parents[1]
TARGETS_FILE = REPO / "scripts" / "build_targets.json"
#: The store's directory under the git common directory: one per repository, shared by every worktree.
STORE_DIR = "cobol-build-store"
#: The version of the key's digest: a change to what the key covers bumps it, so no record made under the old meaning is
#: up to date under the new one. A target with no dependency and every facet hashes exactly as it did in stage 1, so stage
#: 2 (kb/Work PB5231) left this at 1: the passes of `unit` and `characterization` recorded before it still stand.
KEY_VERSION = b"build-target-key 1"
#: The version of an output digest (`output_digest`).
OUTPUT_VERSION = b"generated-output 1"
#: Where a generated target writes, per worktree (`TestResults/` is ignored): `<worktree>/GENERATED_ROOT/<target>/`.
GENERATED_ROOT = Path("TestResults") / "generated"
#: The placeholder of a generated target's command that stands for its output directory.
OUT = "{out}"
#: The classes an exclusion may name.
CLASSES = ("@inert", "@tooling-lane")
#: The observation a dependent's host records when it generated its own output (`GeneratedTarget.own_mark`).
OWN_GENERATION = "GENERATED-OWN"
#: Compiled code a traced run may open: never inside the worktree, where nothing the key covers is its source (`undeclared`).
COMPILED = (".pyc", ".pyo", ".pyd", ".so")
#: The name of a test host's harness observation log in its results directory (`observation_log`).
OBSERVATIONS = ".observations.log"


def observation_log(directory: Path, assembly: str) -> Path:
    """⛔ ONE HOST, ONE OBSERVATION LOG (kb/Work PB5235, bounce of #1530): where the `assembly` host's harness
    observation log (tests/_shared/HarnessLog.cs, WISEOWL_COBOL_HARNESS_LOG) goes in `directory`, the gate's run
    directory (gate.py) and a CI shard's results directory (build-and-test.yml) alike. `GeneratedTarget.recorded_own`
    reads ONE host's log, so a host that may generate its own output without depending on it (the Unit host reads the
    suite's manifest, and its key covers the suite's inputs) never writes into the log a dependent's check reads."""
    return directory / f"{assembly}{OBSERVATIONS}"


class GraphError(ValueError):
    """The targets file is malformed: an unknown kind, facet or dependency, a dependency cycle, a missing field."""


class BuildError(RuntimeError):
    """A target could not be brought up to date; the message names the target and what stopped it."""


class Unresolved(BuildError):
    """An input or output that cannot be resolved: a dependency's output digest that is not known, a declared output
    that was not written, a file written that is not declared, a facet not given. Never a wider key."""


class GenerationFailed(BuildError):
    """A generated target's command exited non-zero."""


class UndeclaredRead(BuildError):
    """A generated target's traced run read outside its declared inputs, or started a process."""


class Nondeterministic(BuildError):
    """A generation at a key reproduced not the output digest the store records for that key."""


class TreeMoved(BuildError):
    """The working tree changed while a target generated, so its output is not the output of the key."""


# ── the targets ─────────────────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True, kw_only=True)
class Target:
    """What every target of the targets file has: its name, its input patterns (`@build` expanded), its exclusions
    (classes and path patterns), the facets its key covers and the targets it depends on."""
    name: str
    inputs: tuple[str, ...]
    exclude: tuple[str, ...]
    facets: tuple[str, ...]
    depends: tuple[str, ...] = ()
    #: Reads the audit cannot resolve by itself, each judged by hand with its reason: `<source file>|<read>` -> why.
    judged: tuple[tuple[str, str], ...] = ()
    KIND: ClassVar[str] = ""

    @property
    def broad(self) -> bool:
        """Whether its inputs are every population input (`@population-inputs`): such a target covers a read it cannot
        resolve; a narrowly declared one cannot (`check`)."""
        return "@population-inputs" in self.inputs

    def matches(self, path: str, cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> bool:
        """Whether the tracked `path` is an input of this target."""
        if any(_excluded(path, e, cls) for e in self.exclude):
            return False
        return any(_matches(path, p, cls) for p in self.inputs)

    def covers(self, read: str, cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> bool:
        """Whether a READ a test source spells (a path, or a directory ending in `/`) lies inside the declared inputs. A
        read of an excluded PATH is not covered (a directory holding one still is: the exclusion is of that path). A
        directory read is covered when the directory itself lies under an input pattern, or when it is a LISTING by a
        pattern (`dir/guard*.sh`) an input pattern equals. For `@population-inputs` a read is covered unless it is a
        tooling-lane path: a test's read of an INERT path is the inert lane's to judge (its watch and its audits:
        `inert_lane.py check`, DOC AUDITS), and the key of such a target already leaves inert paths out."""
        if any(not e.startswith("@") and inert_lane.under(read, e) for e in self.exclude):
            return False
        for p in self.inputs:
            if p == "@population-inputs":
                if not cls.in_tooling_lane(read):
                    return True
            elif p == read or _matches(read, p, cls):
                return True
        return False

    def lists(self, directory: str) -> bool:
        """Whether a LISTING of the tracked `directory` (ending in `/`; "" is the root) lies in the declared inputs: the
        directory lies under an input, or holds one (the import system lists every directory on the module path to find
        a declared module, and a walk lists the directories above what it reads)."""
        return any(_matches(directory, p, inert_lane.CLASSIFICATION) or p.startswith(directory) for p in self.inputs)


@dataclass(frozen=True, kw_only=True)
class TestTarget(Target):
    """A test assembly: what it records is a PASS of its whole population."""
    assembly: str
    project: str
    KIND: ClassVar[str] = "test"


@dataclass(frozen=True, kw_only=True)
class GeneratedTarget(Target):
    """A Python command that writes its declared OUTPUTS into its output directory; what it records is their digest."""
    #: The command: a repository-relative Python script and its arguments; `{out}` is the output directory.
    command: tuple[str, ...]
    #: The output patterns, relative to the output directory: `*` and `?` within one path segment.
    outputs: tuple[str, ...]
    #: The environment variable the gate hands the output directory in to a host of a dependent target.
    env: str
    KIND: ClassVar[str] = "generated"

    @property
    def program(self) -> str:
        """The script the command runs (repository-relative)."""
        return self.command[0]

    @property
    def own_mark(self) -> str:
        """What a dependent's host records in the harness observation log (tests/_shared/HarnessLog.cs) when it was
        handed no output and generated its own (kb/Work PB5231 R1-2): a gate whose host log holds it is RED
        (gate.py `_check_generated`; CI's `ci_plan.py handed`), and the hand-off site must spell it (`scan`)."""
        return f"{OWN_GENERATION}  {self.name}"

    def recorded_own(self, log: Path | None) -> bool:
        """Whether a host's observation log records `own_mark`: the host generated its own output instead of the one it
        was handed (a missing log records nothing). THE ONE TEST of it, the gate's and CI's."""
        try:
            return log is not None and self.own_mark in log.read_text(encoding="utf-8-sig", errors="replace")
        except OSError:
            return False

    def argv(self, out: Path) -> list[str]:
        """The command's arguments after the program, `{out}` replaced by `out`."""
        return [str(out) if a == OUT else a for a in self.command[1:]]


def _matches(path: str, pattern: str, cls: inert_lane.Classification) -> bool:
    if pattern == "@population-inputs":
        return cls.is_population_input(path)
    return inert_lane.under(path, pattern)


def _excluded(path: str, entry: str, cls: inert_lane.Classification) -> bool:
    if entry == "@inert":
        return cls.is_inert(path)
    if entry == "@tooling-lane":
        return cls.in_tooling_lane(path)
    if entry.startswith("@"):
        raise GraphError(f"no such excluded class {entry!r} (one of {', '.join(CLASSES)})")
    return inert_lane.under(path, entry)


@dataclass(frozen=True)
class Graph:
    """The targets file: every target by name (in the file's order), the facets a key may cover, and a TOPOLOGICAL ORDER
    (every target after the targets it depends on). Made by `Graph.of`, which refuses a malformed one."""
    targets: dict[str, Target]
    facets: tuple[str, ...]
    order: tuple[str, ...]

    @classmethod
    def of(cls, targets: Mapping[str, Target], facets: tuple[str, ...]) -> "Graph":
        """The graph of `targets`, checked: each facet is one of `facets`, each dependency names a GENERATED target of
        the graph (a test target makes no output to depend on), a generated target's program is among its own inputs (or
        its own source would not be in its key), its environment variable is one a test host's scrub removes, and there
        is no cycle. Raises GraphError naming what is wrong."""
        if not targets:
            raise GraphError("the targets file declares no target")
        for t in targets.values():
            if bad := [f for f in t.facets if f not in facets]:
                raise GraphError(f"{t.name}: no such facet {', '.join(bad)} (one of {', '.join(facets)})")
            for e in t.exclude:
                if e.startswith("@") and e not in CLASSES:
                    raise GraphError(f"{t.name}: no such excluded class {e!r} (one of {', '.join(CLASSES)})")
            for d in t.depends:
                if d not in targets:
                    raise GraphError(f"{t.name} depends on {d!r}, which the targets file does not declare")
                if not isinstance(targets[d], GeneratedTarget):
                    raise GraphError(f"{t.name} depends on {d!r}, a {targets[d].KIND} target: only a generated "
                                     "target makes an output to depend on")
            if isinstance(t, GeneratedTarget):
                if OUT not in t.command:
                    raise GraphError(f"{t.name}: its command names no {OUT} (where it writes)")
                if any("self-test" in a or _self_test_scope(a) for a in t.command):
                    raise GraphError(f"{t.name}: its command runs a self-test, which the static check reads no code of")
                if not t.program.endswith(".py"):
                    raise GraphError(f"{t.name}: its program {t.program} is not a Python script, so its run cannot be "
                                     "traced")
                if not t.matches(t.program):
                    raise GraphError(f"{t.name}: its program {t.program} is not among its own inputs")
                if not t.outputs:
                    raise GraphError(f"{t.name}: it declares no output")
                if not is_scrubbed(t.env):
                    raise GraphError(f"{t.name}: its variable {t.env} is not one a test host's scrub removes "
                                     "(test_population.is_scrubbed), so a stray one could reach a host the gate did "
                                     "not hand it to")
        return cls(dict(targets), tuple(facets), _topological(targets))

    def tests(self) -> dict[str, TestTarget]:
        return {n: t for n, t in self.targets.items() if isinstance(t, TestTarget)}

    def generated(self) -> dict[str, GeneratedTarget]:
        return {n: t for n, t in self.targets.items() if isinstance(t, GeneratedTarget)}

    def by_assembly(self) -> dict[str, TestTarget]:
        """The test targets by the assembly each runs."""
        return {t.assembly: t for t in self.tests().values()}

    def handed(self, name: str, root: Path) -> dict[str, str]:
        """What the host of target `name` is handed when each generated target's output lies in `root/<generated
        target>` (as `GENERATED_ROOT` lays a worktree's out): for each target it depends on, that target's variable and
        directory. CI's listing and hosts (`ci_plan.py list`, ci_plan's wiring) read it; the gate
        hands the directories it generated (`Gate._handed`)."""
        return {self.targets[d].env: str(root / d) for d in self.targets[name].depends
                if isinstance(self.targets[d], GeneratedTarget)}

    def needs_generated(self, names: Iterable[str]) -> set[str]:
        """Every generated target the targets `names` depend on, directly or through another generated target."""
        seen: set[str] = set()
        todo = [d for n in names for d in self.targets[n].depends]
        while todo:
            d = todo.pop()
            if d not in seen:
                seen.add(d)
                todo += self.targets[d].depends
        return seen


def _topological(targets: Mapping[str, Target]) -> tuple[str, ...]:
    """Every target after its dependencies, ties in the file's order; raises GraphError naming a cycle."""
    order: list[str] = []
    state: dict[str, str] = {}

    def visit(name: str, path: list[str]) -> None:
        if state.get(name) == "done":
            return
        if state.get(name) == "open":
            cycle = path[path.index(name):] + [name]
            raise GraphError(f"a dependency cycle: {' -> '.join(cycle)}")
        state[name] = "open"
        for d in targets[name].depends:
            visit(d, path + [name])
        state[name] = "done"
        order.append(name)

    for n in targets:
        visit(n, [])
    return tuple(order)


def load_targets(path: Path = TARGETS_FILE) -> Graph:
    """The targets file as a checked Graph (`Graph.of`), `@build` expanded; raises GraphError on a malformed one."""
    data = json.loads(path.read_text(encoding="utf-8"))
    build = tuple(data["build"]["inputs"])
    facets = tuple(data["facets"])
    #: Judgments of the shared test sources (tests/_shared, part of `@build`): every TEST target using the file shares them.
    shared_judged = data["build"].get("judged") or {}
    targets: dict[str, Target] = {}
    for name, t in data["targets"].items():
        inputs: list[str] = []
        for p in t["inputs"]:
            inputs += list(build) if p == "@build" else [p]
        common = dict(name=name, inputs=tuple(inputs), exclude=tuple(t.get("exclude") or ()),
                      facets=tuple(t.get("facets") or facets), depends=tuple(t.get("depends") or ()),
                      judged=tuple((k, v) for k, v in {**(shared_judged if t.get("kind") == TestTarget.KIND else {}),
                                                         **(t.get("judged") or {})}.items()))
        kind = t.get("kind")
        try:
            if kind == TestTarget.KIND:
                targets[name] = TestTarget(**common, assembly=t["assembly"], project=t["project"])
            elif kind == GeneratedTarget.KIND:
                targets[name] = GeneratedTarget(**common, command=tuple(t["command"]), outputs=tuple(t["outputs"]),
                                                env=t["env"])
            else:
                raise GraphError(f"{name}: no such kind {kind!r} (one of {TestTarget.KIND}, {GeneratedTarget.KIND})")
        except KeyError as e:
            raise GraphError(f"{name}: a {kind} target declares {e.args[0]!r}") from None
    return Graph.of(targets, facets)


# ── keys ────────────────────────────────────────────────────────────────────────────────────────────────────────────


def tree_entries(repo: Path, tree: str) -> list[tuple[bytes, str]]:
    """Every `git ls-tree -r -z` entry of `tree` (a tree or commit id): (the raw entry, its path)."""
    out = subprocess.run(["git", "ls-tree", "-r", "-z", "--full-tree", tree], cwd=repo, check=True,
                         capture_output=True).stdout
    entries = []
    for entry in out.split(b"\0"):
        _, tab, path = entry.partition(b"\t")
        if tab:
            entries.append((entry, path.decode("utf-8", "surrogateescape")))
    return entries


def key(target: Target, entries: list[tuple[bytes, str]], facets: Mapping[str, str],
        outputs: Mapping[str, str] | None = None, cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> str:
    """THE TARGET'S KEY: sha256 of KEY_VERSION, its name, its own facets, the OUTPUT DIGEST of each target it depends on
    (`outputs`, by name) and every entry its inputs match. Raises Unresolved naming a facet or a dependency's output that
    is not given."""
    if missing := [f for f in target.facets if f not in facets]:
        raise Unresolved(f"{target.name}'s key needs the facet(s) {', '.join(missing)}, which were not given")
    own = {f: facets[f] for f in target.facets}
    h = hashlib.sha256(KEY_VERSION + b"\0" + target.name.encode() + b"\0"
                       + json.dumps(own, sort_keys=True).encode("utf-8") + b"\0")
    for dep in sorted(target.depends):
        digest = (outputs or {}).get(dep)
        if not digest:
            raise Unresolved(f"{target.name}'s key needs the output digest of {dep}, which is not known: {dep} has no "
                             "record at its key (it was never generated at these inputs, or its record was removed)")
        h.update(b"depends\0" + dep.encode() + b"\0" + digest.encode() + b"\0")
    for raw, path in entries:
        if target.matches(path, cls):
            h.update(raw + b"\0")
    return h.hexdigest()


def keys(graph: Graph, entries: list[tuple[bytes, str]], facets: Mapping[str, str],
         outputs: Mapping[str, str] | Callable[[GeneratedTarget, str], str],
         refused: dict[str, str] | None = None) -> dict[str, str]:
    """Every target's key, in topological order. `outputs` gives each generated target's output digest: a mapping (the
    digests a gate's generations made, or a run recorded), or a function of (the target, its key) that resolves it
    (`stored_outputs`). A key that cannot be resolved raises Unresolved; with `refused` given it is left out and its
    reason kept there by target name instead (a re-derivation keys what it can and refuses the rest by name)."""
    known: dict[str, str] = {}
    out: dict[str, str] = {}
    for name in graph.order:
        t = graph.targets[name]
        try:
            out[name] = key(t, entries, facets, known)
        except Unresolved as e:
            if refused is None:
                raise
            refused[name] = str(e)
            continue
        if isinstance(t, GeneratedTarget):
            digest = outputs(t, out[name]) if callable(outputs) else outputs.get(name)
            if digest:
                known[name] = digest
    return out


def stored_outputs(store: "Store") -> Callable[[GeneratedTarget, str], str | None]:
    """A generated target's output digest as the STORE records it at its key (None when it holds no record: a dependent's
    key then raises Unresolved naming it). No command runs: what a re-derivation of an old run and the `keys` command read."""
    return lambda t, k: (store.lookup(t.name, k) or {}).get("outputs")


# ── the store ───────────────────────────────────────────────────────────────────────────────────────────────────────


class Store:
    """THE ONE STORE OF WHAT EACH TARGET PROVED: `<dir>/<target>/<key>.json`, one small record per target and key (a
    test target's pass, a generated target's output digest). A record is written whole (a temporary file renamed over
    it), so a reader never sees half of one."""

    def __init__(self, directory: Path):
        self.dir = directory

    @classmethod
    def for_repo(cls, repo: Path) -> "Store":
        common = subprocess.run(["git", "rev-parse", "--git-common-dir"], cwd=repo, check=True, capture_output=True,
                                text=True, encoding="utf-8").stdout.strip()
        common_dir = Path(common) if Path(common).is_absolute() else repo / common
        return cls(common_dir.resolve() / STORE_DIR)

    def _path(self, target: str, k: str) -> Path:
        """A key (`key`'s sha256 hex) names its file; any other string is hashed first, so no key can name a path."""
        name = k if re.fullmatch(r"[0-9a-f]{64}", k) else hashlib.sha256(k.encode("utf-8")).hexdigest()
        if not re.fullmatch(r"[\w.-]+", target):
            raise ValueError(f"not a target name: {target!r}")
        return self.dir / target / f"{name}.json"

    def lookup(self, target: str, k: str) -> dict | None:
        """The record of `target` at key `k`, or None."""
        try:
            rec = json.loads(self._path(target, k).read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return None
        return rec if is_pass(rec, target, k) else None

    def records(self) -> dict[tuple[str, str], dict]:
        """Every pass this store holds, by (target, key): what `publish` sends."""
        out: dict[tuple[str, str], dict] = {}
        if not self.dir.is_dir():
            return out
        for path in sorted(self.dir.glob("*/*.json")):
            target, k = path.parent.name, path.stem
            rec = self.lookup(target, k)
            if rec is not None:
                out[(target, k)] = rec
        return out


    def up_to_date(self, target: str, k: str) -> bool:
        """THE ONE RULE: the store holds a record at this key (for a generated target `generate` also asks its disk)."""
        return self.lookup(target, k) is not None

    def record(self, target: str, k: str, rec: dict) -> Path:
        """Record what `target` proved at key `k` (the caller has proven it: a green host and an EXACT population, or a
        traced generation whose outputs are all declared)."""
        path = self._path(target, k)
        path.parent.mkdir(parents=True, exist_ok=True)
        body = {**rec, "target": target, "key": k, "result": "pass"}
        fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=".rec-", suffix=".json")
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(json.dumps(body, indent=1, sort_keys=True) + "\n")
        os.replace(tmp, path)
        return path


def is_pass(rec: object, target: str, k: str) -> bool:
    """THE ONE ACCEPTANCE RULE of a record, wherever it was read (this host's store or the store ref): a pass of exactly
    this target at exactly this key."""
    return isinstance(rec, dict) and rec.get("result") == "pass" and rec.get("key") == k and rec.get("target") == target


# ── the transport: the store's records travel to CI on one ref (kb/Work PB5235; DESIGN-test-build-ci.md §3.14.14) ────
#
# THE REF holds one commit, with no parent, whose tree is `<target>/<key>.json`: the store's own layout, so a record is
# found by the key CI computes and by nothing else. A record there is the PUBLISHED projection of a host's record (the
# fields below): the evidence (the run directory, the worktree) stays on the host that made it. `publish` (the landing,
# when it pushes its branch) sends the union of the ref's records and this host's, less those older than PRUNE_DAYS, as a
# new parentless commit pushed with a lease on the commit it read, so two publishers never lose each other's records
# (the loser re-reads and re-sends) and the ref's history is one commit deep. `fetch_published` (CI's plan, scripts/ci_plan.py) reads it.

#: The ref of the repository the records travel on.
STORE_REF = "refs/cobol-build-store/passes"
#: What a published record carries (PB5207: target, key, platform, configuration, toolchain, counts; a generated target's
#: output digest and file count, which a dependent's key is computed from; and where it came from, by commit and run, so
#: a CI log can name the record it accepted).
PUBLISHED_FIELDS = ("target", "key", "result", "assembly", "facets", "counts", "outputs", "files", "head", "tree",
                    "run", "when")
#: A record older than this is pruned when the ref is next published: a branch gated earlier than that re-runs its
#: targets in CI, which costs minutes and never a verdict.
PRUNE_DAYS = 21
#: The identity of the ref's commits (they are data, never a person's work).
_STORE_IDENTITY = {"GIT_AUTHOR_NAME": "cobol-build-store", "GIT_AUTHOR_EMAIL": "cobol-build-store@localhost",
                   "GIT_COMMITTER_NAME": "cobol-build-store", "GIT_COMMITTER_EMAIL": "cobol-build-store@localhost"}
_RECORD_PATH = re.compile(r"([\w.-]+)/([0-9a-f]{64})\.json")


def published(rec: dict) -> dict:
    """A host record as the ref carries it: PUBLISHED_FIELDS only."""
    return {f: rec[f] for f in PUBLISHED_FIELDS if f in rec}


def _when(rec: dict) -> datetime | None:
    try:
        return datetime.strptime(str(rec.get("when")), "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    except ValueError:
        return None


def _git(repo: Path, *args: str, input: bytes | None = None, env: dict[str, str] | None = None,
         check: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=repo, input=input, capture_output=True, check=check,
                          env={**os.environ, **(env or {})})


class StoreRefError(RuntimeError):
    """The store ref names something that is not a commit of records (a blob, a tree, a tag)."""


class Published:
    """The records one commit of the store ref holds, read once (`git ls-tree` and one `git cat-file --batch`). Its
    `lookup` asks `is_pass`, the store's own rule. `refused`: each entry of the commit's tree that is no pass, as
    `<path>: <why>` (a path that is not `<target>/<key>.json`, a body that is not JSON, a record that is not a pass of the
    target and key its path names), so a reader can say what it ignored."""

    def __init__(self, records: dict[tuple[str, str], dict], commit: str | None, refused: tuple[str, ...] = ()):
        self.records, self.commit, self.refused = records, commit, refused

    @classmethod
    def read(cls, repo: Path, commit: str | None) -> "Published":
        if not commit:
            return cls({}, None)
        kind = _git(repo, "cat-file", "-t", commit).stdout.decode().strip()
        if kind != "commit":
            raise StoreRefError(f"{STORE_REF} names a {kind} ({commit[:12]}), not a commit of records")
        listing = _git(repo, "ls-tree", "-r", "-z", "--full-tree", commit).stdout.decode("utf-8", "surrogateescape")
        wanted: list[tuple[str, str, str]] = []
        refused: list[str] = []
        for entry in listing.split("\0"):
            meta, tab, path = entry.partition("\t")
            if not tab:
                continue
            m = _RECORD_PATH.fullmatch(path)
            if m and meta.split()[1] == "blob":
                wanted.append((m.group(1), m.group(2), meta.split()[2]))
            else:
                refused.append(f"{path}: not <target>/<64-hex key>.json")
        out = _git(repo, "cat-file", "--batch", input="".join(f"{oid}\n" for *_, oid in wanted).encode()).stdout
        records: dict[tuple[str, str], dict] = {}
        at = 0
        for target, k, _ in wanted:
            nl = out.index(b"\n", at)
            size = int(out[at:nl].split()[2])
            body = out[nl + 1:nl + 1 + size]
            at = nl + 1 + size + 1
            path = f"{target}/{k}.json"
            try:
                rec = json.loads(body.decode("utf-8"))
            except ValueError:
                refused.append(f"{path}: not JSON")
                continue
            if is_pass(rec, target, k):
                records[(target, k)] = rec
            else:
                refused.append(f"{path}: not a pass of {target} at the key its path names")
        return cls(records, commit, tuple(refused))

    def lookup(self, target: str, k: str) -> dict | None:
        rec = self.records.get((target, k))
        return rec if is_pass(rec, target, k) else None


def remote_store_commit(repo: Path, remote: str) -> str | None:
    """The commit the remote's store ref names, or None when it has none; raises CalledProcessError when the remote
    cannot be read (a caller must not read that as "no records" and publish over them)."""
    out = _git(repo, "ls-remote", remote, STORE_REF).stdout.decode().split()
    return out[0] if out else None


def fetch_published(repo: Path, remote: str) -> Published:
    """The remote's store ref, fetched with its blobs (`--no-filter`: CI's checkout is a blob:none partial clone, which
    would otherwise fetch each record on demand), and read."""
    commit = remote_store_commit(repo, remote)
    if commit is None:
        return Published({}, None)
    _git(repo, "fetch", "--quiet", "--no-tags", "--no-filter", remote, f"+{STORE_REF}:{STORE_REF}")
    return Published.read(repo, commit)


def write_records(repo: Path, records: dict[tuple[str, str], dict]) -> str:
    """A parentless commit whose tree is `<target>/<key>.json` for each record, written through a private index."""
    with tempfile.TemporaryDirectory(prefix="build-store-") as tmp:
        index = {"GIT_INDEX_FILE": str(Path(tmp) / "index")}
        lines = []
        for (target, k), rec in sorted(records.items()):
            body = (json.dumps(rec, indent=1, sort_keys=True) + "\n").encode("utf-8")
            oid = _git(repo, "hash-object", "-w", "--stdin", input=body).stdout.decode().strip()
            lines.append(f"100644 {oid}\t{target}/{k}.json\n")
        _git(repo, "update-index", "--add", "--index-info", input="".join(lines).encode(), env=index)
        tree = _git(repo, "write-tree", env=index).stdout.decode().strip()
    return _git(repo, "commit-tree", tree, "-m", f"cobol-build-store: {len(records)} pass record(s) (kb/Work PB5235)",
                env=_STORE_IDENTITY).stdout.decode().strip()


def publish(repo: Path, store: Store, remote: str = "origin", now: datetime | None = None, attempts: int = 3,
            say=print) -> str | None:
    """PUBLISH THIS HOST'S PASSES: the union of the remote ref's records and `store`'s (projected), less those older
    than PRUNE_DAYS, pushed as one parentless commit with a lease on the commit read, re-read and re-sent when another
    publisher moved the ref first; every record pruned leaves the ref naming an empty tree. Returns the ref's commit;
    None when the remote has no ref and this host no pass. Raises on a remote that cannot be read or a lease lost
    `attempts` times."""
    now = now or datetime.now(timezone.utc)
    cutoff = now - timedelta(days=PRUNE_DAYS)
    local = {tk: published(rec) for tk, rec in store.records().items()}
    for _ in range(attempts):
        theirs = fetch_published(repo, remote)
        merged = {tk: rec for tk, rec in {**theirs.records, **local}.items() if (_when(rec) or cutoff) > cutoff}
        if merged == theirs.records:
            say(f"build store: {remote} {STORE_REF} already holds every pass ({len(merged)} record(s)); nothing pushed")
            return theirs.commit
        if not merged and theirs.commit is None:
            say("build store: no pass to publish")
            return None
        commit = write_records(repo, merged)
        pushed = _git(repo, "push", "--quiet", f"--force-with-lease={STORE_REF}:{theirs.commit or ''}", remote,
                      f"{commit}:{STORE_REF}", check=False)
        if pushed.returncode == 0:
            added = len(set(merged) - set(theirs.records))
            say(f"build store: published {len(merged)} record(s) to {remote} {STORE_REF} ({added} new, "
                f"{len(theirs.records) + added - len(merged)} pruned) at {commit[:12]}")
            return commit
        say(f"build store: {remote} {STORE_REF} moved while publishing ({pushed.stderr.decode().strip()}); re-reading")
    raise RuntimeError(f"could not publish {STORE_REF} to {remote}: the lease was lost {attempts} times")


# ── generated targets ───────────────────────────────────────────────────────────────────────────────────────────────


def _output_matches(rel: str, pattern: str) -> bool:
    """`rel` (posix, relative to the output directory) matches `pattern` segment by segment (`*` never crosses a `/`)."""
    a, b = rel.split("/"), pattern.split("/")
    return len(a) == len(b) and all(fnmatch.fnmatchcase(x, y) for x, y in zip(a, b))


def output_digest(directory: Path, target: GeneratedTarget) -> tuple[str, int]:
    """THE OUTPUT DIGEST of a generated target's output directory, and how many files it holds: sha256 of
    OUTPUT_VERSION and, for every file in the order of its relative POSIX path, that path and the sha256 of its bytes. No
    absolute path, time or platform enters it. Raises Unresolved when the directory is missing, a declared output pattern
    matches no file, or a file matches no declared output."""
    if not directory.is_dir():
        raise Unresolved(f"{target.name}: its output directory {directory} does not exist")
    files = sorted(p.relative_to(directory).as_posix() for p in directory.rglob("*") if p.is_file())
    undeclared = [f for f in files if not any(_output_matches(f, o) for o in target.outputs)]
    missing = [o for o in target.outputs if not any(_output_matches(f, o) for f in files)]
    if undeclared or missing:
        raise Unresolved(f"{target.name}: its outputs in {directory} are not the declared ones (scripts/build_targets.json"
                         + (f"; not written: {', '.join(missing)}" if missing else "")
                         + (f"; written and not declared: {', '.join(undeclared[:10])}" if undeclared else "") + ")")
    h = hashlib.sha256(OUTPUT_VERSION + b"\0")
    for f in files:
        h.update(f.encode("utf-8") + b"\0" + hashlib.sha256((directory / f).read_bytes()).hexdigest().encode() + b"\0")
    return h.hexdigest(), len(files)


#: ⛔ THE TRACER a generated target's command runs under (`traced_run`): stdlib only, imported before the hook, so every
#: repository module the command imports is opened, and seen, after it; its bytecode cache is a fresh empty directory, so
#: every source is opened from source. argv: <log> <script> <args...>. It runs the script as `python <script>` would
#: (sys.path[0] its directory, __name__ "__main__") and writes, at its end, the absolute paths it opened for reading, the
#: directories it listed, the processes it started and the modules it loaded that are not Python sources.
TRACER = r"""
import json, os, runpy, shutil, sys, tempfile, traceback
log, script, argv = sys.argv[1], os.path.abspath(sys.argv[2]), sys.argv[3:]
# THE BYTECODE CACHE IS A FRESH EMPTY DIRECTORY OF THIS RUN'S OWN (kb/Work PB5231 review R2-1): with none to read, every
# source the run executes is OPENED, and seen by the open hook, whatever loader runs it (an import, or
# spec_from_file_location + exec_module, which never enters sys.modules); a warm __pycache__ in the worktree is never read.
sys.pycache_prefix = tempfile.mkdtemp(prefix="buildgraph-pycache-")
before = set(sys.modules)
opened, listed, started = set(), set(), []
WRITE = os.O_WRONLY | os.O_RDWR | os.O_APPEND | os.O_CREAT
def _abs(p):
    try:
        return os.path.abspath(os.fsdecode(p))
    except (TypeError, ValueError):
        return None
def hook(event, args):
    if event == "open":
        path, mode, flags = args
        if isinstance(path, int):
            return
        write = (flags & WRITE) if mode is None else any(c in mode for c in "wax+")
        if not write and (p := _abs(path)):
            opened.add(p)
    elif event in ("os.listdir", "os.scandir"):
        if not isinstance(args[0], int) and (p := _abs(args[0] if args[0] is not None else ".")):
            listed.add(p)
    elif event in ("subprocess.Popen", "os.system", "os.exec", "os.posix_spawn", "os.spawn", "os.startfile", "os.fork"):
        started.append(event + " " + repr(args)[:300])
sys.addaudithook(hook)
sys.argv = [script, *argv]
sys.path[0] = os.path.dirname(script)
code = 0
try:
    runpy.run_path(script, run_name="__main__")
except SystemExit as e:
    code = e.code if isinstance(e.code, int) else (0 if e.code is None else 1)
except BaseException:
    traceback.print_exc()
    code = 1
finally:
    # Every module the run loaded that is NOT a Python source (a compiled extension, whose loading raises no open
    # event; a sourceless .pyc): the one thing the open hook cannot see. Sources are seen when opened (the cache above).
    modules = []
    for name, m in list(sys.modules.items()):
        if name in before:
            continue
        spec = getattr(m, "__spec__", None)
        origin = getattr(spec, "origin", None) or getattr(m, "__file__", None)
        if origin and origin not in ("built-in", "frozen", "namespace") and not origin.endswith(".py"):
            modules.append([name, os.path.abspath(origin)])
    with open(log, "w", encoding="utf-8") as fh:
        json.dump({"opened": sorted(opened), "listed": sorted(listed), "started": started, "modules": modules}, fh)
    try:
        shutil.rmtree(sys.pycache_prefix)
    except OSError as e:
        print(f"buildgraph tracer: the run's bytecode cache {sys.pycache_prefix} could not be removed: {e}",
              file=sys.stderr)
sys.exit(code)
"""


@dataclass(frozen=True)
class Trace:
    """One traced run of a generated target's command: its exit code, its output, and what it read."""
    rc: int
    output: str
    opened: tuple[str, ...]
    listed: tuple[str, ...]
    started: tuple[str, ...]
    #: Every module the run loaded that is not a Python source (a compiled extension, a sourceless .pyc): (its name, the
    #: file it was loaded from). Loading one raises no open event of a source the key could cover.
    modules: tuple[tuple[str, str], ...] = ()


def traced_run(repo: Path, target: GeneratedTarget, out: Path) -> Trace:
    """Run `target`'s command in `repo` under the TRACER, its output to a file (never a pipe, kb/Work PB5098)."""
    with tempfile.TemporaryDirectory(prefix="buildgraph-trace-") as tmp:
        log = Path(tmp) / "trace.json"
        with open(Path(tmp) / "output.txt", "w+", encoding="utf-8", errors="replace", newline="\n") as fh:
            rc = subprocess.run([sys.executable, "-c", TRACER, str(log), str(repo / target.program), *target.argv(out)],
                                cwd=repo, stdout=fh, stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL).returncode
            fh.seek(0)
            output = fh.read()
        try:
            data = json.loads(log.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            data = {"opened": [], "listed": [], "started": ["(the tracer wrote no log: what the run read is unknown)"],
                    "modules": []}
    return Trace(rc, output, tuple(data["opened"]), tuple(data["listed"]), tuple(data["started"]),
                 tuple((n, o) for n, o in data["modules"]))


def undeclared(target: GeneratedTarget, trace: Trace, repo: Path, tracked: set[str],
               cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> list[str]:
    """What a traced run read outside `target`'s declared inputs: a TRACKED path it opened that its inputs do not match
    (an input excluded as @inert or @tooling-lane included: then the classification is wrong), a tracked directory it
    listed that no input lies in or under (`Target.lists`), and every process it started. Untracked and ignored paths
    (its own output, a byte-code cache) and paths outside the repository (the interpreter's library) are not inputs."""
    # ⛔ BOTH SPELLINGS OF THE WORKTREE: the path it was handed and its canonical one. A command that resolves its own
    # location (`Path(__file__).resolve()`, as generate.py does) opens canonical paths, and a worktree under a junction
    # or an 8.3 alias (CI's Windows TEMP, self_tests.py's `aliased_temp`) is spelled otherwise: judged against the handed
    # spelling alone, every read would fall outside the repository and none would be judged.
    roots = sorted({os.path.normcase(os.path.abspath(repo)) + os.sep, os.path.normcase(os.path.realpath(repo)) + os.sep})
    dirs = {""} | {"/".join(f.split("/")[:i]) + "/" for f in tracked for i in range(1, f.count("/") + 1)}

    def rel(p: str) -> str | None:
        n = os.path.normcase(p)
        for root in roots:
            if n + os.sep == root:
                return ""
            if n.startswith(root):
                return p[len(root):].replace(os.sep, "/")
        return None

    found = []
    for p in trace.opened:
        r = rel(p)
        if r is not None and r in tracked and not target.matches(r, cls):
            found.append(f"opened {r}")
        elif r is not None and r not in tracked and r.endswith(COMPILED):
            # The run's bytecode cache is outside the worktree (TRACER), so compiled code opened inside it is code with
            # no tracked source (a sourceless .pyc, a stale cache loaded by hand): nothing the key could cover.
            found.append(f"loaded {r}, compiled code with no tracked source in the repository")
    for p in trace.listed:
        r = rel(p)
        d = "" if not r else r + "/"
        if r is not None and d in dirs and not target.lists(d):
            found.append(f"listed {d or '(the repository root)'}")
    for name, origin in trace.modules:
        # A module that is not a Python source, loaded from inside the repository (a compiled extension, whose loading
        # raises no open event; a sourceless .pyc): its key could cover no source, so it is a failure naming it.
        where = rel(origin)
        if where is not None:
            found.append(f"imported {name} from {where}, which is not a tracked Python source")
    found += [f"started a process, whose reads no hook sees: {s}" for s in trace.started]
    return sorted(set(found))


#: ⛔ THE CALLS THAT ASK WHETHER A PATH EXISTS, OR STAT IT, AND RAISE NO AUDIT EVENT (kb/Work PB5231 R1-3): the trace
#: cannot see them, so the static check (`static_check`) judges each one in a generated target's sources. A method of a
#: path (`p.exists()`), or a function of `os` / `os.path` whose first argument is the path.
PROBE_METHODS = frozenset({"exists", "is_file", "is_dir", "is_symlink", "stat", "lstat"})
PROBE_FUNCTIONS = frozenset({"os.path.exists", "os.path.lexists", "os.path.isfile", "os.path.isdir", "os.path.islink",
                             "os.path.getsize", "os.path.getmtime", "os.stat", "os.lstat"})


def _self_test_scope(name: str) -> bool:
    """A function a generated target's command never runs: its module's self-test (`self_test`, `_selftest_*`), the
    convention scripts/scratch_repo.py's audit reads self-test code by. A target's command never names `--self-test`
    (`Graph.of`)."""
    return "self_test" in name or "selftest" in name


def _probes(rel: str, tree: ast.Module) -> list[tuple[int, str, str, ast.expr]]:
    """Every existence or stat call (`PROBE_METHODS`, `PROBE_FUNCTIONS`) in the Python file `rel` outside self-test code:
    (its line, its enclosing function's dotted name, the call's text, the path expression it probes)."""
    found: list[tuple[int, str, str, ast.expr]] = []

    def walk(node: ast.AST, scope: str) -> None:
        for child in ast.iter_child_nodes(node):
            if isinstance(child, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
                if isinstance(child, ast.ClassDef) or not _self_test_scope(child.name):
                    walk(child, f"{scope}.{child.name}" if scope else child.name)
                continue
            if isinstance(child, ast.Call) and isinstance(child.func, ast.Attribute):
                callee = ast.unparse(child.func)
                if callee in PROBE_FUNCTIONS and child.args:
                    found.append((child.lineno, scope or "<module>", ast.unparse(child), child.args[0]))
                elif child.func.attr in PROBE_METHODS and not callee.startswith("os."):
                    found.append((child.lineno, scope or "<module>", ast.unparse(child), child.func.value))
            walk(child, scope)

    walk(tree, "")
    return found


def static_check(repo: Path, target: GeneratedTarget, tracked: set[str]) -> list[str]:
    """THE STATIC HALF OF A GENERATED TARGET'S DECLARATION (kb/Work PB5231 R1-1, R1-3), decided before any run:
      * its program's IMPORT CLOSURE under the repository (`inert_lane.import_closure`: every `import` anywhere in a file,
        a lazy one included, resolved against the importer's directory and every directory its chain puts on
        `sys.path`) lies in its inputs; a module outside them is a finding naming the importer chain;
      * every existence or stat call in the program and its closure (`_probes`), which no audit hook sees, probes a path
        under its inputs: a path `inert_lane.static_dir` decides from literals and the module's static names is judged
        here; a call it cannot decide is a finding until the target's `judged` map states, under the key
        `<file>|<function>|<call>`, why the path it probes is one of its inputs. A `judged` key that names no call any
        more is a finding too (a stale judgment).
    Returns the findings, each naming its file and line."""
    if target.program not in tracked:
        return [f"{target.name}: its program {target.program} is not a tracked file"]
    closure = list(inert_lane.import_closure(repo, [(target.program, "its program")], tracked))
    found = [f"{target.name}: {target.program} imports {m} ({how}), which is not among its inputs"
             for m, how in closure if not target.matches(m)]
    judged = {k for k, _ in target.judged}
    seen: set[str] = set()
    for rel in [target.program] + sorted(m for m, _ in closure):
        try:
            tree = ast.parse((repo / rel).read_text(encoding="utf-8"), filename=rel)
        except (OSError, UnicodeDecodeError, SyntaxError) as e:
            found.append(f"{target.name}: {rel} cannot be read for its static check ({type(e).__name__}: {e})")
            continue
        names = inert_lane.static_names(tree, rel)
        for line, scope, call, expr in _probes(rel, tree):
            path = inert_lane.static_dir(expr, names)
            if path is not None:
                if not (target.matches(path) or target.lists(path.rstrip("/") + "/")):
                    found.append(f"{target.name}: {rel}:{line}: {call} probes {path or '(the repository root)'}, "
                                 "which is not among its inputs")
                continue
            k = f"{rel}|{scope}|{call}"
            seen.add(k)
            if k not in judged:
                found.append(f"{target.name}: {rel}:{line}: {call} probes a path the static check cannot decide: state "
                             f"why it lies in the inputs, in build_targets.json `judged` under {k!r}")
    found += [f"{target.name}: the judged entry {k!r} names no undecided call any more: remove it" for k in judged - seen]
    return found


@dataclass(frozen=True)
class Generation:
    """A generated target brought up to date: its key, its output digest and file count, its output directory, whether
    its command ran (and how long), and why."""
    target: str
    key: str
    outputs: str
    files: int
    directory: Path
    ran: bool
    seconds: float | None
    why: str

    def describe(self) -> str:
        """The verdict line's entry: `conformance-suite generated in 13.1 s (output 1a2b3c4d5e6f)` or `… up to date …`."""
        how = f"generated in {self.seconds:.1f} s" if self.ran and self.seconds is not None else "up to date"
        return f"{self.target} {how} (output {self.outputs[:12]}, {self.files} file(s))"

    def record(self) -> dict:
        """What a gate's verdict file keeps of it."""
        return {"key": self.key, "outputs": self.outputs, "files": self.files, "directory": str(self.directory),
                "ran": self.ran, "seconds": self.seconds, "why": self.why}


def generate(repo: Path, target: GeneratedTarget, k: str, store: Store, tracked: set[str], *,
             stable: Callable[[], bool] | None = None, run: Callable[[Path, GeneratedTarget, Path], Trace] = traced_run,
             now: Callable[[], dt.datetime] = lambda: dt.datetime.now(dt.timezone.utc)) -> Generation:
    """BRING ONE GENERATED TARGET UP TO DATE at key `k` in the worktree `repo`. Up to date: the store records an output
    digest at `k` AND its output directory (`GENERATED_ROOT/<name>`) holds exactly that digest; nothing runs. Otherwise
    the directory is removed and the command runs, TRACED (`traced_run`), and is refused, by a BuildError naming it, when
    it exits non-zero (GenerationFailed), read outside its inputs (UndeclaredRead), wrote other than its declared outputs
    (Unresolved), reproduced not the digest recorded at `k` (Nondeterministic: its outputs are kept to compare), or ran
    while the working tree changed (`stable` false: TreeMoved). A first generation at `k` is recorded."""
    directory = repo / GENERATED_ROOT / target.name
    rec = store.lookup(target.name, k)
    recorded = rec.get("outputs") if rec else None
    if recorded and directory.is_dir():
        try:
            on_disk, n = output_digest(directory, target)
        except Unresolved:
            on_disk, n = None, 0
        if on_disk == recorded:
            return Generation(target.name, k, recorded, n, directory, False, None,
                              "its outputs on disk have the digest the store records at its key")
        why = "its outputs on disk are not the ones the store records at its key: generated again"
    else:
        why = "no record at its key" if not recorded else "no outputs on disk: generated again"
    if directory.exists():
        shutil.rmtree(directory)
    directory.mkdir(parents=True)
    t = time.monotonic()
    trace = run(repo, target, directory)
    seconds = round(time.monotonic() - t, 1)
    if trace.rc != 0:
        raise GenerationFailed(f"{target.name}: `{' '.join(target.command)}` exited {trace.rc}:\n"
                               + "\n".join(trace.output.rstrip().splitlines()[-40:]))
    if found := undeclared(target, trace, repo, tracked):
        raise UndeclaredRead(f"{target.name} read outside its declared inputs (scripts/build_targets.json), so its key "
                             f"would not cover what made its output: {'; '.join(found[:20])}"
                             + (f" (and {len(found) - 20} more)" if len(found) > 20 else ""))
    digest, n = output_digest(directory, target)
    if recorded and digest != recorded:
        raise Nondeterministic(f"{target.name} is NOT DETERMINISTIC: at key {k[:12]} the store records output "
                               f"{recorded[:12]} (run in {rec.get('worktree')} at {rec.get('when')}) and this run wrote "
                               f"{digest[:12]} from the same inputs; its outputs are kept in {directory} to compare. A "
                               "generator whose output depends on order, time, an absolute path or the platform is a "
                               "defect: fix it at its root")
    if stable is not None and not stable():
        raise TreeMoved(f"{target.name}: the working tree changed while it ran, so its output is not the output of key "
                        f"{k[:12]}; nothing was recorded")
    if not recorded:
        store.record(target.name, k, {"outputs": digest, "files": n, "seconds": seconds, "worktree": str(repo),
                                      "when": f"{now():%Y-%m-%dT%H:%M:%SZ}"})
    return Generation(target.name, k, digest, n, directory, True, seconds, why)


def bring_up_to_date(repo: Path, graph: Graph, entries: list[tuple[bytes, str]], facets: Mapping[str, str],
                     store: Store, *, stable: Callable[[], bool] | None = None,
                     run: Callable[[Path, GeneratedTarget, Path], Trace] = traced_run,
                     only: Iterable[str] | None = None) -> dict[str, Generation]:
    """Every generated target of `graph` (of `only`, when given: a set closed under dependency, `Graph.needs_generated`),
    in topological order, brought up to date (`generate`) at the tree `entries` lists; a dependent's key is made from
    the output digests its dependencies have just been given. Raises the first BuildError."""
    tracked = {p for _, p in entries}
    wanted = None if only is None else set(only)
    done: dict[str, Generation] = {}
    for name in graph.order:
        t = graph.targets[name]
        if isinstance(t, GeneratedTarget) and (wanted is None or name in wanted):
            k = key(t, entries, facets, {n: g.outputs for n, g in done.items()})
            done[name] = generate(repo, t, k, store, tracked, stable=stable, run=run)
    return done


# ── the declared-inputs audit ────────────────────────────────────────────────────────────────────────────────────────────

#: Each TestRepo helper and the directory it is rooted at ("" is the repository root; None: build output, made from src/).
TESTREPO_BASES = {
    "At": "", "Root": "", "Src": "src", "SrcBin": None, "Tests": "tests", "Docs": "docs", "Scripts": "scripts",
    "Specs": "specs", "VersionMatrix": "tests/version-matrix", "ConstructsDir": "tests/version-matrix/constructs",
    "ConstructRows": "tests/version-matrix/constructs", "Nist": "tests/nist", "Suite": "conformance-suite",
    "Configuration": None, "TargetFramework": None,
}
#: The helpers that find a conformance-suite test by its id, and the two trees they list to find it (TestRepo.cs
#: `s_suiteCases`): an id names no path, so the trees are the read.
SUITE_CASE_HELPERS = {"SuiteCase": ("conformance-suite/cases/", "conformance-suite/wiseowl/cases/"),
                      "SuiteFile": ("conformance-suite/cases/", "conformance-suite/wiseowl/cases/")}
TESTREPO_CALL = re.compile(r"\bTestRepo\.(\w+)")
#: The helper that DECLARES the directory a computed path lies in (tests/_shared/TestRepo.cs `Within`): its first
#: argument, a literal directory, is the read.
WITHIN = "Within"
#: What precedes a `TestRepo.Root` that reads nothing: the root made relative (`Path.GetRelativePath(TestRepo.Root, f)`
#: computes a string), or a documentation reference (`<see cref="TestRepo.Root"/>`, `nameof(TestRepo.Root)`).
NOT_A_READ = re.compile(r"(?:Path\.GetRelativePath\(\s*|cref=\"|nameof\(\s*)$")
#: A declared sweep of the tracked tree (tests/_shared/TrackedTree.cs `TreeScope`): its `Under` prefixes.
TREE_SCOPE = re.compile(r"\bTreeScope\s+\w+\s*=\s*new(?:\s+TreeScope)?\(\s*Under:\s*\[([^\]]*)\]")
#: The literal head of an interpolated string, up to its first hole: `$"tests/differential/{name}"`.
INTERPOLATED_HEAD = re.compile(r'(?:\$@?|@\$)"([^"{}\\]*)\{')
#: A member that IS the repository root under another name (`static string HostRoot => TestRepo.Root;`): each use of it
#: is judged as a use of `TestRepo.Root` (kb/Work PB5230 R1-5), so an alias cannot hide a read.
ROOT_ALIAS = re.compile(r"\bstatic\s+string\s+(\w+)\s*(?:=>|\{\s*get;\s*\}\s*=)\s*TestRepo\.Root\s*;")
#: The top-level type a source declares first: an alias's declaring type.
FIRST_TYPE = re.compile(r"^(?:(?:internal|public|static|sealed|partial|abstract|file|readonly)\s+)*"
                        r"(?:class|record|struct)\s+(\w+)", re.MULTILINE)
STRING = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
#: A TOP-LEVEL type declaration (no indentation): what another source must name to use a tests/_shared file. Nested types
#: (`Row`, `Edition`) are reached through their outer type, and their common names would link every file to every other.
TYPE_DECL = re.compile(r"^(?:(?:internal|public|static|sealed|partial|abstract|file|readonly)\s+)*"
                       r"(?:class|record|struct|enum|interface)\s+(\w+)", re.MULTILINE)
#: A listing of a helper's directory by a literal pattern: `Directory.EnumerateFiles(TestRepo.Scripts(), "guard*.sh")`.
LISTING = re.compile(r"Directory\.(?:Enumerate|Get)(?:Files|Directories|FileSystemEntries)\(\s*TestRepo\.(\w+)"
                     r"\(((?:\s*\"[^\"]*\"\s*,?)*)\)\s*,\s*\"([^\"]+)\"")


def _top_dirs(repo: Path) -> list[str]:
    """The top-level tracked directories: what a literal path is rooted at."""
    names = subprocess.run(["git", "ls-tree", "--name-only", "HEAD"], cwd=repo, check=True, capture_output=True,
                           text=True, encoding="utf-8").stdout.split()
    return [n for n in names if (repo / n).is_dir()]


def _args(text: str, start: int) -> str | None:
    """The text between the `(` (or `[`) at `start` and its matching close; None when `start` is neither."""
    if start >= len(text) or text[start] not in "([":
        return None
    opener, closer = text[start], ")" if text[start] == "(" else "]"
    depth = 0
    for i in range(start, len(text)):
        depth += {opener: 1, closer: -1}.get(text[i], 0)
        if depth == 0:
            return text[start + 1:i]
    return None


def _split_top(args: str) -> list[str]:
    parts, depth, cur, quoted = [], 0, [], False
    for i, ch in enumerate(args):
        if ch == '"' and (i == 0 or args[i - 1] != "\\"):
            quoted = not quoted
        if not quoted:
            depth += {"(": 1, ")": -1, "[": 1, "]": -1, "{": 1, "}": -1}.get(ch, 0)
            if ch == "," and depth == 0:
                parts.append("".join(cur).strip())
                cur = []
                continue
        cur.append(ch)
    parts.append("".join(cur).strip())
    return parts


def _spread(parts: list[str]) -> list[str]:
    """Call arguments with a lone collection expression spread into its elements: `At(["src", "X", .. rest])` names
    the same path as `At("src", "X", rest)`; a spread element (`.. rest`) is computed, so the literals stop there."""
    if len(parts) == 1 and parts[0].startswith("[") and parts[0].endswith("]"):
        return [x for x in _split_top(parts[0][1:-1]) if x]
    return parts


def _leading_literals(args: list[str]) -> tuple[int, list[str]]:
    """How many leading arguments are plain string literals, and their text split on `/`: a path's segments known before
    run time."""
    n, segs = 0, []
    for a in args:
        m = re.fullmatch(r'"([^"{}\\]*)"', a)
        if m is None:
            # An interpolated string (`$"tests/differential/{name}"`) spells the directories before its first hole and
            # nothing after: its complete leading segments count, and the path stops there (kb/Work PB5230 R1-1).
            if (i := INTERPOLATED_HEAD.match(a)) is not None:
                segs += [s for s in i.group(1).rpartition("/")[0].split("/") if s]
            break
        n += 1
        segs += [s for s in m.group(1).split("/") if s]
    return n, segs


def _line_at(text: str, i: int) -> str:
    """The source line holding offset `i`."""
    end = text.find("\n", i)
    return text[text.rfind("\n", 0, i) + 1:end if end >= 0 else len(text)]


def _root_use(text: str, start: int, end: int) -> list[str]:
    """The reads one use of the repository root (`TestRepo.Root`, or an alias of it) at `text[start:end]` spells: none
    when it is TEXT, else a path, a directory (ending in `/`) or "" (a bare root, unresolved).

    It is TEXT exactly when: it lies in a comment or in a string literal's text; it is made relative
    (`Path.GetRelativePath(TestRepo.Root, …)`, a string computed from paths) or named (`cref`, `nameof`); or it is an
    interpolated string's hole whose neighbours in that string are neither a path separator nor the string's own quotes
    (`$"… '{TestRepo.Root}'; …"`). A hole the string continues with a separator (`$"{TestRepo.Root}/tests/x.out"`) is a
    read of the literal path that follows it, up to the next hole; a hole preceded by a separator, a hole that is the
    whole string (`$"{TestRepo.Root}"`, which flows wherever it is passed) and a hole with a format are bare roots.
    `Path.Combine(TestRepo.Root, "a", …)` and `Path.Combine([TestRepo.Root, "a", …])` read their literal segments; any
    other use is a bare root (kb/Work PB5230 review R1-2: a form the audit cannot classify is a read, never text)."""
    where = csharp_lexer.kind_at(text, start)
    if where in ("comment", "string") or ROOT_ALIAS.search(_line_at(text, start)):
        return []   # text, or an alias's declaration: each USE of the alias is judged where it is (`root_aliases`)
    before = text[max(0, start - 60):start]
    if NOT_A_READ.search(before):
        return []
    if where == "hole" and text[start - 1:start] == "{":
        if text[end:end + 1] != "}":
            return [""]
        nxt, prv = text[end + 1:end + 2], text[start - 2:start - 1]
        if nxt in ("/", "\\"):
            tail = re.match(r'[^"{]*', text[end + 1:]).group(0)
            closed = text[end + 1 + len(tail):end + 2 + len(tail)] == '"'
            segs = [s for s in re.split(r"[/\\]+", tail) if s]
            if not closed and not tail.endswith(("/", "\\")):
                segs = segs[:-1]
            if not segs:
                return [""]
            return ["/".join(segs) if closed else "/".join(segs) + "/"]
        if prv in ("/", "\\") or (prv == '"' and nxt == '"'):
            return [""]
        return []
    call = re.search(r"Path\.Combine\(\s*(\[\s*)?$", before)
    if call is not None:
        open_at = text.rindex("[" if call.group(1) else "(", 0, start)
        parts = [x for x in _split_top(_args(text, open_at) or "")[1:] if x]
        n, segs = _leading_literals(parts)
        path = "/".join(segs)
        return [path if n == len(parts) and n > 0 else (path + "/" if path else "")]
    return [""]


def root_aliases(sources: dict[str, str]) -> list[str]:
    """The uses of every root alias the sources declare (ROOT_ALIAS), as patterns: `Type.Member` anywhere, and the bare
    member in its declaring file (`(?<!\\.)Member`, applied to that file only by `reads_in`'s caller)."""
    out = []
    for text in sources.values():
        owner = FIRST_TYPE.search(text)
        for m in ROOT_ALIAS.finditer(text):
            out.append(rf"\b{re.escape(owner.group(1))}\.{re.escape(m.group(1))}\b" if owner else rf"\b{m.group(1)}\b")
    return out


#: THE ONE-HOME RULE of the repository's locator (kb/Work PB3814): its `Anchors` list names where a hand-rolled locator
#: starts from, and `bypasses_in` refuses each of them (`root_anchors`), so the rule's list is the one list.
ONE_HOME = "tests/WiseOwl.Cobol.Tests.RepoDrift/TestRepoDriftTests.cs"


def root_anchors(repo: Path = REPO) -> tuple[str, ...]:
    """The one-home rule's base-directory anchors (`TestRepoDriftTests.Anchors`), read from its source. A rule that no
    longer declares the list is a failure naming it, never an empty list that would refuse nothing."""
    m = re.search(r"\bAnchors\s*=\s*\[(.*?)\];", (repo / ONE_HOME).read_text(encoding="utf-8"), re.S)
    anchors = tuple(re.findall(r'"([^"]*)"', m.group(1))) if m else ()
    if not anchors:
        raise GraphError(f"{ONE_HOME}: no `Anchors = [...]` list: the one-home rule moved; repoint buildgraph.ONE_HOME")
    return anchors


#: ⛔ THE FILE-SYSTEM BYPASSES beyond the one-home rule's anchors (kb/Work PB5230 review R2-2): each reaches the file
#: system around TestRepo and ScratchDirectory, so the audit cannot see what it reads. Each is (kind, pattern over a line).
BYPASSES = (
    ("an assembly's location", re.compile(r"\bAssembly\w*(?:\(\))?\.(?:Location|CodeBase)\b|\.Assembly\.(?:Location|CodeBase)\b")),
    ("the working directory", re.compile(r"\bEnvironment\.CurrentDirectory\b")),
)
#: A System.IO call that OPENS or RESOLVES a path given as its first argument: a relative literal there resolves against
#: the working directory, a rooted one is a fixed machine path. The type is named bare or as `System.IO.` (the round-2
#: review planted `System.IO.Path.Combine(...)`), never as another object's member (`x.File.Method(`, a member named File).
IO_TYPE = r"(?:(?<![\w.])|(?<=\bSystem\.IO\.))"
FS_CALL = re.compile(rf"{IO_TYPE}(?:File|Directory)\.\w+\(|{IO_TYPE}Path\.GetFullPath\(|"
                     r"\bnew\s+(?:System\.IO\.)?(?:FileInfo|DirectoryInfo|FileStream|StreamReader|StreamWriter)\(")
#: A call that only JOINS path segments: a relative literal there is a fragment some other base completes (a TestRepo
#: helper's argument), so only a ROOTED literal first argument bypasses TestRepo.
JOIN_CALL = re.compile(rf"{IO_TYPE}Path\.(?:Combine|Join)\(")
#: A rooted path literal: a drive letter, a POSIX root or a UNC prefix.
ROOTED = re.compile(r'@?"(?:[A-Za-z]:[\\/]|/|\\\\)')
#: Any System.IO path call, for "an environment variable forming a path".
PATH_CALL = re.compile(FS_CALL.pattern + "|" + JOIN_CALL.pattern)
#: An environment variable read.
ENV_READ = re.compile(r"\bEnvironment\.GetEnvironmentVariable\(")
#: The `csharp_lexer.kind_at` kinds that RUN: top-level code and an interpolation hole's code.
RUNS = ("code", "hole")


def bypasses_in(text: str, anchors: tuple[str, ...]) -> list[tuple[int, str]]:
    """Every statement (line) of `text` that reaches the file system around TestRepo and ScratchDirectory (R2-2): one of
    the one-home rule's `anchors` (`root_anchors`), an assembly's location, the working directory, an environment variable
    read on a line that forms a
    path (a System.IO path call), or a string LITERAL as the first argument of a System.IO path call (a rooted literal is
    a fixed machine path; a relative one resolves against the working directory). Only code counts, an interpolation
    hole's included (it runs): comments and the text of other literals do not (`csharp_lexer.kind_at`)."""
    out: list[tuple[int, str]] = []
    offset = 0
    rule = ("a base-directory anchor of the one-home rule", re.compile("|".join(rf"(?<!\w){re.escape(a)}" for a in anchors)))
    for n, line in enumerate(text.split("\n"), 1):
        hits: list[str] = []
        for kind, pattern in (rule, *BYPASSES):
            if any(csharp_lexer.kind_at(text, offset + m.start()) in RUNS for m in pattern.finditer(line)):
                hits.append(kind)
        if PATH_CALL.search(line) and any(csharp_lexer.kind_at(text, offset + m.start()) in RUNS for m in ENV_READ.finditer(line)):
            hits.append("an environment variable forming a path")
        for pattern, literal in ((FS_CALL, re.compile(r'@?"(?!")')), (JOIN_CALL, ROOTED)):
            for m in pattern.finditer(line):
                if csharp_lexer.kind_at(text, offset + m.start()) in RUNS and literal.match(line[m.end():].lstrip()):
                    hits.append("a literal path handed to System.IO")
        if hits:
            out.append((n, ", ".join(dict.fromkeys(hits))))
        offset += len(line) + 1
    return out


def own_root_aliases(text: str) -> list[str]:
    """The bare uses, inside the file that declares it, of a root alias (`HostRoot` beside `static string HostRoot =>
    TestRepo.Root;`)."""
    return [rf"(?<![\w.]){re.escape(m.group(1))}\b" for m in ROOT_ALIAS.finditer(text)]


def reads_in(text: str, top_dirs: list[str], aliases: list[str] | tuple[str, ...] = ()) -> list[tuple[int, str]]:
    """Every repository read `text` (a C# source) spells, with its line: a path when every segment is a literal, else the
    directory its literal segments reach, ending in `/` ("" is the whole repository: a bare root, unresolved). `aliases`
    are the patterns of the root's other names (`root_aliases`); this file's own are added."""
    found: list[tuple[int, str]] = []
    aliases = [*aliases, *own_root_aliases(text)]

    def line_of(i: int) -> int:
        return text.count("\n", 0, i) + 1

    listed: set[int] = set()
    for m in LISTING.finditer(text):
        base = TESTREPO_BASES.get(m.group(1))
        if base is None:
            continue
        _, segs = _leading_literals([x for x in _split_top(m.group(2)) if x])
        listed.add(text.index("TestRepo.", m.start()))
        found.append((line_of(m.start()), "/".join(x for x in [base, *segs, m.group(3)] if x)))
    for m in TESTREPO_CALL.finditer(text):
        helper = m.group(1)
        before = text[max(0, m.start() - 60):m.start()]
        if helper == WITHIN:
            # `TestRepo.Within("src/", rel)`: a computed path DECLARED to lie in a literal directory, and held there at
            # run time (TestRepo.Within throws otherwise), so the directory is the read.
            parts = [x for x in _split_top(_args(text, m.end()) or "") if x]
            n, segs = _leading_literals(parts[:1])
            found.append((line_of(m.start()), "/".join(segs) + "/" if n and segs else ""))
            continue
        if helper in SUITE_CASE_HELPERS:
            found += [(line_of(m.start()), tree) for tree in SUITE_CASE_HELPERS[helper]]
            continue
        base = TESTREPO_BASES.get(helper, None) if helper in TESTREPO_BASES else None
        if base is None or m.start() in listed:
            continue
        if helper == "Root":
            found += [(line_of(m.start()), r) for r in _root_use(text, m.start(), m.end())]
            continue
        segs: list[str] = []
        complete = False
        args = _args(text, m.end())
        if args is not None:
            parts = _spread([x for x in _split_top(args) if x])
            n, segs = _leading_literals(parts)
            complete = n == len(parts) and n > 0
        path = "/".join(x for x in [base, *segs] if x)
        found.append((line_of(m.start()), path if complete else (path + "/" if path else "")))
    for alias in aliases:
        # A use of a root alias is a use of the root (ROOT_ALIAS); its own declaration is not.
        for m in re.finditer(alias, text):
            if ROOT_ALIAS.search(_line_at(text, m.start())) is None:
                found += [(line_of(m.start()), r) for r in _root_use(text, m.start(), m.end())]
    roots = "|".join(re.escape(d) for d in top_dirs)
    lit = re.compile(rf"^(?:{roots})/[^\s{{}}<>|]*$") if roots else None
    for m in STRING.finditer(text):
        s = m.group(1)
        interpolated = text[max(0, m.start() - 2):m.start()].endswith("$") or text[max(0, m.start() - 2):m.start()] == "$@"
        if interpolated and "{" in s:
            # An interpolated string's literal head, up to its first hole, is the directory it reads within (R1-1).
            head = s.split("{", 1)[0]
            if lit is not None and lit.match(head):
                found.append((line_of(m.start()), head if head.endswith("/") else head.rpartition("/")[0] + "/"))
            continue
        if lit is None or not lit.match(s):
            continue
        cut = re.split(r"[*?]", s, maxsplit=1)[0]
        found.append((line_of(m.start()), s if cut == s else (cut if cut.endswith("/") else cut.rsplit("/", 1)[0] + "/")))
    for m in TREE_SCOPE.finditer(text):
        # A sweep of the tracked tree reads every path its scope's `Under` prefixes hold (TreeScope.Holds compares
        # ordinally, so "tests" holds tests/ and any sibling spelled "tests…"): a prefix ending in `/` is that directory;
        # any other is the directory that holds its last segment, and a prefix at the top ("tests", "") is the whole tree,
        # a bare root reference (kb/Work PB5230 review R1-3).
        for prefix in re.findall(r'"([^"]*)"', m.group(1)):
            found.append((line_of(m.start()), prefix if prefix.endswith("/")
                          else (prefix.rpartition("/")[0] + "/" if "/" in prefix else "")))
    return [(line, _resolved(read)) for line, read in found]


def _resolved(read: str) -> str:
    """A read with its `.` and `..` segments resolved (review R2-1: `TestRepo.Src("..", "tests", "d")` reads `tests/d`, which
    is judged there, not dropped as a path no file has). A read that climbs above the repository keeps a leading `../`
    (refused by name in `scan`); one that resolves to the repository itself is a bare root ("")."""
    if not read or not any(s in (".", "..") for s in read.rstrip("/").split("/")):
        return read
    resolved = posixpath.normpath(read)
    if resolved == ".":
        return ""
    return resolved + "/" if read.endswith("/") and not resolved.endswith("/") else resolved


def assembly_sources(repo: Path, target: TestTarget) -> list[Path]:
    """The C# sources compiled into `target`'s assembly that it USES: its project's own, and each tests/_shared file
    that declares a type they name, transitively (the test-tree props link every shared file into every project)."""
    own = sorted((repo / target.project).rglob("*.cs"))
    own = [p for p in own if not any(part in ("bin", "obj") for part in p.relative_to(repo / target.project).parts)]
    shared = {p: p.read_text(encoding="utf-8", errors="replace") for p in sorted((repo / "tests" / "_shared").glob("*.cs"))}
    declares = {p: set(TYPE_DECL.findall(t)) for p, t in shared.items()}
    text = "\n".join(p.read_text(encoding="utf-8", errors="replace") for p in own)
    used: list[Path] = []
    changed = True
    while changed:
        changed = False
        for p, names in declares.items():
            if p not in used and any(re.search(rf"\b{re.escape(n)}\b", text) for n in names):
                used.append(p)
                text += "\n" + shared[p]
                changed = True
    return own + sorted(used)


@dataclass(frozen=True)
class Scan:
    """THE BUILD TARGETS AUDIT for one target, from ONE pass over its sources. `undeclared`: each repository read they
    spell that the inputs do not cover, as `<file>:<line>: <read>`. `roots`: the bare repository-root references no
    statement judgment covers (`TestRepo.Root` or an alias of it used in a form `_root_use` cannot resolve, `At()` with no
    literal segment, a top-level TreeScope prefix), as `<file>:<line>: <statement>`: a finding in a narrowly declared
    target (`findings_of`), counted in a broad one (kb/Work PB5230 review R1-1). `through`: each read that is the program
    of a target this one depends on (`<file>:<line>: <read>`), the run a host makes only when no gate handed it the
    dependency's output; it belongs to that target. `handed`: each dependency's environment variable the sources spell
    (`GeneratedTarget.env`). `used`: the hand judgments (`judged` keys) a read or root of these sources matched; a judgment
    no target uses is stale."""
    undeclared: tuple[str, ...]
    roots: tuple[str, ...]
    through: tuple[str, ...] = ()
    handed: tuple[str, ...] = ()
    used: frozenset[str] = frozenset()
    #: Each statement that reaches the file system around TestRepo and ScratchDirectory (`bypasses_in`), unjudged.
    bypasses: tuple[str, ...] = ()
    #: Each read that names no tracked path and is neither a build output nor ignored (`<file>:<line>: <read>`).
    unknown: tuple[str, ...] = ()


def _build_output(read: str) -> bool:
    """A path inside a project's build output: a `bin` or `obj` segment (TestRepo.SrcBin's, a test host's own)."""
    return bool({"bin", "obj"} & set(read.strip("/").split("/")))


def _ignored(repo: Path, reads: list[str]) -> set[str]:
    """The reads git ignores (`git check-ignore --no-index`: the rules alone, whether the path exists or not): a cache, a
    scratch or a generated location the repository declares untracked (TestResults/, .cache/, docs/DIAGNOSTICS.md)."""
    if not reads:
        return set()
    # NUL-separated bytes both ways: text mode would write CRLF on Windows and git would read a `\r` into each path.
    out = subprocess.run(["git", "check-ignore", "--no-index", "--stdin", "-z"], cwd=repo,
                         input="\0".join(reads).encode("utf-8") + b"\0", capture_output=True).stdout
    return {p for p in out.decode("utf-8", "surrogateescape").split("\0") if p}


def scan(repo: Path, target: TestTarget, top_dirs: list[str], tracked: tuple[set[str], set[str]],
         cls: inert_lane.Classification = inert_lane.CLASSIFICATION,
         dependencies: tuple[GeneratedTarget, ...] = (), *, anchors: tuple[str, ...]) -> Scan:
    """Read `target`'s sources once and return every half of its audit. The tree's top directories and tracked paths
    are the caller's, computed once for every target (kb/Work PB5218 review R1-1: per-file recomputation cost ~60 s).
    `dependencies`: the generated targets it depends on, whose PROGRAM a source may name (a read reached through the
    dependency) and whose ENVIRONMENT VARIABLE its sources must spell. `anchors`: the one-home rule's base-directory
    anchors (`root_anchors`, read once by the caller)."""
    judged = {k for k, _ in target.judged}
    programs = {d.program: d for d in dependencies}
    used: set[str] = set()
    bad: set[str] = set()
    roots: set[str] = set()
    through: set[str] = set()
    handed: set[str] = set()
    bypass: set[str] = set()
    untracked: list[tuple[str, int, str]] = []
    sources = {src.relative_to(repo).as_posix(): src.read_text(encoding="utf-8", errors="replace")
               for src in assembly_sources(repo, target)}
    aliases = root_aliases(sources)
    for rel, text in sources.items():
        lines = text.split("\n")
        # THE ONE HAND-OFF SITE of a dependency (kb/Work PB5231 R1-2): the source that names its variable AND the mark it
        # records when it had to generate its own (`GeneratedTarget.own_mark`, which the gate turns RED).
        site = {d.name for d in dependencies if f'"{d.env}"' in text and f'"{d.own_mark}"' in text}
        handed |= {d.env for d in dependencies if d.name in site}
        for line, read in reads_in(text, top_dirs, aliases):
            if read == "":
                # A bare root is judged by hand only STATEMENT by statement: `<file>|<its line, trimmed>`, so a new root
                # in the same file is never covered by an old judgment (kb/Work PB5230 review R1-1).
                key = f"{rel}|{lines[line - 1].strip()}"
                if key in judged:
                    used.add(key)
                else:
                    roots.add(f"{rel}:{line}: {lines[line - 1].strip()}")
                continue
            if read == ".." or read.startswith("../"):
                bad.add(f"{rel}:{line}: {read} (climbs out of the repository: no target can declare it)")
                continue
            if f"{rel}|{read}" in judged:
                used.add(f"{rel}|{read}")
                continue   # judged by hand
            known = _normalized(read, tracked)
            if known is None:
                # ⛔ A read that names no tracked path is DROPPED only when it is provably not an input: a build output (a
                # `bin` or `obj` segment) or a path git ignores (`ignored`, one `git check-ignore` for the whole scan).
                # Anything else, a typo, a stale path or a file that does not exist yet, is reported (review R2-1).
                if not _build_output(read):
                    untracked.append((rel, line, read))
                continue
            read = known
            if f"{rel}|{read}" in judged:
                used.add(f"{rel}|{read}")
                continue   # judged by hand
            if read in programs and programs[read].name in site:
                through.add(f"{rel}:{line}: {read}")
            elif read in programs:
                # ⛔ Anywhere else, running or reading the dependency's program reads everything it reads (kb/Work PB5231
                # R1-2: a test running `generate.py --check` reads the inventory), none of it in this target's key.
                bad.add(f"{rel}:{line}: {read} (the program of {programs[read].name}, outside its one hand-off site: "
                        f"this reads every input of {programs[read].name}, and none is in this target's key)")
            elif not target.covers(read, cls):
                bad.add(f"{rel}:{line}: {read}")
        # The file system reached around TestRepo and ScratchDirectory (review R2-2), judged statement by statement.
        for line, kind in bypasses_in(text, anchors):
            key = f"{rel}|{lines[line - 1].strip()}"
            if key in judged:
                used.add(key)
            else:
                bypass.add(f"{rel}:{line}: {lines[line - 1].strip()}  ({kind})")
    ignored = _ignored(repo, [r for _, _, r in untracked])
    unknown = tuple(sorted(f"{rel}:{line}: {read}" for rel, line, read in untracked if read not in ignored))
    return Scan(tuple(sorted(bad)), tuple(sorted(roots)), tuple(sorted(through)), tuple(sorted(handed)), frozenset(used),
                tuple(sorted(bypass)), unknown)


def _tracked(repo: Path) -> tuple[set[str], set[str]]:
    """(every tracked path, every directory holding one, each ending in `/`)."""
    names = subprocess.run(["git", "ls-files", "-z"], cwd=repo, check=True, capture_output=True).stdout.decode(
        "utf-8", "surrogateescape").split("\0")
    files = {n for n in names if n}
    dirs = {"/".join(f.split("/")[:i]) + "/" for f in files for i in range(1, f.count("/") + 1)}
    return files, dirs


def _normalized(read: str, tracked: tuple[set[str], set[str]]) -> str | None:
    """A read as the tree knows it: a tracked directory spelled without its `/` gains it; a pattern stays; a read that
    names no tracked path (and no directory holding one) is None; "" (the whole repository) is None here."""
    files, dirs = tracked
    if not read:
        return None
    if "*" in read or "?" in read:
        return read
    if read.endswith("/"):
        return read if read in dirs else None
    if read in files:
        return read
    return read + "/" if read + "/" in dirs else None


def check(repo: Path = REPO) -> int:
    graph = load_targets(repo / "scripts" / "build_targets.json")
    top, tracked, anchors = _top_dirs(repo), _tracked(repo), root_anchors(repo)
    scans = {name: scan(repo, t, top, tracked, dependencies=tuple(graph.targets[d] for d in t.depends), anchors=anchors)
             for name, t in graph.tests().items()}
    unhanded = [f"{name}: no source of its assembly names both {graph.targets[d].env}, the variable the gate hands "
                f"{d}'s output in, and {graph.targets[d].own_mark!r}, what its host records when it generated its own: "
                "its hosts could run a suite its key does not name"
                for name, t in graph.tests().items() for d in t.depends if graph.targets[d].env not in scans[name].handed]
    statics = [f for t in graph.generated().values() for f in static_check(repo, t, tracked[0])]
    findings = findings_of(graph.tests(), scans) + unhanded + statics
    for f in findings:
        print(f"  {f}")
    residue = ", ".join(f"{name} {len(sc.roots)}" for name, sc in scans.items() if graph.targets[name].broad)
    through = ", ".join(f"{name} {len(sc.through)}" for name, sc in scans.items() if sc.through)
    if findings:
        print(f"=== BUILD TARGETS: RED — {len(findings)} finding(s): a repository read outside its target's declared "
              "inputs (scripts/build_targets.json: declare it with its reader, or remove the read), an unresolved read "
              "in a narrowly declared target, a file-system bypass of TestRepo or ScratchDirectory there, a read naming no "
              "tracked path that is neither a build output nor ignored, a climb out of the repository, a stale judgment, "
              "a dependency's output its assembly does not take from "
              "the gate, or a generated target's import or probe outside its inputs ===")
        return 1
    print(f"=== BUILD TARGETS: GREEN — {len(graph.targets)} target(s), {len(graph.generated())} generated, in the order "
          f"{' -> '.join(graph.order)}; every repository read the test sources spell lies in their declared inputs"
          + (f" or is a dependency's program ({through})" if through else "")
          + "; every narrowly declared target resolves every read and reaches the file system only through TestRepo "
          "and ScratchDirectory or a judged statement, and names no untracked path that is not a build output or "
          "ignored; each generated target's import closure and probes "
          f"lie in its inputs (bare root references of the targets declaring every population input, judged by the "
          f"dynamic proof: {residue or 'none'}) ===")
    return 0


def findings_of(targets: Mapping[str, Target], scans: Mapping[str, Scan]) -> list[str]:
    """THE AUDIT'S FINDINGS over every test target's scan, each once: a read outside its target's inputs; in a target
    declared narrowly, an unresolved root; a hand judgment no target's sources match (stale)."""
    findings: list[str] = []
    for name, sc in scans.items():
        findings += [f"{name}: {b}" for b in sc.undeclared]
        # ⛔ A target declared narrowly (no `@population-inputs`) cannot cover a read it cannot resolve: an unresolved
        # root there is a finding naming its file and line, never a count (owner 2026-11-10: "If it cannot be resolved,
        # there is a problem/bug"; kb/Work PB5230 review R1-1).
        if not targets[name].broad:
            findings += [f"{name}: {r}  (an unresolved repository-root read: spell its path, declare its directory with "
                         "TestRepo.Within, or judge the statement by hand with its reason)" for r in sc.roots]
            findings += [f"{name}: {b}: the file system reached around TestRepo and ScratchDirectory (review R2-2): use "
                         "them, or judge the statement by hand with its reason" for b in sc.bypasses]
            findings += [f"{name}: {u} names no tracked path, and is neither a build output nor ignored: a typo, a stale "
                         "path or a file that does not exist yet (review R2-1); correct it, or judge it with its reason"
                         for u in sc.unknown]
    used = set().union(*(sc.used for sc in scans.values())) if scans else set()
    findings += [f"stale judgment {k!r} (scripts/build_targets.json): no read or root of any target matches it; remove "
                 "it, or correct it" for n, t in targets.items() if n in scans for k, _ in t.judged if k not in used]
    return list(dict.fromkeys(findings))


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────

#: The planted generator of the self-test: a manifest of the ids of `inv/*.json` (their `verdict` never reaches it), and,
#: when `NONDET` is planted beside it, the time.
PLANTED_GENERATOR = """import json, os, sys, time
from pathlib import Path
here = Path(__file__).resolve().parent
out = Path(sys.argv[sys.argv.index("--out") + 1])
ids = sorted(json.loads(p.read_text(encoding="utf-8"))["id"] for p in (here.parent / "inv").glob("*.json"))
extra = {"at": time.time_ns()} if (here / "NONDET").exists() else {}
if (here / "READ_DOCS").exists():
    (here.parent / "docs" / "n.md").read_text(encoding="utf-8")
(out / "suite").mkdir(parents=True, exist_ok=True)
(out / "suite" / "manifest.json").write_text(json.dumps({"ids": ids, **extra}) + "\\n", encoding="utf-8", newline="\\n")
"""


def transport_arms(root: Path, arm) -> None:
    """The store ref, end to end, on fixtures (a bare remote, a publishing clone, a second publisher and a blob:none
    clone as CI checks out): never the live repository or its remote."""
    from scratch_repo import ScratchRepo  # noqa: PLC0415
    host = ScratchRepo.init(root / "t-host")
    remote = host.origin(root / "t-remote.git")
    remote.git("config", "uploadpack.allowFilter", "true")
    host.commit("base", {"src/A.cs": "class A {}\n"})
    host.git("push", "-q", "origin", "HEAD:refs/heads/main")
    k1, k2 = "1" * 64, "2" * 64
    now = datetime(2026, 11, 10, 12, 0, tzinfo=timezone.utc)
    stamp = f"{now:%Y-%m-%dT%H:%M:%SZ}"
    facets = {"platform": "windows", "configuration": "Debug", "toolchain": "t1"}
    store = Store(root / "t-store")
    store.record("conformance", k1, {
        "assembly": "Conformance", "facets": facets, "worktree": "E:/secret/wt", "evidence": "E:/secret/wt/run",
        "run": "000007", "head": "f" * 40, "tree": "a" * 40, "when": stamp,
        "counts": {"discovered": 9, "ran": 9, "skipped": 0, "carried": 0}})
    said: list[str] = []
    first = publish(host.path, store, "origin", now, say=said.append)
    ci = ScratchRepo.clone(f"file://{remote.path.as_posix()}", root / "t-ci")
    ci.git("config", "remote.origin.partialclonefilter", "blob:none")  # the partial clone CI's `changes` checkout is
    ci.git("config", "remote.origin.promisor", "true")
    got = fetch_published(ci.path, "origin")
    rec = got.lookup("conformance", k1) or {}
    arm("the transport: a pass published from the host's store is read back from the remote ref by a partial clone, "
        "carrying only the published fields (no worktree or evidence path), at its key and at no other",
        first is not None and got.commit == first and sorted(rec) == sorted(
            ("assembly", "counts", "facets", "head", "key", "result", "run", "target", "tree", "when"))
        and got.lookup("conformance", k2) is None and got.lookup("unit", k1) is None,
        f"{first} {got.commit} {sorted(rec)}")
    forged = Published({("unit", k1): {"target": "unit", "key": k2, "result": "pass"},
                        ("conformance", k1): {"target": "conformance", "key": k1, "result": "red"}}, "x")
    arm("a record read from the ref is held to the store's one rule (`is_pass`): one filed under a key it does not "
        "name, or one that is not a pass, is no pass",
        forged.lookup("unit", k1) is None and forged.lookup("conformance", k1) is None
        and Store(root / "t-store").lookup("conformance", k1) is not None)
    other = Store(root / "t-store-2")
    other.record("unit", k2, {"assembly": "Unit", "facets": facets, "run": "000002", "head": "e" * 40,
                              "tree": "b" * 40, "when": stamp, "counts": {"ran": 4}})
    second = publish(host.path, other, "origin", now, say=said.append)
    both = fetch_published(ci.path, "origin")
    stale = _git(host.path, "push", "--quiet", f"--force-with-lease={STORE_REF}:{first}", "origin",
                 f"{write_records(host.path, {})}:{STORE_REF}", check=False)
    again = publish(host.path, other, "origin", now, say=said.append)
    # a lost lease INSIDE publish (review R1-3): another publisher lands between this call's read and its push
    k3, k4 = "3" * 64, "4" * 64
    mine, racer = Store(root / "t-store-mine"), Store(root / "t-store-racer")
    for st, kk in ((mine, k3), (racer, k4)):
        st.record("unit", kk, {"assembly": "Unit", "facets": facets, "run": "000003", "head": "d" * 40,
                               "tree": "c" * 40, "when": stamp, "counts": {"ran": 1}})
    module = globals()
    real_git, raced = module["_git"], []

    def racing_git(repo: Path, *args: str, **kw) -> subprocess.CompletedProcess:
        if args[:1] == ("push",) and not raced:
            raced.append(True)
            module["_git"] = real_git
            try:
                publish(host.path, racer, "origin", now, say=said.append)
            finally:
                module["_git"] = racing_git
        return real_git(repo, *args, **kw)

    module["_git"] = racing_git
    try:
        recovered = publish(host.path, mine, "origin", now, say=said.append)
    finally:
        module["_git"] = real_git
    after_race = fetch_published(ci.path, "origin")
    arm("a publisher that loses its lease to another mid-publish re-reads and re-sends: the ref ends holding both "
        "publishers' records and every earlier one",
        bool(raced) and recovered == after_race.commit and any("re-reading" in s for s in said)
        and set(after_race.records) == {("conformance", k1), ("unit", k2), ("unit", k3), ("unit", k4)},
        f"{raced} {recovered} {after_race.commit} {sorted(after_race.records)} {said}")
    later = publish(host.path, Store(root / "t-store-empty"), "origin", now + timedelta(days=PRUNE_DAYS + 1),
                    say=said.append)
    pruned = fetch_published(ci.path, "origin")
    arm("two publishers' records are a union on the ref (neither loses the other's); a push leased on a commit the ref "
        "no longer names is refused; publishing nothing new pushes nothing; a record older than PRUNE_DAYS is pruned, "
        "leaving the ref on an empty tree",
        set(both.records) == {("conformance", k1), ("unit", k2)} and stale.returncode != 0
        and again == second and any("nothing pushed" in s for s in said)
        and later is not None and later != second and pruned.commit == later and pruned.records == {}
        and remote_store_commit(ci.path, "origin") == later,
        f"{sorted(both.records)} stale={stale.returncode} {again} {second} {later} {pruned.commit} {said}")

#: The one-home rule's anchors as the self-test plants them (`root_anchors` reads the real list).
ANCHORS = ("AppContext.BaseDirectory", "AppDomain.CurrentDomain.BaseDirectory", "Directory.GetCurrentDirectory(")


def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    isolate()
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

    def test(name: str, project: str, inputs: tuple[str, ...], exclude: tuple[str, ...] = ("@inert", "@tooling-lane"),
             depends: tuple[str, ...] = ()) -> TestTarget:
        return TestTarget(name=name, assembly=name.capitalize(), project=project, inputs=inputs, exclude=exclude,
                          facets=("platform", "configuration", "toolchain"), depends=depends)

    root = scratch_dir("buildgraph-selftest-")
    try:
        repo = ScratchRepo.init(root / "repo")
        files = {
            "src/A.cs": "class A {}\n", "docs/n.md": "# n\n", "kb/Work/X.md": "x\n",
            "conformance-suite/cases/c.cob": "c\n", "tests/version-matrix/inventory/r.json": "{}\n",
            "tests/version-matrix/constructs/k.json": "{}\n", "scripts/orchestrator/planted_tool.py": "x = 1\n",
            "tests/_shared/TestRepo.cs": "static class TestRepo {}\n",
        }
        repo.commit("base", files)
        targets = {
            "unit": test("unit", "tests/U/", ("@population-inputs",)),
            "conformance": test("conformance", "tests/C/",
                                ("src/", "conformance-suite/", "tests/version-matrix/constructs/")),
        }
        facets = {"platform": "windows", "configuration": "Debug", "toolchain": "t1"}

        def keys_after(edit: dict[str, str], f: dict[str, str] = facets) -> dict[str, str]:
            repo.commit("edit", {**files, **edit}, allow_empty=True)
            entries = tree_entries(repo.path, repo.git("rev-parse", "HEAD^{tree}"))
            return {n: key(t, entries, f) for n, t in targets.items()}

        k0 = keys_after({})
        inv = keys_after({"tests/version-matrix/inventory/r.json": '{"v": 2}\n'})
        doc = keys_after({"docs/n.md": "# edited\n", "kb/Work/X.md": "y\n", "scripts/orchestrator/planted_tool.py": "x=2\n"})
        src = keys_after({"src/A.cs": "class A { int b; }\n"})
        tool = keys_after({}, {**facets, "toolchain": "t2"})
        plat = keys_after({}, {**facets, "platform": "linux"})
        arm("a change to an input of one target only (the inventory) changes that target's key and not the other's",
            inv["unit"] != k0["unit"] and inv["conformance"] == k0["conformance"], f"{k0} {inv}")
        arm("a docs, kb or tooling-lane change leaves every key; a src/ change, another toolchain or another platform "
            "changes every key",
            doc == k0 and all(src[t] != k0[t] and tool[t] != k0[t] and plat[t] != k0[t] for t in targets), f"{doc} {src}")
        store_a, store_b = Store(root / "store"), Store(root / "store")
        store_a.record("conformance", k0["conformance"], {"worktree": "A", "run": "000001", "counts": {"ran": 3}})
        arm("the store: a pass recorded at a key (worktree A) is up to date at that key for another reader (worktree B), "
            "and at no other key or target",
            store_b.up_to_date("conformance", k0["conformance"]) and not store_b.up_to_date("conformance", src["conformance"])
            and not store_b.up_to_date("unit", k0["conformance"])
            and store_b.lookup("conformance", k0["conformance"])["worktree"] == "A", str(list((root / "store").rglob("*"))))
        bad_path = Store(root / "store")._path("unit", k0["unit"])
        bad_path.parent.mkdir(parents=True, exist_ok=True)
        bad_path.write_text(json.dumps({"target": "unit", "key": k0["unit"], "result": "red"}), encoding="utf-8",
                            newline="\n")
        arm("a record that is not a pass is not up to date", not store_b.up_to_date("unit", k0["unit"]))
        real = Store.for_repo(repo.path)
        wt = repo.worktree(root / "linked", detach=True)
        arm("every worktree of a repository has the ONE store (its git common directory)",
            Store.for_repo(wt.path).dir == real.dir and real.dir.name == STORE_DIR, f"{real.dir} {Store.for_repo(wt.path).dir}")

        # the declared-inputs audit, on planted sources
        conf = test("conformance", "tests/C/", ("src/", "conformance-suite/", "tests/nist/"), ("@inert",))
        planted = ('class T { void M() { var a = TestRepo.Nist("corpus.tsv"); var b = TestRepo.Suite("harness", x);\n'
                   ' var c = Path.Combine(TestRepo.Root, "conformance-suite", "cases"); var d = "src/X/Y.cs"; } }\n')
        undeclared_read = planted + 'class U { string e = TestRepo.VersionMatrix("inventory", id); }\n'
        top = ["src", "tests", "conformance-suite", "docs", "scripts"]
        audit = ScratchRepo.init(root / "audit")
        tdir = audit.path
        tracked_data = {"tests/nist/corpus.tsv": "x\n", "conformance-suite/harness/run.py": "x\n",
                        "conformance-suite/harness/gen.py": "x\n",
                        "conformance-suite/cases/c.cob": "c\n", "src/X/Y.cs": "class Y {}\n",
                        "docs/rearchitecture/x.json": "{}\n", "tests/version-matrix/inventory/r.json": "{}\n",
                        "tests/_shared/Helper.cs":
                            'static class Helper { static string P = TestRepo.Docs("rearchitecture", "x.json"); }\n',
                        "tests/C/T.cs": planted}
        audit.commit("planted", tracked_data)
        def undeclared_now(t: TestTarget = conf, deps: tuple[GeneratedTarget, ...] = ()) -> Scan:
            return scan(tdir, t, top, _tracked(tdir), dependencies=deps, anchors=ANCHORS)

        clean = undeclared_now().undeclared
        audit.commit("undeclared", {"tests/C/U.cs": undeclared_read})
        planted_bad = undeclared_now().undeclared
        audit.commit("shared", {"tests/C/V.cs": "class V { void M() { Helper.Use(); } }\n"})
        through_shared = undeclared_now().undeclared
        arm("the BUILD TARGETS audit: sources whose reads lie in the declared inputs are clean; a planted read of "
            "tests/version-matrix/inventory (undeclared) is a finding naming file and line; a tests/_shared file is "
            "audited exactly when the assembly uses a type it declares",
            clean == () and planted_bad == ("tests/C/U.cs:3: tests/version-matrix/inventory/",)
            and "tests/_shared/Helper.cs:1: docs/rearchitecture/x.json" in through_shared, f"{clean} {planted_bad} {through_shared}")

        # a listing pattern matches as the test's .NET listing does: case-insensitive (review R1-2)
        guards = test("conformance", "tests/C/", ("scripts/guard*.sh",), ())
        cased = [guards.matches(p) for p in ("scripts/guard-a.sh", "scripts/Guard-legacy.sh", "scripts/guard-x.SH",
                                             "scripts/sub/guard-a.sh", "scripts/other.sh")]
        g_repo = ScratchRepo.init(root / "guards")
        g_repo.commit("base", {"scripts/guard-a.sh": "a\n"})
        g0 = key(guards, tree_entries(g_repo.path, g_repo.git("rev-parse", "HEAD^{tree}")), facets)
        g_repo.commit("cased", {"scripts/guard-a.sh": "a\n", "scripts/Guard-legacy.sh": "b\n"})
        g1 = key(guards, tree_entries(g_repo.path, g_repo.git("rev-parse", "HEAD^{tree}")), facets)
        arm("a listing pattern (scripts/guard*.sh) matches a differently cased file and not one below or beside it, and "
            "adding scripts/Guard-legacy.sh changes the key",
            cased == [True, True, True, False, False] and g0 != g1, f"{cased} {g0} {g1}")
        transport_arms(root, arm)
        real_graph = load_targets()
        rt = real_graph.targets
        arm("the real targets file loads: unit, conformance, characterization and the generated conformance-suite, which "
            "conformance depends on and which comes first; @build expanded; the inventory is the generator's input and "
            "not conformance's",
            sorted(real_graph.tests()) == ["characterization", "conformance", "repo-drift", "unit"]
            and list(real_graph.generated()) == ["conformance-suite"] and rt["conformance"].depends == ("conformance-suite",)
            and real_graph.order.index("conformance-suite") < real_graph.order.index("conformance")
            and "src/" in rt["conformance"].inputs and "@build" not in rt["conformance"].inputs
            and "src/" in rt["unit"].inputs and not rt["unit"].broad and rt["repo-drift"].broad
            and rt["conformance-suite"].matches("tests/version-matrix/inventory/r.json")
            and not rt["conformance"].matches("tests/version-matrix/inventory/r.json")
            and not rt["conformance"].matches("conformance-suite/harness/generate.py")
            and rt["conformance"].matches("conformance-suite/harness/run.py"), str(real_graph.order))

        # ── generated targets (kb/Work PB5231) ──
        def gen_target(name: str = "suite", depends: tuple[str, ...] = ()) -> GeneratedTarget:
            return GeneratedTarget(name=name, inputs=("gen/", "inv/"), exclude=("@inert", "@tooling-lane"),
                                   facets=("toolchain",), depends=depends,
                                   command=("gen/make.py", "--out", OUT), outputs=("suite/manifest.json",),
                                   env="WISEOWL_COBOL_GATE_SUITE")

        def graph_of(*ts: Target) -> Graph:
            return Graph.of({t.name: t for t in ts}, ("platform", "configuration", "toolchain"))

        def refused(thunk: Callable[[], object]) -> str:
            try:
                thunk()
            except (GraphError, BuildError) as e:
                return f"{type(e).__name__}: {e}"
            return ""

        consumer = test("conformance", "tests/C/", ("src/", "tests/C/"), depends=("suite",))
        graph = graph_of(gen_target(), consumer)
        g = ScratchRepo.init(root / "gen")
        gfiles = {".gitignore": "TestResults/\n__pycache__/\n", "gen/make.py": PLANTED_GENERATOR,
                  "inv/r1.json": '{"id": "r1", "verdict": "GAP"}\n', "src/A.cs": "class A {}\n",
                  "tests/C/T.cs": "class T {}\n", "docs/n.md": "# n\n"}
        g.commit("base", gfiles)
        gstore = Store(root / "gen-store")

        def at_head(where: Path = g.path, store: Store = gstore) -> tuple[dict[str, Generation], dict[str, str]]:
            entries = tree_entries(where, g.git("rev-parse", "HEAD^{tree}", cwd=where))
            gens = bring_up_to_date(where, graph, entries, facets, store)
            return gens, keys(graph, entries, facets, {n: x.outputs for n, x in gens.items()})

        g0s, gk0 = at_head()
        g1s, gk1 = at_head()
        arm("a generated target's first run is traced and recorded; a second at the same key, its outputs on disk "
            "unchanged, runs nothing",
            g0s["suite"].ran and not g1s["suite"].ran and gk0 == gk1
            and gstore.lookup("suite", gk0["suite"])["outputs"] == g0s["suite"].outputs, f"{g0s} {g1s}")
        g.commit("verdict", {"inv/r1.json": '{"id": "r1", "verdict": "PASS"}\n'})
        g2s, gk2 = at_head()
        arm("an inventory edit that leaves the generator's output unchanged re-runs the generator (its key changed) and "
            "leaves the dependent's key unchanged",
            gk2["suite"] != gk0["suite"] and g2s["suite"].ran and g2s["suite"].outputs == g0s["suite"].outputs
            and gk2["conformance"] == gk0["conformance"], f"{gk0} {gk2}")
        g.commit("row", {"inv/r2.json": '{"id": "r2", "verdict": "GAP"}\n'})
        g3s, gk3 = at_head()
        arm("an inventory edit that changes the generator's output changes the dependent's key",
            g3s["suite"].outputs != g2s["suite"].outputs and gk3["conformance"] != gk2["conformance"], f"{gk2} {gk3}")
        g.commit("generator", {"gen/make.py": PLANTED_GENERATOR + "# a comment\n"})
        g4s, gk4 = at_head()
        arm("a generator source edit re-runs the generator; its unchanged output leaves the dependent's key",
            gk4["suite"] != gk3["suite"] and g4s["suite"].ran and gk4["conformance"] == gk3["conformance"], f"{gk3} {gk4}")
        (g.path / GENERATED_ROOT / "suite" / "suite" / "manifest.json").write_text("{}\n", encoding="utf-8", newline="\n")
        g5s, gk5 = at_head()
        arm("outputs on disk that are not the recorded ones are generated again, to the recorded digest",
            g5s["suite"].ran and g5s["suite"].outputs == g4s["suite"].outputs and gk5 == gk4, g5s["suite"].why)
        second = g.worktree(root / "second-path" / "wt", detach=True)
        g6s, gk6 = at_head(second.path)
        arm("a second worktree at another path generates the same output digest (no absolute path, time or order in it), "
            "so its keys are the first's",
            g6s["suite"].ran and g6s["suite"].outputs == g4s["suite"].outputs and gk6 == gk4, f"{gk4} {gk6}")
        cycle = refused(lambda: graph_of(gen_target("a", ("b",)), gen_target("b", ("a",))))
        unknown = refused(lambda: graph_of(test("conformance", "tests/C/", ("src/",), depends=("nope",))))
        on_test = refused(lambda: graph_of(test("unit", "tests/U/", ("src/",)),
                                           test("conformance", "tests/C/", ("src/",), depends=("unit",))))
        arm("a dependency cycle is refused, naming it; so is a dependency on an undeclared target or on a test target",
            cycle.startswith("GraphError: a dependency cycle: a -> b -> a") and "does not declare" in unknown
            and "only a generated target" in on_test, f"{cycle} | {unknown} | {on_test}")
        unresolved = refused(lambda: keys(graph, tree_entries(g.path, "HEAD"), facets, stored_outputs(Store(root / "empty"))))
        arm("a missing dependency record is not up to date: the dependent's key is UNRESOLVED, naming the dependency, "
            "never a key without it",
            unresolved.startswith("Unresolved: conformance's key needs the output digest of suite"), unresolved)
        g.commit("nondeterministic", {"gen/NONDET": "x\n"})
        entries_nd = tree_entries(g.path, g.git("rev-parse", "HEAD^{tree}"))
        k_nd = key(graph.targets["suite"], entries_nd, facets)
        first_nd = generate(g.path, graph.targets["suite"], k_nd, gstore, {p for _, p in entries_nd})
        remove_tree(g.path / GENERATED_ROOT / "suite")   # the same worktree, the same inputs: only the time differs
        nondet = refused(lambda: generate(g.path, graph.targets["suite"], k_nd, gstore, {p for _, p in entries_nd}))
        arm("a nondeterministic output (planted: the time) is refused when a second run at the key does not reproduce "
            "the recorded digest",
            first_nd.ran and nondet.startswith("Nondeterministic: suite is NOT DETERMINISTIC"), nondet)
        g.commit("deterministic again", {"gen/NONDET": None, "gen/READ_DOCS": "x\n", "docs/n.md": "# m\n"})
        entries_rd = tree_entries(g.path, g.git("rev-parse", "HEAD^{tree}"))
        rd_target = replace(graph.targets["suite"], inputs=("gen/", "inv/", "docs/"))
        undeclared_doc = refused(lambda: generate(g.path, graph.targets["suite"], "k-undeclared", gstore,
                                                  {p for _, p in entries_rd}))
        inert_doc = refused(lambda: generate(g.path, rd_target, "k-inert", gstore, {p for _, p in entries_rd}))
        arm("a traced generation that opens a tracked path outside its inputs is refused, naming it; so is one that "
            "reads an input its key excludes as @inert",
            "UndeclaredRead" in undeclared_doc and "opened docs/n.md" in undeclared_doc
            and "opened docs/n.md" in inert_doc and gstore.lookup("suite", "k-undeclared") is None,
            f"{undeclared_doc} | {inert_doc}")
        g.commit("undeclared output", {"gen/READ_DOCS": None,
                                       "gen/make.py": PLANTED_GENERATOR + "(out / 'stray.txt').write_text('x')\n"})
        entries_uo = tree_entries(g.path, g.git("rev-parse", "HEAD^{tree}"))
        stray = refused(lambda: generate(g.path, graph.targets["suite"], "k-stray", gstore, {p for _, p in entries_uo}))
        g.commit("deterministic", {"gen/make.py": PLANTED_GENERATOR})
        entries_ok = tree_entries(g.path, g.git("rev-parse", "HEAD^{tree}"))
        moved = refused(lambda: generate(g.path, graph.targets["suite"], "k-moved", gstore, {p for _, p in entries_ok},
                                         stable=lambda: False))
        arm("an output the target does not declare is refused (Unresolved, naming it); a run while the tree changed "
            "records nothing (TreeMoved)",
            "Unresolved" in stray and "stray.txt" in stray and moved.startswith("TreeMoved")
            and gstore.lookup("suite", "k-moved") is None and gstore.lookup("suite", "k-stray") is None,
            f"{stray} | {moved}")

        # ── review round 1 (kb/Work PB5231): imports from a warm cache, the static check, the one hand-off site ──
        imp = ScratchRepo.init(root / "imp")
        importer = ("import sys\nfrom pathlib import Path\nhere = Path(__file__).resolve().parent\n"
                    "sys.path.insert(0, str(here.parent / 'lib'))\nimport decl, helper\n"
                    "out = Path(sys.argv[sys.argv.index('--out') + 1])\n"
                    "(out / 'm.json').write_text(f'{decl.D} {helper.VALUE}', encoding='utf-8')\n")
        imp.commit("base", {".gitignore": "TestResults/\n", "lib/decl.py": "D = 1\n", "lib/helper.py": "VALUE = 1\n",
                            "gen/make.py": importer})
        it = GeneratedTarget(name="imp", inputs=("gen/", "lib/decl.py"), exclude=(), facets=(),
                             command=("gen/make.py", "--out", OUT), outputs=("m.json",), env="WISEOWL_COBOL_GATE_IMP")
        subprocess.run([sys.executable, "-c", "import sys; sys.path.insert(0, 'lib'); import helper"], cwd=imp.path,
                       check=True)   # another process leaves lib/__pycache__/helper.*.pyc behind: the import is warm
        ientries = tree_entries(imp.path, imp.git("rev-parse", "HEAD^{tree}"))
        warm = refused(lambda: generate(imp.path, it, "k-warm", Store(root / "imp-store"), {p for _, p in ientries}))
        imp_tracked = {p for _, p in ientries}
        static_imp = static_check(imp.path, it, imp_tracked)
        declared_imp = static_check(imp.path, replace(it, inputs=("gen/", "lib/")), imp_tracked)
        arm("an undeclared import that is already cached is refused by the trace (the run's own empty bytecode cache "
            "makes it open the source); the static import closure refuses it without running anything, and is clean "
            "once declared",
            any((imp.path / "lib" / "__pycache__").glob("helper.*.pyc")) and "UndeclaredRead" in warm
            and "opened lib/helper.py" in warm
            and any("imports lib/helper.py" in f for f in static_imp) and declared_imp == [],
            f"{warm} | {static_imp} | {declared_imp}")
        loader = ("import sys, importlib.util\nfrom pathlib import Path\nhere = Path(__file__).resolve().parent\n"
                  "s = importlib.util.spec_from_file_location('h', here.parent / 'lib' / 'helper.py')\n"
                  "helper = importlib.util.module_from_spec(s)\ns.loader.exec_module(helper)\n"
                  "out = Path(sys.argv[sys.argv.index('--out') + 1])\n"
                  "(out / 'm.json').write_text(str(helper.VALUE), encoding='utf-8')\n")
        imp.commit("hook loader", {"gen/make.py": loader})
        subprocess.run([sys.executable, "-c", "import importlib.util\n"
                        "s = importlib.util.spec_from_file_location('h', 'lib/helper.py')\n"
                        "s.loader.exec_module(importlib.util.module_from_spec(s))"], cwd=imp.path, check=True)
        hentries = tree_entries(imp.path, imp.git("rev-parse", "HEAD^{tree}"))
        hooked = refused(lambda: generate(imp.path, it, "k-hook", Store(root / "imp-store"), {p for _, p in hentries}))
        arm("a module loaded by spec_from_file_location + exec_module (never in sys.modules), with a warm cache in the "
            "default location, is refused by the trace: the run opens its source",
            "UndeclaredRead" in hooked and "opened lib/helper.py" in hooked
            and Store(root / "imp-store").lookup("imp", "k-hook") is None, hooked)
        probing = ("from pathlib import Path\nhere = Path(__file__).resolve().parent\n"
                   "def probe(p):\n    return p.is_file()\n"
                   "def self_test():\n    return Path('anything').exists()\n"
                   "x = (here.parent / 'docs' / 'x.md').exists()\n")
        imp.commit("probes", {"gen/make.py": importer + probing})
        pt = replace(it, inputs=("gen/", "lib/"))
        ptracked = {p for _, p in tree_entries(imp.path, "HEAD")}
        undecided = static_check(imp.path, pt, ptracked)
        judged_ok = static_check(imp.path, replace(pt, inputs=("gen/", "lib/", "docs/"),
                                                   judged=(("gen/make.py|probe|p.is_file()", "its callers pass inputs"),)),
                                 ptracked)
        stale = static_check(imp.path, replace(pt, inputs=("gen/", "lib/", "docs/"),
                                               judged=(("gen/make.py|probe|p.is_file()", "x"), ("gen/make.py|gone|q()", "y"))),
                             ptracked)
        arm("the static check refuses an existence probe of a path outside the inputs that it decides from literals, "
            "and one it cannot decide until it is judged with its reason; a self-test's probe is not read; a stale "
            "judgment is a finding",
            any("gen/make.py:" in f and "probes docs/x.md" in f for f in undecided)
            and any("gen/make.py|probe|p.is_file()" in f for f in undecided)
            and not any("anything" in f for f in undecided) and judged_ok == []
            and any("gen/make.py|gone|q()" in f and "names no undecided call" in f for f in stale),
            f"{undecided} | {judged_ok} | {stale}")
        site = ("class S { const string V = \"WISEOWL_COBOL_GATE_SUITE\"; const string M = \"GENERATED-OWN  suite\";\n"
                " void G() { PythonInstrument.Run(B, TestRepo.Suite(\"harness\", \"generate.py\"), \"--out\", d); } }\n")
        second = ('class G { void T() { PythonInstrument.Run(B, TestRepo.Suite("harness", "generate.py"), '
                  '"--check"); } }\n')
        audit.commit("hand-off site", {"conformance-suite/harness/generate.py": "x\n", "tests/D/Site.cs": site})
        gsuite = GeneratedTarget(name="suite", inputs=("conformance-suite/", "tests/version-matrix/"), exclude=(),
                                 facets=(), command=("conformance-suite/harness/generate.py", "--out", OUT),
                                 outputs=("m.json",), env="WISEOWL_COBOL_GATE_SUITE")
        dep = test("conformance", "tests/D/", ("tests/D/", "conformance-suite/"),
                   ("conformance-suite/harness/generate.py",), ("suite",))
        one_site = undeclared_now(dep, (gsuite,))
        audit.commit("second site", {"tests/D/GeneratorCheck.cs": second})
        two_sites = undeclared_now(dep, (gsuite,))
        arm("the generator's program is reached through the dependency only at its ONE hand-off site (the source naming "
            "its variable and its own-generation mark); a second source that runs it is an undeclared read of "
            "everything it reads",
            one_site.undeclared == () and len(one_site.through) == 1 and one_site.handed == ("WISEOWL_COBOL_GATE_SUITE",)
            and any(u.startswith("tests/D/GeneratorCheck.cs:1: conformance-suite/harness/generate.py (the program of "
                                 "suite") for u in two_sites.undeclared), f"{one_site} | {two_sites}")
        # The audit's reading of a C# source (kb/Work PB5230): a root as text is no read; a collection expression, a
        # declared Within directory and a suite case's trees are read; a whole-tree scope is a bare root reference.
        spelled = reads_in(
            'var a = Path.GetRelativePath(TestRepo.Root, f); var b = $"at {TestRepo.Root}";\n'
            'var c = TestRepo.At(["docs", "x.json"]); var d = Path.Combine([TestRepo.Root, "src", .. rest]);\n'
            'var e = TestRepo.Within("src/A/", rel); var g = TestRepo.SuiteCase(id);\n'
            'static readonly TreeScope All = new(Under: [""], Kinds: [".cs"]);\n'
            'var h = TestRepo.At(rel);\n', ["src", "docs", "tests", "conformance-suite"])
        arm("the audit reads a source as written: a root made relative or interpolated is no read; At([..]) and "
            "Path.Combine([Root, ..]) are read through their literals; Within reads its declared directory; SuiteCase "
            "its two trees; a whole-tree scope and a computed At are bare root references",
            spelled == [(2, "docs/x.json"), (2, "src/"), (3, "src/A/"), (3, "conformance-suite/cases/"),
                        (3, "conformance-suite/wiseowl/cases/"), (5, ""), (3, "src/A/"), (4, "")], str(spelled))

        # Review round 1 (kb/Work PB5230): what the reviewer planted, each a read the audit now SEES.
        tops = ["src", "docs", "tests", "conformance-suite"]
        interp = reads_in(
            'var a = File.ReadAllText($"{TestRepo.Root}/tests/d/x.out");\n'
            'var b = $"carries the root \'{TestRepo.Root}\'; pass a relative path";\n'
            'var c = File.ReadAllText($"{TestRepo.Root}");\n'
            'var d = new[] {TestRepo.Root};\n'
            '// a comment naming TestRepo.Root\n'
            'var e = "TestRepo.Root inside a literal";\n'
            'var f = File.ReadAllText(TestRepo.At($"tests/d/{name}"));\n'
            'var g = Path.Combine(TestRepo.Root, $"tests/d/{name}");\n', tops)
        arm("R1-1/R1-2: an interpolated hole followed by a separator reads the literal path after it; one followed by "
            "text is text; a whole-string hole and a hole outside a string are bare roots; a comment or a literal's text "
            "is nothing; an interpolated argument reads its literal head's directory",
            interp == [(1, "tests/d/x.out"), (3, ""), (4, ""), (7, "tests/d/"), (8, "tests/d/"), (7, "tests/d/"),
                       (8, "tests/d/")], str(interp))
        scopes = reads_in('static readonly TreeScope A = new(Under: ["tests"], Kinds: [".out"]);\n'
                          'static readonly TreeScope B = new(Under: ["src/A", "docs/"]);\n', tops)
        arm("R1-3: every Under prefix of a TreeScope is a read: a directory prefix that directory, a partial prefix the "
            "directory holding it, a top-level one (`tests`) the whole tree",
            # (the literal "src/A" is also read as a path by the literal rule; the scope rule adds its directory)
            sorted(scopes) == sorted([(1, ""), (2, "src/A"), (2, "src/"), (2, "docs/"), (2, "docs/")]), str(scopes))
        alias_decl = 'internal static class Holder\n{\n    public static string HostRoot => TestRepo.Root;\n' \
                     '    static void M() => Use(HostRoot);\n}\n'
        aliases = root_aliases({"Holder.cs": alias_decl})
        used_alias = reads_in('var x = File.ReadAllText(Path.Combine(Holder.HostRoot, "tests", "d", f));\n', tops, aliases)
        arm("R1-5: a member that IS the root is followed to every use: `Holder.HostRoot` in Path.Combine reads its "
            "literal segments; the bare alias in its own file is a bare root, its declaration is not",
            used_alias == [(1, "tests/d/")] and reads_in(alias_decl, tops) == [(4, "")], f"{used_alias} {aliases}")

        # The verdict: a bare root in a NARROW target is a finding; in a broad one it is counted; a judgment covers its
        # statement only, and a judgment nothing matches is stale.
        narrow = replace(test("n", "tests/N/", ("src/",), ()),
                         judged=(("tests/N/A.cs|var j = TestRepo.At(judged);", "planted"),
                                 ("tests/N/A.cs|var gone = TestRepo.At(x);", "planted")))
        broad = test("b", "tests/B/", ("@population-inputs",), ())
        vrepo = ScratchRepo.init(root / "verdict")
        vrepo.commit("planted", {"src/S.cs": "class S {}\n",
                                 "tests/N/A.cs": "var j = TestRepo.At(judged);\nvar k = TestRepo.At(other);\n",
                                 "tests/B/B.cs": "var k = TestRepo.At(other);\n"})
        vtop, vtracked = _top_dirs(vrepo.path), _tracked(vrepo.path)
        vt = {"n": narrow, "b": broad}
        vf = findings_of(vt, {k: scan(vrepo.path, t, vtop, vtracked, anchors=ANCHORS) for k, t in vt.items()})
        arm("R1-1: a bare root in a narrowly declared target is a finding naming file and line; its judged statement is "
            "not; in a broad target it is only counted; a judgment no source matches is a stale finding",
            len(vf) == 2 and any("tests/N/A.cs:2: var k = TestRepo.At(other);" in f for f in vf)
            and any("stale judgment" in f and "gone" in f for f in vf) and not any("tests/B/" in f for f in vf), str(vf))

        # Review round 2 (kb/Work PB5230): a `..` is resolved before the read is judged, a climb out of the repository is
        # refused, a read naming no tracked path is reported unless it is a build output or ignored, and a statement that
        # reaches the file system around TestRepo and ScratchDirectory is refused unless judged.
        dots = reads_in('var a = TestRepo.Src("..", "tests", "d", "x.out");\nvar b = TestRepo.At("src", ".", "S.cs");\n'
                        'var c = TestRepo.At("..", "outside.txt");\n', tops)
        arm("R2-1: a read's `.` and `..` segments are resolved before it is judged: Src(\"..\", \"tests\", ..) reads tests/, "
            "At(\"src\", \".\", ..) reads src/, and a read above the repository keeps its leading ../",
            dots == [(1, "tests/d/x.out"), (2, "src/S.cs"), (3, "../outside.txt")], str(dots))
        planted = ['var a = File.ReadAllText(TestRepo.Src("..", "tests", "D", "x.out"));',
                   'var b = TestRepo.At("..", "outside.txt");',
                   'var c = TestRepo.Src("Missing.cs");',
                   'var d = TestRepo.Src("bin", "Debug", "S.dll");',
                   'var e = TestRepo.Src("cache", "k.json");',
                   'var f = AppContext.BaseDirectory;',
                   'var g = File.ReadAllText("relative.txt");',
                   'var h = Path.Combine("/etc", "passwd");',
                   'var i = Path.Combine(dir, "a"); // AppContext.BaseDirectory, in a comment',
                   'var j = Path.Combine(Environment.GetEnvironmentVariable("X")!, "kb");',
                   'var k = typeof(S).Assembly.Location;',
                   'var l = Environment.CurrentDirectory;',
                   'var m = """AppContext.BaseDirectory, in a raw literal""";',
                   'var n = Directory.GetCurrentDirectory();',
                   'var o = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("X")!, "kb");',
                   'var p = System.IO.File.ReadAllText("relative.txt"); var q = u.File.StartsWith("x");']
        rrepo = ScratchRepo.init(root / "round2")
        rrepo.commit("planted", {".gitignore": "cache/\n", "src/S.cs": "class S {}\n", "tests/D/x.out": "x\n",
                                 "tests/R/A.cs": "\n".join(planted) + "\n"})
        r2 = {"n": replace(test("n", "tests/R/", ("src/",), ()),
                           judged=(("tests/R/A.cs|var k = typeof(S).Assembly.Location;", "planted"),)),
              "b": test("b", "tests/R/", ("@population-inputs",), ())}
        rtop, rtracked = _top_dirs(rrepo.path), _tracked(rrepo.path)
        rf = findings_of(r2, {k: scan(rrepo.path, t, rtop, rtracked, anchors=ANCHORS) for k, t in r2.items()})
        want = {1: "tests/D/x.out", 2: "climbs out of the repository", 3: "src/Missing.cs names no tracked path",
                6: "one-home rule", 7: "a literal path handed to System.IO", 8: "a literal path handed to System.IO",
                10: "an environment variable forming a path", 12: "the working directory", 14: "one-home rule",
                15: "an environment variable forming a path", 16: "a literal path handed to System.IO"}
        got = {int(f.split("tests/R/A.cs:")[1].split(":")[0]): f for f in rf if f.startswith("n: tests/R/A.cs:")}
        arm("R2-1/R2-2: in a narrow target a read through `..` is judged at its resolved path (tests/D/x.out, undeclared), "
            "a climb out of the repository and a read naming no tracked path are findings, a build output and an ignored "
            "path are dropped; each statement reaching the file system around TestRepo (an anchor of the one-home rule, "
            "the working directory, a literal or rooted path handed to System.IO, an environment variable forming a "
            "path) is a finding with its line, a judged one, a comment and a raw literal's text are not; a broad target "
            "is refused only the climb out, which no declaration can cover",
            sorted(got) == sorted(want) and all(want[n] in got[n] for n in want) and len(got) == len(want)
            and [f for f in rf if f.startswith("b: ")] == ["b: tests/R/A.cs:2: ../outside.txt (climbs out of the "
                                                           "repository: no target can declare it)"], "\n".join(rf))
        home = ScratchRepo.init(root / "onehome")
        home.commit("planted", {ONE_HOME: 'class T { static readonly string[] Anchors =\n    ["A.Base", "B.Cwd("];\n}\n'})
        bare = ScratchRepo.init(root / "nohome")
        bare.commit("planted", {ONE_HOME: "class T { }\n"})
        arm("R2-2: the anchors are read from the one-home rule's own list (TestRepoDriftTests.Anchors); a rule with no "
            "list is a failure naming it, never an empty list that refuses nothing",
            root_anchors(home.path) == ("A.Base", "B.Cwd(") and "repoint buildgraph.ONE_HOME" in refused(
                lambda: root_anchors(bare.path)), str(root_anchors(home.path)))

        # Review round 3 (kb/Work PB5230): a verbatim literal ending in an escaped quote (`@"x"""`) holds three quotes;
        # the three-quote pattern took two such literals for one raw literal and hid every line of code between them.
        q = '"'
        verb = (f"static readonly Regex A = new(@{q}x{q * 3});\n"
                f"var a = File.ReadAllText(Path.Combine(System.AppContext.BaseDirectory, {q}../../x.out{q}));\n"
                "var b = File.ReadAllText(Path.Combine(TestRepo.Root, Name()));\n"
                f"static readonly Regex D = new(@{q}y{q * 3});\n")
        vb, vr = bypasses_in(verb, ANCHORS), reads_in(verb, tops)
        arm("R3-1: code between two verbatim literals ending in an escaped quote is code: its bypass and its bare root "
            "are found", [n for n, _ in vb] == [2] and vr == [(3, "")], f"{vb} {vr}")
        # The lexer's own arms (every literal form, as `kind_at` reports it) are scripts/selftests/csharp_lexer/test_lexer.py.
    finally:
        remove_tree(root)
    bad = [n for n, ok, _ in results if not ok]
    print(f"=== buildgraph SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(bad)}" if bad else "") + ") ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd")
    k = sub.add_parser("keys", help="print each target's key at a tree (a generated target's output from the store)")
    k.add_argument("--tree", default="HEAD^{tree}")
    k.add_argument("--repo", default=str(REPO))
    k.add_argument("--toolchain", required=True)
    k.add_argument("--platform", default="windows" if os.name == "nt" else "linux")
    k.add_argument("--configuration", default="Debug")
    d = sub.add_parser("digest", help="print the output digest of a generated target's output directory")
    d.add_argument("--target", required=True)
    d.add_argument("directory")
    sub.add_parser("check", help="the BUILD TARGETS audit")
    pub = sub.add_parser("publish", help=f"send this host's passes to the remote's {STORE_REF} (the landing's push)")
    pub.add_argument("--remote", default="origin")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.cmd == "check":
        return check()
    if args.cmd == "publish":
        publish(REPO, Store.for_repo(REPO), args.remote)
        return 0
    if args.cmd == "digest":
        t = load_targets().generated().get(args.target)
        if t is None:
            print(f"no generated target {args.target!r} in {TARGETS_FILE}")
            return 2
        try:
            digest, n = output_digest(Path(args.directory), t)
        except Unresolved as e:
            print(f"UNRESOLVED — {e}")
            return 1
        print(f"{t.name} {digest} ({n} file(s))")
        return 0
    if args.cmd == "keys":
        repo = Path(args.repo)
        facets = {"platform": args.platform, "configuration": args.configuration, "toolchain": args.toolchain}
        try:
            got = keys(load_targets(), tree_entries(repo, args.tree), facets, stored_outputs(Store.for_repo(repo)))
        except Unresolved as e:
            print(f"UNRESOLVED — {e}")
            return 1
        for name, value in got.items():
            print(f"{name} {value}")
        return 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
