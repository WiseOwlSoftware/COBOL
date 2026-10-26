#!/usr/bin/env python3
"""impacted_tests.py — what a change REACHES, as the order plan's TIERS (kb/Work PB1683, PB1708 M11, PB1712).

The map ORDERS the gate's plan; it never FILTERS a leg (owner, 2026-09-28, kb/Work PB1708). Every gate PLANS the whole
discovered population of Conformance, Unit and Characterization into two legs; this script tells `scripts/gate_plan.py`
which tests a change can affect, so they come FIRST. Under batched gating (kb/Work PB2515) leg 1 is the only local run,
so these tiers decide which cases it holds, and the loop records a map for every main commit it adopts
(kb/Work PB3663) so that a change is attributed instead of falling back to the cheapest slice of everything. It reads
the RECORDED impact map
(`scripts/spec/record_impact_map.py`: per test, the METHODS its execution reached, each with the line ranges it
owns) and puts every recorded test in a tier (docs/rearchitecture/DESIGN-test-build-ci.md §3.13):

  tier 1  executed a changed method body (the direct hits);
  tier 2  reached only through a widening — a static constructor, a declaration change, the file level;
  tier 3  reached by nothing the change touched.

How a changed C# file reaches tests:
  * a hunk INSIDE the lines a method owns reaches the tests that executed that method (tier 1); a static
    constructor's change also reaches every test that executed any member of its type (tier 2 — a static
    initializer runs once);
  * a hunk that only touches blank or comment lines reaches nothing;
  * any other hunk — outside every method: a new member, a signature, a constant, an enum value, a field, an
    attribute, a `using` — reaches every test that executed ANY code in the file, plus every test that executed a
    file NAMING a type the file declares (tier 2: constants and enum values are compiled INTO their consumers);
  * without `--base` (files named on the command line) every file is taken at that file level.
A changed TEST file reaches tests the same way (the test assemblies are recorded too): an edited harness — a helper
a test calls — is tier 1 for the rows that executed it. It also names the tier-0a facts only the diff knows: the
test METHODS the change ADDS OR EDITS (`tests_changed`, kb/Work PB3190: a hunk inside a test method's attribute
block, declaration or body, or inside the `[MemberData]` source of a theory — the change's own tests, which the
batched gate's leg 1 must run, where tier 1 is only budgeted), and the suite tests whose files or keyed entries the
change touches (`gate_plan.py` puts their cases in leg 1). A whole changed test FILE is not tier 0a: a harness edit
reaches every row of its theory, and 68b X's (b4) put 5,774 cases and 62.8 % of the work ahead of its first red.
From the diff alone it names the tier-0d drift tests too: those the drift-rule index says govern a changed file
(kb/Work PB3531), and those governing a register that cites what the change removes — a deleted member, a renamed
test, a deleted file (`removals`, `drift_rules.citing`, kb/Work PB3131).

WHICH MAP SERVES THE BASE (kb/Work PB3663; `impact_store.nearest`): the one whose commit's tree differs from the base's
in the fewest source files, whatever its age or ancestry. A change to a file the two trees share is attributed by line
range exactly; a change to a file that DIFFERS between them (`drift`: main moved on after the map was recorded) is taken
at the FILE level, tier 1 for every test that executed any method of it, because the map's line ranges describe another
version of that file. So a map a few hours behind the base still tiers almost every change; the rule it replaces called
any map older than the base by more than documentation stale and tiered nothing.

EVERY TEST IS TIER 1, with the reason printed, whenever the change cannot be attributed: no map in the store at all, a
`.g4`, project, build or source-generator input, a src file the map has never seen, non-C# data under `src/`, a
method reached only outside every test's context. Nothing here can drop a test: a test the map under-reaches (static
FIELD reads are invisible to the recorder — PB1712; a test added since the map's commit is not in it) lands in a later
tier of the SAME plan and still runs in CI's merge-group run.

⛔ NO FILTER. This script prints no vstest filter and takes no `--plus`: M11 deleted the narrowing (a map for a base
re-armed a silent under-selection, PB1712), and M13 deleted the whole-assembly filter line and `--plus` together with
`build-local.ps1 -Filter` and every caller (kb/Work PB1721). Its one consumer is `scripts/gate_plan.py`, which the
gate driver `scripts/run_gate_legs.py` runs in-process; stdout is the tier summary, for people.

Usage:
    python scripts/spec/impacted_tests.py --base <sha>                 # the worktree's change against its base
    python scripts/spec/impacted_tests.py --base <sha> --plan tiers.json
    python scripts/spec/impacted_tests.py src/WiseOwl.Cobol.Compiler/Binding/DataBinder.Constants.cs
    python scripts/spec/impacted_tests.py --self-test
"""

from __future__ import annotations

import argparse
import codecs
import gzip
import json
import re
import subprocess
import sys
import tempfile
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import NamedTuple

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts"))
from test_population import GATED_ASSEMBLIES  # noqa: E402
import drift_rules  # noqa: E402  (scripts/spec, beside this file)
import cs_members  # noqa: E402  (scripts/spec, beside this file)
import impact_store  # noqa: E402  (scripts/spec, beside this file: THE store and the rule for which map serves a base)
from impact_store import SCHEMA, bit_ids, is_doc, pack, unpack  # noqa: E402
sys.path.append(str(REPO / "scripts" / "orchestrator"))  # appended: no module of it shadows one of scripts/ (as run_gate_legs.py)
from coord import is_work  # noqa: E402  (THE paths that are never work: coord.NEVER_WORK, kb/Work PB3465, PB3474)
from standard_streams import ensure_utf8  # noqa: E402
CONFORMANCE_DIR = "tests/WiseOwl.Cobol.Tests.Conformance/"
# The gated test projects, by the assembly name the map and the gate use ("Conformance", "Unit", ...).
TEST_ASSEMBLIES = {f"{d}/": k for k, d in GATED_ASSEMBLIES.items()}
SHARED_TEST_DIR = "tests/_shared/"   # linked into every test project
TEST_PROJECTS = (*TEST_ASSEMBLIES, SHARED_TEST_DIR)
# Structural inputs: every assembly may change.
STRUCTURAL_RE = re.compile(r"(\.g4|\.csproj|\.props|\.targets|\.sln|global\.json|nuget\.config|\.editorconfig)$"
                           r"|^src/WiseOwl\.Cobol\.Compiler\.SourceGen/|(^|/)Directory\.[^/]+$", re.IGNORECASE)
#: Two non-C# files under `src/` that no assembly embeds and no test executes: the public-API baselines of the Roslyn
#: analyzer (`<AdditionalFiles>` of the runtime project, RS0016/RS0017: read at build time) and the runtime ABI pin
#: (`RuntimeAbi.pin`, which scripts/abi_pin.py measures and RuntimeAbiPinDriftTests reads; the version the build uses is
#: `Directory.Build.props`, a structural input). A change to one is data a test may READ (`readers`), not "a non-C#
#: input under src/" that puts every test in tier 1. Measured (kb/Work PB3663): two of 22 code-changing landings of a
#: replayed day fell back to tier 1 for the baselines alone, and PB3666's own change for the pin alone.
READ_ONLY_DATA_RE = re.compile(r"(^|/)(PublicAPI\.(Shipped|Unshipped)\.txt|RuntimeAbi\.pin)$")
HUNK_RE = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")
#: The conformance suite's test trees (conformance-suite/harness/generate.py's TREES): a file of one of their clause
#: folders belongs to suite tests by id (`suite_tests`), and the two keyed files beside them name tests by id.
SUITE_TREES = ("conformance-suite/cases/", "conformance-suite/wiseowl/cases/")
SUITE_KEYED = ("conformance-suite/wiseowl/diagnostics.json", "conformance-suite/wiseowl/options.json")
HEAD: str | None = None  # --head: diff base..HEAD instead of the worktree (a replay, or a landed branch)
TIER_DIRECT, TIER_WIDENED, TIER_UNREACHED = 1, 2, 3


@dataclass
class Reach:
    """What a change reaches. `direct` and `widened` are masks of map entries (tiers 1 and 2); `every_tier1` holds
    the reasons the change cannot be attributed, which put every test in tier 1."""
    direct: int = 0
    widened: int = 0
    direct_names: set[str] = field(default_factory=set)            # display-name substrings reached outside the map
    changed_tests: dict[str, set[str]] = field(default_factory=dict)  # assembly -> test methods it adds or edits
    goldens: set[str] = field(default_factory=set)                 # suite test ids the change touches
    drift_tests: set[str] = field(default_factory=set)             # drift classes the index says govern a changed file
    every_tier1: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)


@dataclass
class Analysis:
    """One change against one map: the map that serves the base (`impact_store.nearest`; with drift, its line ranges
    describe the files the two trees share and a change to any other file is taken at the file level), what the change
    reaches, and each recorded test's tier per assembly, keyed by its RAW display name."""
    map: dict | None
    map_state: str            # "exact" · "documentation-only drift" · "nearest" (source files differ) · "none"
    reach: Reach
    tiers: dict[str, dict[str, int]]


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout.strip()


def load_map(path: Path) -> dict:
    with gzip.open(path, "rt", encoding="utf-8") as fh:
        m = json.load(fh)
    if m.get("schema") != SCHEMA:
        raise SystemExit(f"{path}: impact map schema {m.get('schema')} — this script reads schema {SCHEMA}; "
                         f"re-record it (record_impact_map.py)")
    return m


