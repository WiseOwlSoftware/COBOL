#!/usr/bin/env python3
"""gate_differential.py - THE PROGRAM DIFFERENTIAL (kb/Work PB4333, step 10 of PB4323; DESIGN-test-build-ci.md section
3.14.13): which Conformance cases a local gate need not run because each is BYTE-IDENTICAL IN EVERY INPUT to a case that
already passed on this host.

    python scripts/gate_differential.py status [--tree <tree>]   # the harness key at a tree and whether passes stand there

A CASE IS CARRIED, never run, exactly when this gate computed for it a CASE DIGEST that the store of passed cases holds at
this gate's HARNESS KEY. Both are digests of inputs, never of names or time:

  * the HARNESS KEY (`harness_key`) covers every input of the Conformance target whose class is `everything`
    (scripts/build_targets.json `differential`: the runtime, the harness, the build, the suite's adapters and profiles),
    the target's facets (platform, configuration, toolchain), the suite manifest's content that is no case's entry, and
    the differential's own inputs (the capture's host, this file, the classification). Any change there is a new harness
    key, where no pass is recorded yet: the whole population runs.
  * the CASE DIGEST (`case_digests`) of one listed case of a carried family covers its theory and row (edition, id), its
    manifest entry as the generator wrote it, the blob of every file that entry names (source, data, expected output),
    and the oracle's three digests of the compile the theory performs (`WiseOwl.Cobol.ArchOracle digests`): the emitted
    C#, the diagnostic stream, and the PROGRAM BYTES (the assembly and its runtime configuration). A `program`-class input
    (the compiler, src/ less the runtime) reaches a carried case only through those, a `case`-class input (a test's own
    folder and sidecars) only through its entry and its named files.

So a case runs when its program or diagnostics changed, when a file it names or its entry changed, when it is new, and
when no pass stands at the harness key; and every case NOT of a carried family (`FAMILIES`) runs, as does every case the
digest rule cannot name exactly (a truncated display name, a multi-unit or server test, a flagged test, a compile that
threw in the capture, a program that reads the compile clock, whose bytes differ every compile).

THE STORE OF PASSED CASES is the pass store's own directory (`buildgraph.Store`, one store, kb/Work PB5218):
`<store>/conformance-cases/<harness key>.txt`, one case digest per line, sorted. A gate whose Conformance population was
EXACT and green, at a key that did not move while it ran, adds the digest of every case it listed (`CaseStore.record`).
A digest is written only from such a pass, so a digest in the file is a pass of exactly those inputs.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import tempfile
import time
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable, Mapping

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import buildgraph  # noqa: E402  - THE targets, their keys and the one store (kb/Work PB5218)
import lane_classification  # noqa: E402

#: The version of the harness key's and the case digest's composition: a change to either is a change of this.
SCHEMA = b"program-differential 1"
#: The assembly whose cases are carried, and its target in the targets file.
ASSEMBLY = "Conformance"
#: The suite manifest a carried case's entry is read from (the generator's output, `conformance-suite`).
SUITE_MANIFEST = ("wiseowl", "manifest.json")
#: Where a manifest entry's paths are relative to (SuiteManifest.Root).
SUITE_ROOT = "conformance-suite/"
#: The store of passed cases, under the store's directory: `<harness key>.txt`.
CASE_DIR = "conformance-cases"
#: A case set not written for this long is pruned when another is written (a harness key no tree has any more).
PRUNE_DAYS = 21
#: The capture's file in the gate's run directory.
DIGESTS_FILE = "oracle-digests.tsv"

_ARG = r'"([^"\\]+)"'
#: THE CARRIED FAMILIES: a listed display name -> (family, edition, id). A name no pattern matches exactly (another
#: theory, a truncated argument `"..."...`) is never carried.
FAMILIES = (
    ("suite", re.compile(r"WiseOwl\.Cobol\.Tests\.Conformance\.ConformanceSuiteTests_P\d+\."
                         rf"SuiteTest_IsNoWorseThanItsVerdictAllows\(edition: {_ARG}, id: {_ARG}\)"), ("edition", "id")),
    ("refusal", re.compile(r"WiseOwl\.Cobol\.Tests\.Conformance\.ConformanceSuiteTests_P\d+\."
                           rf"WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic\(id: {_ARG}, edition: {_ARG}\)"),
     ("id", "edition")),
)
#: The fields of an entry the rule understands, by family: an entry holding any other (a multi-unit test's `build`, a
#: server test's `servers`, a flagged test's `control` and `line`, a field a later generator adds) is not carried.
ENTRY_FIELDS = {
    "suite": frozenset({"id", "clause", "source", "source-format", "rules", "files", "requires", "words", "expect",
                        "implementor-defined", "expected-output", "tree", "portable", "in-suite", "options"}),
    "refusal": frozenset({"source", "source-format", "files", "options", "refuse-at", "expect"}),
}


class DifferentialError(RuntimeError):
    """The differential cannot select: the selection is not made, and the whole population runs."""


def _blob(raw: bytes) -> str:
    """A `git ls-tree` entry's object id."""
    return raw.split(b"\t", 1)[0].split()[2].decode("ascii")


