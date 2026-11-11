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
top-level tracked directory (`reads_in`, `_root_use`). A read outside the assembly's declared inputs is a finding naming
its file and line, and so, in a target declared narrowly (no `@population-inputs`), is a read it cannot resolve: a bare
repository root that no statement judgment covers (kb/Work PB5230 review R1-1). Its residue, named: in `repo-drift` the
bare roots it counts, and in every target what a process the test starts reads; the generator of the conformance suite
was traced instead (kb/Work PB5218), and the dynamic proof (the Linux shards' file watch, per target) is the remaining
work kb/Work PB5218 states.
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
    #: Judgments of the shared sources (tests/_shared, part of `@build`): every target that uses the file shares them.
    shared_judged = data["build"].get("judged") or {}
    targets: dict[str, Target] = {}
    for name, t in data["targets"].items():
        inputs: list[str] = []
        for p in t["inputs"]:
            inputs += list(build) if p == "@build" else [p]
        targets[name] = Target(name, t["assembly"], t["project"], tuple(inputs), tuple(t.get("exclude", ())),
                               tuple((k, v) for k, v in {**shared_judged, **(t.get("judged") or {})}.items()))
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


def _context(text: str, pos: int) -> str:
    """Where offset `pos` lies on its line: "code", "comment", "string" (a literal's text) or "hole" (an interpolated
    string's `{…}`), read by a small scanner of C# string and comment syntax from the line's start. A string a line
    break continues (a raw or verbatim literal over lines) is not followed; such a line reads as code, which is the
    side that reports a read rather than hides one."""
    i = text.rfind("\n", 0, pos) + 1
    stack: list[tuple[str, bool]] = []   # ("S" string | "I" interpolated | "H" hole, verbatim)
    while i < pos:
        c = text[i]
        kind, verbatim = stack[-1] if stack else ("code", False)
        if kind in ("S", "I"):
            if c == "\\" and not verbatim:
                i += 2
                continue
            if c == '"':
                if verbatim and text[i + 1:i + 2] == '"':
                    i += 2
                    continue
                stack.pop()
            elif kind == "I" and c == "{":
                if text[i + 1:i + 2] == "{":
                    i += 2
                    continue
                stack.append(("H", False))
            i += 1
            continue
        if c == "/" and text[i + 1:i + 2] == "/":
            return "comment"
        if c == '"':
            prefix = text[max(0, i - 2):i]
            stack.append(("I" if prefix.endswith("$") or prefix in ("$@", "@$") else "S", prefix.endswith("@")))
        elif c == "}" and kind == "H":
            stack.pop()
        i += 1
    if not stack:
        return "code"
    return {"S": "string", "I": "string", "H": "hole"}[stack[-1][0]]


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
    where = _context(text, start)
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
    spell that the inputs do not cover, as `<file>:<line>: <read>`. `roots`: the bare repository-root references no
    statement judgment covers (`TestRepo.Root` or an alias of it used in a form `_root_use` cannot resolve, `At()` with no
    literal segment, a top-level TreeScope prefix), as `<file>:<line>: <statement>`: a finding in a narrowly declared
    target (`findings_of`), counted in a broad one (kb/Work PB5230 review R1-1)."""
    undeclared: tuple[str, ...]
    roots: tuple[str, ...]
    #: The hand judgments (`judged` keys) a read or root of these sources matched: a judgment no target uses is stale.
    used: frozenset[str] = frozenset()


def scan(repo: Path, target: Target, top_dirs: list[str], tracked: tuple[set[str], set[str]],
         cls: inert_lane.Classification = inert_lane.CLASSIFICATION) -> Scan:
    """Read `target`'s sources once and return both halves of its audit. The tree's top directories and tracked paths
    are the caller's, computed once for every target (kb/Work PB5218 review R1-1: per-file recomputation cost ~60 s)."""
    judged = {k for k, _ in target.judged}
    used: set[str] = set()
    bad: set[str] = set()
    roots: set[str] = set()
    sources = {src.relative_to(repo).as_posix(): src.read_text(encoding="utf-8", errors="replace")
               for src in assembly_sources(repo, target)}
    aliases = root_aliases(sources)
    for rel, text in sources.items():
        lines = text.split("\n")
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
            read = _normalized(read, tracked)
            if read is not None and f"{rel}|{read}" in judged:
                used.add(f"{rel}|{read}")
                continue
            if read is None:
                continue   # names no tracked path (build output, a cache, a scratch name)
            if not target.covers(read, cls):
                bad.add(f"{rel}:{line}: {read}")
    return Scan(tuple(sorted(bad)), tuple(sorted(roots)), frozenset(used))


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
    findings = findings_of(targets, scans)
    for f in findings:
        print(f"  {f}")
    residue = ", ".join(f"{name} {len(sc.roots)}" for name, sc in scans.items() if targets[name].broad)
    if findings:
        print(f"=== BUILD TARGETS: RED — {len(findings)} finding(s): a repository read outside its target's declared "
              "inputs, an unresolved read in a narrowly declared target, or a stale judgment "
              "(scripts/build_targets.json) ===")
        return 1
    print(f"=== BUILD TARGETS: GREEN — {len(targets)} target(s); every repository read their sources spell lies in "
          f"their declared inputs, and every narrowly declared target resolves every read (bare root references of the "
          f"targets declaring every population input, judged by the dynamic proof: {residue or 'none'}) ===")
    return 0


def findings_of(targets: dict[str, Target], scans: dict[str, Scan]) -> list[str]:
    """THE AUDIT'S FINDINGS over every target's scan, each once: a read outside its target's inputs; in a target declared
    narrowly, an unresolved root; a hand judgment no target's sources match (stale)."""
    findings: list[str] = []
    for name, sc in scans.items():
        findings += [f"{name}: {b}" for b in sc.undeclared]
        # ⛔ A target declared narrowly (no `@population-inputs`) cannot cover a read it cannot resolve: an unresolved
        # root there is a finding naming its file and line, never a count (owner 2026-11-10: "If it cannot be resolved,
        # there is a problem/bug"; kb/Work PB5230 review R1-1).
        if not targets[name].broad:
            findings += [f"{name}: {r}  (an unresolved repository-root read: spell its path, declare its directory with "
                         "TestRepo.Within, or judge the statement by hand with its reason)" for r in sc.roots]
    used = set().union(*(sc.used for sc in scans.values()))
    findings += [f"stale judgment {k!r} (scripts/build_targets.json): no read or root of any target matches it; remove "
                 "it, or correct it" for t in targets.values() for k, _ in t.judged if k not in used]
    return list(dict.fromkeys(findings))


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
        arm("the real targets file loads: unit, repo-drift, conformance, characterization, each with its assembly, @build "
            "expanded; Unit's inputs are narrow (no @population-inputs) and repo-drift's are every population input",
            sorted(real_targets) == ["characterization", "conformance", "repo-drift", "unit"]
            and "src/" in real_targets["conformance"].inputs and "@build" not in real_targets["conformance"].inputs
            and "src/" in real_targets["unit"].inputs and "@population-inputs" not in real_targets["unit"].inputs
            and real_targets["repo-drift"].inputs == ("@population-inputs",)
            and real_targets["repo-drift"].assembly == "RepoDrift", str(real_targets.keys()))

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
        narrow = Target("n", "N", "tests/N/", ("src/",), (), (("tests/N/A.cs|var j = TestRepo.At(judged);", "planted"),
                                                              ("tests/N/A.cs|var gone = TestRepo.At(x);", "planted")))
        broad = Target("b", "B", "tests/B/", ("@population-inputs",), ())
        vrepo = ScratchRepo.init(root / "verdict")
        vrepo.commit("planted", {"src/S.cs": "class S {}\n",
                                 "tests/N/A.cs": "var j = TestRepo.At(judged);\nvar k = TestRepo.At(other);\n",
                                 "tests/B/B.cs": "var k = TestRepo.At(other);\n"})
        vtop, vtracked = _top_dirs(vrepo.path), _tracked(vrepo.path)
        vt = {"n": narrow, "b": broad}
        vf = findings_of(vt, {k: scan(vrepo.path, t, vtop, vtracked) for k, t in vt.items()})
        arm("R1-1: a bare root in a narrowly declared target is a finding naming file and line; its judged statement is "
            "not; in a broad target it is only counted; a judgment no source matches is a stale finding",
            len(vf) == 2 and any("tests/N/A.cs:2: var k = TestRepo.At(other);" in f for f in vf)
            and any("stale judgment" in f and "gone" in f for f in vf) and not any("tests/B/" in f for f in vf), str(vf))
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
