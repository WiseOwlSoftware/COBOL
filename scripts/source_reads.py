"""source_reads.py — WHAT A BUILD FILE, A COMPILER TEST'S SOURCE OR A SCRIPT READS, STATICALLY (kb/Work PB5398, PB4395, PB4453; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.12): an MSBuild `Exec` and the properties it names, the repository paths a compiler test spells through `TestRepo`, the C# string literals of a test source, a Python script's import closure and the Markdown readers. The static twin of the inotify watch (`inert_watch.py`); the lanes themselves are `lane_classification.py`'s, and the INERT LANE audit that holds the two together is `inert_lane.py check`."""
from __future__ import annotations

import ast
import functools
import os
import posixpath
import re
from collections.abc import Iterable
from pathlib import Path
import csharp_lexer  # THE one lexer of a C# source, shared with buildgraph.py (kb/Work PB5230 review R2-1)
import selftest_layout  # this directory's: where a case file's subject is (kb/Work PB5428)
from lane_classification import REPO, listing_glob, under

#: A build step that runs a script: an MSBuild `<Exec Command="…">`, and a property it names.
EXEC_RE = re.compile(r"<Exec\b[^>]*\bCommand=\"(?P<command>[^\"]*)\"")


PROPERTY_RE = re.compile(r"<(?P<name>[A-Za-z_][\w.]*)>(?P<value>[^<]*)</(?P=name)>")


MSBUILD_REF_RE = re.compile(r"\$\((?P<name>[\w.]+)\)")


SCRIPT_TOKEN_RE = re.compile(r"[\w./\\-]+\.(?:ps1|py|sh)\b")


#: A compiler test naming a repository path through the one helper (tests/_shared/TestRepo.cs): the method, then its
#: arguments up to the closing parenthesis (a nested call ends the match, and its literal prefix is what is read).
TEST_REPO_RE = re.compile(r"\bTestRepo\.(?P<method>Scripts|At|Src|Tests|Docs|Suite)\((?P<args>[^()]*)\)?")


TEST_REPO_PREFIX = {"Scripts": "scripts", "At": "", "Src": "src", "Tests": "tests", "Docs": "docs",
                    "Suite": "conformance-suite"}


STRING_LITERAL_RE = re.compile(r'^\s*"(?P<s>[^"\\{}]*)"\s*$')


#: A bare script file name a compiler test spells (`[InlineData("r2_cost.py", …)]`): what a computed `TestRepo` call
#: under a literal directory may run (`_test_readers`).
SCRIPT_NAME_RE = re.compile(r'"(?P<name>[\w.-]+\.(?:py|ps1|sh))"')


#: A compiler test LISTING a repository directory: `Directory.EnumerateFiles(TestRepo.<method>(…), "<pattern>"[, option])`
#: (or `GetFiles`). With a literal pattern it reads the files the pattern matches in that directory alone, or in its
#: whole subtree when the option is `SearchOption.AllDirectories` (or computed); a computed pattern is not this form.
ENUMERATE_RE = re.compile(r"\bDirectory\.(?:EnumerateFiles|GetFiles)\(\s*(?P<call>TestRepo\.\w+\([^()]*\))\s*,\s*"
                          r'"(?P<pattern>[^"\\{}]*)"\s*(?:,\s*(?P<option>[\w.]+)\s*)?\)')


def _repo_call_path(m: re.Match[str]) -> str | None:
    """The repository path a `TEST_REPO_RE` match names ("" for the repository root), or None for a computed argument."""
    if not m.group(0).endswith(")"):
        return None  # a nested call: a computed argument
    literals = [STRING_LITERAL_RE.match(a) for a in m.group("args").split(",") if a.strip()]
    if not all(literals):
        return None
    return "/".join(p for p in [TEST_REPO_PREFIX[m.group("method")]] + [lit.group("s") for lit in literals] if p)