def manifest_path(suite_dir: Path) -> Path:
    return suite_dir.joinpath(*SUITE_MANIFEST)


def read_manifest(suite_dir: Path) -> dict:
    try:
        return json.loads(manifest_path(suite_dir).read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise DifferentialError(f"the suite manifest {manifest_path(suite_dir)} could not be read ({e})") from None


def _canonical(value: object) -> bytes:
    return json.dumps(value, sort_keys=True, ensure_ascii=False, separators=(",", ":")).encode("utf-8")


def harness_key(target: buildgraph.TestTarget, entries: list[tuple[bytes, str]], facets: Mapping[str, str],
                manifest: dict) -> str:
    """THE HARNESS KEY (module doc): every `everything`-class entry the target's key covers, its facets, the differential's
    own inputs and the manifest's non-case content. A path the target's key covers that the classification does not class
    counts as `everything` (load_targets refuses that graph, so it never happens)."""
    diff = target.differential
    if diff is None:
        raise DifferentialError(f"{target.name} has no differential classification (scripts/build_targets.json)")
    h = hashlib.sha256(SCHEMA + b"\0harness\0" + _canonical({f: facets[f] for f in target.facets}) + b"\0")
    for raw, path in entries:
        if target.matches(path) and diff.class_of(path) in ("everything", None):
            h.update(b"input\0" + raw + b"\0")
        if any(lane_classification.under(path, p) for p in diff.inputs):
            h.update(b"own\0" + raw + b"\0")
    h.update(b"suite\0" + _canonical({k: v for k, v in manifest.items() if k not in ("editions", "diagnostics")}))
    return h.hexdigest()


def read_digests(path: Path) -> dict[str, tuple[str, str, str]]:
    """The capture (`WiseOwl.Cobol.ArchOracle digests`): oracle id -> (C#, diagnostics, program) digests. A malformed file
    is a DifferentialError: nothing is carried from it."""
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as e:
        raise DifferentialError(f"the capture {path} could not be read ({e})") from None
    rows: dict[str, tuple[str, str, str]] = {}
    for line in text.splitlines():
        if not line:
            continue
        parts = line.split("\t")
        if len(parts) != 5:
            raise DifferentialError(f"the capture {path} has a malformed line: {line[:120]!r}")
        rows[parts[0]] = (parts[2], parts[3], parts[4])
    if not rows:
        raise DifferentialError(f"the capture {path} names no case")
    return rows


@dataclass
class Digests:
    """The case digests of one listing: listed name -> its digest, for each case the rule can name exactly; and why each
    other listed case cannot be carried (reason -> count)."""
    by_name: dict[str, str] = field(default_factory=dict)
    uncarriable: Counter = field(default_factory=Counter)


def _family(name: str) -> tuple[str, str, str] | None:
    for family, pattern, order in FAMILIES:
        if m := pattern.fullmatch(name):
            args = dict(zip(order, m.groups()))
            return family, args["edition"], args["id"]
    return None


def case_digests(listed: Counter, oracle: Mapping[str, tuple[str, str, str]], manifest: dict,
                 blobs: Mapping[str, str]) -> Digests:
    """Each listed case's digest (module doc), or the reason it has none. `blobs`: tracked path -> blob id at the measured
    tree."""
    out = Digests()
    editions = manifest.get("editions") or {}
    tests = {ed: {t.get("id"): t for t in ts} for ed, ts in editions.items() if isinstance(ts, list)}
    refusals = manifest.get("diagnostics") or {}
    for name, count in listed.items():
        fam = _family(name)
        if fam is None:
            out.uncarriable["not a carried family"] += count
            continue
        if count != 1:
            out.uncarriable["listed more than once"] += count
            continue
        family, edition, case_id = fam
        if family == "suite":
            entry, oracle_id = tests.get(edition, {}).get(case_id), f"suite/{edition}/{case_id}"
            named = [entry.get("source"), *(entry.get("files") or []), *(entry.get("expected-output") or {}).values()] \
                if isinstance(entry, dict) else []
            plain = isinstance(entry, dict) and entry.get("expect") == "accept"
        else:
            entry, oracle_id = refusals.get(case_id), f"refusal/{case_id}@{edition}"
            named = [entry.get("source"), *(entry.get("files") or [])] if isinstance(entry, dict) else []
            plain = isinstance(entry, dict)
        if not isinstance(entry, dict):
            out.uncarriable["no manifest entry"] += 1
            continue
        if not plain or set(entry) - ENTRY_FIELDS[family]:
            out.uncarriable["an entry the rule does not carry (build steps, servers, a flag test)"] += 1
            continue
        digests = oracle.get(oracle_id)
        if digests is None or "!" in digests:
            out.uncarriable["no digest of its compile"] += 1
            continue
        files = []
        for p in sorted({str(p) for p in named}):
            blob = blobs.get(SUITE_ROOT + p)
            if blob is None:
                files = None
                break
            files.append(f"{p}\0{blob}")
        if files is None:
            out.uncarriable["a file its entry names is not tracked"] += 1
            continue
        h = hashlib.sha256(SCHEMA + b"\0case\0" + "\0".join([family, edition, case_id, *digests]).encode("utf-8")
                           + b"\0entry\0" + _canonical(entry) + b"\0files\0" + "\0".join(files).encode("utf-8"))
        out.by_name[name] = h.hexdigest()
    return out


class CaseStore:
    """THE STORE OF PASSED CASES (module doc): `<dir>/<harness key>.txt`, one case digest per line, sorted."""

    def __init__(self, directory: Path):
        self.dir = directory

    @classmethod
    def of(cls, store: buildgraph.Store) -> "CaseStore":
        return cls(store.dir / CASE_DIR)

    def _path(self, harness: str) -> Path:
        if not re.fullmatch(r"[0-9a-f]{64}", harness):
            raise ValueError(f"not a harness key: {harness!r}")
        return self.dir / f"{harness}.txt"

    def lookup(self, harness: str) -> frozenset[str] | None:
        """The case digests that passed at `harness`; None when no pass stands there."""
        try:
            text = self._path(harness).read_text(encoding="ascii")
        except (OSError, ValueError):
            return None
        return frozenset(line for line in text.splitlines() if re.fullmatch(r"[0-9a-f]{64}", line))

    def record(self, harness: str, digests: Iterable[str], now: float | None = None) -> int:
        """Add `digests` (each a case that passed at `harness`) to its set, written whole (a temporary file renamed over
        it); two writers at once can lose each other's additions, never add a digest no pass made. Prunes the sets not
        written for PRUNE_DAYS. Returns the set's size."""
        path = self._path(harness)
        path.parent.mkdir(parents=True, exist_ok=True)
        merged = sorted((self.lookup(harness) or frozenset()) | set(digests))
        fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=".cases-", suffix=".txt")
        with os.fdopen(fd, "w", encoding="ascii", newline="\n") as fh:
            fh.write("".join(f"{d}\n" for d in merged))
        os.replace(tmp, path)
        cutoff = (time.time() if now is None else now) - PRUNE_DAYS * 86400
        for other in self.dir.glob("*.txt"):
            try:
                if other != path and other.stat().st_mtime < cutoff:
                    other.unlink()
            except OSError:
                pass   # another gate pruned it first
        return len(merged)


