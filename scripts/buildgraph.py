#!/usr/bin/env python3
"""buildgraph.py — THE BUILD'S GRAPH: ITS TARGETS, THEIR KEYS, THEIR DEPENDENCIES AND THE ONE STORE OF WHAT EACH PROVED
(kb/Work PB5207 stages 1 and 2, PB5218, PB5231).

    python scripts/buildgraph.py keys --toolchain S [--tree T] [--platform P] [--configuration C]
                                                # each target's key; a generated dependency's output from the store
    python scripts/buildgraph.py digest --target NAME DIR   # the output digest of a generated target's output directory
    python scripts/buildgraph.py check          # the BUILD TARGETS audit: every repository read a test source names
                                                # lies in its assembly's declared inputs (or is its dependency's run)
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
opens for reading, every directory it lists and every process it starts. A tracked path it opened that its inputs do not
match, a tracked directory it listed that no input lies in, or any process it started (whose reads no hook sees) refuses
the generation by name (`undeclared`): its declared inputs are verified at every run, not trusted.

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
it is the PROGRAM of a target the assembly depends on (the read that runs the dependency when no gate handed its output:
a read reached only through the generator belongs to the generator's target). A test target that depends on a generated
target must name, in its sources, the environment variable the gate hands the output in (`GeneratedTarget.env`), or its
hosts would make their own. Its residue, named: a path computed at run time from a non-literal, and what a process the
test starts reads; the dynamic proof (the Linux shards' file watch, per target) is the remaining work kb/Work PB5218 states.
"""
from __future__ import annotations

import argparse
import datetime as dt
import fnmatch
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
from collections.abc import Callable, Mapping
from dataclasses import dataclass, replace
from pathlib import Path
from typing import ClassVar

sys.path.insert(0, str(Path(__file__).resolve().parent))
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
    targets: dict[str, Target] = {}
    for name, t in data["targets"].items():
        inputs: list[str] = []
        for p in t["inputs"]:
            inputs += list(build) if p == "@build" else [p]
        common = dict(name=name, inputs=tuple(inputs), exclude=tuple(t.get("exclude") or ()),
                      facets=tuple(t.get("facets") or facets), depends=tuple(t.get("depends") or ()),
                      judged=tuple((k, v) for k, v in (t.get("judged") or {}).items()))
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
         outputs: Mapping[str, str] | Callable[[GeneratedTarget, str], str]) -> dict[str, str]:
    """Every target's key, in topological order. `outputs` gives each generated target's output digest: a mapping (the
    digests a gate's generations made), or a function of (the target, its key) that resolves it (`stored_outputs`)."""
    known: dict[str, str] = {}
    out: dict[str, str] = {}
    for name in graph.order:
        t = graph.targets[name]
        out[name] = key(t, entries, facets, known)
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
        return rec if isinstance(rec, dict) and rec.get("result") == "pass" and rec.get("key") == k \
            and rec.get("target") == target else None

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
#: repository module the command imports is opened, and seen, after it. argv: <log> <script> <args...>. It runs the
#: script as `python <script>` would (sys.path[0] its directory, __name__ "__main__") and writes, at its end, the
#: absolute paths it opened for reading, the directories it listed and the processes it started.
TRACER = r"""
import json, os, runpy, sys, traceback
log, script, argv = sys.argv[1], os.path.abspath(sys.argv[2]), sys.argv[3:]
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
    with open(log, "w", encoding="utf-8") as fh:
        json.dump({"opened": sorted(opened), "listed": sorted(listed), "started": started}, fh)
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
            data = {"opened": [], "listed": [], "started": ["(the tracer wrote no log: what the run read is unknown)"]}
    return Trace(rc, output, tuple(data["opened"]), tuple(data["listed"]), tuple(data["started"]))


def undeclared(target: GeneratedTarget, trace: Trace, repo: Path, tracked: set[str],
               cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> list[str]:
    """What a traced run read outside `target`'s declared inputs: a TRACKED path it opened that its inputs do not match
    (an input excluded as @inert or @tooling-lane included: then the classification is wrong), a tracked directory it
    listed that no input lies in or under (`Target.lists`), and every process it started. Untracked and ignored paths
    (its own output, a byte-code cache) and paths outside the repository (the interpreter's library) are not inputs."""
    root = os.path.normcase(os.path.abspath(repo)) + os.sep
    dirs = {"/".join(f.split("/")[:i]) + "/" for f in tracked for i in range(0, f.count("/") + 1)}

    def rel(p: str) -> str | None:
        if os.path.normcase(p) + os.sep == root:
            return ""
        return p[len(root):].replace(os.sep, "/") if os.path.normcase(p).startswith(root) else None

    found = []
    for p in trace.opened:
        r = rel(p)
        if r is not None and r in tracked and not target.matches(r, cls):
            found.append(f"opened {r}")
    for p in trace.listed:
        r = rel(p)
        d = "" if not r else r + "/"
        if r is not None and d in dirs and not target.lists(d):
            found.append(f"listed {d or '(the repository root)'}")
    found += [f"started a process, whose reads no hook sees: {s}" for s in trace.started]
    return sorted(found)


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
                     run: Callable[[Path, GeneratedTarget, Path], Trace] = traced_run) -> dict[str, Generation]:
    """Every generated target of `graph`, in topological order, brought up to date (`generate`) at the tree `entries`
    lists; a dependent's key is made from the output digests its dependencies have just been given. Raises the first
    BuildError."""
    tracked = {p for _, p in entries}
    done: dict[str, Generation] = {}
    for name in graph.order:
        t = graph.targets[name]
        if isinstance(t, GeneratedTarget):
            k = key(t, entries, facets, {n: g.outputs for n, g in done.items()})
            done[name] = generate(repo, t, k, store, tracked, stable=stable, run=run)
    return done