def _repo_call_prefix(m: re.Match[str]) -> str | None:
    """The literal directory a computed `TEST_REPO_RE` call names before its first computed argument
    (`TestRepo.Scripts("arch", script)` is scripts/arch), or None when no literal argument leads it."""
    if not m.group(0).endswith(")"):
        return None
    lead: list[str] = []
    for a in m.group("args").split(","):
        lit = STRING_LITERAL_RE.match(a)
        if not lit:
            break
        lead.append(lit.group("s"))
    return "/".join(p for p in [TEST_REPO_PREFIX[m.group("method")]] + lead if p) if lead else None


@functools.cache


def path_literal_re(trees: Iterable[str]) -> re.Pattern[str]:
    """A string literal that spells a path in one of the tooling `trees` (each a directory entry ending in `/`)."""
    return re.compile(r'"(?P<path>(?:' + "|".join(re.escape(t) for t in trees) + r')[\w./-]*)"')


def _build_readers(root: Path, paths: list[str]) -> Iterable[tuple[str, str]]:
    """Every script an MSBuild `Exec` of a tracked project or props/targets file runs, resolved against the properties
    of the same file and the project's directory (`$(MSBuildProjectDirectory)`, `$(MSBuildThisFileDirectory)`)."""
    for rel in paths:
        if not rel.endswith((".csproj", ".props", ".targets")):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8")
        except OSError:
            continue
        here = rel.rpartition("/")[0]
        props = {m.group("name"): m.group("value").strip() for m in PROPERTY_RE.finditer(text)}
        props |= {"MSBuildProjectDirectory": here, "MSBuildThisFileDirectory": here + "/" if here else ""}
        for m in EXEC_RE.finditer(text):
            command = m.group("command").replace("&quot;", '"')
            for _ in range(4):  # a property may name another
                command = MSBUILD_REF_RE.sub(lambda r: props.get(r.group("name"), r.group(0)), command)
            for token in SCRIPT_TOKEN_RE.findall(command):
                yield os.path.normpath(token.replace("\\", "/")).replace("\\", "/"), f"the build ({rel}, an Exec)"


def _test_readers(root: Path, paths: list[str], literal: re.Pattern[str]) -> Iterable[tuple[str, str]]:
    """Every repository path a compiler test NAMES through `TestRepo.<method>(…)` with string-literal arguments only: the
    file, or the directory (every path below it), the literals spell; with none, the method's own directory
    (`TestRepo.Scripts()` is all of scripts/), except the directory of a LISTING by a literal pattern
    (`Directory.EnumerateFiles(TestRepo.Scripts(), "guard*.sh")`, `ENUMERATE_RE`), which reads only the files the pattern
    matches there, or below it with `SearchOption.AllDirectories`. A call with a computed argument names no path
    this scan can know; what such a test reads is the watch's to see (module docstring), and its literal prefix alone
    would make whole trees code (measured 2026-11-01: 26 classes' computed calls spelled the whole repository)."""
    tracked_set = set(paths)
    for rel in paths:
        if not (rel.startswith("tests/") and rel.endswith(".cs")):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8")
        except OSError:
            continue
        code = "\n".join(x for x in text.splitlines() if not x.lstrip().startswith("//"))
        # A listing of one directory by a literal pattern reads the files it matches, never the directory's whole tree
        # (kb/Work PB4395 review R1-1: CorpusManifestTests lists only the top-level scripts/guard*.sh).
        listed: set[int] = set()
        for e in ENUMERATE_RE.finditer(code):
            call = TEST_REPO_RE.match(code, e.start("call"))
            where = _repo_call_path(call) if call else None
            if where is None:
                continue  # a computed directory: the generic scan below and the watch
            listed.add(e.start("call"))
            option = e.group("option")
            glob = listing_glob(e.group("pattern"), option is not None and option != "SearchOption.TopDirectoryOnly")
            prefix = where + "/" if where else ""
            yield from ((p, rel) for p in paths if p.startswith(prefix) and glob.fullmatch(p[len(prefix):]))
            if option is None or option == "SearchOption.TopDirectoryOnly":
                # The pattern itself: a file the change ADDS that it matches is listed too (kb/Work PB4395 review
                # R2-1), so a listing pattern entry must cover it (`under`).
                yield prefix + e.group("pattern"), rel
        names = {s.group("name") for s in SCRIPT_NAME_RE.finditer(code)}
        for m in TEST_REPO_RE.finditer(code):
            if m.start() in listed:
                continue
            path = _repo_call_path(m)
            if path:  # None is computed; "" is `TestRepo.At()`, the root itself, whose walks the watch sees
                yield path, rel
            elif path is None and (prefix := _repo_call_prefix(m)):
                # A script run by a computed name under a literal directory (`TestRepo.Scripts("arch", script)` with
                # `[InlineData("r2_cost.py", …)]`): each script name the file spells that the directory holds.
                yield from ((f"{prefix}/{n}", rel) for n in sorted(names) if f"{prefix}/{n}" in tracked_set)
        # A repository path spelled whole in a string literal (`"scripts/orchestrator/groups.py"`, a TreeScope's
        # `Under: ["scripts/", ".claude/"]`): a message names no path (it holds a space or a placeholder).
        for m in literal.finditer(code):
            yield m.group("path"), rel