@dataclass
class Selection:
    """What the differential decided for one gate's Conformance listing: the harness key, every case digest, the cases
    CARRIED (a pass stands at their digest: listed name -> 1), and how many ran by which rule."""
    harness: str
    digests: Digests
    stands: bool                       # a pass set stands at the harness key
    carried: Counter = field(default_factory=Counter)
    runs: Counter = field(default_factory=Counter)   # reason -> count of the cases that run

    def describe(self) -> str:
        why = ", ".join(f"{n:,} {r}" for r, n in self.runs.most_common())
        return (f"program differential (kb/Work PB4333): harness key {self.harness[:12]}, "
                f"{sum(self.carried.values()):,} case(s) carried as byte-identical to a passed case, "
                f"{sum(self.runs.values()):,} run ({why or 'none'})")

    def record(self) -> dict:
        return {"harness": self.harness, "stands": self.stands, "carried": sum(self.carried.values()),
                "ran": dict(self.runs)}


def select(harness: str, digests: Digests, passed: frozenset[str] | None) -> Selection:
    """THE SELECTION: a case is carried when its digest is in `passed` (the set at the harness key); every other case
    runs, by its reason."""
    sel = Selection(harness, digests, passed is not None)
    sel.runs.update(digests.uncarriable)
    for name, digest in digests.by_name.items():
        if passed is not None and digest in passed:
            sel.carried[name] = 1
        else:
            sel.runs["no pass at its digest (its program, diagnostics, entry or files changed, or it is new)"
                     if passed is not None else "no pass stands at this harness key"] += 1
    return sel


