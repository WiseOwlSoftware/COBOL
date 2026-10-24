#!/usr/bin/env python3
"""The drift-rule index: every *DriftTests class's rule, and the repo paths it scans — ONE generated view.

    python scripts/spec/drift_rules.py <path> [...]    # the drift rules that govern these files — run BEFORE editing
    python scripts/spec/drift_rules.py --sweeps        # every tree-wide sweep's class (the `Sweep` trait's set)
    python scripts/spec/drift_rules.py --scopes        # every declared TreeScope, as JSON {class: {field: scope}}
    python scripts/spec/drift_rules.py --held <json>   # {"Class.Field": [paths]} -> the paths each scope holds
    python scripts/spec/drift_rules.py --out docs/DRIFT_RULES.md   # render the index (a gitignored VIEW, never committed)
    python scripts/spec/drift_rules.py --check         # exit 1 when a drift test states no rule, an unreadable scope or
                                                       # an unreadable TestRepo helper (CI `audits`, the gate)
    python scripts/spec/drift_rules.py --self-test     # fire the source-reading arms on a planted tree

The index is a RENDERED VIEW (kb/Work PB3026; scripts/rendered_views.py): two pull requests that each add a drift test
would both rewrite a committed index's count line and collide, so the tree holds the tests and their summaries — the
sources — and nothing here is compared with a copy on disk.

Why (owner, 2026-09-25): 216 drift tests each carry their rule in their own <summary>, but no skill surfaced them, so
agents learned each rule only by tripping it at the gate. The rule stays in ONE place — the test that enforces it — and
this script derives the index and answers "which rules govern the file I am about to edit" from the tests themselves.

A file is governed SPECIFICALLY by a drift test when the test names that file, or a directory at least SPECIFIC_DEPTH
segments deep above it (through TestRepo.X(...) segments, their leading string literals, Path.Combine(TestRepo.Root, ...)
or a repo-relative string literal), or when the test's rule
text names the file's basename. A test that scans only a whole tree above it (src/<project>, tests, docs) is listed as a
TREE-WIDE SWEEP — it still applies, but it is not the first thing to read.

A test that reads the COMMITTED TREE (tests/_shared/TrackedTree.cs) names no path at all: it filters `git ls-files`. It
reads the tree only through a declared scope, `static readonly TreeScope Name = new(Under: [...], Except: [...],
Kinds: [...]);` of string literals, and this index reads that declaration (`scopes_of`) and applies it exactly as the
test does (`in_scope` mirrors `TreeScope.Holds`), so the test governs every file its scope holds — specifically when the
prefix that holds the file is SPECIFIC_DEPTH segments deep, as a tree-wide sweep otherwise (kb/Work PB3531: the seven
such classes were invisible here, and one stopped PB3523's landing). An Except entry is a directory the rule does not
apply to, or ONE EXACT PATH: a file the scope sets aside BY NAME is the rule's OWNER (where its pattern may be written,
or the test itself), so the class governs that file SPECIFICALLY too (`owners`): editing the owner edits the rule
(PB3531 review R1-1: CatalogCoverageDriftTests owns scripts/spec/spec_headings.py). `TrackedTreeScopeDriftTests` holds every
declaration this reads equal to the value the compiler builds (`--scopes`), and every `TrackedTree.Files` call to a
declared field of a drift test.

A test that reads the tree through a HELPER CLASS names no path either (kb/Work PB3247: ConstantNameLiteralPositionDriftTests
reads the corpus through SuiteManifest, ClosesRowsBackLinkDriftTests the register through WorkRegister, and a golden or
a note governed neither). So the index reads every top-level type a drift test uses by name, in its own test project or
tests/_shared (`Sources`), and the test governs, transitively, every path those helpers name (`Sources.inherited`; the
per-file query says `— through <helper>`). Those paths govern a changed file but never make a class a sweep
(`is_sweep`). TestRepo's path helpers are read from tests/_shared/TestRepo.cs (`test_repo`), never copied here, and a
suite test it names by a literal id governs that program specifically. `--self-test` fires each arm on a planted tree.

A drift test that reads a REGISTER — a data file it names, such as the traceability inventory or docs/CONFORMANCE.md —
and checks that the register's citations resolve is broken by a change that never touches the register: one that
deletes a member a code-location names, renames a test a test-ref names, or deletes a file. `citing` answers that
case for the gate's planner (kb/Work PB3131): given what a diff removes (`removal_of`, fed by
scripts/spec/impacted_tests.py), the classes that govern each register citing it (`Citations.broken_by` reads the
three citation shapes a register uses: a path, a path and member, a dotted name).
"""
import json
import pathlib
import re
import subprocess
import sys
from dataclasses import dataclass

ROOT = pathlib.Path(__file__).resolve().parents[2]
#: THE repo-root locator every test project shares. Its path helpers are READ FROM ITS SOURCE (`test_repo`), never
#: copied here: the hand copy this replaced lacked `Kb`, so no drift test reading kb/Work through it governed a note
#: (kb/Work PB3247).
TEST_REPO = "tests/_shared/TestRepo.cs"
#: `X(params string[] segments) => Path.Combine([Root, .. segments])`: the helper rooted at the repository itself.
ROOT_HELPER = re.compile(r"\bstatic\s+string\s+(\w+)\(\s*params\s+string\[\]\s+segments\s*\)\s*=>\s*"
                         r"Path\.Combine\(\[\s*Root\s*,\s*\.\.\s*segments\s*\]\)")
#: `X(params string[] segments) => Y(["a", "b", .. segments])`: a helper prefixing another's segments.
SEGMENT_HELPER = re.compile(r'\bstatic\s+string\s+(\w+)\(\s*params\s+string\[\]\s+segments\s*\)\s*=>\s*(\w+)\(\['
                            r'((?:\s*"[^"\\\n]+"\s*,)+)\s*\.\.\s*segments\s*\]\)')