def static_dir(node: ast.expr, names: dict[str, str]) -> str | None:
    """The repository-relative POSIX directory (or file) a path expression of a script names, evaluated statically
    ("" is the repository root), or None when it is computed past what this reader knows. `names` holds the module's
    own static names (`HERE = Path(__file__).resolve().parent`), and `__file__` the script itself. The forms are the ones
    the scripts write: `Path(__file__)` / `pathlib.Path(…)`, `.resolve()` / `.absolute()`, `.parent`, `.parents[n]`,
    `/ "x"`, `str(…)` / `os.fspath(…)`, and `os.path.dirname` / `abspath` / `realpath` / `join`."""
    def up(p: str | None, n: int) -> str | None:
        for _ in range(n):
            if p is None or p == "":
                return None  # above the repository: nothing tracked
            p = p.rpartition("/")[0]
        return p

    def join(a: str | None, b: str) -> str | None:
        if a is None:
            return None
        j = posixpath.normpath(posixpath.join(a, b)) if a else posixpath.normpath(b)
        return None if j.startswith("..") or posixpath.isabs(j) else ("" if j == "." else j)

    if isinstance(node, ast.Name):
        return names.get(node.id)
    if isinstance(node, ast.Attribute) and node.attr == "parent":
        return up(static_dir(node.value, names), 1)
    if (isinstance(node, ast.Subscript) and isinstance(node.value, ast.Attribute) and node.value.attr == "parents"
            and isinstance(node.slice, ast.Constant) and isinstance(node.slice.value, int)):
        return up(static_dir(node.value.value, names), node.slice.value + 1)
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Div) and isinstance(node.right, ast.Constant) \
            and isinstance(node.right.value, str):
        return join(static_dir(node.left, names), node.right.value)
    if not isinstance(node, ast.Call):
        return None
    f, args = node.func, node.args
    callee = f.id if isinstance(f, ast.Name) else (
        f"{f.value.id}.{f.attr}" if isinstance(f, ast.Attribute) and isinstance(f.value, ast.Name) else
        f"{f.value.value.id}.{f.value.attr}.{f.attr}" if isinstance(f, ast.Attribute)
        and isinstance(f.value, ast.Attribute) and isinstance(f.value.value, ast.Name) else None)
    if isinstance(f, ast.Attribute) and f.attr in ("resolve", "absolute") and not args:
        return static_dir(f.value, names)
    if callee in ("Path", "pathlib.Path", "str", "os.fspath", "os.path.abspath", "os.path.realpath") and len(args) == 1:
        return static_dir(args[0], names)
    if callee == "os.path.dirname" and len(args) == 1:
        return up(static_dir(args[0], names), 1)
    if callee == "os.path.join" and args:
        out = static_dir(args[0], names)
        for a in args[1:]:
            if not (isinstance(a, ast.Constant) and isinstance(a.value, str)):
                return None
            out = join(out, a.value)
        return out
    return None