class MapIndex:
    """The map, decoded once: per file its entries and their line ranges; per recorded test its bitset. `drift` is the
    set of source files that differ between the map's commit and the base (`impact_store.Choice.drift`): their recorded
    line ranges describe another version of the file, so a change to one is taken at the file level."""

    def __init__(self, m: dict, drift: frozenset[str] = frozenset()):
        self.m = m
        self.drift = drift
        self.entries = m["entries"]
        self.ranges: dict[str, list[tuple[int, int, int]]] = {}   # file -> [(lo, hi, id)], lines 0 = type-level
        self.file_ids: dict[str, int] = {}                          # file -> mask of every entry in it
        for i, (_, segs) in enumerate(self.entries):
            for f, lo, hi in segs:
                self.ranges.setdefault(f, []).append((lo, hi, i))
                self.file_ids[f] = self.file_ids.get(f, 0) | (1 << i)
        self.implied_by: dict[int, int] = {}                        # cctor id -> mask of the entries implying it
        for k, cctors in m["implies"].items():
            for c in cctors:
                self.implied_by[c] = self.implied_by.get(c, 0) | (1 << int(k))
        # tests[i] = [assembly, method, display name, class, "assembly:collection", seconds, listed]
        self.tests = m["tests"]
        self.bits = [unpack(b) for b in m["test_bits"]]
        self.by_assembly: dict[str, list[int]] = {}
        for i, t in enumerate(self.tests):
            self.by_assembly.setdefault(t[0], []).append(i)
        self.class_bits = {k: unpack(v) for k, v in m["class_bits"].items()}            # "assembly:class"
        self.collection_bits = {k: unpack(v) for k, v in m["collection_bits"].items()}  # "assembly:collection"
        self.reached = 0
        for b in [*self.bits, *self.class_bits.values(), *self.collection_bits.values()]:
            self.reached |= b
        self.ambient = unpack(m.get("ambient_bits", ""))
        self.known = set(m["known_files"])
        self.executed_files = {f for f, ids in self.file_ids.items() if ids & self.reached}
        # (method name, declaring type's simple name) -> entries: what a new overload can steal calls from
        self.by_name: dict[tuple[str, str], int] = {}
        for i, (name, _) in enumerate(self.entries):
            if "::" not in name:
                continue
            owner, _, member = name.partition("::")
            simple = re.split(r"[./]", owner.split(" ")[-1])[-1]
            key = (member.split("(")[0], simple)
            self.by_name[key] = self.by_name.get(key, 0) | (1 << i)

    def named(self, path: str, method: str) -> int:
        """Every entry called `method` on a type the file declares (a constructor when it is the type's name)."""
        types = set(TYPE_DECL_RE_ALL.findall(text_of(REPO / path)))
        mask = 0
        for t in types:
            mask |= self.by_name.get((".ctor" if method == t else method, t), 0)
        return mask

    def expand(self, mask: int) -> int:
        """A changed static constructor reaches every test that touched its type."""
        for c, users in self.implied_by.items():
            if mask >> c & 1:
                mask |= users
        return mask

    def executed(self, i: int) -> int:
        """Everything test i's execution reached: its own context, its class's (constructor, fixture) and its
        collection's."""
        asm, _, _, cls, coll = self.tests[i][:5]
        return self.bits[i] | self.class_bits.get(f"{asm}:{cls}", 0) | self.collection_bits.get(coll, 0)


def changed_files(base: str) -> list[tuple[str, str]]:
    """(status, path) for every file the worktree changed against `base`, untracked files as added, coord.NEVER_WORK's
    paths aside (coord.is_work: the harness's settings file, the checkpoint file, the gate's results)."""
    out = []
    for line in git("diff", "--name-status", "--no-renames", base, *([HEAD] if HEAD else [])).splitlines():
        status, _, path = line.partition("\t")
        out.append((status[:1], path))
    if HEAD is None:  # the worktree: untracked files are added files
        out += [("A", p) for p in git("ls-files", "--others", "--exclude-standard").splitlines() if p]
    return sorted({(s, p) for s, p in out if p and is_work(p)}, key=lambda x: x[1])


class Hunk(NamedTuple):
    """One `-U0` hunk: where it sits on each side (a count of 0 is a pure insertion or deletion AFTER that line), the
    lines it removes and adds, and whether each of them carries code on its side (`cs_members.code_lines`)."""
    old_start: int
    old_count: int
    new_start: int
    new_count: int
    removed: list[str]
    added: list[str]
    removed_code: list[bool]
    added_code: list[bool]

    def trivial(self) -> bool:
        """Whether it changes only comment lines and blank lines outside every string literal."""
        return not any(self.removed_code) and not any(self.added_code)


def parse_hunks(diff: str, old: str, new: str) -> list[Hunk]:
    """The hunks of one C# file's `git diff -U0` output, whose two sides are the texts `old` and `new` ("" for an
    absent side). Which lines carry code is read from the LEXED texts (`cs_members.code_lines`), never from a diff
    line alone: a raw-string line `      *> note` or a blank line inside a string is code (kb/Work PB3190 review R1-1).
    Every `-`/`+` line after the first hunk header is content, one starting `--`/`++` included (the `---`/`+++` file
    headers precede the first hunk)."""
    out: list[Hunk] = []
    for line in diff.splitlines():
        h = HUNK_RE.match(line)
        if h:
            out.append(Hunk(int(h.group(1)), int(h.group(2) if h.group(2) is not None else 1),
                            int(h.group(3)), int(h.group(4) if h.group(4) is not None else 1), [], [], [], []))
        elif out and line.startswith("-"):
            out[-1].removed.append(line[1:])
        elif out and line.startswith("+"):
            out[-1].added.append(line[1:])
    old_code, new_code = cs_members.code_lines(old), cs_members.code_lines(new)
    for h in out:
        h.removed_code.extend(h.old_start + k in old_code for k in range(len(h.removed)))
        h.added_code.extend(h.new_start + k in new_code for k in range(len(h.added)))
    return out


def text_at(rev: str | None, path: str) -> str:
    """`path`'s text at the commit `rev` (None: the worktree), unstripped so its line numbers are the diff's. Its
    callers read a side the file EXISTS on, so a failed read raises: an empty text would make every line of the
    diff a non-code line, and a real edit would read as nothing changed."""
    if rev is None:
        return text_of(REPO / path)
    r = subprocess.run(["git", "show", f"{rev}:{path}"], cwd=REPO, capture_output=True, text=True, encoding="utf-8",
                       errors="replace")
    if r.returncode != 0:
        raise RuntimeError(f"impacted_tests: git show {rev}:{path} exited {r.returncode}: {r.stderr.strip()}")
    return r.stdout


def hunks(base: str, path: str) -> list[Hunk]:
    """The -U0 hunks of the C# file `path` against `base`."""
    return parse_hunks(git("diff", "-U0", "--no-color", "--no-renames", base, *([HEAD] if HEAD else []), "--", path),
                       text_at(base, path), text_at(HEAD, path))


MODIFIERS = r"(?:public|private|protected|internal|static|readonly|sealed|abstract|virtual|override|async|unsafe|new|" \
            r"extern|partial|const|file|required|volatile|event|implicit|explicit)"
DECL_RE = re.compile(rf"^\s*(?:\[[^\]]*\]\s*)*((?:{MODIFIERS}\s+)+)(.*)$")
TYPE_DECL_RE = re.compile(r"\b(class|record|struct|interface|enum)\s+([A-Za-z_]\w*)")
DISPATCH_RE = re.compile(r"\b(override|virtual|abstract|extern|partial|new|implicit|explicit|operator)\b")
TEST_ATTR_RE = re.compile(r"^\s*\[(Fact|Theory|InlineData|MemberData|ClassData)\b")


def method_ids(ix: MapIndex, path: str, h: Hunk) -> int | None:
    """The entries a hunk changes, or None when it is a DECLARATION change the method level cannot bound."""
    start, count = h.old_start, h.old_count
    ranges = [(lo, hi, i) for lo, hi, i in ix.ranges.get(path, []) if lo > 0]
    mask = 0
    if count == 0:  # pure insertion after line `start`
        inside = [i for lo, hi, i in ranges if lo <= start and start + 1 <= hi]
        for i in inside:
            mask |= 1 << i
        return mask if inside else inserted_members(ix, path, h)
    for n, code in zip(range(start, start + count), h.removed_code):
        owners = [i for lo, hi, i in ranges if lo <= n <= hi]
        if owners:
            for i in owners:
                mask |= 1 << i
        elif code:
            return None  # a declaration line changed or removed: a signature, a constant, a field, an attribute
    extra = inserted_members(ix, path, h)
    return None if extra is None else mask | extra


def inserted_members(ix: MapIndex, path: str, h: Hunk) -> int | None:
    """What ADDED code can change beyond the lines it adds. A new ordinary method runs only when changed code calls
    it — except through overload resolution, so it reaches the tests that reached any same-named method of the
    file's types. Anything that changes dispatch or state for UNCHANGED code — an override, a virtual, an
    operator or conversion, an extension method, a field, property or constant, a type with a base list, an
    attribute — returns None (a declaration change). A new xunit test attribute adds nothing here: the test
    method it declares or feeds is tier 0a (`Reach.changed_tests`). Only the hunk's added CODE lines are read
    (`Hunk.added_code`)."""
    mask = 0
    for line, code in zip(h.added, h.added_code):
        if not code or TEST_ATTR_RE.match(line):
            continue
        if re.match(r"^\s*\[", line):
            return None  # an attribute: read by reflection, not by a call
        t = TYPE_DECL_RE.search(line)
        if t and DECL_RE.match(line):
            if ":" in line.split(t.group(2), 1)[1] or t.group(1) in ("interface", "enum"):
                return None
            continue
        d = DECL_RE.match(line)
        if not d:
            continue  # a body line of the added member
        mods, rest = d.group(1), d.group(2)
        if DISPATCH_RE.search(mods) or DISPATCH_RE.search(rest.split("(", 1)[0]):
            return None
        call = re.search(r"([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(", rest)
        if not call or "=" in rest.split("(", 1)[0] or re.search(r"\(\s*this\s", rest):
            return None  # a field, property, constant or event; or an extension method
        mask |= ix.named(path, call.group(1))
    return mask


_TEXT: dict[Path, str] = {}