#: `X(string id) => ... s_suiteCases ...`: the program of the conformance-suite test `id`, wherever its clause folder
#: files it; `Y(string id, string name) => ... X(id) ...`: a file beside that program.
CASE_HELPER = re.compile(r"\bstatic\s+string\s+(\w+)\(\s*string\s+id\s*\)\s*=>[^;]*\bs_suiteCases\b")
BESIDE_HELPER = re.compile(r"\bstatic\s+string\s+(\w+)\(\s*string\s+id\s*,\s*string\s+name\s*\)\s*=>[^;]*\b(\w+)\(\s*id\s*\)")
#: The trees `s_suiteCases` files a test id under: `s_suiteCases = new(() => new[] { Suite("cases"), ... }`.
CASE_TREES = re.compile(r"\bs_suiteCases\s*=\s*new\([^{]*\{([^}]*)\}")
PUBLIC_STRING = re.compile(r"\bpublic\s+static\s+string\s+(\w+)\s*\(")
#: A helper call: `TestRepo.X(args)` in a test, `X(args)` inside TestRepo itself.
CALL = re.compile(r"\bTestRepo\.(\w+)\(([^()]*)\)")
BARE_CALL = re.compile(r"(?<![\w.])(\w+)\(([^()]*)\)")
#: `Path.Combine(TestRepo.Root, "a", "b")` (or `Path.Combine([TestRepo.Root, "a", ...])`): the root helper written out.
ROOT_COMBINE = re.compile(r"\bPath\.Combine\(\s*\[?\s*TestRepo\.Root\s*,([^()\]]*)\]?\s*\)")
#: A C# type DECLARATION (one rule, two readers): its indent (group 1), its modifiers (group 2) and its name (group 3),
#: matched only at a line start, so a comment (`// so a record is two trees`), a string ("record which of the two") or
#: prose declares no type. Every test source uses a file-scoped namespace, so a TOP-LEVEL type (the helper classes a
#: drift test may read the tree through, `Sources`, kb/Work PB3247) has no indent; a removed one, nested or not, is what
#: a register's dotted citation can name (`declared_types`, kb/Work PB3131 review R1-1).
TYPE_DECL = re.compile(r"^([ \t]*)((?:(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|"
                       r"file|ref|unsafe|new)[ \t]+)*)(?:class|struct|interface|enum|record(?:[ \t]+(?:struct|class))?)"
                       r"[ \t]+([A-Za-z_]\w*)", re.M)
#: A use of a type by name: a member access `T.M`, a construction `new T`, `typeof(T)`, a base type `class C : T`,
#: `using static N.T;`. Comments are removed first: a summary naming a helper is no read of its tree.
TYPE_USE = re.compile(r"(?<![\w.])(\w+)\s*\.\s*[A-Za-z_]|\bnew\s+(\w+)\b|\btypeof\s*\(\s*(\w+)"
                      r"|\b(?:class|struct|record)\s+\w+(?:<[^>]*>)?(?:\([^)]*\))?\s*:\s*(\w+)"
                      r"|\busing\s+static\s+(?:\w+\.)*(\w+)\s*;")
COMMENT = re.compile(r"//[^\n]*")
#: The top-level trees a repo-relative path a test reads, or a register cites, starts with.
TREE_ROOTS = r"(?:src|tests|docs|scripts|specs|kb|grammars|\.github|adjudication|conformance-suite)"
LITERAL = re.compile(r'"(' + TREE_ROOTS + r'/[^"\s{}*]+)"')
SUMMARY = re.compile(r"///\s*<summary>(.*?)</summary>", re.S)
CLASS = re.compile(r"\bclass\s+(\w*DriftTests)\b")
CREF = re.compile(r'<(?:see|seealso)\s+(?:cref|langword|href)="(?:[A-Z]:)?([^"]+)"\s*/>')
PARAMREF = re.compile(r'<(?:paramref|typeparamref)\s+name="([^"]+)"\s*/>')
TAG = re.compile(r"<[^>]+>")
SPECIFIC_DEPTH = 3   # src/<project> (2 segments) is a tree-wide sweep; src/<project>/<dir> and deeper are specific
#: A declared scope of the committed tree (tests/_shared/TrackedTree.cs `TreeScope`): `TreeScope Name = new(` or
#: `= new TreeScope(`; `scopes_of` reads its arguments up to the matching parenthesis.
SCOPE_DECL = re.compile(r"\bTreeScope\s+(\w+)\s*=\s*new(?:\s+TreeScope)?\s*\(")
SCOPE_ARG = re.compile(r'\s*(Under|Except|Kinds)\s*:\s*\[\s*((?:"[^"\\\n]*"\s*,?\s*)*)\]\s*,?')
STRING = re.compile(r'"([^"\\\n]*)"')


def test_sources(root=ROOT, pattern="*.cs"):
    # Sort on the repo-relative POSIX string: Path ordering is case-INsensitive on Windows and case-sensitive on
    # Linux, so sorting Path objects made the generated index differ between a local run and CI.
    for f in sorted((root / "tests").rglob(pattern), key=lambda p: p.relative_to(root).as_posix()):
        parts = set(f.relative_to(root).parts)
        if "bin" in parts or "obj" in parts:
            continue
        yield f


def drift_files(root=ROOT):
    return test_sources(root, "*DriftTests.cs")