def static_names(tree: ast.Module, rel: str) -> dict[str, str]:
    """The module-level names of the Python file `rel` (parsed: `tree`) that `static_dir` can evaluate, in order, through
    if/try/with blocks, `__file__` being `rel` itself: what `_imports` and buildgraph.py's static check of a generated
    target (kb/Work PB5231) evaluate a path expression over."""
    names = {"__file__": rel}

    def bind(body: list[ast.stmt]) -> None:
        for s in body:
            if isinstance(s, (ast.Assign, ast.AnnAssign)) and s.value is not None:
                targets = s.targets if isinstance(s, ast.Assign) else [s.target]
                value = static_dir(s.value, names)
                for t in targets:
                    if isinstance(t, ast.Name) and value is not None:
                        names[t.id] = value
            for block in ("body", "orelse", "finalbody"):
                if isinstance(s, (ast.If, ast.Try, ast.With)) and getattr(s, block, None):
                    bind(getattr(s, block))
            if isinstance(s, ast.Try):
                for h in s.handlers:
                    bind(h.body)

    bind(tree.body)
    return names


def _imports(rel: str, text: str, root: Path = REPO) -> tuple[list[tuple[int, str, list[str]]], list[str]]:
    """What the Python file `rel` imports and where it looks: every `import`/`from … import` anywhere in it (a lazy import
    inside a function included) as (relative level, module, imported names), and every directory a `sys.path.insert` /
    `sys.path.append` of it names statically (`static_dir`, over its module-level names). A file that does not parse
    imports nothing this reader can know (the watch sees what it opens).

    A file under a `selftests/` tree has two more facts (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5428): the
    runner starts it with `scripts/` on `PYTHONPATH`, so `import selftest_cases` resolves there (that directory is one
    it looks in), and the call `selftest_cases.subject(__file__)` is an import of its subject, `selftest_layout.subject_of`
    (a Python one) in the tree at `root`, whose own sibling imports then resolve as they do when the subject runs."""
    try:
        tree = ast.parse(text, filename=rel)
    except (SyntaxError, ValueError):
        return [], []
    names = static_names(tree, rel)
    imports: list[tuple[int, str, list[str]]] = []
    dirs: list[str] = []
    for n in ast.walk(tree):
        if isinstance(n, ast.Import):
            imports += [(0, a.name, []) for a in n.names]
        elif isinstance(n, ast.ImportFrom):
            imports.append((n.level, n.module or "", [a.name for a in n.names]))
        elif (isinstance(n, ast.Call) and isinstance(n.func, ast.Attribute) and n.func.attr in ("insert", "append")
              and isinstance(n.func.value, ast.Attribute) and n.func.value.attr == "path"
              and isinstance(n.func.value.value, ast.Name) and n.func.value.value.id == "sys" and n.args):
            d = static_dir(n.args[-1], names)
            if d is not None:
                dirs.append(d)
    if selftest_layout.in_tree(rel):
        dirs.append("scripts")
        if any(isinstance(n, ast.Call) and isinstance(n.func, ast.Attribute) and n.func.attr == "subject"
               and isinstance(n.func.value, ast.Name) and n.func.value.id == selftest_layout.COLLECTOR for n in ast.walk(tree)):
            subject = selftest_layout.subject_of(rel, root)
            if subject is not None and subject.endswith(".py"):
                dirs.append(subject.rpartition("/")[0])
                imports.append((0, subject.rpartition("/")[2][:-3], []))
    return imports, dirs


def _module_files(base: str, dotted: list[str], tracked_set: set[str], tracked_dirs: set[str]) -> list[str]:
    """The tracked module files `import a.b.c` reaches from the directory `base`: each component's `<name>.py` or
    `<name>/__init__.py`, descending while a package (or a namespace directory) holds the next one."""
    out: list[str] = []
    here = base
    for part in dotted:
        stem = f"{here}/{part}" if here else part
        if f"{stem}.py" in tracked_set:
            out.append(f"{stem}.py")
            break  # a module file: what follows is its attributes
        if f"{stem}/__init__.py" in tracked_set:
            out.append(f"{stem}/__init__.py")
        elif stem not in tracked_dirs:
            break
        here = stem
    return out