@dataclass
class Prepared:
    """The differential at one gate's measured tree before any host: its harness key, the pass set standing there (None:
    none), the suite manifest and the tree's blobs the case digests read."""
    harness: str
    passed: frozenset[str] | None
    manifest: dict
    blobs: dict[str, str]


class Differ:
    """THE GATE'S COLLABORATOR (kb/Work PB4333; the shape of `gate_generated.Generations`): it returns what it found and
    never calls back into `Gate`. Every failure is said and leaves the selection unmade, so the whole population runs:
    the differential can only ever carry less, never decide a verdict."""

    def __init__(self, host, say):
        self.host, self.say = host, say

    def prepare(self, out, facets: Mapping[str, str], handed: Mapping[str, str]) -> Prepared | None:
        """The harness key at the measured tree with the suite the Conformance host is handed (`handed`), and the pass
        set standing at it; None (said) when either cannot be read."""
        target = self.host.targets().get(ASSEMBLY)
        suite = next(iter(handed.values()), None) if handed else None
        try:
            if target is None or suite is None or not out.tree:
                raise DifferentialError("no Conformance target, no suite handed to its host, or no measured tree")
            manifest = read_manifest(Path(suite))
            entries = self.host.tree_entries(out.tree)
            harness = harness_key(target, entries, facets, manifest)
        except (DifferentialError, buildgraph.GraphError, OSError, ValueError, KeyError) as e:
            self.say(f"build-local: program differential not prepared ({e}); Conformance runs whole")
            return None
        passed = CaseStore.of(self.host.store()).lookup(harness)
        self.say(f"build-local: program differential: harness key {harness[:12]}, "
                 + (f"{len(passed):,} passed case(s) stand there, so the capture decides what runs"
                    if passed is not None else "no pass stands there yet, so Conformance runs whole and its pass "
                    "records one"))
        return Prepared(harness, passed, manifest, {p: _blob(raw) for raw, p in entries})

    def capture(self, run: Path, spawn, handed: Mapping[str, str], say) -> dict[str, tuple[str, str, str]] | None:
        """The oracle's digests of this tree's compiles, into `<run>/oracle-digests.tsv` (`Host.oracle_digests`); None
        (said through `say`: the gate's console before the hosts, the lines of work beside them) when it failed."""
        t = time.monotonic()
        try:
            path = self.host.oracle_digests(run / DIGESTS_FILE, spawn, dict(handed))
            rows = read_digests(path)
        except (DifferentialError, OSError) as e:
            say(f"build-local: program differential: the capture failed ({e}); nothing is carried")
            return None
        say(f"build-local: program differential: {len(rows):,} compile(s) captured in {time.monotonic() - t:.1f} s")
        return rows

    def select(self, prepared: Prepared, oracle: Mapping[str, tuple[str, str, str]] | None,
               listed: Counter) -> Selection | None:
        """The selection over `listed` (the Conformance listing); None when the capture failed."""
        if oracle is None:
            return None
        sel = select(prepared.harness, case_digests(listed, oracle, prepared.manifest, prepared.blobs), prepared.passed)
        self.say(f"build-local: {sel.describe()}")
        return sel

    def record(self, out, selection: Selection | None) -> None:
        """After an EXACT green Conformance pass recorded at a key that did not move (`out.recorded`), every listed case
        with a digest passed: add them to the set at the harness key. Said when not written; never changes the verdict."""
        if selection is None or ASSEMBLY not in out.recorded:
            return
        try:
            n = CaseStore.of(self.host.store()).record(selection.harness, selection.digests.by_name.values())
        except OSError as e:
            self.say(f"build-local: program differential: the passed cases were not recorded ({e}); the verdict stands")
            return
        out.differential["recorded"] = n
        self.say(f"build-local: program differential: {len(selection.digests.by_name):,} passed case(s) recorded at "
                 f"harness key {selection.harness[:12]} ({n:,} stand there)")