def test_repo(root=ROOT):
    """TestRepo's path helpers, read from its source: {"helpers": {name: repo-relative prefix}, "case": the helpers
    naming a conformance-suite test's program by id, "beside": those naming a file beside it, "trees": the trees an id
    is filed under, "unread": every public string method the index cannot read}. A method that builds a path into a
    build output (`"bin"`) reads no tracked file and is not unread. `--check` fails on an unread method, so a helper
    added to TestRepo in a shape this cannot read turns the audit red instead of governing nothing."""
    text = (root / TEST_REPO).read_text(encoding="utf-8", errors="replace")
    helpers = {m.group(1): "" for m in ROOT_HELPER.finditer(text)}
    chain = {m.group(1): (m.group(2), STRING.findall(m.group(3))) for m in SEGMENT_HELPER.finditer(text)}
    while ready := {n: "/".join(([helpers[b]] if helpers[b] else []) + segs)
                    for n, (b, segs) in chain.items() if b in helpers and n not in helpers}:
        helpers.update(ready)
    case = [m.group(1) for m in CASE_HELPER.finditer(text)]
    beside = [m.group(1) for m in BESIDE_HELPER.finditer(text) if m.group(2) in case]
    trees_at = CASE_TREES.search(text)
    trees = [p for name, args in BARE_CALL.findall(trees_at.group(1)) if (p := prefixed(helpers, name, args)) is not None] \
        if trees_at else []

    def builds(m):
        return '"bin"' in text[m.end():text.find(";", m.end())]

    unread = [m.group(1) for m in PUBLIC_STRING.finditer(text)
              if m.group(1) not in helpers and m.group(1) not in case and m.group(1) not in beside and not builds(m)]
    if case and not trees:
        unread += [f"{c} (the trees s_suiteCases files an id under)" for c in case]
    return {"helpers": helpers, "case": case, "beside": beside, "trees": trees, "unread": unread}


def prefixed(helpers, name, args):
    """The repo-relative path a prefix helper call names: its prefix and its string-literal arguments; None when `name`
    is no prefix helper."""
    if name not in helpers:
        return None
    return "/".join(([helpers[name]] if helpers[name] else []) + leading_literals(args)).strip("/")


def leading_literals(args):
    """The string-literal arguments of a call, up to the first argument that is not one: `"Work", note + ".md"` reads
    under kb/Work, never a file `kb/Work/.md` (EcRaiseSiteRegistryDriftTests' `TestRepo.Kb("Work", note + ".md")`).
    A collection expression is its elements: `["Cobol.Net.Compiler", "CodeGen", .. parts]` reads under its two."""
    out = []
    args = args.strip()
    if args.startswith("[") and args.endswith("]"):
        args = args[1:-1]
    for arg in re.findall(r'(?:"(?:[^"\\]|\\.)*"|[^,"])+', args):
        m = re.fullmatch(r'\s*"([^"\\]+)"\s*', arg)
        if not m:
            break
        out.append(m.group(1))
    return out


def rule_of(text):
    """The class summary as one line: references kept by name, other XML stripped, whitespace collapsed, cut at the
    first sentence end past 60 characters (and at 400)."""
    m = SUMMARY.search(text)
    if not m:
        return ""
    s = re.sub(r"\s*///\s?", " ", m.group(1))
    s = CREF.sub(lambda c: c.group(1).split("(")[0].split(".")[-1], s)
    s = PARAMREF.sub(lambda c: c.group(1), s)
    s = TAG.sub("", s).replace("&lt;", "<").replace("&gt;", ">").replace("&amp;", "&")
    s = re.sub(r"\s+", " ", s).strip()
    cut = re.search(r"(?<=[.!?])\s", s[60:])
    s = s[: 60 + cut.start()] if cut else s
    return s[:400] + ("…" if len(s) > 400 else "")


class Sources:
    """The test sources the index reads, once per run: TestRepo's helpers (`test_repo`), the conformance-suite test ids
    it resolves, and the top-level types every test source declares — the HELPER CLASSES a drift test may read the tree
    through (kb/Work PB3247: ConstantNameLiteralPositionDriftTests reads the whole corpus through SuiteManifest and names
    no path itself, so a golden it scans governed nothing and the gate left the class in leg 2)."""

    def __init__(self, root=ROOT):
        self.root = root
        self.repo = test_repo(root)
        self._cases = None
        self._texts: dict[str, str] = {}
        self._own: dict[str, list[str]] = {}
        self._uses: dict[str, list[str]] = {}
        self.types: dict[tuple[str, str], list[str]] = {}
        for f in test_sources(root):
            rel = f.relative_to(root).as_posix()
            for m in TYPE_DECL.finditer(self.text(rel)):
                if not m.group(1):   # top-level only: a nested type is no helper class
                    self.types.setdefault((project(rel), m.group(3)), []).append(rel)

    def text(self, rel):
        if rel not in self._texts:
            self._texts[rel] = (self.root / rel).read_text(encoding="utf-8", errors="replace")
        return self._texts[rel]

    def case(self, test_id):
        """The repo-relative program of the conformance-suite test `test_id` (TestRepo's `s_suiteCases`, mirrored: a
        `.cob` directly inside a clause folder of one of its trees), or None."""
        if self._cases is None:
            self._cases = {}
            for tree in self.repo["trees"]:
                base = self.root / tree
                for folder in sorted(p for p in base.iterdir() if p.is_dir()) if base.is_dir() else []:
                    for f in sorted(folder.glob("*.cob")):
                        self._cases.setdefault(f.stem, f.relative_to(self.root).as_posix())
        return self._cases.get(test_id)

    def paths_of(self, text):
        """The repo paths a source names itself: TestRepo helper calls (a conformance-suite test by its literal id
        included) and repo-relative string literals."""
        seen = []
        for name, args in CALL.findall(text):
            p = prefixed(self.repo["helpers"], name, args)
            if p is None and (name in self.repo["case"] or name in self.repo["beside"]):
                lits = re.findall(r'"([^"]+)"', args)
                program = self.case(lits[0]) if lits else None
                p = program if program and name in self.repo["case"] else (
                    str(pathlib.PurePosixPath(program).parent / lits[1]) if program and len(lits) > 1 else None)
            if p and p not in seen:
                seen.append(p)
        for args in ROOT_COMBINE.findall(text):
            p = "/".join(leading_literals(args))
            if p and p not in seen:
                seen.append(p)
        for lit in LITERAL.findall(text):
            lit = lit.rstrip("/.,;:")
            if lit not in seen:
                seen.append(lit)
        return seen

    def own(self, rel):
        """The paths the source `rel` names itself, its TreeScope declarations blanked out (`scopes_of`)."""
        if rel not in self._own:
            self._own[rel] = self.paths_of(scopes_of(self.text(rel))[1])
        return self._own[rel]

    def uses(self, rel):
        """The test sources declaring a top-level type `rel` uses by name, in its own project or tests/_shared."""
        if rel not in self._uses:
            names = {n for m in TYPE_USE.finditer(COMMENT.sub("", self.text(rel))) for n in m.groups() if n}
            seen = {"_shared", project(rel)}
            self._uses[rel] = sorted({g for n in names for s in seen for g in self.types.get((s, n), []) if g != rel})
        return self._uses[rel]

    def inherited(self, rel):
        """{path: the helper source it is read through} for every path a source reaches only through the helper classes
        it uses, transitively (a helper using another helper), never a path it names itself."""
        own, out = set(self.own(rel)), {}
        queue = [(u, u) for u in self.uses(rel)]
        reached = {rel} | {u for u, _ in queue}
        while queue:
            g, via = queue.pop(0)
            for p in self.own(g):
                if p not in own:
                    out.setdefault(p, via)
            for u in self.uses(g):
                if u not in reached:
                    reached.add(u)
                    queue.append((u, via))
        return out


