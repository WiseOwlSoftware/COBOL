#!/usr/bin/env python3
"""buildgraph.py — THE BUILD'S TARGETS, THEIR KEYS AND THE ONE STORE OF THEIR PASSES (kb/Work PB5207 stage 1, PB5218).

    python scripts/buildgraph.py keys [--tree T] [--toolchain S] [--platform P] [--configuration C]   # each target's key
    python scripts/buildgraph.py check          # the BUILD TARGETS audit: every repository read a test source names
                                                # lies in its assembly's declared inputs
    python scripts/buildgraph.py --self-test

THE MODEL (owner 2026-11-10: "This is precisely what the Unix make and Windows MSBuild utilities are for. They rerun only
what is required due to changes."). A TARGET is a named step with DECLARED INPUTS (path patterns, scripts/build_targets.json)
and FACETS (platform, configuration, toolchain). Its KEY is a digest of its name, its facets and the `git ls-tree -r`
entries (mode, object id, path: no file is read) of every tracked path its inputs match and its excluded classes do not.
A target is UP TO DATE exactly when the STORE holds a pass at its key (`Store.up_to_date`): that is the only rule. The
store is one directory in the git common directory, `cobol-build-store/<target>/<key>.json`, so every worktree of the
repository shares it: a pass recorded in worktree A is up to date in worktree B at the same content. It holds only what
was proven (a gated assembly whose population check was EXACT and whose host was green): a red, a partial or an unmeasured
run records nothing.

THE INPUT PATTERNS are matched by the one matcher of the classification, `inert_lane.under`: `dir/` is every path below
it; a pattern with `*` or `?` is a listing pattern (within one directory and case-insensitive, as a .NET directory
listing matches, so a key changes for every file a test's listing reads); anything else is that path or, when it names a
directory, every path below it. `@build` is the shared build inputs of the targets file;
`@population-inputs` is every path a compiler test or the build may read (`inert_lane.Classification.is_population_input`).
The exclusions `@inert` (no test reads it, proven by the inert lane's watch) and `@tooling-lane` (no compiler test or build
reads it, proven by the same watch and the INERT LANE audit) are the classes a test target never reads.

THE DECLARATIONS ARE CHECKED, NOT TRUSTED (`check`, `undeclared`). The audit reads every test source of an assembly and
the `tests/_shared` files it uses (by the types it names, transitively) and finds each repository read it spells: a
`TestRepo` helper with its leading literal segments, `Path.Combine(TestRepo.Root, "…")`, and a string literal rooted at a
top-level tracked directory. A read outside the assembly's declared inputs is a finding naming its file and line. Its
residue, named: a path computed at run time from a non-literal, and what a process the test starts reads; the generator of
the conformance suite was traced instead (kb/Work PB5218), and the dynamic proof (the Linux shards' file watch, per
target) is the remaining work kb/Work PB5218 states.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import inert_lane  # noqa: E402  — THE classification: what is inert, what is tooling no compiler test reads
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
TARGETS_FILE = REPO / "scripts" / "build_targets.json"
#: The store's directory under the git common directory: one per repository, shared by every worktree.
STORE_DIR = "cobol-build-store"
#: The version of the key's digest: a change to what the key covers bumps it, so no pass recorded under the old meaning
#: is up to date under the new one.
KEY_VERSION = b"build-target-key 1"


@dataclass(frozen=True)
class Target:
    """One target of the targets file: its name, the test assembly it runs, its project directory, its input patterns
    (`@build` expanded) and its excluded classes."""
    name: str
    assembly: str
    project: str
    inputs: tuple[str, ...]
    exclude: tuple[str, ...]
    #: Reads the audit cannot resolve by itself, each judged by hand with its reason: `<source file>|<read>` -> why.
    judged: tuple[tuple[str, str], ...] = ()

    def matches(self, path: str, cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> bool:
        """Whether the tracked `path` is an input of this target."""
        if any(_excluded(path, e, cls) for e in self.exclude):
            return False
        return any(_matches(path, p, cls) for p in self.inputs)

    def covers(self, read: str, cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> bool:
        """Whether a READ a test source spells (a path, or a directory ending in `/`) lies inside the declared inputs. A
        directory read is covered when the directory itself lies under an input pattern, or when it is a LISTING by a
        pattern (`dir/guard*.sh`) an input pattern equals. For `@population-inputs` a read is covered unless it is a
        tooling-lane path: a test's read of an INERT path is the inert lane's to judge (its watch and its audits:
        `inert_lane.py check`, DOC AUDITS), and the key of such a target already leaves inert paths out."""
        for p in self.inputs:
            if p == "@population-inputs":
                if not cls.in_tooling_lane(read):
                    return True
            elif p == read or _matches(read, p, cls):
                return True
        return False


def _matches(path: str, pattern: str, cls: inert_lane.Classification) -> bool:
    if pattern == "@population-inputs":
        return cls.is_population_input(path)
    return inert_lane.under(path, pattern)


def _excluded(path: str, cls_name: str, cls: inert_lane.Classification) -> bool:
    if cls_name == "@inert":
        return cls.is_inert(path)
    if cls_name == "@tooling-lane":
        return cls.in_tooling_lane(path)
    raise ValueError(f"no such excluded class {cls_name!r} (one of @inert, @tooling-lane)")


def load_targets(path: Path = TARGETS_FILE) -> dict[str, Target]:
    """The targets file, `@build` expanded; raises ValueError on a malformed one."""
    data = json.loads(path.read_text(encoding="utf-8"))
    build = tuple(data["build"]["inputs"])
    targets: dict[str, Target] = {}
    for name, t in data["targets"].items():
        inputs: list[str] = []
        for p in t["inputs"]:
            inputs += list(build) if p == "@build" else [p]
        targets[name] = Target(name, t["assembly"], t["project"], tuple(inputs), tuple(t.get("exclude", ())),
                               tuple((k, v) for k, v in (t.get("judged") or {}).items()))
    if not targets:
        raise ValueError(f"{path} declares no target")
    return targets


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


def key(target: Target, entries: list[tuple[bytes, str]], facets: dict[str, str],
        cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> str:
    """THE TARGET'S KEY: sha256 of KEY_VERSION, its name, its facets and every entry its inputs match."""
    h = hashlib.sha256(KEY_VERSION + b"\0" + target.name.encode() + b"\0"
                       + json.dumps(facets, sort_keys=True).encode("utf-8") + b"\0")
    for raw, path in entries:
        if target.matches(path, cls):
            h.update(raw + b"\0")
    return h.hexdigest()