def text_of(path: Path) -> str:
    """A file's text, read once per run ("" when unreadable) — the consumer and corpus searches re-read the same
    thousand files for every changed file otherwise."""
    if path not in _TEXT:
        try:
            _TEXT[path] = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            _TEXT[path] = ""
    return _TEXT[path]


def test_sources() -> list[Path]:
    out = []
    for proj in TEST_PROJECTS:
        out += [p for p in (REPO / proj).rglob("*.cs") if "/bin/" not in p.as_posix() and "/obj/" not in p.as_posix()]
    return out


def readers(name: str, sources: list[Path]) -> list[str]:
    """Test source files (repo-relative) that name `name` inside a STRING LITERAL — the tests that can open a data
    or doc file. A mention in a comment (`<c>DESIGN-….md</c> §3.12`) is a citation, not a read; counting it made
    every harness file that cites its design doc a reader of it."""
    literal = re.compile(r'"[^"\n]*' + re.escape(name) + r'[^"\n]*"')
    return [p.relative_to(REPO).as_posix() for p in sources if literal.search(text_of(p))]


def classes_declared(path: Path) -> list[str]:
    return sorted(set(re.findall(r"\b(?:class|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)", text_of(path))))


def assemblies_of(path: str) -> list[str]:
    """The gated test assemblies a test source file is compiled into (a `tests/_shared/` file: all of them)."""
    if path.startswith(SHARED_TEST_DIR):
        return list(TEST_ASSEMBLIES.values())
    return [a for d, a in TEST_ASSEMBLIES.items() if path.startswith(d)]


TYPE_DECL_RE_ALL = re.compile(r"\b(?:class|record|struct|enum|interface)\s+([A-Za-z_]\w*)")


def declared_types(texts: list[str]) -> list[str]:
    """The OUTERMOST types a file declares — those at its shallowest declaration indentation. A nested type is
    named from outside only through its container, and a private nested `Entry` or `Result` would otherwise
    match half the tree."""
    names: set[str] = set()
    for text in texts:
        decls = [(len(m.group(1)), m.group(2)) for m in
                 re.finditer(r"(?m)^([ \t]*)(?:[\w\[\]]+\s+)*(?:class|record|struct|enum|interface)\s+([A-Za-z_]\w{3,})",
                             text)]
        if decls:
            top = min(d for d, _ in decls)
            names |= {n for d, n in decls if d == top}
    return sorted(names)


def consumers(ix: MapIndex, path: str, base: str | None) -> list[str] | None:
    """The EXECUTED files that name a type `path` declares (in the base or the worktree version) — the code a
    constant, an enum value or a type shape is compiled into. None when it declares no type."""
    texts = [text_of(REPO / path)]
    if base:
        try:
            texts.append(git("show", f"{base}:{path}"))
        except subprocess.CalledProcessError:
            pass
    types = declared_types(texts)
    if not types:
        return None
    pattern = re.compile(r"\b(?:" + "|".join(map(re.escape, types)) + r")\b")
    return [f for f in sorted(ix.executed_files)
            if f != path and f.endswith(".cs") and pattern.search(text_of(REPO / f))]


def suite_tests(path: str) -> set[str] | None:
    """The conformance-suite tests (by id: the suite runner's rows carry `id: "<id>"`) a file of a clause folder
    belongs to: the test itself (`<id>.cob`), the test an expected output names (`<id>[.<edition>][.<item>=<value>]
    .out`, conformance-suite/README.md), or every test of the folder whose header declares the file (`files`, a
    WiseOwl library folder's `<library>/<file>` included), read by the generator's one header reader. None when no
    test of the folder claims it."""
    tree = next(t for t in SUITE_TREES if path.startswith(t))
    p = Path(path)
    rel = Path(path[len(tree):]).parts   # (<clause folder>, <file>) or (<clause folder>, <library>, <file>)
    if len(rel) < 2:
        return None
    if len(rel) == 2 and p.suffix.lower() == ".cob":
        return {p.stem}
    if len(rel) == 2 and p.suffix == ".out":
        return {p.name.split(".")[0]}
    sys.path.insert(0, str(REPO / "conformance-suite" / "harness"))
    import generate  # noqa: PLC0415  (the suite's one header reader; only a change to a declared file pays for it)
    declared = "/".join(rel[1:])
    owners = set()
    for cob in sorted((REPO / tree / rel[0]).glob("*.cob")):
        header, _ = generate.read_header(text_of(cob))
        if header is not None and declared in (header.fields.get("files") or []):
            owners.add(cob.stem)
    return owners or None


def changed_entries(path: str, base: str) -> set[str]:
    """The entries of a keyed JSON file (`wiseowl/diagnostics.json`, `wiseowl/options.json`: test id -> its entry)
    that the change ADDS or CHANGES (present now with a value other than the base's); `$`-keys are documentation."""
    def entries(rev: str | None) -> dict:
        try:
            text = git("show", f"{rev}:{path}") if rev else text_of(REPO / path)
            return {k: v for k, v in json.loads(text).items() if not k.startswith("$")}
        except (subprocess.CalledProcessError, ValueError, AttributeError):
            return {}  # absent on that side (an added or deleted file)
    now, then = entries(HEAD), entries(base)
    return {k for k, v in now.items() if then.get(k) != v}


class MapChoice(NamedTuple):
    """The map that serves a base: `map` None when the store holds none; `state` "exact" (the map's commit IS the base),
    "documentation-only drift" (the two trees differ in documentation only), "nearest" (source files differ: `drift`)
    or "none"; `drift` the source files that differ between the map's commit and the base."""
    map: dict | None
    state: str
    drift: frozenset[str]


def explicit_map(path: Path, base: str) -> MapChoice:
    """A map named on the command line (`--map`): served as `find_map` serves one of the store, by the files that
    differ between its commit's tree and `base`'s. A commit git does not know (a hand-made map) tells nothing, so the
    map is taken as describing the base exactly, as it always was."""
    m = load_map(path)
    drift = impact_store.drift_between(REPO, str(m.get("commit", "")), base)
    if drift is None or m.get("commit") == base:
        return MapChoice(m, "exact", frozenset())
    return MapChoice(m, "nearest" if drift else "documentation-only drift", frozenset(drift))


def find_map(store: Path, base: str, reach: Reach) -> MapChoice:
    """The map that serves `base` (`impact_store.nearest`: the fewest source files differing between its commit and the
    base, whatever its age or ancestry; kb/Work PB3663). Its line ranges describe every file the two trees share
    exactly, so only a file in `drift` is taken at the file level (`reach_cs`); no usable map at all puts every test in
    tier 1, with the reason printed."""
    choice = impact_store.nearest(store, base, REPO)
    if choice is None:
        reach.every_tier1.append(f"no impact map recorded in {store} for the base {base[:12]} (the loop records one for "
                                 f"every main commit it adopts; by hand: record_impact_map.py --commit {base[:12]})")
        return MapChoice(None, "none", frozenset())
    try:
        m = load_map(choice.path)
    except (OSError, EOFError, ValueError) as e:   # gzip.BadGzipFile is an OSError; a torn JSON body a ValueError
        # A map pruned or replaced between the choice and the read, or torn by a killed recorder: the plan goes on
        # unaimed and says why, never fails (a schema mismatch is another matter: load_map exits, loudly).
        reach.every_tier1.append(f"the impact map {choice.sha[:12]} for the base {base[:12]} could not be read "
                                 f"({type(e).__name__}: {e}) — record it again")
        return MapChoice(None, "none", frozenset())
    if choice.sha == base:
        return MapChoice(m, "exact", frozenset())
    if not choice.drift:
        reach.notes.append(f"map {choice.sha[:12]} differs from the base {base[:12]} only by documentation — used")
        return MapChoice(m, "documentation-only drift", frozenset())
    reach.notes.append(f"map {choice.sha[:12]} serves the base {base[:12]}: {len(choice.drift)} source file(s) differ "
                       f"between them (first: {choice.drift[0]}) — a change to one of those is taken at the file level, "
                       f"every other file by line range")
    return MapChoice(m, "nearest", frozenset(choice.drift))


def tests_changed(text: str, diff: list[Hunk] | None) -> set[str]:
    """The xunit test methods of a C# source (`text`, the changed side) that a diff ADDS OR EDITS (kb/Work PB3190): a
    test method whose lines — its attribute block, its declaration, its body — a hunk touches, and a theory whose
    `[MemberData]` source a hunk touches (its rows are that member's values). A hunk that changes only comment lines
    and blank lines outside every string literal touches nothing (`Hunk.trivial`: a string's lines, the embedded COBOL
    program a test compiles, are code). `diff` None (a new file, or one named without a diff), or a source whose code
    braces do not balance once lexed (`cs_members.balance`: the spans cannot be trusted): every test method."""
    found = cs_members.members(text)
    if diff is None or cs_members.balance(text):
        return {m.name for m in found if m.test}
    real = [h for h in diff if not h.trivial()]
    touched = {m for m in found if any(m.touched_by(h.new_start, h.new_count) for h in real)}
    sources = {m.name for m in touched if not m.test}
    return {m.name for m in found if m.test and (m in touched or sources.intersection(m.data))}


def reach_test_file(path: str, base: str | None, reach: Reach) -> None:
    """Tier 0a from the diff alone: the test methods a changed test source file ADDS OR EDITS (`tests_changed`; every
    one of them when the file is new or named without a diff), in each assembly the file is compiled into."""
    text = text_at(HEAD, path)
    existed = base is not None and subprocess.run(["git", "cat-file", "-e", f"{base}:{path}"], cwd=REPO,
                                                  capture_output=True).returncode == 0
    changed = tests_changed(text, hunks(base, path) if existed else None)
    if existed and cs_members.balance(text):
        reach.notes.append(f"{path}: its code braces do not balance once lexed (cs_members.balance) — every test "
                           f"method of it is taken as edited")
    for asm in assemblies_of(path):
        reach.changed_tests.setdefault(asm, set()).update(changed)
    if changed:
        reach.notes.append(f"{path}: adds or edits test method(s) {', '.join(sorted(changed))} — tier 0a")