def project(rel):
    """The test project a test source compiles into: `tests/<project>/...`; tests/_shared is linked into every one
    (tests/Directory.Build.props), so its types are visible from all of them."""
    parts = rel.split("/")
    return parts[1] if len(parts) > 2 else ""


def _closing(text, i):
    """The index of the `)` that closes the parenthesis opened just before `i`, skipping string literals; -1 if none."""
    depth = 1
    while i < len(text):
        c = text[i]
        if c == '"':
            i += 1
            while i < len(text) and text[i] != '"':
                i += 2 if text[i] == "\\" else 1
        elif c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            if depth == 0:
                return i
        i += 1
    return -1


def scopes_of(text):
    """Every TreeScope a test source declares, as [{"name", "under", "except", "kinds"}] (an omitted argument is None;
    a declaration that is not literal named arguments carries an "error"), and the source with the declarations blanked
    out: an Except literal names a path the test does NOT read, so `Sources.paths_of` must not see it."""
    scopes, spans = [], []
    for m in SCOPE_DECL.finditer(text):
        sc = {"name": m.group(1), "under": None, "except": None, "kinds": None}
        end = _closing(text, m.end())
        if end < 0:
            scopes.append(sc | {"error": "the declaration has no closing parenthesis"})
            continue
        args, pos = text[m.end():end], 0
        while pos < len(args):
            a = SCOPE_ARG.match(args, pos)
            if not a or a.end() == pos or sc[a.group(1).lower()] is not None:
                break
            sc[a.group(1).lower()] = STRING.findall(a.group(2))
            pos = a.end()
        if args[pos:].strip() or sc["under"] is None:
            sc["error"] = ("not a declaration of string literals in named arguments (Under: [...], Except: [...], "
                           f"Kinds: [...]): {' '.join(args.split())[:160]!r}")
        scopes.append(sc)
        spans.append((m.start(), end + 1))
    for start, end in reversed(spans):
        text = text[:start] + " " * (end - start) + text[end:]
    return scopes, text


def segments(prefix):
    """The number of path segments of a scope's prefix ("" — the whole tree — has none)."""
    return len([s for s in prefix.split("/") if s])


def in_scope(scope, rel):
    """`TreeScope.Holds` (tests/_shared/TrackedTree.cs), mirrored exactly: under a prefix (ordinal), under no excepted
    directory (ending in /) and no excepted path, and ending in a kind (case-insensitive) when kinds are given."""
    return (any(rel.startswith(u) for u in scope["under"])
            and not any(rel.startswith(e) if e.endswith("/") else rel == e for e in scope["except"] or [])
            and (scope["kinds"] is None or any(rel.lower().endswith(k.lower()) for k in scope["kinds"])))


def owners(scope):
    """The paths a scope sets aside BY NAME (an Except entry that is one exact path, not a directory ending in /): the
    rule's owners. The sweep does not read them because the rule lives there, so the class governs them specifically."""
    return [e for e in scope["except"] or [] if not e.endswith("/")]


def index(root=ROOT, sources=None):
    """One row per drift test: its class, file, rule, the paths it names ("paths"), the paths it reads only through a
    helper class ("through": {path: helper source}) and its declared scopes."""
    sources = sources or Sources(root)
    rows = []
    for f in drift_files(root):
        rel = f.relative_to(root).as_posix()
        text = sources.text(rel)
        m = CLASS.search(text)
        rows.append({"test": m.group(1) if m else f.stem, "file": rel, "rule": rule_of(text),
                     "paths": sources.own(rel), "through": sources.inherited(rel), "scopes": scopes_of(text)[0]})
    return rows


def reads(row):
    """Every path a row reads: the ones it names, then the ones it reads through a helper class."""
    return row["paths"] + [p for p in row["through"] if p not in row["paths"]]


def readable(row):
    """The scopes of a row that the index can apply (an unreadable one fails `--check`)."""
    return [sc for sc in row["scopes"] if "error" not in sc]


def scope_text(sc):
    return (" + ".join(f"`{u}`" if u else "the whole tree" for u in sc["under"])
            + (f" except {', '.join(f'`{e}`' for e in sc['except'])}" if sc["except"] else "")
            + (f" ({' '.join(sc['kinds'])})" if sc["kinds"] else ""))


def render(rows):
    out = ["# Drift rules — generated index",
           "",
           "<!-- RENDERED VIEW of the *DriftTests summaries (scripts/spec/drift_rules.py --out) — gitignored, never committed or edited. -->",
           "",
           "Every `*DriftTests` class enforces ONE structural rule, and that rule's single home is the test's `<summary>`.",
           "This page is derived from them. **Before editing a file, ask which rules govern it:**",
           "`python scripts/spec/drift_rules.py <path>` (specific rules first, then the tree-wide sweeps).",
           "",
           f"{len(rows)} drift tests.",
           "",
           "| Drift test | Rule | Scans |",
           "|---|---|---|"]
    for r in rows:
        rule = r["rule"].replace("|", "\\|") or "⚠ no `<summary>` — the rule is undocumented"
        items = ([scope_text(sc) for sc in readable(r)] + [f"`{p}`" for p in r["paths"]]
                 + [f"`{p}` (through {pathlib.PurePosixPath(via).stem})" for p, via in r["through"].items()])
        scans = ", ".join(items[:6]) + (" …" if len(items) > 6 else "")
        out.append(f"| [{r['test']}](../{r['file']}) | {rule} | {scans or '—'} |")
    return "\n".join(out) + "\n"