def keys(repo: Path, tree: str, facets: dict[str, str], targets: dict[str, Target] | None = None) -> dict[str, str]:
    """Every target's key at `tree` (one `git ls-tree`)."""
    targets = targets if targets is not None else load_targets()
    entries = tree_entries(repo, tree)
    return {name: key(t, entries, facets) for name, t in targets.items()}


class Store:
    """THE ONE STORE OF PASSES: `<dir>/<target>/<key>.json`, one small record per target and key. A record is written whole
    (a temporary file renamed over it), so a reader never sees half of one."""

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
        """The pass recorded for `target` at key `k`, or None."""
        try:
            rec = json.loads(self._path(target, k).read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return None
        return rec if isinstance(rec, dict) and rec.get("result") == "pass" and rec.get("key") == k \
            and rec.get("target") == target else None

    def up_to_date(self, target: str, k: str) -> bool:
        """THE ONE RULE: the store holds a pass at this key."""
        return self.lookup(target, k) is not None

    def record(self, target: str, k: str, rec: dict) -> Path:
        """Record a PASS of `target` at key `k` (the caller has proven it: a green host, an EXACT population)."""
        path = self._path(target, k)
        path.parent.mkdir(parents=True, exist_ok=True)
        body = {**rec, "target": target, "key": k, "result": "pass"}
        fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=".rec-", suffix=".json")
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(json.dumps(body, indent=1, sort_keys=True) + "\n")
        os.replace(tmp, path)
        return path


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