# ── the declared-inputs audit ────────────────────────────────────────────────────────────────────────────────────────────

#: Each TestRepo helper and the directory it is rooted at ("" is the repository root; None: build output, made from src/).
TESTREPO_BASES = {
    "At": "", "Root": "", "Src": "src", "SrcBin": None, "Tests": "tests", "Docs": "docs", "Scripts": "scripts",
    "Specs": "specs", "VersionMatrix": "tests/version-matrix", "ConstructsDir": "tests/version-matrix/constructs",
    "ConstructRows": "tests/version-matrix/constructs", "Nist": "tests/nist", "Suite": "conformance-suite",
    "SuiteCase": "conformance-suite", "SuiteFile": "conformance-suite", "Configuration": None, "TargetFramework": None,
}
TESTREPO_CALL = re.compile(r"\bTestRepo\.(\w+)")
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
    """The argument text of a call whose `(` is at `start`, to its matching `)`; None when `start` is no `(`."""
    if start >= len(text) or text[start] != "(":
        return None
    depth = 0
    for i in range(start, len(text)):
        depth += {"(": 1, ")": -1}.get(text[i], 0)
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


def _leading_literals(args: list[str]) -> tuple[int, list[str]]:
    """How many leading arguments are plain string literals, and their text split on `/`: a path's segments known before
    run time."""
    n, segs = 0, []
    for a in args:
        m = re.fullmatch(r'"([^"{}\\]*)"', a)
        if m is None:
            break
        n += 1
        segs += [s for s in m.group(1).split("/") if s]
    return n, segs