def is_sweep(row):
    """A TREE-WIDE SWEEP reads some path above SPECIFIC_DEPTH: a whole tree, or a file at its top. ONE rule: the
    per-file query lists such a test after the specific ones, and the gate's plan runs every cheap one in leg 1 at
    every gate (tier 0c, scripts/gate_plan.py), through the `[Trait("Sweep", "tree")]` its class carries
    (HostWitnessTraitDriftTests holds the two equal; kb/Work PB3022). A declared scope of the committed tree reads
    above SPECIFIC_DEPTH when one of its prefixes does. Only the class's OWN scan decides (the paths it names, its
    scopes), never a path it reads through a helper class (`Sources.inherited`): that closure is file-grained, so it
    governs a changed file (`governed`, tier 0d) but must not make a class a sweep, or a path literal added to one
    helper would move the `Sweep` trait of every class that uses it (kb/Work PB3247)."""
    return (any(len(p.split("/")) < SPECIFIC_DEPTH for p in row["paths"])
            or any(segments(u) < SPECIFIC_DEPTH for sc in readable(row) for u in sc["under"]))


def relative(target):
    """A command-line target as a repo-relative POSIX path (it may be absolute, or relative to the current directory)."""
    rel = pathlib.Path(target).resolve()
    try:
        return rel.relative_to(ROOT).as_posix()
    except ValueError:
        return pathlib.PurePosixPath(target).as_posix()


def governed(rows, paths):
    """The drift-test classes that govern any of the repo-relative `paths`, specifically or as a sweep: what the gate's
    planner puts in leg 1 for a change to them (tier 0d; scripts/spec/impacted_tests.py, scripts/gate_plan.py)."""
    out = set()
    for rel in paths:
        specific, sweeps = governing(rows, rel)
        out |= {r["test"] for r in specific + sweeps}
    return out


def governing(rows, rel):
    """(the drift tests that govern the repo-relative `rel` specifically, those that sweep it)."""
    base = pathlib.PurePosixPath(rel).name
    stem = pathlib.PurePosixPath(rel).stem
    specific, sweeps = [], []
    for r in rows:
        under = [p for p in reads(r) if rel == p or rel.startswith(p.rstrip("/") + "/")]
        held = [u for sc in readable(r) if in_scope(sc, rel) for u in sc["under"] if rel.startswith(u)]
        deep = (any(p == rel or len(p.split("/")) >= SPECIFIC_DEPTH for p in under)
                or any(segments(u) >= SPECIFIC_DEPTH for u in held)
                or any(rel in owners(sc) for sc in readable(r)))
        by_name = len(stem) > 3 and re.search(r"\b" + re.escape(stem) + r"\b", r["rule"]) is not None
        if deep or by_name or base == pathlib.PurePosixPath(r["file"]).name:
            specific.append(r)
        elif under or held:
            sweeps.append(r)
    return specific, sweeps


@dataclass(frozen=True)
class Removal:
    """What a change takes away from one tracked file, as a CITATION can name it: the file itself (`deleted`), the
    identifiers its old text had and its new text lacks (`names`), the types it declares (`classes`, old text and new)
    — the left-hand side of a dotted `Class.Member` citation — and the types it deletes WHOLE (`dropped`): a type
    declared on a removed line, not `partial`, whose name the new text lacks (or whose file is deleted)."""
    path: str
    deleted: bool
    names: frozenset[str]
    classes: frozenset[str]
    dropped: frozenset[str] = frozenset()


IDENT = re.compile(r"[A-Za-z_]\w*")
def declared_types(path, text):
    """(type, partial?) for every type the C# `text` of `path` declares; none for a file that is not C#."""
    return [(name, "partial" in mods.split()) for _, mods, name in TYPE_DECL.findall(text)] if path.endswith(".cs") else []


def removal_of(path, removed, after):
    """The Removal of `path` from a diff: its `removed` lines, and its text after the change (None: deleted). An
    identifier the old text had and the new one lacks sits on a removed line, so the removed lines suffice. None when
    the change removes nothing a citation can name. Deleting one file of a `partial` type deletes no type: its other
    files still declare it (cf9c85914 deleted IndexedConnector.WholeStore.cs, and IndexedConnector.Start lived on)."""
    removed_text = "\n".join(removed)
    gone = declared_types(path, removed_text)
    classes = frozenset(t for t, _ in gone + declared_types(path, after or ""))
    whole = frozenset(t for t, partial in gone if not partial)
    if after is None:
        return Removal(path, True, frozenset(), classes, whole)
    names = frozenset(IDENT.findall(removed_text)) - frozenset(IDENT.findall(after))
    return Removal(path, False, names, classes, whole & names) if names else None


def data_files(rows):
    """The DATA files the drift tests read by name — every non-C# file a row's paths name exactly — each with the
    classes that govern it specifically: the registers that cite tree symbols (the traceability inventory's
    code-locations and test-refs, docs/CONFORMANCE.md's pins) and the tests that check those citations resolve."""
    out = {}
    for p in sorted({p for r in rows for p in reads(r) if not p.endswith(".cs")}):
        if (ROOT / p).is_file():
            out[p] = sorted(r["test"] for r in governing(rows, p)[0])
    return out


PATH_MEMBER = re.compile(r"(" + TREE_ROOTS + r"/[\w./-]+)#([A-Za-z_][\w.]*)")
#: Anchored at a word start and possessive: unanchored, the scan of the 18 MB of registers took 2 s, not 0.6 s.
DOTTED = re.compile(r"\b[A-Za-z_]\w*+(?:\.[A-Za-z_]\w*+)+")
#: What surrounds a path citation in a register and is not the path: a scheme before it (`conformance:`), a member
#: after it (`#Bind`) and the punctuation of the prose or list holding it.
SCHEME = re.compile(r"^(?:[\w-]+:)+")
OPENERS, CLOSERS = " \t\n\"'`|(<[*", " \t\n\"'`|)>]*"