def assembly_sources(repo: Path, target: Target) -> list[Path]:
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
    the audit's verdict, never judged: the dynamic proof is where they are judged (kb/Work PB5218)."""
    undeclared: tuple[str, ...]
    roots: tuple[str, ...]


def scan(repo: Path, target: Target, top_dirs: list[str], tracked: tuple[set[str], set[str]],
         cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> Scan:
    """Read `target`'s sources once and return both halves of its audit. The tree's top directories and tracked paths
    are the caller's, computed once for every target (kb/Work PB5218 review R1-1: per-file recomputation cost ~60 s)."""
    judged = {k for k, _ in target.judged}
    bad: set[str] = set()
    roots: set[str] = set()
    for src in assembly_sources(repo, target):
        rel = src.relative_to(repo).as_posix()
        text = src.read_text(encoding="utf-8", errors="replace")
        for line, read in reads_in(text, top_dirs):
            if read == "":
                roots.add(f"{rel}:{line}")
                continue
            read = _normalized(read, tracked)
            if read is None or f"{rel}|{read}" in judged:
                continue   # names no tracked path (build output, a cache, a scratch name), or judged by hand
            if not target.covers(read, cls):
                bad.add(f"{rel}:{line}: {read}")
    return Scan(tuple(sorted(bad)), tuple(sorted(roots)))


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
    targets = load_targets(repo / "scripts" / "build_targets.json")
    top, tracked = _top_dirs(repo), _tracked(repo)
    scans = {name: scan(repo, t, top, tracked) for name, t in targets.items()}
    for name, sc in scans.items():
        for b in sc.undeclared:
            print(f"  {name}: {b}")
    n = sum(len(sc.undeclared) for sc in scans.values())
    residue = ", ".join(f"{name} {len(sc.roots)}" for name, sc in scans.items())
    if n:
        print(f"=== BUILD TARGETS: RED — {n} repository read(s) outside their target's declared inputs "
              "(scripts/build_targets.json): declare each with its reader, or remove the read ===")
        return 1
    print(f"=== BUILD TARGETS: GREEN — {len(targets)} target(s); every repository read their sources spell lies in "
          f"their declared inputs (bare root references, judged only by the dynamic proof: {residue}) ===")
    return 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────


def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    isolate()
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f" — {detail}"))

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
            "unit": Target("unit", "Unit", "tests/U/", ("@population-inputs",), ("@inert", "@tooling-lane")),
            "conformance": Target("conformance", "Conformance", "tests/C/",
                                  ("src/", "conformance-suite/", "tests/version-matrix/constructs/"),
                                  ("@inert", "@tooling-lane")),
        }
        facets = {"platform": "windows", "configuration": "Debug", "toolchain": "t1"}

        def keys_after(edit: dict[str, str], f: dict[str, str] = facets) -> dict[str, str]:
            repo.commit("edit", {**files, **edit}, allow_empty=True)
            return keys(repo.path, repo.git("rev-parse", "HEAD^{tree}"), f, targets)

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
        conf = Target("conformance", "Conformance", "tests/C/", ("src/", "conformance-suite/", "tests/nist/"), ("@inert",))
        planted = ('class T { void M() { var a = TestRepo.Nist("corpus.tsv"); var b = TestRepo.Suite("harness", x);\n'
                   ' var c = Path.Combine(TestRepo.Root, "conformance-suite", "cases"); var d = "src/X/Y.cs"; } }\n')
        undeclared_read = planted + 'class U { string e = TestRepo.VersionMatrix("inventory", id); }\n'
        top = ["src", "tests", "conformance-suite", "docs", "scripts"]
        audit = ScratchRepo.init(root / "audit")
        tdir = audit.path
        tracked_data = {"tests/nist/corpus.tsv": "x\n", "conformance-suite/harness/run.py": "x\n",
                        "conformance-suite/cases/c.cob": "c\n", "src/X/Y.cs": "class Y {}\n",
                        "docs/rearchitecture/x.json": "{}\n", "tests/version-matrix/inventory/r.json": "{}\n",
                        "tests/_shared/Helper.cs":
                            'static class Helper { static string P = TestRepo.Docs("rearchitecture", "x.json"); }\n',
                        "tests/C/T.cs": planted}
        audit.commit("planted", tracked_data)
        def undeclared_now() -> tuple[str, ...]:
            return scan(tdir, conf, top, _tracked(tdir)).undeclared

        clean = undeclared_now()
        audit.commit("undeclared", {"tests/C/U.cs": undeclared_read})
        planted_bad = undeclared_now()
        audit.commit("shared", {"tests/C/V.cs": "class V { void M() { Helper.Use(); } }\n"})
        through_shared = undeclared_now()
        arm("the BUILD TARGETS audit: sources whose reads lie in the declared inputs are clean; a planted read of "
            "tests/version-matrix/inventory (undeclared) is a finding naming file and line; a tests/_shared file is "
            "audited exactly when the assembly uses a type it declares",
            clean == () and planted_bad == ("tests/C/U.cs:3: tests/version-matrix/inventory/",)
            and "tests/_shared/Helper.cs:1: docs/rearchitecture/x.json" in through_shared, f"{clean} {planted_bad} {through_shared}")

        # a listing pattern matches as the test's .NET listing does: case-insensitive (review R1-2)
        guards = Target("conformance", "Conformance", "tests/C/", ("scripts/guard*.sh",), ())
        cased = [guards.matches(p) for p in ("scripts/guard-a.sh", "scripts/Guard-legacy.sh", "scripts/guard-x.SH",
                                             "scripts/sub/guard-a.sh", "scripts/other.sh")]
        g_repo = ScratchRepo.init(root / "guards")
        g_repo.commit("base", {"scripts/guard-a.sh": "a\n"})
        g0 = keys(g_repo.path, g_repo.git("rev-parse", "HEAD^{tree}"), facets, {"conformance": guards})
        g_repo.commit("cased", {"scripts/guard-a.sh": "a\n", "scripts/Guard-legacy.sh": "b\n"})
        g1 = keys(g_repo.path, g_repo.git("rev-parse", "HEAD^{tree}"), facets, {"conformance": guards})
        arm("a listing pattern (scripts/guard*.sh) matches a differently cased file and not one below or beside it, and "
            "adding scripts/Guard-legacy.sh changes the key",
            cased == [True, True, True, False, False] and g0 != g1, f"{cased} {g0} {g1}")
        real_targets = load_targets()
        arm("the real targets file loads: unit, conformance, characterization, each with its assembly, @build expanded",
            sorted(real_targets) == ["characterization", "conformance", "unit"]
            and "src/" in real_targets["conformance"].inputs and "@build" not in real_targets["conformance"].inputs
            and real_targets["unit"].inputs == ("@population-inputs",), str(real_targets.keys()))
    finally:
        remove_tree(root)
    bad = [n for n, ok, _ in results if not ok]
    print(f"=== buildgraph SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results)} arms"
          + (f"; failed: {'; '.join(bad)}" if bad else "") + ") ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd")
    k = sub.add_parser("keys", help="print each target's key at a tree")
    k.add_argument("--tree", default="HEAD^{tree}")
    k.add_argument("--repo", default=str(REPO))
    k.add_argument("--toolchain", required=True)
    k.add_argument("--platform", default="windows" if os.name == "nt" else "linux")
    k.add_argument("--configuration", default="Debug")
    sub.add_parser("check", help="the BUILD TARGETS audit")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.cmd == "check":
        return check()
    if args.cmd == "keys":
        facets = {"platform": args.platform, "configuration": args.configuration, "toolchain": args.toolchain}
        for name, value in keys(Path(args.repo), args.tree, facets).items():
            print(f"{name} {value}")
        return 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