def reach_cs(ix: MapIndex, path: str, status: str, base: str | None, reach: Reach) -> None:
    if path in ix.drift:
        drifted_change(ix, path, base, reach)
        return
    if path not in ix.ranges:
        if path.startswith(TEST_PROJECTS):
            return  # never executed by the recording: its added and edited tests are tier 0a (reach_test_file)
        if path not in ix.known:
            reach.every_tier1.append(f"{path}: the map has never seen it (a file NEW since the map's commit)")
            return
        declaration_change(ix, path, base, reach, "declarations only (no method of it has sequence points)")
        return
    if base is None or status != "M":
        declaration_change(ix, path, base, reach, "named without a diff" if base is None else f"status {status}")
        return
    mask = 0
    for h in hunks(base, path):
        ids = method_ids(ix, path, h)
        if ids is None:
            declaration_change(ix, path, base, reach, f"a change at line {h.old_start} outside every method")
            return
        mask |= ids
    reach.direct |= mask
    reach.widened |= ix.expand(mask)
    if bad := ambient_only(ix, mask):
        reach.every_tier1.append(f"{len(bad)} changed method(s) executed only OUTSIDE every test's context "
                                 f"(e.g. {bad[0]}) — cannot attribute them")
    names = sorted({ix.entries[i][0].split("::")[-1].split("(")[0] for i in bit_ids(mask)})
    reach.notes.append(f"{path}: {len(names)} changed method(s) {', '.join(names[:6])}"
                       + (" …" if len(names) > 6 else "") if names else f"{path}: comments and blank lines only")


def ambient_only(ix: MapIndex, mask: int) -> list[str]:
    """⛔ The entries of `mask` reached ONLY outside every test's context (the ambient array, never a test's). In a test
    file that is xunit DISCOVERY running a theory's data source — the theories naming a changed source are already tier
    0a (`tests_changed`) — so those are no finding; in product code it is a finalizer or a thread that does not flow the
    context, and nothing attributes it. Asked of the methods a hunk lands in and of a file no test reached anything of,
    never of a file-level widening that merely CONTAINS such a method (a finalizer beside test-reached code does not make
    a change to the file unattributable; measured: two of 22 replayed landings, both editing WiseOwl.Cobol.Runtime.CobolObject)."""
    return [ix.entries[i][0] for i in bit_ids(mask & ix.ambient & ~ix.reached)
            if not all(f.startswith(TEST_PROJECTS) for f, _, _ in ix.entries[i][1])]


def drifted_change(ix: MapIndex, path: str, base: str | None, reach: Reach) -> None:
    """A change to a file that DIFFERS between the map's commit and the base (kb/Work PB3663): the map's line ranges
    describe another version of it, so which method the change lands in cannot be read from them. Every test that
    executed any method of the file is tier 1 (the file-level superset of the method-level answer), and the file-level
    widening of `declaration_change` follows. A file the map has never seen is unattributable."""
    if path not in ix.known:
        reach.every_tier1.append(f"{path}: differs between the map's commit and the base, and the map has never seen it")
        return
    reach.direct |= ix.file_ids.get(path, 0)
    declaration_change(ix, path, base, reach, "differs between the map's commit and the base (its recorded line ranges "
                                              "describe another version)")


def declaration_change(ix: MapIndex, path: str, base: str | None, reach: Reach, why: str) -> None:
    """The file level: every test that executed the file, and every test that executed a file naming its types."""
    users = consumers(ix, path, base)
    own = ix.file_ids.get(path, 0)
    if not own and not users:
        reach.every_tier1.append(f"{path}: {why}, and no executed file names a type it declares — cannot bound it")
        return
    if not (own & ix.reached) and not any(ix.file_ids[u] & ix.reached for u in users or []) and (bad := ambient_only(ix, own)):
        reach.every_tier1.append(f"{path}: {why}, and every entry of it ran only OUTSIDE every test's context "
                                 f"(e.g. {bad[0]}) — cannot attribute it")
        return
    reach.widened |= ix.expand(own)
    for u in users or []:
        reach.widened |= ix.file_ids[u]
    reach.notes.append(f"{path}: {why} — the file level, plus the {len(users or [])} executed file(s) naming its types")


def removed_lines(diff: str) -> dict[str, list[str]]:
    """The removed lines of a `git diff -U0` output, per old path. A file header (`--- a/<path>`) is read only between
    `diff --git` and the first `@@`, so a removed line whose text is `-- a/x` stays a removed line; `--- /dev/null`
    (an added file) removes nothing."""
    removed: dict[str, list[str]] = {}
    path, header = None, False
    for line in diff.splitlines():
        if line.startswith("diff --git "):
            path, header = None, True
        elif header and line.startswith("--- "):
            path = header_path(line[4:])
        elif line.startswith("@@"):
            header = False
        elif path and not header and line.startswith("-"):
            removed.setdefault(path, []).append(line[1:])
    return removed


def header_path(field: str) -> str | None:
    """The old path a patch's `--- ` header names (`field`, the header after `--- `), or None for `/dev/null`. git ends
    the field with a TAB when the path holds a space (`docs/GnuCOBOL extensions.md`), and quotes it C-style when the
    path holds a quote, a backslash or a control character (PB3131 review R1-2)."""
    field = field.rstrip("\t")
    if len(field) > 1 and field[0] == field[-1] == '"':
        field = codecs.escape_decode(field[1:-1].encode("utf-8"))[0].decode("utf-8", errors="replace")
    return field[2:] if field.startswith("a/") else None