def key(path):
    """A file's identity as a citation names it: its path without its extension (`cases/X/t` names `t.cob` and
    `t.out`)."""
    head, _, base = path.rpartition("/")
    return f"{head}/{base.rsplit('.', 1)[0]}" if "." in base else path


class TreeFiles:
    """The tree BEFORE the change, as a register's path citation resolves against it. A citation names the files whose
    path ENDS with its path, the extension optional (`conformance:cases/5.5-X/t`, `src/P/Binder.cs#Bind`), so
    `scripts/test.py` never names `scripts/foo/test.py`. A golden pin (`2023/<test>`, docs/CONFORMANCE.md) is the one
    path whose directory is an EDITION, a number: it names every file whose name is its last segment. A run with no
    directory names a file only by its whole name (`Binder.cs`): a bare word is no path."""

    def __init__(self, paths):
        self.by_name, self.by_stem = {}, {}
        for p in paths:
            base = p.rpartition("/")[2]
            self.by_name.setdefault(base, []).append(p)
            if "." in base:
                self.by_stem.setdefault(base.rsplit(".", 1)[0], []).append(p)

    def resolve(self, cite):
        """The files the path citation `cite` names: () when it names none, or is no path."""
        path = SCHEME.sub("", cite).split("#", 1)[0].rstrip(".,;:")
        dirs, _, last = path.rpartition("/")
        if not dirs:
            return tuple(self.by_name.get(last, ())) if "." in last else ()
        named = self.by_name.get(last, []) + self.by_stem.get(last, [])
        under = tuple(p for p in named if p.rpartition("/")[0] == dirs or p.rpartition("/")[0].endswith("/" + dirs))
        return under or (tuple(named) if dirs.isdigit() else ())


class Citations:
    """A data file's citations of tree symbols, indexed once so a removal is checked by set lookups (a regex per
    removed identifier over the 7.5 MB inventory cost 12-49 s a plan). A register cites a symbol in three shapes:
    a PATH (`src/.../File.cs`, `cases/<clause>/<test>`), a PATH AND MEMBER (`src/.../File.cs#Member`, `#Class.Member`,
    a grammar's `#rule`) and a DOTTED NAME (`unit:Class.Method`, `Namespace.Class.Method`)."""

    def __init__(self, text, wanted, types):
        """Index `text`, keeping only the citations whose member is one of the `wanted` (removed) identifiers, and the
        dotted names that qualify one of the removed `types`."""
        self.text = text
        self.members = {}   # (path, a segment after its `#`) -> the citation
        for path, member in PATH_MEMBER.findall(text):
            for seg in filter(None, member.split(".")):
                if seg in wanted:
                    self.members.setdefault((path, seg), f"{path}#{member}")
        self.qualified = {}   # (a segment, any later segment of the same dotted name) -> the dotted name
        self.typed = {}   # a removed type -> a dotted name qualifying it
        for name in set(DOTTED.findall(text)):
            segs = name.split(".")
            for i, right in enumerate(segs):
                if right in wanted:
                    for left in segs[:i]:
                        self.qualified.setdefault((left, right), name)
                if right in types and qualifies(segs, i):
                    self.typed.setdefault(right, name)

    def broken_by(self, removal, tree=None, gone=frozenset()):
        """The first citation `removal` breaks, or None: a PATH AND MEMBER by removing any segment after the `#`; a
        DOTTED NAME by removing a later segment from a file declaring the earlier one as a type, or by removing the
        type itself (or its file); a PATH by deleting the file it names, when every file it names is `gone` (the keys
        the change deletes) — resolved against the `tree` before the change, so `docs/README.md` is not broken by
        deleting `docs/sub/README.md`, and a bare `test` or `Program` is no path at all (PB3131 review R1-1)."""
        for n in sorted(removal.names):
            if (removal.path, n) in self.members:
                return self.members[removal.path, n]
            for c in sorted(removal.classes):
                if (c, n) in self.qualified:
                    return self.qualified[c, n]
        for t in sorted(removal.dropped):
            if t in self.typed:
                return self.typed[t]
        if not removal.deleted or tree is None:
            return None
        stem = key(removal.path).rpartition("/")[2]
        seen = set()
        for hit in re.finditer(r"(?<![\w-])" + re.escape(stem) + r"(?![\w-])", self.text) if stem in self.text else ():
            run = self.run_at(hit.start(), hit.end())
            if run in seen:
                continue
            seen.add(run)
            named = tree.resolve(run)
            if removal.path in named and all(key(p) in gone for p in named):
                return run[:160]
        return None


    def run_at(self, start, end):
        """The citation holding the mention at [start, end): the run of text between the delimiters around it, walked
        a character at a time (a `rfind` per delimiter scans back to the file's start when that delimiter is absent,
        which cost seconds for a common stem's thousand mentions), at most RUN_REACH characters each way."""
        lo, hi, text = start, end, self.text
        while lo > max(0, start - RUN_REACH) and text[lo - 1] not in OPENERS:
            lo -= 1
        while hi < min(len(text), end + RUN_REACH) and text[hi] not in CLOSERS:
            hi += 1
        return text[lo:hi]


RUN_REACH = 512


def qualifies(segs, i):
    """Whether segment `i` of a dotted name cites it as a TYPE: a member follows it (`Class.Method`). A member's name
    starts upper-case or `_`, so `Program.cs` is a file name, not a member of `Program`; and a name the segment ENDS
    (`SourceElementKind.Program`, `Scope.Program`) is a member or an enum value of another type, not this one."""
    return i + 1 < len(segs) and (segs[i + 1][:1].isupper() or segs[i + 1][:1] == "_")