def status_main(tree: str | None) -> int:
    """`status`: the harness key at a tree (HEAD's by default) with the suite the gate last generated in this worktree,
    and whether a pass set stands there."""
    graph = buildgraph.load_targets()
    target = graph.by_assembly()[ASSEMBLY]
    suite = REPO / "TestResults" / "generated" / "conformance-suite"
    import gate_host  # noqa: PLC0415  (the host's facets: platform, configuration, toolchain)
    host = gate_host.Host(REPO)
    tools = host.toolchain()
    facets = {"platform": host.platform(), "configuration": gate_host.GATE_CONFIGURATION, "toolchain": tools.tools}
    entries = buildgraph.tree_entries(REPO, tree or "HEAD")
    h = harness_key(target, entries, facets, read_manifest(suite))
    passed = CaseStore.of(buildgraph.Store.for_repo(REPO)).lookup(h)
    print(f"=== PROGRAM DIFFERENTIAL: harness key {h} at {tree or 'HEAD'}; "
          + (f"{len(passed):,} passed case(s) stand there ===" if passed is not None else "no pass stands there ==="))
    return 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("status", help="the harness key at a tree and whether passes stand there")
    s.add_argument("--tree", help="a tree or commit (default HEAD)")
    args = ap.parse_args(argv)
    return status_main(args.tree)


if __name__ == "__main__":
    from standard_streams import ensure_utf8  # noqa: E402
    ensure_utf8()
    sys.exit(main())