def import_closure(root: Path, seeds: list[tuple[str, str]], tracked_set: set[str]) -> Iterable[tuple[str, str]]:
    """THE IMPORT CLOSURE (kb/Work PB4395 review R2-1): every tracked Python module a script in `seeds` (path, reader)
    imports, directly or through a module it imports, with its reader (`<reader>, through <importer>`). A module is
    resolved against the importing file's directory and every directory the importing chain put on `sys.path` statically
    (`_imports`: a `sys.path` edit lasts for the process, so an imported module inherits its importer's), and over-reads
    rather than under-reads: every directory that holds the name counts, whatever the order."""
    tracked_dirs = {p[:i] for p in tracked_set for i, ch in enumerate(p) if ch == "/"}
    seeded = {p for p, _ in seeds}
    parsed: dict[str, tuple[list[tuple[int, str, list[str]]], list[str]]] = {}
    search: dict[str, set[str]] = {}
    via: dict[str, str] = {}
    todo = [(p, r, frozenset()) for p, r in seeds if p.endswith(".py") and p in tracked_set]
    while todo:
        rel, reader, inherited = todo.pop()
        known = search.setdefault(rel, set())
        if rel in via and inherited <= known:
            continue
        via.setdefault(rel, reader)
        if rel not in parsed:
            try:
                parsed[rel] = _imports(rel, (root / rel).read_text(encoding="utf-8"), root)
            except (OSError, UnicodeDecodeError):
                parsed[rel] = ([], [])
        imports, dirs = parsed[rel]
        here = rel.rpartition("/")[0]
        known |= inherited | {here, *dirs}
        for level, module, names in imports:
            dotted = [x for x in module.split(".") if x]
            if level:
                base = here
                for _ in range(level - 1):
                    base = base.rpartition("/")[0]
                bases = [base]
            else:
                bases = sorted(known)
            for base in bases:
                for target in (_module_files(base, dotted, tracked_set, tracked_dirs)
                               + [f for n in names for f in _module_files(base, dotted + [n], tracked_set, tracked_dirs)]):
                    if target != rel:
                        todo.append((target, via.get(target, f"{via[rel]}, through {rel}"), frozenset(known)))
    yield from ((rel, reader) for rel, reader in via.items() if rel not in seeded)


def code_readers(root: Path, paths: list[str], trees: tuple[str, ...]) -> list[tuple[str, str]]:
    """THE STATIC TWIN OF THE TOOLING LANE'S WATCH (module docstring): (tracked path in one of `trees`, its reader) for
    every tooling path the build or a compiler test is seen to read: a csproj `Exec`'s script, the script of every
    self-test that reads the built assemblies (`SELF-TEST-NEEDS: build`, scripts/self_tests.py), every path or
    directory a compiler test names through `TestRepo` or spells whole in a string literal, and every Python module one
    of those scripts IMPORTS, transitively (`import_closure`, kb/Work PB4395 review R2-1: a script outside the tooling
    trees, conformance-suite/harness/run.py, imports scripts/standard_streams.py). `trees` are the tooling trees
    (TOOLING_UNDER: a directory ends in `/`); a directory read is every tracked path below it."""
    import self_tests  # noqa: PLC0415 — it imports this module; the build marker's one home is its NEEDS_RE

    found: list[tuple[str, str]] = list(_build_readers(root, paths))
    found += [(t.path, "its own post-build self-test (SELF-TEST-NEEDS: build)") for t in self_tests.discover(root)
              if t.needs_build]
    found += list(_test_readers(root, paths, path_literal_re(t for t in trees if t.endswith("/"))))
    tracked_set = set(paths)
    files = [(p, r) for p, r in found if p in tracked_set]
    below = [(p, r) for p, r in found if "*" in p or "?" in p]  # a listing's pattern: a read of the names it matches
    for path, reader in found:
        if path not in tracked_set and not ("*" in path or "?" in path):
            prefix = path if path.endswith("/") else path + "/"
            below += [(p, reader) for p in paths if p.startswith(prefix) or not prefix.strip("/")]
    # The scripts a test or the build RUNS are named file by file (a literal path, a listing's matches, a computed name
    # under a literal directory): their imports are read too. A directory a test names (`TestRepo.Scripts("spec")`, an
    # existence check) is a read of every file below it, never a run of every script there, so it seeds no import.
    read = files + below + list(import_closure(root, files, tracked_set))
    return sorted({(p, r) for p, r in read if any(under(p, t) for t in trees)})