def tracked_tree():
    """The committed tree's paths (`git ls-files`): `citing`'s default tree, with the deleted paths added back."""
    out = subprocess.run(["git", "-c", "core.quotePath=false", "ls-files", "-z"], cwd=ROOT, check=True,
                         capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
    return [p for p in out.split("\0") if p]


def citing(rows, removals, read=None, tree=None):
    """{drift class: (data file, citation)} for every data file (`data_files`) holding a citation one of `removals`
    breaks: the classes that govern that file specifically, which the gate's planner puts in leg 1 with the governing
    ones (tier 0d; scripts/spec/impacted_tests.py). A change that deletes a member, renames a test or deletes a file a
    register cites breaks the register's check WITHOUT touching the register (kb/Work PB3131: four landings stopped in
    CI's Linux unit leg on SpecTraceabilityInventoryDriftTests after a Windows leg 1 that never ran it). `read`
    (repo-relative path -> text) defaults to the tree; `tree` (the paths before the change, which a deleted file's
    path citation resolves against) defaults to the tracked tree plus the deleted paths."""
    read = read or (lambda p: (ROOT / p).read_text(encoding="utf-8", errors="replace"))
    out = {}
    if not removals:
        return out
    wanted = frozenset().union(*(r.names for r in removals))
    types = frozenset().union(*(r.dropped for r in removals))
    deleted = [r.path for r in removals if r.deleted]
    files = TreeFiles(set(tree if tree is not None else tracked_tree()) | set(deleted)) if deleted else None
    gone = frozenset(key(p) for p in deleted)
    for data, classes in data_files(rows).items():
        if all(c in out for c in classes):
            continue
        cites = Citations(read(data), wanted, types)
        for removal in removals:
            if removal.path == data:
                continue   # the register's own edit is governed (`governed`), not cited
            cite = cites.broken_by(removal, files, gone)
            if cite:
                for c in classes:
                    out.setdefault(c, (data, cite))
                break
    return out


def self_test():
    """Fire each arm of the index's reading of the test sources on a planted tree (kb/Work PB3247): TestRepo's helpers
    read from its source (the real one, copied), a helper class's paths inherited transitively and only within the
    using test's project, a comment naming a helper is no use of it, a suite test resolved by its literal id, an
    inherited path never making a class a sweep, and an unreadable TestRepo helper reported."""
    import shutil
    import tempfile
    failures = 0

    def check(label, ok, detail):
        nonlocal failures
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}: {detail}")

    def drift(name, body):
        return f"/// <summary>The planted rule of {name}.</summary>\npublic sealed class {name}\n{{\n{body}\n}}\n"

    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        planted = {
            "tests/_shared/Corpus.cs": "internal static class Corpus\n{\n    internal static string Root { get; } = "
                                       "TestRepo.Suite();\n    internal sealed class Nested { }\n}\n",
            "tests/_shared/Notes.cs": 'internal static class Notes\n{\n    internal static string Dir => TestRepo.Kb("Work");\n}\n',
            "tests/P/Chain.cs": "internal static class Chain\n{\n    internal static object Of => Corpus.Root;\n}\n",
            "tests/Q/Chain.cs": 'internal static class Chain\n{\n    static string D => TestRepo.Docs("OTHER.md");\n}\n',
            "tests/P/ReadsCorpusDriftTests.cs": drift("ReadsCorpusDriftTests", "    object X => Chain.Of;"),
            "tests/P/ReadsNotesDriftTests.cs": drift("ReadsNotesDriftTests", "    string X => Notes.Dir;"),
            "tests/P/NamesCaseDriftTests.cs": drift("NamesCaseDriftTests", '    string A => TestRepo.SuiteCase("g1");\n'
                                                    '    string B => TestRepo.SuiteFile("g1", "g1.out");'),
            "tests/P/CommentOnlyDriftTests.cs": drift("CommentOnlyDriftTests", "    // Corpus.Root is not read here"),
            "tests/P/OwnPathDriftTests.cs": drift("OwnPathDriftTests",
                                                  '    string X => TestRepo.Kb("Work", note + ".md");\n'
                                                  '    string Y => TestRepo.Src(["Proj", "Dir", .. parts]);\n'
                                                  '    string Z => Path.Combine(TestRepo.Root, "specs", "S.md");\n'
                                                  '    string W => Path.Combine([TestRepo.Root, "docs", "D", .. p]);'),
            "conformance-suite/cases/1.2-Clause/g1.cob": "", "conformance-suite/cases/1.2-Clause/g1.out": "",
            "kb/Work/N1.md": "", "docs/OTHER.md": "", "src/Proj/Dir/A.cs": ""}
        for rel, text in planted.items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(text, encoding="utf-8", newline="\n")
        shutil.copy(ROOT / TEST_REPO, root / TEST_REPO)
        repo = test_repo(root)
        check("TestRepo's helpers are read from its source, `Kb` among them, and every one is readable",
              repo["helpers"].get("Kb") == "kb" and repo["helpers"].get("VersionMatrix") == "tests/version-matrix"
              and repo["case"] and repo["beside"] and not repo["unread"], repo)
        rows = index(root)
        by = {r["test"]: r for r in rows}

        def gov(rel):
            return governed(rows, [rel])
        check("a helper's path is inherited through a helper chain: a suite golden governs the class (tier 0d)",
              "ReadsCorpusDriftTests" in gov("conformance-suite/cases/1.2-Clause/g1.cob"),
              by["ReadsCorpusDriftTests"]["through"])
        check("a path read through TestRepo.Kb governs a kb/Work note", "ReadsNotesDriftTests" in gov("kb/Work/N1.md"),
              by["ReadsNotesDriftTests"]["through"])
        check("a helper of another test project is not this project's (tests/Q/Chain.cs reads docs/OTHER.md)",
              "ReadsCorpusDriftTests" not in gov("docs/OTHER.md"), by["ReadsCorpusDriftTests"]["through"])
        check("a comment naming a helper is no use of it",
              "CommentOnlyDriftTests" not in gov("conformance-suite/cases/1.2-Clause/g1.cob"),
              by["CommentOnlyDriftTests"]["through"])
        spec = {r["test"] for r in governing(rows, "conformance-suite/cases/1.2-Clause/g1.out")[0]}
        check("a suite test named by its literal id governs its program and the file beside it specifically",
              "NamesCaseDriftTests" in spec and "NamesCaseDriftTests" in
              {r["test"] for r in governing(rows, "conformance-suite/cases/1.2-Clause/g1.cob")[0]},
              by["NamesCaseDriftTests"]["paths"])
        check("a call's path stops at its first argument that is not a literal, a collection expression included, and "
              "Path.Combine(TestRepo.Root, ...) is the root helper written out",
              by["OwnPathDriftTests"]["paths"] == ["kb/Work", "src/Proj/Dir", "specs/S.md", "docs/D"],
              by["OwnPathDriftTests"]["paths"])
        check("a path read only through a helper never makes the class a sweep (the Sweep trait follows its own scan)",
              not is_sweep(by["ReadsCorpusDriftTests"]) and not is_sweep(by["ReadsNotesDriftTests"]),
              [r["test"] for r in rows if is_sweep(r)])
        text = (root / TEST_REPO).read_text(encoding="utf-8")
        odd = re.sub(r"(\bclass\s+TestRepo\s*\{)", r"\1 public static string Odd(int n) => At(n.ToString());", text,
                     count=1)
        (root / TEST_REPO).write_text(odd, encoding="utf-8", newline="\n")
        check("a TestRepo helper in a shape the index cannot read is reported (`--check` fails on it)",
              test_repo(root)["unread"] == ["Odd"], test_repo(root)["unread"])
    print("ALL GREEN" if not failures else f"⛔ {failures} SELF-TEST FAILURE(S)")
    return 1 if failures else 0