def tree_before(base: str) -> list[str]:
    """The paths of `base`'s tree: what a register's path citation resolved against before the change."""
    out = subprocess.run(["git", "-c", "core.quotePath=false", "ls-tree", "-r", "-z", "--name-only", base], cwd=REPO,
                         check=True, capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
    return [p for p in out.split("\0") if p]


def removals(changes: list[tuple[str, str]], base: str | None) -> list[drift_rules.Removal]:
    """What the diff against `base` takes away from each changed file that a register can cite (a deleted file, an
    identifier gone from its text, the types it declares): `drift_rules.citing`'s input. One `git diff` for the whole
    change; a file's new text is the worktree's (or `HEAD`'s for a replay). Empty without a base (files named on the
    command line have no diff)."""
    if not base:
        return []
    def lenient(*args: str) -> str:   # a register or a NIST file need not be UTF-8: never fail the plan on one
        return subprocess.run(["git", "-c", "core.quotePath=false", *args], cwd=REPO, check=True, capture_output=True,
                              text=True, encoding="utf-8", errors="replace").stdout

    status = dict((p, s) for s, p in changes)
    removed = removed_lines(lenient("diff", "-U0", "--no-color", "--no-renames", base, *([HEAD] if HEAD else [])))
    out = []
    for p, lines in sorted(removed.items()):
        if status.get(p) == "D":
            after = None
        elif HEAD:
            after = lenient("show", f"{HEAD}:{p}")
        else:
            after = text_of(REPO / p)
        r = drift_rules.removal_of(p, lines, after)
        if r is not None:
            out.append(r)
    return out


def reach_of(ix: MapIndex | None, changes: list[tuple[str, str]], base: str | None, reach: Reach) -> None:
    """Fill `reach` for the changed files. The tier-0a facts (added and edited test methods, suite tests) and the
    tier-0d ones come from the diff alone and are gathered with or without a map: the drift tests the drift-rule
    index says govern a changed file (kb/Work PB3531), and those governing a register that cites what the change
    removes — a deleted file, a deleted member, a renamed test (`drift_rules.citing`, kb/Work PB3131). The tiers
    need the map."""
    if changes:
        rows = drift_rules.index()
        reach.drift_tests |= drift_rules.governed(rows, [path for _, path in changes])
        cited: dict[tuple[str, str], list[str]] = {}
        removed = removals(changes, base)
        tree = tree_before(base) if base and any(r.deleted for r in removed) else None
        for cls, at in drift_rules.citing(rows, removed, tree=tree).items():
            cited.setdefault(at, []).append(cls)
        for (data, cite), classes in sorted(cited.items()):
            reach.drift_tests.update(classes)
            reach.notes.append(f"{data}: cites '{cite}', which the change removes — its drift test(s) "
                               f"{', '.join(sorted(classes))} (tier 0d)")
    sources = None
    for status, path in changes:
        if path.startswith("tools/impact/"):
            reach.notes.append(f"{path}: the impact recorder itself — only a recording build compiles it")
            continue
        if STRUCTURAL_RE.search(path):
            reach.every_tier1.append(f"{path}: a grammar, build or source-generator input — every assembly may change")
            continue
        if path.endswith(".cs") and path.startswith(("src/", "tests/")):
            if path.startswith("tests/") and not path.startswith(TEST_PROJECTS):
                reach.notes.append(f"{path}: in a test project outside the three gated assemblies — no gated test "
                                   f"runs it")
                continue
            if ix is not None:
                reach_cs(ix, path, status, base, reach)
            if path.startswith(TEST_PROJECTS) and status != "D":  # a deleted file has no test left to run
                reach_test_file(path, base, reach)
            continue
        if path.startswith("src/") and not READ_ONLY_DATA_RE.search(path):
            reach.every_tier1.append(f"{path}: a non-C# input under src/ (embedded data or resources)")
            continue
        if path in SUITE_KEYED:
            if base is None:
                reach.notes.append(f"{path}: named without a diff — which entries changed is unknown")
            else:
                changed = changed_entries(path, base)
                reach.goldens |= changed
                reach.notes.append(f"{path}: entries added or changed: {', '.join(sorted(changed)) or 'none'}")
            continue
        if path.startswith(SUITE_TREES):
            tests = suite_tests(path)
            if tests is None:
                reach.every_tier1.append(f"{path}: a suite file no test of its folder declares — cannot attribute it")
            else:
                reach.goldens |= tests
                reach.notes.append(f"{path}: the suite test(s) {', '.join(sorted(tests))}")
            continue
        if ix is None:
            continue
        if path.startswith("tests/nist/"):
            reach.direct_names.add("Nist")
        if path.startswith("tests/version-matrix/"):
            reach.direct_names.add("VersionMatrix")
        # Anything else is DATA some test may READ: the tests whose source names it.
        sources = sources if sources is not None else test_sources()
        name = Path(path).name
        who = readers(name, sources)
        if who:
            for r in who:
                if r in ix.file_ids:
                    reach.direct |= ix.file_ids[r]
                else:
                    reach.direct_names.update(f".{c}." for c in classes_declared(REPO / r))
            reach.notes.append(f"{path}: read by {len(who)} test source file(s) that name '{name}'")
        elif path.startswith(("tests/nist/", "tests/version-matrix/")):
            reach.notes.append(f"{path}: the {'Nist' if 'nist' in path else 'VersionMatrix'} tests read it")
        elif is_doc(path) or path.endswith("manifest.json"):
            reach.notes.append(f"{path}: no test source names it — documentation or data the tiers do not need")
        elif path.startswith("tests/"):
            reach.every_tier1.append(f"{path}: test data no test source names and no rule attributes")
        else:
            reach.notes.append(f"{path}: no test source names it and it is not built — not a test input")

def tiers(ix: MapIndex, reach: Reach) -> dict[str, dict[str, int]]:
    """Per assembly, per recorded display name, its tier (1 · 2 · 3). Rows sharing a display name (xunit truncates
    long theory arguments) take the lowest tier among them."""
    out: dict[str, dict[str, int]] = {}
    for i, t in enumerate(ix.tests):
        asm, display = t[0], t[2]
        if reach.every_tier1:
            tier = TIER_DIRECT
        else:
            executed = ix.executed(i)
            if executed & reach.direct or any(n in display for n in reach.direct_names):
                tier = TIER_DIRECT
            elif executed & reach.widened:
                tier = TIER_WIDENED
            else:
                tier = TIER_UNREACHED
        per = out.setdefault(asm, {})
        per[display] = min(per.get(display, TIER_UNREACHED), tier)
    return out


def analyse(base: str | None, files: list[str] | None = None, map_path: Path | None = None,
            store: Path | None = None) -> Analysis:
    """The change (the worktree, or `HEAD`, against `base`; or `files` at the file level) against its map."""
    reach = Reach()
    base = git("rev-parse", base) if base else None
    if map_path:
        found = explicit_map(map_path, base or git("rev-parse", "HEAD"))
        if found.map.get("partial"):
            reach.notes.append(f"⚠ {map_path} is a PARTIAL (trial) map over {found.map.get('filter')!r}")
    else:
        found = find_map(store or impact_store.default_store(REPO), base or git("rev-parse", "HEAD"), reach)
    ix = MapIndex(found.map, found.drift) if found.map is not None else None
    changes = [("M", f) for f in files] if files else (changed_files(base) if base else [])
    if not changes:
        reach.notes.append("no changed files")
    reach_of(ix, changes, base if not files else None, reach)
    return Analysis(found.map, found.state, reach, tiers(ix, reach) if ix is not None else {})


def plan_json(a: Analysis) -> dict:
    """The analysis as `--plan` writes it — the same one gate_plan.py computes in-process through `analyse`."""
    return {"map": a.map["commit"] if a.map else None, "map_state": a.map_state,
            "every_tier1": a.reach.every_tier1, "tiers": a.tiers,
            "tier0a": {"changed_tests": {k: sorted(v) for k, v in sorted(a.reach.changed_tests.items())},
                       "goldens": sorted(a.reach.goldens)},
            "tier0d": {"drift_tests": sorted(a.reach.drift_tests)},
            "notes": a.reach.notes}


def synthetic_map() -> dict:
    """Two methods in A.cs (lines 10-20 and 30-40), a finalizer there (42-44) that only the ambient array ever reached, a
    static constructor of A implied by One(), a type-level entry in B.cs reached only ambiently, and three Conformance
    tests: M1 reaches One(), M2 only the constructor, M3 nothing of A."""
    return {"schema": SCHEMA, "commit": "0" * 40,
            "entries": [["N.A::One()", [["src/WiseOwl.Cobol.Compiler/A.cs", 10, 20]]],
                        ["N.A::Two()", [["src/WiseOwl.Cobol.Compiler/A.cs", 30, 40]]],
                        ["N.B <type-level>", [["src/WiseOwl.Cobol.Compiler/B.cs", 0, 0]]],
                        ["N.A::.cctor()", [["src/WiseOwl.Cobol.Compiler/A.cs", 5, 8]]],
                        ["N.A::Finalize()", [["src/WiseOwl.Cobol.Compiler/A.cs", 42, 44]]]],
            "implies": {"0": [3]},
            "known_files": ["src/WiseOwl.Cobol.Compiler/A.cs", "src/WiseOwl.Cobol.Compiler/B.cs",
                            "src/WiseOwl.Cobol.Compiler/Enum.cs"],
            "tests": [["Conformance", "N.T.M1", "N.T.M1", "N.T", "Conformance:c1", 1.0, True],
                      ["Conformance", "N.T.M2", "N.T.M2", "N.T", "Conformance:c1", 1.0, True],
                      ["Conformance", "N.T.M3", "N.T.M3", "N.T", "Conformance:c1", 1.0, True]],
            "test_bits": [pack(0b0001), pack(0b1000), pack(0)], "class_bits": {}, "collection_bits": {},
            "ambient_bits": pack(0b10100)}


def worktree_changes_in_scratch_repo() -> list[tuple[str, str]]:
    """changed_files of a scratch repository whose worktree holds every kind of change: the self-test's subject, run
    with the module's REPO pointed at the scratch repository (the git() helper reads it)."""
    from scratch_repo import ScratchRepo  # noqa: PLC0415  (scripts/: the one scratch-repository fixture, kb/Work PB3584)
    global REPO
    saved = REPO
    with tempfile.TemporaryDirectory() as d:
        try:
            scratch = ScratchRepo.init(Path(d) / "repo")
            scratch.commit("base", {".claude/settings.local.json": "{}\n", "src/A.cs": "class A {}\n"})
            base = scratch.git("rev-parse", "HEAD")
            scratch.write({".claude/settings.local.json": '{"edited": 1}\n', "src/A.cs": "class A { int X; }\n",
                           "STATUS.md": "STATUS-AT: x\n", "TestResults/a/b.trx": "x\n", "src/New.cs": "class N {}\n"})
            REPO = scratch.path
            return changed_files(base)
        finally:
            REPO = saved


def map_service_arms(check) -> None:
    """Which map serves a base, driven through `analyse` on a scratch repository (kb/Work PB3663). The synthetic map is
    stored under the first commit's name; later commits are 'main moving on' (documentation, B.cs, A.cs, a new file),
    and each scenario analyses one edit of A.cs line 12 (inside `One()`, lines 10-20 of the map) against a base. The
    module's REPO and HEAD point at the scratch repository for the duration."""
    from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    global REPO, HEAD
    saved = (REPO, HEAD)
    a_path, b_path = "src/WiseOwl.Cobol.Compiler/A.cs", "src/WiseOwl.Cobol.Compiler/B.cs"
    a_lines = [f"    int x{n} = {n};" for n in range(1, 46)]
    a_edited = [*a_lines[:11], "    int x12 = 99;", *a_lines[12:]]

    def text(lines: list[str]) -> str:
        return "\n".join(lines) + "\n"

    tmp = scratch_dir("impacted-map-service-")
    try:
        repo = ScratchRepo.init(tmp / "repo")
        repo.commit("the map's commit", {a_path: text(a_lines), b_path: "b\n", "docs/d.md": "d\n"})
        recorded = repo.git("rev-parse", "HEAD")
        repo.commit("documentation moves", {"docs/d.md": "d2\n", "scripts/t.py": "t\n"})
        docs = repo.git("rev-parse", "HEAD")
        repo.commit("B.cs moves", {b_path: "b2\n"})
        b_moved = repo.git("rev-parse", "HEAD")
        repo.commit("A.cs moves", {a_path: text([*a_lines[:30], "    int y = 0;", *a_lines[31:]])})
        a_moved = repo.git("rev-parse", "HEAD")
        repo.commit("a new file", {"src/WiseOwl.Cobol.Compiler/New.cs": "n\n"})
        new_file = repo.git("rev-parse", "HEAD")
        store = tmp / "store"
        store.mkdir()
        with gzip.open(impact_store.map_file(store, recorded), "wt", encoding="utf-8", newline="\n") as fh:
            json.dump(synthetic_map(), fh)

        def served(label: str, base: str, edited: str, text_after: str, want_state: str, want_drift: set[str],
                   want_tiers: dict[str, int] | None, every_tier1: bool = False) -> None:
            """The edit of `edited` (to `text_after`) on top of `base`, analysed against the store."""
            global HEAD
            repo.git("checkout", "-q", "--detach", base)
            repo.commit(f"edit for {label}", {edited: text_after})
            try:
                REPO, HEAD = repo.path, repo.git("rev-parse", "HEAD")
                found = find_map(store, base, Reach())
                a = analyse(base, store=store)
                got = a.tiers.get("Conformance")
                check(label, a.map_state == want_state and set(found.drift) == want_drift
                      and bool(a.reach.every_tier1) == every_tier1 and (want_tiers is None or got == want_tiers),
                      f"{a.map_state}; drift {sorted(found.drift)}; {a.reach.every_tier1[:1]}; {got}")
            finally:
                REPO, HEAD = repo.path, None

        REPO = repo.path
        served("a map older than the base by documentation only describes it exactly (tier 1: the method edited)",
               docs, a_path, text(a_edited), "documentation-only drift", set(), {"N.T.M1": 1, "N.T.M2": 3, "N.T.M3": 3})
        served("a map older than the base by OTHER source files still attributes a file the trees share by line range",
               b_moved, a_path, text(a_edited), "nearest", {b_path}, {"N.T.M1": 1, "N.T.M2": 3, "N.T.M3": 3})
        served("a file that differs between the map's commit and the base is taken at the file level: every test that "
               "executed any method of it is tier 1", a_moved, a_path, text([*a_edited[:30], "    int y = 1;", *a_edited[31:]]),
               "nearest", {a_path, b_path}, {"N.T.M1": 1, "N.T.M2": 1, "N.T.M3": 3})
        served("a hunk inside a method only the ambient array reached (a finalizer) cannot be attributed: every test is tier 1",
               docs, a_path, text([*a_lines[:42], "    int x43 = 99;", *a_lines[43:]]), "documentation-only drift", set(),
               {"N.T.M1": 1, "N.T.M2": 1, "N.T.M3": 1}, every_tier1=True)
        served("a file the map has never seen, new since its commit, puts every test in tier 1",
               new_file, "src/WiseOwl.Cobol.Compiler/New.cs", "n2\n", "nearest", {a_path, b_path, "src/WiseOwl.Cobol.Compiler/New.cs"},
               {"N.T.M1": 1, "N.T.M2": 1, "N.T.M3": 1}, every_tier1=True)

        # A map recorded AFTER the base serves it by comparing trees: ancestry plays no part.
        later = tmp / "later"
        later.mkdir()
        with gzip.open(impact_store.map_file(later, b_moved), "wt", encoding="utf-8", newline="\n") as fh:
            json.dump(synthetic_map(), fh)
        repo.git("checkout", "-q", "--detach", docs)
        repo.commit("edit for the newer map", {a_path: text(a_edited)})
        REPO, HEAD = repo.path, repo.git("rev-parse", "HEAD")
        try:
            found = find_map(later, docs, Reach())
            a = analyse(docs, store=later)
            check("a map recorded AFTER the base serves it: its drift is the files the two trees differ in, and a file "
                  "they share is attributed by line range",
                  a.map_state == "nearest" and set(found.drift) == {b_path}
                  and a.tiers.get("Conformance") == {"N.T.M1": 1, "N.T.M2": 3, "N.T.M3": 3},
                  f"{a.map_state}; {sorted(found.drift)}; {a.tiers.get('Conformance')}")
            # A map named on the command line is served by the same comparison (its commit is the one it was recorded at).
            named = tmp / "named.json.gz"
            with gzip.open(named, "wt", encoding="utf-8", newline="\n") as fh:
                json.dump({**synthetic_map(), "commit": recorded}, fh)
            a = analyse(docs, map_path=named)
            check("a map named with --map is served by the same comparison: its commit differs from the base in "
                  "documentation only", a.map_state == "documentation-only drift"
                  and a.tiers.get("Conformance") == {"N.T.M1": 1, "N.T.M2": 3, "N.T.M3": 3},
                  f"{a.map_state}; {a.tiers.get('Conformance')}")
            unknown = tmp / "unknown.json.gz"
            with gzip.open(unknown, "wt", encoding="utf-8", newline="\n") as fh:
                json.dump(synthetic_map(), fh)   # its commit is 40 zeros: git holds no such commit
            check("... and one whose commit git does not know describes the base as given",
                  explicit_map(unknown, docs).state == "exact", explicit_map(unknown, docs).state)
            torn = tmp / "torn"
            torn.mkdir()
            impact_store.map_file(torn, recorded).write_bytes(b"this is not a gzip file")
            r = Reach()
            found = find_map(torn, docs, r)
            check("a map file that cannot be read (torn by a killed recorder, or pruned meanwhile) leaves the plan unaimed "
                  "with the cause printed, never a failed plan",
                  found.map is None and found.state == "none" and any("could not be read" in w for w in r.every_tier1),
                  r.every_tier1[:1])
        finally:
            REPO, HEAD = repo.path, None
    finally:
        REPO, HEAD = saved
        remove_tree(tmp)


def self_test() -> int:
    """Prove each arm FIRES against a synthetic map (feedback_prove_the_watchdog_fails)."""
    from scratch_repo import ScratchRepo  # noqa: PLC0415  (scripts/: the one scratch-repository fixture, kb/Work PB3584)
    ix = MapIndex(synthetic_map())
    failures = 0

    def check(label: str, ok: bool, detail: object) -> None:
        nonlocal failures
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}: {detail}")

    def tiers_for(files: list[str]) -> tuple[dict[str, int], Reach]:
        reach = Reach()
        reach_of(ix, [("M", f) for f in files], None, reach)
        return tiers(ix, reach)["Conformance"], reach

    t, _ = tiers_for(["src/WiseOwl.Cobol.Compiler/A.cs"])
    check("a mapped file (file level) puts its tests in tier 2 and the others in tier 3 (A.cs also holds a finalizer only "
          "the ambient array reached: it does not make the file unattributable)",
          t == {"N.T.M1": 2, "N.T.M2": 2, "N.T.M3": 3}, t)
    for label, f in [("an UNMAPPED (new) src file puts every test in tier 1", "src/WiseOwl.Cobol.Compiler/New.cs"),
                     ("a known file no test executed puts every test in tier 1", "src/WiseOwl.Cobol.Compiler/Enum.cs"),
                     ("an ambient-only file puts every test in tier 1", "src/WiseOwl.Cobol.Compiler/B.cs"),
                     ("a grammar file puts every test in tier 1", "src/WiseOwl.Cobol.Frontend/Grammar/Core/X.g4"),
                     ("a props file puts every test in tier 1", "Directory.Build.props"),
                     ("a runtime data file puts every test in tier 1", "src/WiseOwl.Cobol.Runtime/Collation/Data/x.bin")]:
        t, reach = tiers_for([f])
        check(label, set(t.values()) == {1} and bool(reach.every_tier1), reach.every_tier1[:1])
    t, reach = tiers_for(["src/WiseOwl.Cobol.Runtime/PublicAPI.Unshipped.txt", "src/WiseOwl.Cobol.Runtime/PublicAPI.Shipped.txt",
                          "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin"])
    check("a public-API analyzer baseline is data a test may read, not a non-C# input: no test goes to tier 1 for it "
          "(nor for the runtime ABI pin)",
          not reach.every_tier1 and set(t.values()) == {3}, (reach.every_tier1[:1], t))
    planted =["public sealed class T", "{", "    [Fact]", "    public void A() { }", "    [Theory]",
               "    [InlineData(\"x]\")]", "    public async Task B(string s) { }", "    [Fact(Skip = \"y\")]",
               "    [Trait(\"k\", \"v\")]", "    public void C<TSlot>() { }",
               "    private static int Helper() => 1;", "}"]
    got = tests_changed("\n".join(planted), None)
    check("a new file's test methods are all tier 0a (Fact, Theory with data, async, generic; never a helper)",
          got == {"A", "B", "C"}, sorted(got))
    # kb/Work PB3190: an EDITED test method is tier 0a, read from a real `git diff -U0` of the two versions (the
    # trailing comments are list indices, as `edit` takes them).
    q = '"'
    q3 = q * 3
    edited = ["public sealed class E", "{", "    [Fact]", "    public void Edited()", "    {", "        Run(1);", "    }",
              "    [Fact]", "    public void Untouched() => Run(2);", "    [Theory]", "    [MemberData(nameof(Rows))]",
              "    public void Fed(int n) => Run(n);", "    public static TheoryData<int> Rows => new() { 1, 2 };",
              "    [Theory]", "    [InlineData(1)]", "    public void Inline(int n) => Run(n);",
              "    private static void Run(int n) { }", "    [Fact]", "    public void Commented() { }",
              # PB3190 review R1-1: a test's embedded COBOL program, whose lines may look like C# comments or be blank
              "    [Fact]", "    public void Embedded() => Run(Compile(" + q3,                   # 19, 20
              "          *> a fixed-form comment line: its column-7 indicator is the input",     # 21
              "", "          DISPLAY 'X'.", "          " + q3 + "));",                          # 22, 23, 24
              "    [Fact]", "    public void Verbatim() => Run(Compile(@" + q,                    # 25, 26
              "-- a verbatim line at column 0", q + "));", "}"]                                 # 27, 28, 29

    def diff_between(old: list[str], after: list[str]) -> list[Hunk]:
        before, now = "\n".join(old) + "\n", "\n".join(after) + "\n"
        with tempfile.TemporaryDirectory() as d:
            a, b = Path(d) / "a.cs", Path(d) / "b.cs"
            a.write_text(before, encoding="utf-8", newline="\n")
            b.write_text(now, encoding="utf-8", newline="\n")
            # `diff --no-index` exits 1 on a difference: the fixture's unchecked form, outside any repository
            return parse_hunks(ScratchRepo(d).run("diff", "--no-index", "-U0", "--no-color", str(a), str(b)).stdout,
                               before, now)

    def diff_of(after: list[str]) -> list[Hunk]:
        return diff_between(edited, after)

    def edit(i: int, line: str | None) -> list[str]:
        return edited[:i] + ([line] if line is not None else []) + edited[i + 1:]

    edited_row = edited[:15] + ["    [InlineData(2)]"] + edited[15:]
    cases = [("tier 0a holds an edited test method: a changed line inside an existing test body",
              edit(5, "        Run(3);"), {"Edited"}),
             ("tier 0a holds an edited test method: a line deleted from an existing test body",
              edit(5, None), {"Edited"}),
             ("tier 0a holds an edited test method: an added data row of an existing theory", edited_row, {"Inline"}),
             ("tier 0a holds an edited test method: a changed [MemberData] source (its theory's rows)",
              edit(12, "    public static TheoryData<int> Rows => new() { 1, 3 };"), {"Fed"}),
             ("tier 0a holds an edited test method: a changed helper is no test's own change (the map tiers its "
              "callers)", edit(16, "    private static void Run(int n) { _ = n; }"), set()),
             ("tier 0a holds an edited test method: a comment line added inside a test body",
              edited[:6] + ["        // why"] + edited[6:], set()),
             ("tier 0a holds an edited test method: a raw-string line that looks like a comment (the embedded "
              "program's `*>` line, its indicator moved one column)",
              edit(21, "           *> a fixed-form comment line: its column-7 indicator is the input"), {"Embedded"}),
             ("tier 0a holds an edited test method: a blank line inserted into a test's raw string (the embedded "
              "program's line numbers move)", edited[:23] + [""] + edited[23:], {"Embedded"}),
             ("tier 0a holds an edited test method: a deleted verbatim-string line that starts `--` (its diff line "
              "starts `---`, and is content, not a file header)", edit(27, None), {"Verbatim"})]
    for label, after, want in cases:
        got = tests_changed("\n".join(after), diff_of(after))
        check(label, got == want, sorted(got))
    unbalanced = "\n".join(edited[:-1])   # the class's closing brace lost: the spans cannot be trusted
    got = tests_changed(unbalanced, diff_of(edited[:5] + ["        Run(3);"] + edited[6:]))
    check("a source whose braces do not balance once lexed puts every test method it names in tier 0a",
          got == {"Edited", "Untouched", "Fed", "Inline", "Commented", "Embedded", "Verbatim"}, sorted(got))
    t, reach = tiers_for(["tests/_shared/TestPartitioning.cs"])
    check("a shared test file is compiled into every assembly",
          set(reach.changed_tests) == set(TEST_ASSEMBLIES.values()), sorted(reach.changed_tests))
    # A replay (--head) of a commit that DELETES a test file: its text is absent at the head, so reading it crashed
    # the analysis (19c9289af replayed, 2026-10-23); a deleted file has no test to run.
    global HEAD
    saved, HEAD = HEAD, "HEAD"
    try:
        reach = Reach()
        reach_of(None, [("D", "tests/WiseOwl.Cobol.Tests.Unit/NoSuchDeletedTests.cs")], "HEAD", reach)
        check("a replayed deletion of a test file names no test and does not read the absent file",
              not any(reach.changed_tests.values()), reach.changed_tests)
    finally:
        HEAD = saved
    copy = "conformance-suite/cases/7.2.3-COPY/"
    got = [suite_tests(copy + f) for f in ("copy-library-text.cob", "copy-library-text.2002.out", "CSCOPY01.cpy",
                                           "NO-SUCH-TEXT.cpy")]
    check("a suite file belongs to its test by id: the program, an expected output, a declared file (the header's "
          "`files`); an undeclared file to none", got == [{"copy-library-text"}, {"copy-library-text"},
                                                          {"copy-library-text"}, None], got)
    _, reach = tiers_for([copy + "CSCOPY01.cpy"])
    check("a declared suite file's test is tier 0a, every other row keeps its tier",
          reach.goldens == {"copy-library-text"} and not reach.every_tier1, (reach.goldens, reach.every_tier1))
    # Tier 0d, from the REAL drift-rule index: the sweep that stopped PB3523's landing reads every tracked script
    # through its declared TreeScope, and names no path (kb/Work PB3531).
    _, reach = tiers_for(["scripts/differential_fuzz.py"])
    check("a changed file names every drift test the index says governs it, a sweep of the committed tree included "
          "(tier 0d)", "CatalogCoverageDriftTests" in reach.drift_tests
          and "NulByteDriftTests" not in reach.drift_tests, sorted(reach.drift_tests))
    # A file a scope sets aside BY NAME is the rule's owner, which the sweep does not read: the class still governs it
    # (PB3531 review R1-1 — the general-format heading pattern lives in spec_headings.py).
    _, reach = tiers_for(["scripts/spec/spec_headings.py"])
    check("a change to the file a sweep sets aside by name (its rule's owner) names that sweep (tier 0d)",
          "CatalogCoverageDriftTests" in reach.drift_tests, sorted(reach.drift_tests))
    # Tier 0d with NO impact map (kb/Work PB3247): each changed file names the drift test the landing's Linux leg found
    # red on it — a test naming the file, and tests reading it only through a shared helper class (SuiteManifest) or a
    # TestRepo helper the index once lacked.
    for f, want in [("tests/version-matrix/traceability-inventory.json", {"SpecTraceabilityInventoryDriftTests"}),
                    ("docs/CONFORMANCE.md", {"AnnexA1RegisterDriftTests"}),
                    (copy + "copy-library-text.cob", {"ConstantNameLiteralPositionDriftTests"}),
                    # kb/Work PB3112: the reads a test spells with a nested call, a constant or a wrapper's callers.
                    ("src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4", {"ArithmeticOperandRoleDriftTests",
                                                                          "SendingOperandDriftTests",
                                                                          "ReceivingOperandFormDriftTests"}),
                    ("src/WiseOwl.Cobol.Compiler/Binding/ClassConditionModel.cs", {"ClassConditionTableDriftTests"}),
                    ("src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs", {"FixedFormRecordDriftTests"}),
                    # A wrapper's trailing parameters (`AssertDeclares(string script, params string[] cases)`) are no path.
                    ("scripts/spec/cite.py", {"CitationAuditSelfTestDriftTests"})]:
        reach = Reach()
        reach_of(None, [("M", f)], None, reach)
        check(f"with no map, a change to {f} names {', '.join(sorted(want))} (tier 0d)", want <= reach.drift_tests,
              sorted(want - reach.drift_tests))
    # Tier 0d through a CITATION (kb/Work PB3131): a planted register read by a planted drift test, and the three
    # shapes a removal breaks; a removal the register does not cite promotes nothing.
    rows = [{"test": "RegisterDriftTests", "file": "tests/U/RegisterDriftTests.cs", "rule": "", "scopes": [],
             "paths": ["docs/register.json"], "through": {}}]
    register = ('{"code-location": "src/P/Binder.cs#Bind; src/P/Binder.cs#Outer.Inner", '
                '"test-ref": "unit:TurnTests.UnknownName_IsDiagnosed; conformance:cases/7.3-X/old_case", '
                '"note": "a record is two trees, which is the test of docs/README.md and scripts/test.py; see '
                'unit:Harness.Opens, SourceKind.Program and the golden 2023/old_pin"}')
    planted_data = {"docs/register.json": register}
    planted_tree = ["src/P/Binder.cs", "tests/U/TurnTests.cs", "tests/U/Harness.cs", "tests/U/Store.cs",
                    "conformance-suite/cases/7.3-X/old_case.cob", "conformance-suite/cases/7.3-X/old_case.out",
                    "conformance-suite/cases/7.3-X/old_pin.cob", "docs/README.md", "docs/sub/README.md",
                    "scripts/foo/test.py", "src/X/Program.cs"]   # no scripts/test.py: its directory holds none
    citing = lambda removals: drift_rules.citing(rows, removals, planted_data.__getitem__, planted_tree)  # noqa: E731
    real_files, drift_rules.data_files = drift_rules.data_files, lambda rs: {"docs/register.json": ["RegisterDriftTests"]}
    try:
        for label, removal, want in [
            ("a deleted member a register cites as path#member names the register's drift test (tier 0d)",
             drift_rules.removal_of("src/P/Binder.cs", ["    public void Bind() { }"], "class Binder { }"),
             "src/P/Binder.cs#Bind"),
            ("a member removed from a #Class.Member chain names the register's drift test (tier 0d)",
             drift_rules.removal_of("src/P/Binder.cs", ["    void Inner() { }"], "class Binder { class Outer { } }"),
             "src/P/Binder.cs#Outer.Inner"),
            ("a renamed test a register cites as Class.Method names the register's drift test (tier 0d)",
             drift_rules.removal_of("tests/U/TurnTests.cs", ["    public void UnknownName_IsDiagnosed() { }"],
                                    "public sealed class TurnTests { public void Renamed() { } }"),
             "TurnTests.UnknownName_IsDiagnosed"),
            ("a deleted suite test a register cites by its stem names the register's drift test (tier 0d)",
             drift_rules.removal_of("conformance-suite/cases/7.3-X/old_case.cob", ["       PROGRAM-ID. OLD."], None),
             "conformance:cases/7.3-X/old_case"),
            ("a deleted suite test a pin cites as <edition>/<test> names the register's drift test (tier 0d)",
             [drift_rules.removal_of("conformance-suite/cases/7.3-X/old_pin.cob", ["       PROGRAM-ID. PIN."], None)],
             "2023/old_pin"),
            ("a deleted type a register cites as Class.Member names the register's drift test (tier 0d)",
             [drift_rules.removal_of("tests/U/Harness.cs", ["public sealed class Harness", "{", "}"], None)],
             "Harness.Opens"),
            # PB3131 review R1-1: a deleted file's comments and strings declared `is` and `which` as types, and its
            # stem matched any whole word, so one deletion promoted nearly every register's class into tier 0d.
            ("a deleted file's comments and strings declare no type: its prose words are no citation",
             [drift_rules.removal_of("tests/U/Store.cs", ["/// so a record is two trees", '    Fail("record which");',
                                                          "public sealed class Store { }"], None)],
             None),
            ("a deleted file's common stem (test.py, README.md) breaks no citation of another file of that name",
             [drift_rules.removal_of("scripts/foo/test.py", ["def test(): pass"], None),
              drift_rules.removal_of("docs/sub/README.md", ["# A test"], None)],
             None),
            ("a deleted type is no citation where a dotted name ENDS with it (an enum value of another type)",
             [drift_rules.removal_of("src/X/Program.cs", ["public static class Program", "{", "}"], None)],
             None),
            ("deleting one file of a partial type deletes no type: its Class.Member citations stand",
             [drift_rules.removal_of("tests/U/Harness.Part.cs", ["public sealed partial class Harness", "{", "}"],
                                     None)],
             None),
            ("a removal no register cites names no drift test",
             drift_rules.removal_of("src/P/Binder.cs", ["    void Helper() { }"], "class Binder { void Bind() { } }"),
             None),
        ]:
            got = citing(removal if isinstance(removal, list) else [removal])
            check(label, (got == {} if want is None else got == {"RegisterDriftTests": ("docs/register.json", want)}),
                  got)
        diff = "\n".join(["diff --git a/src/P/Binder.cs b/src/P/Binder.cs", "--- a/src/P/Binder.cs",
                          "+++ b/src/P/Binder.cs", "@@ -3,2 +3,0 @@", "-    void Bind() { }", "--- a/not/a/header",
                          "diff --git a/docs/new.md b/docs/new.md", "--- /dev/null", "+++ b/docs/new.md",
                          "@@ -0,0 +1 @@", "+text"])
        got = removed_lines(diff)
        check("a diff's removed lines are read per file, a removed `-- a/x` line among them, an added file's none",
              got == {"src/P/Binder.cs": ["    void Bind() { }", "-- a/not/a/header"]}, got)
        # PB3131 review R1-2: git ends a header with a TAB when its path holds a space, and C-quotes an odd path.
        diff = "\n".join(["diff --git a/docs/A b.md b/docs/A b.md", "--- a/docs/A b.md\t", "+++ /dev/null",
                          "@@ -1 +0,0 @@", "-gone", 'diff --git "a/docs/q\\"t.md" "b/docs/q\\"t.md"',
                          '--- "a/docs/q\\"t.md"', '+++ "b/docs/q\\"t.md"', "@@ -1 +1 @@", "-old", "+new"])
        got = removed_lines(diff)
        check("a header path holding a space (git's trailing TAB) or C-quoted by git is the file's path",
              got == {"docs/A b.md": ["gone"], 'docs/q"t.md': ["old"]}, got)
        check("a change that removes nothing a citation can name carries no removal",
              drift_rules.removal_of("src/P/Binder.cs", ["    x = 1;"], "class Binder { void M() { x = 2; } }") is None,
              "removal_of")
        # The wiring: reach_of puts a citing register's drift tests in Reach.drift_tests, with or without a map.
        global removals
        real_removals, real_citing = removals, drift_rules.citing
        removals = lambda changes, base: [drift_rules.Removal("src/P/Binder.cs", False, frozenset({"Bind"}),  # noqa: E731
                                                              frozenset({"Binder"}))]
        drift_rules.citing = lambda rs, rm, read=None, tree=None: real_citing(rows, rm, planted_data.__getitem__)  # noqa: E731
        try:
            reach = Reach()
            reach_of(None, [("M", "src/P/Binder.cs")], "b" * 40, reach)
        finally:
            removals, drift_rules.citing = real_removals, real_citing
        check("reach_of puts the drift test of a register citing a removed member in tier 0d, with no map",
              "RegisterDriftTests" in reach.drift_tests and any("src/P/Binder.cs#Bind" in n for n in reach.notes),
              sorted(reach.drift_tests))
    finally:
        drift_rules.data_files = real_files
    # ... and the REAL index: the traceability inventory's first unit test-ref, its method renamed, names
    # SpecTraceabilityInventoryDriftTests (the class four landings found only in CI's Linux unit leg).
    inventory = drift_rules.ROOT / "tests" / "version-matrix" / "traceability-inventory.json"
    ref = re.search(r"unit:([A-Z]\w*)\.([A-Za-z_]\w*)", text_of(inventory))
    got = drift_rules.citing(drift_rules.index(), [drift_rules.Removal(
        f"tests/WiseOwl.Cobol.Tests.Unit/{ref.group(1)}.cs", False, frozenset({ref.group(2)}),
        frozenset({ref.group(1)}))]) if ref else {}
    check("the real inventory: renaming a test it cites names SpecTraceabilityInventoryDriftTests (tier 0d)",
          "SpecTraceabilityInventoryDriftTests" in got, sorted(got))

    # The method level: the entries a hunk changes (0 = One(), which M1 reached; 1 = Two(), which no test reached),
    # or None for a declaration change, which falls back to the file level.
    def lexed(lines: list[str]) -> list[bool]:
        code = cs_members.code_lines("\n".join(lines))
        return [n in code for n in range(1, len(lines) + 1)]

    method_cases = [
        ("a hunk inside an unreached method reaches no test", (32, 1, ["x = 1;"], ["x = 2;"]), {1}),
        ("a hunk inside a reached method changes that method", (12, 1, ["x = 1;"], ["x = 2;"]), {0}),
        ("a comment-only hunk between methods reaches nothing", (25, 0, [], ["// note"]), set()),
        ("code inserted between methods is a declaration change", (25, 0, [], ["    private int y = 3;"]), None),
        ("an override inserted between methods is a declaration change",
         (25, 0, [], ["    public override string ToString()", "    {", "        return \"\";", "    }"]), None),
        ("a new ordinary method reaches no test by itself",
         (25, 0, [], ["    private static int Helper(int x)", "    {", "        return x;", "    }"]), set()),
    ]
    for label, (start, count, removed, added), want in method_cases:
        got = method_ids(ix, "src/WiseOwl.Cobol.Compiler/A.cs",
                         Hunk(start, count, start, len(added), removed, added, lexed(removed), lexed(added)))
        check(label, (got is None) if want is None else (got is not None and set(bit_ids(got)) == want), got)
    # The sibling arm of PB3190 review R1-1: a line deleted from a string literal between methods (here a class-level
    # raw-string constant, lines 24-26 of a planted A.cs) is a declaration change even when it reads like a comment.
    a_cs = [f"    // line {n}" for n in range(1, 24)] + ["    const string H = " + q3, "      // generated",
                                                          "      " + q3 + ";", "}"]
    got = [method_ids(ix, "src/WiseOwl.Cobol.Compiler/A.cs", h) for h in diff_between(a_cs, a_cs[:24] + a_cs[25:])]
    check("a comment-looking line deleted from a string literal between methods is a declaration change",
          got == [None], got)
    got = [method_ids(ix, "src/WiseOwl.Cobol.Compiler/A.cs", h) for h in diff_between(a_cs, a_cs[:1] + a_cs[2:])]
    check("a real comment line deleted between methods reaches nothing", got == [0], got)
    reach = Reach()
    reach.direct |= 1 << 0
    reach.widened |= ix.expand(1 << 0)
    t = tiers(ix, reach)["Conformance"]
    check("a hunk inside a reached method puts its tests in tier 1 and the rest in tier 3",
          t == {"N.T.M1": 1, "N.T.M2": 3, "N.T.M3": 3}, t)
    reach = Reach()
    reach.direct |= 1 << 3
    reach.widened |= ix.expand(1 << 3)
    t = tiers(ix, reach)["Conformance"]
    check("a changed static constructor: its own tests tier 1, its type's users tier 2, the rest tier 3",
          t == {"N.T.M1": 2, "N.T.M2": 1, "N.T.M3": 3}, t)

    r = Reach()
    found = find_map(Path(REPO / "tests" / "no-such-impact-store"), git("rev-parse", "HEAD"), r)
    check("no map for the base puts every test in tier 1",
          found.map is None and found.state == "none" and bool(r.every_tier1), r.every_tier1[:1])

    # The map that serves a base, against a real scratch repository (kb/Work PB3663): its tree's difference from the
    # base's decides what the map can say, never its age or its ancestry.
    map_service_arms(check)

    # The files a worktree changed leave out coord.NEVER_WORK (kb/Work PB3474): the harness's edit of its tracked settings
    # file, the checkpoint file and the gate's results are no change of the code, whatever the diff against the base says.
    got = worktree_changes_in_scratch_repo()
    check("a worktree's changed files leave out coord.NEVER_WORK (the settings file, STATUS.md, TestResults/)",
          got == [("M", "src/A.cs"), ("A", "src/New.cs")], got)
    print("ALL GREEN" if not failures else f"⛔ {failures} SELF-TEST FAILURE(S)")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("files", nargs="*", help="changed files (repo-relative), taken at the FILE level; default: "
                                              "the diff against --base, taken at the method level")
    ap.add_argument("--base", help="the change's base commit (the worktree's cut point)")
    ap.add_argument("--head", help="diff --base..--head instead of the worktree (replaying a branch or commit)")
    ap.add_argument("--map", type=Path, help="an explicit map file (otherwise the store is searched)")
    ap.add_argument("--store", type=Path, help="the map store (default <git common dir>/cobol-impact)")
    ap.add_argument("--plan", type=Path, help="write the tiers (JSON) that gate_plan.py reads to this file")
    ap.add_argument("--explain", action="store_true", help="print every reach decision")
    ap.add_argument("--audit-members", action="store_true",
                    help="the C# member reader's net over the gated test projects (cs_members.audit)")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if args.audit_members:
        findings = cs_members.audit([REPO / d for d in TEST_PROJECTS])
        for f in findings:
            print(f"  ⛔ {f}")
        print(f"=== CS MEMBERS AUDIT: {'CLEAN' if not findings else f'{len(findings)} FINDING(S)'} "
              f"({', '.join(TEST_PROJECTS)})")
        return 1 if findings else 0
    global HEAD
    HEAD = git("rev-parse", args.head) if args.head else None
    a = analyse(args.base, args.files, args.map, args.store)
    if args.plan:
        args.plan.write_text(json.dumps(plan_json(a), indent=1, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    err = sys.stderr
    for why in a.reach.every_tier1:
        print(f"  every test tier 1: {why}", file=err)
    if args.explain or not a.reach.every_tier1:
        for n in a.reach.notes:
            print(f"  {n}", file=err)
    counts = {asm: dict(sorted(Counter(t.values()).items())) for asm, t in sorted(a.tiers.items())}
    print(f"impacted_tests: map {a.map_state}; tiers per assembly {counts or '(no map: every test tier 1)'}; "
          f"tier 0a: {len(set().union(*a.reach.changed_tests.values()))} added or edited test method(s), "
          f"{len(a.reach.goldens)} "
          f"suite test(s). The gate runs the WHOLE population in this order (scripts/run_gate_legs.py)")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