#: ⛔ NO COMPILER TEST OPENS A MARKDOWN FILE (owner 2026-11-02, kb/Work PB4453: "There is no compelling reason a compiler
#: change needs to read an markdown note"). The Markdown names a compiler test may spell, each with why: the ISO text is a
#: READ entry (`Classification.read`, derived, not listed here), and this is the rest.
TESTS_MAY_NAME_MARKDOWN = {
    "DIAGNOSTICS.md": "the gitignored rendered view whose raw render DiagnosticRegistryDriftTests writes to WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC "
                      "(scripts/gen-diagnostics-doc.ps1); no test reads it",
}


DOCS_WALK_RE = re.compile(r"\bTestRepo\s*\.\s*Docs\s*\(\s*\)")


TREE_SCOPE_RE = re.compile(r"\bnew\s+TreeScope\s*\(|\bTreeScope\s+\w+\s*=\s*new\s*\(")


def _balanced(code: str, open_at: int) -> str:
    """The text between the parenthesis at `open_at` and its partner (code only: no literal can hide a bracket)."""
    depth = 0
    for k in range(open_at, len(code)):
        depth += code[k] == "("
        depth -= code[k] == ")"
        if depth == 0:
            return code[open_at:k + 1]
    return code[open_at:]


def markdown_readers(root: Path, paths: list[str], names_allowed: dict[str, str]) -> list[str]:
    """THE DRIFT TEST THAT NO TEST PROJECT OPENS A MARKDOWN FILE (kb/Work PB4453), static and on every host; the Linux inotify
    watch is its dynamic twin. Of every compiler test source (a `.cs` under tests/): a string literal that ends `.md` (a
    `TestRepo.At("README.md")`, a `"*.md"` listing, a `Kinds: [".md"]`) unless its file name is one a test may name
    (`names_allowed`: the ISO text, which is a READ entry, and the gitignored view a test writes); `TestRepo.Docs()` with no
    argument, which walks the documentation tree; and a `TreeScope` that names no `Kinds`, which reads every tracked file
    under its roots, Markdown included. A name allowed that no test spells any more is stale, and a finding."""
    findings: list[str] = []
    spelled: set[str] = set()
    for rel in paths:
        if not (rel.startswith("tests/") and rel.endswith(".cs")):
            continue
        try:
            code, literals = csharp_lexer.parts((root / rel).read_text(encoding="utf-8"))
        except OSError:
            continue
        for lit in literals:
            value = lit.strip()
            if re.search(r"\.md$", value, re.IGNORECASE):
                name = posixpath.basename(value.replace("\\", "/"))
                spelled.add(name)
                if name not in names_allowed:
                    findings.append(f"{rel} spells the Markdown path {value!r}: no compiler test opens a Markdown file (owner "
                                    "2026-11-02, kb/Work PB4453); make the check an audit (scripts/doc_audits.py), or move "
                                    "the datum it needs into a non-Markdown file the Markdown documents")
        if DOCS_WALK_RE.search(code):
            findings.append(f"{rel} calls TestRepo.Docs() with no argument, which walks the documentation tree (Markdown): "
                            "no compiler test opens a Markdown file (kb/Work PB4453)")
        for m in TREE_SCOPE_RE.finditer(code):
            if "Kinds" not in _balanced(code, m.end() - 1):
                findings.append(f"{rel} declares a TreeScope with no Kinds, which reads every tracked file under its roots, "
                                "Markdown included: name the kinds it reads (kb/Work PB4453)")
    findings += [f"the Markdown name {n!r} is allowed to a compiler test and none spells it any more: remove it"
                 for n in sorted(names_allowed) if n not in spelled]
    return findings