def through(row, rel):
    """` — through H` when `row` governs `rel` only by a path it reads through the helper class H (else "")."""
    def holds(p):
        return rel == p or rel.startswith(p.rstrip("/") + "/")
    if any(holds(p) for p in row["paths"]):
        return ""
    vias = sorted({pathlib.PurePosixPath(v).stem for p, v in row["through"].items() if holds(p)})
    return f" — through {', '.join(vias)}" if vias else ""


def main(argv):
    # The rules quote <summary> text verbatim (⛔, §, em dashes); a Windows console's cp1252 stdout cannot encode
    # them, and the first such rule crashed the per-file query mid-listing.
    sys.stdout.reconfigure(encoding="utf-8")
    if argv[:1] == ["--self-test"]:
        return self_test()
    rows = index()
    if argv[:1] == ["--sweeps"]:
        for r in rows:
            if is_sweep(r):
                print(r["test"])
        return 0
    if argv[:1] == ["--scopes"]:
        scopes: dict = {}
        for r in rows:
            for sc in r["scopes"]:
                scopes.setdefault(r["test"], {})[sc["name"]] = {k: v for k, v in sc.items() if k != "name"}
        print(json.dumps(scopes, ensure_ascii=False, indent=1, sort_keys=True))
        return 0
    if argv[:1] == ["--held"]:
        # {"Class.Field": [path, ...]} in the named JSON file -> {"Class.Field": [the paths in_scope holds]}: the one way
        # TrackedTreeScopeDriftTests compares this mirror with TreeScope.Holds, path by path.
        if len(argv) != 2:
            print("usage: drift_rules.py --held <json file>")
            return 2
        declared = {f"{r['test']}.{sc['name']}": sc for r in rows for sc in readable(r)}
        asked = json.loads(pathlib.Path(argv[1]).read_text(encoding="utf-8"))
        unknown = sorted(k for k in asked if k not in declared)
        if unknown:
            print(f"no readable TreeScope declaration named: {', '.join(unknown)}")
            return 1
        print(json.dumps({k: [p for p in paths if in_scope(declared[k], p)] for k, paths in asked.items()},
                         ensure_ascii=False, sort_keys=True))
        return 0
    if argv[:1] == ["--check"]:
        if not rows:   # an index of nothing renders, and proves only that the scan found no drift test at all
            print("no drift test found: the scan read no *DriftTests class (train 1063 review)")
            return 1
        render(rows)   # the view must render from the tests as they are
        undocumented = [r["test"] for r in rows if not r["rule"]]
        if undocumented:   # a drift test's rule has ONE home, its <summary>; a test without one enforces an unwritten rule
            print(f"{len(undocumented)} drift test(s) state no rule — add a class <summary>: {', '.join(undocumented)}")
            return 1
        # A scope the index cannot read leaves its sweep governing nothing here (kb/Work PB3531).
        unreadable = [f"{r['test']}.{sc['name']}: {sc['error']}" for r in rows for sc in r["scopes"] if "error" in sc]
        if unreadable:
            print(f"{len(unreadable)} TreeScope declaration(s) the index cannot read:\n  " + "\n  ".join(unreadable))
            return 1
        # A TestRepo helper the index cannot read leaves every test calling it governing nothing (kb/Work PB3247).
        unread = test_repo()["unread"]
        if unread:
            print(f"{len(unread)} {TEST_REPO} path helper(s) the index cannot read (test_repo): {', '.join(unread)}")
            return 1
        print(f"drift-rule index renders — {len(rows)} drift tests, every one with its rule")
        return 0
    if argv[:1] == ["--out"]:
        if len(argv) != 2:
            print("usage: drift_rules.py --out <path>")
            return 2
        out = pathlib.Path(argv[1])
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(render(rows), encoding="utf-8", newline="\n")
        print(f"wrote {out} — {len(rows)} drift tests")
        return 0
    if not argv:
        print(__doc__)
        return 2
    for target in argv:
        rel = relative(target)
        specific, sweeps = governing(rows, rel)
        print(f"== {rel}: {len(specific)} specific drift rule(s)")
        for r in specific:
            print(f"  {r['test']}  ({r['file']}){through(r, rel)}\n    {r['rule'] or '(no summary — read the test)'}")
        if sweeps:
            print(f"  + {len(sweeps)} tree-wide sweep(s) also scan it: "
                  + ", ".join(r["test"] + through(r, rel) for r in sweeps))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