def reads_in(text: str, top_dirs: list[str]) -> list[tuple[int, str]]:
    """Every repository read `text` (a C# source) spells, with its line: a path when every segment is a literal, else the
    directory its literal segments reach, ending in `/` ("" is the whole repository)."""
    found: list[tuple[int, str]] = []

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
        base = TESTREPO_BASES.get(helper, None) if helper in TESTREPO_BASES else None
        if base is None or m.start() in listed:
            continue
        segs: list[str] = []
        complete = False
        if helper == "Root":
            call = re.search(r"Path\.Combine\(\s*$", text[max(0, m.start() - 40):m.start()])
            if call is not None:
                open_at = text.rindex("(", 0, m.start())
                whole = _args(text, open_at)
                parts = [x for x in _split_top(whole or "")[1:] if x]
                n, segs = _leading_literals(parts)
                complete = n == len(parts) and n > 0
        else:
            args = _args(text, m.end())
            if args is not None:
                parts = [x for x in _split_top(args) if x]
                n, segs = _leading_literals(parts)
                complete = n == len(parts) and n > 0
        path = "/".join(x for x in [base, *segs] if x)
        found.append((line_of(m.start()), path if complete else (path + "/" if path else "")))
    roots = "|".join(re.escape(d) for d in top_dirs)
    lit = re.compile(rf"^(?:{roots})/[^\s{{}}<>|]*$") if roots else None
    for m in STRING.finditer(text):
        s = m.group(1)
        if lit is None or not lit.match(s):
            continue
        cut = re.split(r"[*?]", s, maxsplit=1)[0]
        found.append((line_of(m.start()), s if cut == s else (cut if cut.endswith("/") else cut.rsplit("/", 1)[0] + "/")))
    return found


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
    spell that the inputs do not cover, as `<file>:<line>: <read>`. `roots`: the bare repository-root references
    (`TestRepo.Root`, `At()` with no literal segment): a working directory, a path to strip from a message, or a
    whole-tree read (`TrackedTree`'s `git ls-files`), which no static reading can tell apart. The roots are counted in
    the audit's verdict, never judged: the dynamic proof is where they are judged (kb/Work PB5218). `through`: each read
    that is the program of a target this one depends on (`<file>:<line>: <read>`), the run a host makes only when no gate
    handed it the dependency's output; it belongs to that target. `handed`: each dependency's environment variable the
    sources spell (`GeneratedTarget.env`)."""
    undeclared: tuple[str, ...]
    roots: tuple[str, ...]
    through: tuple[str, ...] = ()
    handed: tuple[str, ...] = ()


def scan(repo: Path, target: TestTarget, top_dirs: list[str], tracked: tuple[set[str], set[str]],
         cls: inert_lane.Classification = inert_lane.CLASSIFICATION,
         dependencies: tuple[GeneratedTarget, ...] = ()) -> Scan:
    """Read `target`'s sources once and return every half of its audit. The tree's top directories and tracked paths
    are the caller's, computed once for every target (kb/Work PB5218 review R1-1: per-file recomputation cost ~60 s).
    `dependencies`: the generated targets it depends on, whose PROGRAM a source may name (a read reached through the
    dependency) and whose ENVIRONMENT VARIABLE its sources must spell."""
    judged = {k for k, _ in target.judged}
    programs = {d.program for d in dependencies}
    bad: set[str] = set()
    roots: set[str] = set()
    through: set[str] = set()
    handed: set[str] = set()
    for src in assembly_sources(repo, target):
        rel = src.relative_to(repo).as_posix()
        text = src.read_text(encoding="utf-8", errors="replace")
        handed |= {d.env for d in dependencies if f'"{d.env}"' in text}
        for line, read in reads_in(text, top_dirs):
            if read == "":
                roots.add(f"{rel}:{line}")
                continue
            read = _normalized(read, tracked)
            if read is None or f"{rel}|{read}" in judged:
                continue   # names no tracked path (build output, a cache, a scratch name), or judged by hand
            if read in programs:
                through.add(f"{rel}:{line}: {read}")
            elif not target.covers(read, cls):
                bad.add(f"{rel}:{line}: {read}")
    return Scan(tuple(sorted(bad)), tuple(sorted(roots)), tuple(sorted(through)), tuple(sorted(handed)))


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
    top, tracked = _top_dirs(repo), _tracked(repo)
    scans = {name: scan(repo, t, top, tracked, dependencies=tuple(graph.targets[d] for d in t.depends))
             for name, t in graph.tests().items()}
    unhanded = [f"{name}: no source of its assembly names {graph.targets[d].env}, the variable the gate hands "
                f"{d}'s output in, so its hosts would generate their own and its key would not be what they ran"
                for name, t in graph.tests().items() for d in t.depends if graph.targets[d].env not in scans[name].handed]
    for name, sc in scans.items():
        for b in sc.undeclared:
            print(f"  {name}: {b}")
    for u in unhanded:
        print(f"  {u}")
    n = sum(len(sc.undeclared) for sc in scans.values()) + len(unhanded)
    residue = ", ".join(f"{name} {len(sc.roots)}" for name, sc in scans.items())
    through = ", ".join(f"{name} {len(sc.through)}" for name, sc in scans.items() if sc.through)
    if n:
        print(f"=== BUILD TARGETS: RED — {n} finding(s): a repository read outside its target's declared inputs "
              "(scripts/build_targets.json: declare it with its reader, or remove the read), or a dependency's output "
              "its assembly does not take from the gate ===")
        return 1
    print(f"=== BUILD TARGETS: GREEN — {len(graph.targets)} target(s), {len(graph.generated())} generated, in the order "
          f"{' -> '.join(graph.order)}; every repository read the test sources spell lies in their declared inputs"
          + (f" or is a dependency's program ({through})" if through else "")
          + f" (bare root references, judged only by the dynamic proof: {residue}) ===")
    return 0


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
            return scan(tdir, t, top, _tracked(tdir), dependencies=deps)

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
        real_graph = load_targets()
        rt = real_graph.targets
        arm("the real targets file loads: unit, conformance, characterization and the generated conformance-suite, which "
            "conformance depends on and which comes first; @build expanded; the inventory is the generator's input and "
            "not conformance's",
            sorted(real_graph.tests()) == ["characterization", "conformance", "unit"]
            and list(real_graph.generated()) == ["conformance-suite"] and rt["conformance"].depends == ("conformance-suite",)
            and real_graph.order.index("conformance-suite") < real_graph.order.index("conformance")
            and "src/" in rt["conformance"].inputs and "@build" not in rt["conformance"].inputs
            and rt["unit"].inputs == ("@population-inputs",)
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
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.cmd == "check":
        return check()
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
