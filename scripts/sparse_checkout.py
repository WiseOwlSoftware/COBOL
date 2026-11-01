#!/usr/bin/env python3
"""sparse_checkout.py — ⛔ A CI JOB THAT CHECKS OUT ONLY SOME PATHS LISTS EVERY REPOSITORY FILE ITS SCRIPTS REACH
(kb/Work PB3787, PB3810, PB4395).

    python scripts/sparse_checkout.py --check       # every sparse job of every workflow; exit 1 naming each file left out
    python scripts/sparse_checkout.py --self-test   # each arm on a planted workflow, then the real workflows

Every job of a workflow under `.github/workflows/` whose checkout has a `sparse-checkout` list (the `supersede` job of
kb/Work PB3769, `toolchain-cache` and `ci-gate` of kb/Work PB4007, and the nightly workflow's) runs on a tree without
the rest, so each file its steps name, each file a local action it uses names, and each repository module a Python
script it runs imports anywhere in its text (transitively, a function body's lazy import included) must be inside that
list; the job sets `sparse-checkout-cone-mode: false`, the mode the list is read in. `supersede`'s step is
`continue-on-error`, so a missing `scripts/standard_streams.py` made it cancel nothing while the run stayed green and no
test failed (kb/Work PB3637 added the file by hand). The first shape was `cancel_superseded.py --self-test` as kb/Work
PB3787 reproduced it: the reviewer's `git archive` of `scripts/orchestrator` alone fails with `ModuleNotFoundError: No
module named 'standard_streams'`.

WHAT A JOB REACHES: every token of its steps (comments and the sparse lists themselves set aside) that names an existing
repository file, and the files of a local action (`uses: ./…`) it calls; from a Python file, the files of every
repository module it imports anywhere in its text (an `import`, a `from … import` or a submodule the latter names, at
module level, in a class body, in a function body or under an `if`: a lazy import runs on the job's path when its
function does, and an over-approximated closure is loud where an under-approximated one is silent, kb/Work PB3810); from
a shell script, a PowerShell script or an action, the same token scan of its non-comment text. Python resolves a bare
module name through `sys.path`, which the scripts extend themselves (`sys.path.insert(0, str(HERE))`), so the audit
searches the entry script's directory, the importing file's directory and each directory above it. It does not run
Python: a module named by `importlib` or a path string in a Python file is not seen (the two limits of a static read; a
`subprocess` call of another script is the sibling of the second).

THE LIST is read as `git sparse-checkout` reads it in non-cone mode (gitignore patterns: a name covers the file or
directory and everything beneath it; `*`, `**` and `?` glob; a leading `/` anchors), so a job that does not set
`sparse-checkout-cone-mode: false` is refused (the action defaults to cone mode, where the same lines name directories
only), and a `!` pattern is refused as unmodelled. A job's checkout and the checkout's one retry (kb/Work PB4324) each
carry a list, and scripts/ci_bounds.py holds the two equal: the patterns are the first list's, and NO list is part of the
steps' text the scripts are read from.

The jobs are read by scripts/ci_workflow.py, the one reader of a workflow's jobs (never an `on:` trigger: kb/Work PB3810,
where a reader over the whole file returned the triggers as jobs). Until kb/Work PB4395 this audit was a C# class of the
compiler's test assembly (SparseCheckoutDriftTests over SparseCheckoutAudit), so a change to a workflow or a script was
a compiler change that only a build could check; it is the pipeline's, and runs in the tooling lane.
"""
from __future__ import annotations

import posixpath
import re
import subprocess
import sys
from collections import deque
from collections.abc import Callable, Iterable, Iterator
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))  # scripts/, for the shared modules
import ci_workflow  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
#: The tracked workflows the audit reads.
WORKFLOWS = ".github/workflows/"
WORKFLOW_KINDS = (".yml", ".yaml")

#: A repository-relative path's text, or None when no such file exists.
Reader = Callable[[str], "str | None"]

SPARSE_KEY = re.compile(r"^(?P<ind>[ \t]*)sparse-checkout:[ \t]*(?P<value>.*)$")
CONE_OFF = re.compile(r"^[ \t]*sparse-checkout-cone-mode:[ \t]*false[ \t]*$", re.MULTILINE)
TOKEN = re.compile(r"[A-Za-z0-9_.\-/]+")
LOCAL_ACTION = re.compile(r"\buses:[ \t]*(?P<dir>\./[^\s'\"]+)")
IMPORT_STATEMENT = re.compile(r"^(?P<ind>[ \t]*)import[ \t]+(?P<mods>[^#;\r\n]+)")
FROM_STATEMENT = re.compile(r"^(?P<ind>[ \t]*)from[ \t]+(?P<dots>\.*)(?P<mod>[\w.]*)[ \t]+import[ \t]+(?P<names>[^#\r\n]*)")


@dataclass(frozen=True)
class Job:
    """A job's id and its code (no comment-only line), tagged with the workflow file it came from."""
    file: str
    id: str
    body: str


@dataclass(frozen=True)
class SparseJob:
    """A sparse-checkout job: its steps' text without the lists, and the first list's patterns."""
    file: str
    id: str
    steps: str
    patterns: tuple[str, ...]
    cone_mode_off: bool


@dataclass(frozen=True)
class Reached:
    """One repository file a sparse job reaches, with the file that led to it."""
    path: str
    via: str


def jobs(file: str, workflow: str) -> list[Job]:
    """The jobs of one workflow file, each with its code: the jobs scripts/ci_workflow.py reads beneath `jobs:`."""
    return [Job(file, j.id, j.code) for j in ci_workflow.jobs(workflow)]


def sparse_jobs(found: Iterable[Job]) -> list[SparseJob]:
    """The jobs of `found` that give their checkout a sparse list."""
    out: list[SparseJob] = []
    for job in found:
        lines = job.body.split("\n")
        if not any(SPARSE_KEY.match(x) for x in lines):
            continue
        patterns: list[str] = []
        steps: list[str] = []
        first = True
        i = 0
        while i < len(lines):
            key = SPARSE_KEY.match(lines[i])
            if not key:
                steps.append(lines[i])
                i += 1
                continue
            at = len(key.group("ind"))
            value = key.group("value").strip()
            these: list[str] = []
            if value and not value.startswith(("|", ">")):
                these.append(value.strip("'\""))
            else:
                while i + 1 < len(lines):
                    line = lines[i + 1]
                    if line.strip() and len(line) - len(line.lstrip()) <= at:
                        break
                    if line.strip():
                        these.append(line.strip())
                    i += 1
            if first:
                patterns = these
            first = False
            i += 1
        out.append(SparseJob(job.file, job.id, "\n".join(steps), tuple(patterns), bool(CONE_OFF.search(job.body))))
    return out


def reach(job: SparseJob, read: Reader) -> list[Reached]:
    """Every repository file `job` reaches, each once, in the order found."""
    reached: list[Reached] = []
    seen: set[str] = set()
    origin = f"{job.file} job `{job.id}`"
    work: deque[tuple[str, str, str]] = deque((p, origin, posixpath.dirname(p)) for p in _token_files(job.steps, read))
    while work:
        path, via, entry = work.popleft()
        if path in seen:
            continue
        seen.add(path)
        reached.append(Reached(path, via))
        text = read(path) or ""
        if path.endswith(".py"):
            following: Iterable[str] = imported_files(path, text, entry, read)
        elif path.endswith((".sh", ".ps1", ".yml", ".yaml")):
            following = _token_files(text, read)
        else:
            following = ()
        work.extend((n, path, entry) for n in following)
    return reached


def problems(found: Iterable[SparseJob], read: Reader) -> list[str]:
    """The problems with each sparse job: a list read in cone mode, an unmodelled pattern, and each reached file the
    list leaves out, with the chain that reaches it. Empty when every job is sound."""
    out: list[str] = []
    for job in found:
        name = f"{job.file} job `{job.id}`"
        if not job.cone_mode_off:
            out.append(f"{name}: no `sparse-checkout-cone-mode: false` (the action defaults to cone mode, where the list "
                       "names directories only)")
            continue
        negated = next((p for p in job.patterns if p.startswith("!")), None)
        if negated is not None:
            out.append(f"{name}: the pattern `{negated}` is a negation, which this audit does not model")
            continue
        out += [f"{name}: `{r.path}` (reached from {r.via}) is not in its sparse-checkout list, so the job runs on a tree "
                "without it" for r in reach(job, read) if not covers(job.patterns, r.path)]
    return out


def covers(patterns: Iterable[str], path: str) -> bool:
    """Whether non-cone sparse-checkout `patterns` include the repository-relative `path`: a pattern that matches the
    path or any directory above it."""
    parts = path.split("/")
    pats = list(patterns)
    return any(_matches(p, "/".join(parts[:i])) for i in range(1, len(parts) + 1) for p in pats)


def _matches(pattern: str, prefix: str) -> bool:
    p = pattern.rstrip("/")
    anchored = p.startswith("/")
    p = p.lstrip("/")
    anchored |= "/" in p
    target = prefix if anchored else prefix[prefix.rfind("/") + 1:]
    rx = (re.escape(p).replace(r"\*\*", "\x01").replace(r"\*", "[^/]*").replace(r"\?", "[^/]").replace("\x01", ".*"))
    return re.fullmatch(rx, target) is not None


def _token_files(text: str, read: Reader) -> Iterator[str]:
    """The existing repository files `text` names, comments set aside: a token with a `/` or an extension that `read`
    finds, and the `action.yml` of a local action it uses."""
    code = _without_comments(text)
    for m in LOCAL_ACTION.finditer(code):
        d = m.group("dir")[2:].rstrip("/")
        for name in ("action.yml", "action.yaml"):
            if read(f"{d}/{name}") is not None:
                yield f"{d}/{name}"
    for m in TOKEN.finditer(code):
        token = m.group(0)[2:] if m.group(0).startswith("./") else m.group(0)
        token = token.rstrip("./")
        if not token or token.startswith("/") or ".." in token or not ("/" in token or "." in token):
            continue
        if read(token) is not None:
            yield token


def _without_comments(text: str) -> str:
    """The text without whole-line comments and trailing ` # …` comments (YAML, shell and PowerShell)."""
    def strip(line: str) -> str:
        if line.lstrip().startswith("#"):
            return ""
        c = line.find(" #")
        return line if c < 0 else line[:c]
    return "\n".join(strip(x) for x in text.split("\n"))


def imported_files(path: str, text: str, entry_dir: str, read: Reader) -> Iterator[str]:
    """The repository Python files `text` (the file at `path`) imports: every import statement, a function body's lazy
    import included, and not a docstring's text."""
    here = posixpath.dirname(path)
    roots = [entry_dir, here]
    d = here
    while d:
        d = posixpath.dirname(d)
        roots.append(d)
    roots = list(dict.fromkeys(roots))
    for level, module, names in _imports(text):
        yield from _resolve(roots if level == 0 else [_up(here, level - 1)], module, names, read)


def _up(d: str, levels: int) -> str:
    for _ in range(levels):
        d = posixpath.dirname(d)
    return d


def _join(a: str, b: str) -> str:
    return b if not a else a if not b else f"{a}/{b}"


def _resolve(bases: list[str], module: str, names: list[str], read: Reader) -> Iterator[str]:
    """The module files for `module` (a dotted name, empty for `from . import x`) under the first of `bases` that holds
    it, its packages' `__init__.py` files, and the submodules `names` names."""
    rel = module.replace(".", "/")
    for root in bases:
        found: list[str] = []
        if module:
            file = next((f for f in (_join(root, rel + ".py"), _join(root, rel + "/__init__.py")) if read(f) is not None),
                        None)
            if file is not None:
                parts = module.split(".")
                found += [init for i in range(1, len(parts))
                          if read(init := _join(root, "/".join(parts[:i]) + "/__init__.py")) is not None]
                found.append(file)
        package = _join(root, rel)
        for n in names:
            sub = next((f for f in (_join(package, n + ".py"), _join(package, n + "/__init__.py")) if read(f) is not None),
                       None)
            if sub is not None:
                found.append(sub)
        if found:
            yield from found
            return


def _imports(text: str) -> Iterator[tuple[int, str, list[str]]]:
    """Each import statement of a Python file, at any depth, as (relative level, dotted module, imported names)."""
    lines = text.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    quote: str | None = None
    i = 0
    while i < len(lines):
        line = lines[i]
        i += 1
        if quote is not None:
            if quote in line:
                quote = None
            continue
        trimmed = line.lstrip()
        if not trimmed or trimmed[0] == "#":
            continue
        quote = _open_triple_quote(line)
        imp = IMPORT_STATEMENT.match(line)
        if imp:
            for part in imp.group("mods").split(","):
                module = part.strip().split(" ", 1)[0]
                if module:
                    yield 0, module, []
            continue
        frm = FROM_STATEMENT.match(line)
        if not frm:
            continue
        names = frm.group("names")
        if "(" in names and ")" not in names:
            while i < len(lines):
                names += " " + lines[i].split("#")[0]
                i += 1
                if ")" in lines[i - 1]:
                    break
        imported = [n.strip().split(" ", 1)[0] for n in names.replace("(", " ").replace(")", " ").split(",")]
        yield len(frm.group("dots")), frm.group("mod"), [n for n in imported if n and n != "*"]


def _open_triple_quote(line: str) -> str | None:
    """The triple-quote delimiter a line opens and does not close (its string goes on to the lines below), or None."""
    opened: str | None = None
    i = 0
    while i + 2 < len(line):
        three = line[i:i + 3]
        if three not in ('"""', "'''"):
            i += 1
            continue
        opened = three if opened is None else None if opened == three else opened
        i += 3
    return opened


# ── the real tree ────────────────────────────────────────────────────────────────────────────────────────────────

def workflows(repo: Path = REPO) -> list[str]:
    """The tracked workflow files (`git ls-files`, so an untracked scratch workflow is never audited)."""
    out = subprocess.run(["git", "-c", "core.quotePath=false", "ls-files", "-z", "--", WORKFLOWS], cwd=repo,
                         capture_output=True, text=True, encoding="utf-8", check=True).stdout
    return sorted(p for p in out.split("\0") if p.endswith(WORKFLOW_KINDS))


def repo_reader(repo: Path = REPO) -> Reader:
    """A reader of the working tree: a file's text, or None when no file is there (a directory is not a file)."""
    def read(rel: str) -> str | None:
        p = repo / rel
        return p.read_text(encoding="utf-8", errors="replace") if p.is_file() else None
    return read


def real_jobs(repo: Path = REPO, text_of: Callable[[str], str] | None = None) -> list[Job]:
    """Every job of every tracked workflow; `text_of` gives a workflow's text (the file's, unless a self-test plants
    another)."""
    read = repo_reader(repo)
    return [j for f in workflows(repo) for j in jobs(f, text_of(f) if text_of else read(f) or "")]


def check(repo: Path = REPO) -> int:
    found = sparse_jobs(real_jobs(repo))
    bad = problems(found, repo_reader(repo))
    for p in bad:
        print(f"  {p}")
    print(f"=== SPARSE CHECKOUT: {'RED — ' + str(len(bad)) + ' problem(s)' if bad else 'GREEN'} "
          f"({len(found)} sparse job(s) read) ===")
    return 1 if bad else 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

def _planted(*files: tuple[str, str]) -> Reader:
    table = dict(files)
    return table.get


def _one_job(jid: str, patterns: list[str], steps: str, cone_off: bool = True) -> list[SparseJob]:
    """A workflow with one job, `jid`, whose checkout lists `patterns`."""
    return sparse_jobs(jobs("w.yml", "jobs:\n"
                            f"  {jid}:\n    runs-on: ubuntu-26.04\n    steps:\n      - uses: actions/checkout@v7\n"
                            "        with:\n          sparse-checkout: |\n"
                            + "".join(f"            {p}\n" for p in patterns)
                            + ("          sparse-checkout-cone-mode: false\n" if cone_off else "")
                            + steps + "\n"))


def self_test(repo: Path = REPO) -> int:
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: object = "") -> None:
        results.append((name, bool(ok), "" if ok else str(detail)))

    run = "      - run: python3 tools/run.py"
    read = _planted(("tools/run.py", "import json\nimport mid  # noqa\n"), ("tools/mid.py", "from leaf import thing\n"),
                    ("leaf.py", "thing = 1\n"))
    found = problems(_one_job("j", ["tools"], run), read)
    arm("a transitive import left out of the list is flagged with its chain, and listing it clears the job",
        len(found) == 1 and "`leaf.py` (reached from tools/mid.py)" in found[0]
        and not problems(_one_job("j", ["tools", "leaf.py"], run), read), found)

    read = _planted(("tools/run.py", "import leaf\n"), ("leaf.py", "pass\n"))
    retry = ("      - if: steps.checkout.outcome != 'success'\n        uses: actions/checkout@v7\n        with:\n"
             "          sparse-checkout: |\n            tools\n            leaf.py\n"
             "          sparse-checkout-cone-mode: false\n")
    sj = _one_job("j", ["tools"], retry + run)
    found = problems(sj, read)
    arm("a retry checkout's repeated list is not a step's text: the patterns are the first list's, and the file is "
        "still reached from the script that imports it",
        len(sj) == 1 and sj[0].patterns == ("tools",) and "leaf.py" not in sj[0].steps and len(found) == 1
        and "`leaf.py` (reached from tools/run.py)" in found[0], (sj, found))

    found = problems(_one_job("j", ["other"], run), _planted(("tools/run.py", "pass\n"), ("other/x.py", "pass\n")))
    arm("the script the job runs must be in the list too",
        len(found) == 1 and "`tools/run.py` (reached from w.yml job `j`)" in found[0], found)

    read = _planted(
        ("tools/run.py", '"""Usage:\n    import docstring_mod\n"""\n# import comment_mod\n'
                         "def f(\n    a,\n):\n    import lazy_mod\n    return a\nasync def g():\n    from lazy2 import x\n"
                         "class C:\n    import class_mod\n    def m(self):\n        import method_mod\n"
                         "if True:\n    import if_mod\n"),
        *((f"{m}.py", "") for m in ("docstring_mod", "comment_mod", "lazy_mod", "lazy2", "class_mod", "method_mod",
                                    "if_mod")))
    got = [r.path for r in reach(_one_job("j", ["tools"], run)[0], read)]
    arm("a docstring and a comment are not followed, but a lazy import, a class body and an if block are",
        got == ["tools/run.py", "lazy_mod.py", "lazy2.py", "class_mod.py", "method_mod.py", "if_mod.py"], got)

    read = _planted(("tools/run.py", "def main():\n    import coord\n    coord.go()\n"), ("coord.py", "pass\n"))
    found = problems(_one_job("j", ["tools"], run), read)
    arm("a lazily imported module left out of the list is flagged with its chain (kb/Work PB3810), and listing it "
        "clears the job",
        len(found) == 1 and "`coord.py` (reached from tools/run.py)" in found[0]
        and not problems(_one_job("j", ["tools", "coord.py"], run), read), found)

    planted = [j.id for j in jobs("w.yml", "on:\n  push:\n    branches: [main]\n  merge_group:\njobs:\n  only:\n"
                                           "    runs-on: ubuntu-26.04\n    steps:\n      - run: true\n")]
    arm("the job reader sees the keys beneath `jobs:` and not the triggers (planted)", planted == ["only"], planted)

    read = _planted(
        ("pkg/run.py", "from . import sibling\nfrom .sub import deep\nfrom helpers import (\n    one,\n    two as t,\n)\n"
                       "import pkg2.mod\n"),
        *((p, "") for p in ("pkg/sibling.py", "pkg/sub/__init__.py", "pkg/sub/deep.py", "helpers/__init__.py",
                            "helpers/one.py", "helpers/two.py", "pkg2/__init__.py", "pkg2/mod.py")))
    got = sorted(r.path for r in reach(_one_job("j", ["x"], "      - run: python3 pkg/run.py")[0], read))
    arm("relative imports, packages and submodules are resolved",
        got == ["helpers/__init__.py", "helpers/one.py", "helpers/two.py", "pkg/run.py", "pkg/sibling.py",
                "pkg/sub/__init__.py", "pkg/sub/deep.py", "pkg2/__init__.py", "pkg2/mod.py"], got)

    read = _planted(
        (".github/actions/setup/action.yml", "runs:\n  steps:\n    - run: bash scripts/install.sh # not scripts/commented.sh\n"
                                             "    # bash scripts/commented-line.sh\n"),
        ("scripts/install.sh", "# see scripts/other.sh\nsource scripts/lib.sh\n"), ("scripts/lib.sh", "echo hi\n"),
        ("scripts/other.sh", ""), ("scripts/commented.sh", ""), ("scripts/commented-line.sh", ""))
    found = problems(_one_job("j", [".github/actions"], "      # scripts/step-comment.sh\n"
                                                         "      - uses: ./.github/actions/setup"), read)
    arm("a local action and the scripts it runs are reached, and a mention in a comment is not",
        len(found) == 2 and any("`scripts/install.sh` (reached from .github/actions/setup/action.yml)" in p for p in found)
        and any("`scripts/lib.sh` (reached from scripts/install.sh)" in p for p in found), found)

    cases = (("scripts/a.py", "scripts/a.py", True), ("scripts", "scripts/a.py", True),
             ("scripts/", "scripts/sub/a.py", True), ("scripts", "scriptsx/a.py", False),
             ("scripts/*.py", "scripts/a.py", True), ("scripts/*.py", "scripts/sub/a.py", False),
             ("scripts/**/*.py", "scripts/sub/deep/a.py", True), ("/global.json", "global.json", True),
             ("global.json", "src/global.json", True), ("/global.json", "src/global.json", False),
             ("scripts/a.py", "scripts/b.py", False))
    wrong = [c for c in cases if covers([c[0]], c[1]) != c[2]]
    arm(f"a sparse pattern covers a path as git reads it without cone mode ({len(cases)} cases)", not wrong, wrong)

    read = _planted(("a.py", ""))
    cone = problems(_one_job("j", ["a.py"], "      - run: python3 a.py", cone_off=False), read)
    negation = problems(_one_job("j", ["a.py", "!b.py"], "      - run: python3 a.py"), read)
    plain = sparse_jobs(jobs("w.yml", "jobs:\n  full:\n    runs-on: ubuntu-26.04\n    steps:\n"
                                      "      - uses: actions/checkout@v7\n      - run: python3 a.py\n"))
    arm("a cone-mode list and a negation are refused, and a job without a sparse list is ignored",
        len(cone) == 1 and "sparse-checkout-cone-mode: false" in cone[0] and len(negation) == 1
        and "negation" in negation[0] and plain == [], (cone, negation, plain))

    # THE REAL TREE. Each check must also see its population: a reader that found no sparse job would pass over nothing.
    files = workflows(repo)
    real = real_jobs(repo)
    sparse = sparse_jobs(real)
    declared = sum(1 for j in real if any(x.lstrip().startswith("sparse-checkout:") for x in j.body.split("\n")))
    ids = [j.id for j in sparse]
    found = problems(sparse, repo_reader(repo))
    arm("THE REAL TREE: every sparse-checkout job lists every repository file it reaches, and the audit reads every "
        "declared list (at least two; supersede and toolchain-cache among them)",
        declared >= 2 and declared == len(sparse) and "supersede" in ids and "toolchain-cache" in ids and not found,
        f"declared {declared}, read {ids}; " + "; ".join(found))

    real_ids = [j.id for j in jobs(".github/workflows/build-and-test.yml",
                                   (repo / ".github/workflows/build-and-test.yml").read_text(encoding="utf-8"))]
    arm("THE REAL TREE: the job reader sees build-and-test.yml's jobs and none of its triggers",
        "supersede" in real_ids and "toolchain-cache" in real_ids
        and not {"push", "pull_request", "merge_group"} & set(real_ids), real_ids)

    by_id = {j.id: j for j in sparse}
    read = repo_reader(repo)
    supersede = [r.path for r in reach(by_id["supersede"], read)] if "supersede" in by_id else []
    cache = [r.path for r in reach(by_id["toolchain-cache"], read)] if "toolchain-cache" in by_id else []
    want_supersede = ("scripts/orchestrator/cancel_superseded.py", "scripts/orchestrator/queue_facts.py",
                      "scripts/orchestrator/ruleset.py", "scripts/standard_streams.py",
                      "scripts/live_tools.py")  # standard_streams.live_python imports live_tools inside a function
    want_cache = (".github/actions/setup-toolchain/action.yml", "scripts/install-tool.sh", "toolchain.env",
                  "scripts/toolchain.py")
    arm("THE REAL TREE: the supersede and toolchain-cache jobs reach the files they run, the lazy imports included",
        all(f in supersede for f in want_supersede) and all(f in cache for f in want_cache),
        f"supersede misses {[f for f in want_supersede if f not in supersede]}, "
        f"toolchain-cache misses {[f for f in want_cache if f not in cache]}")

    # The acceptance of kb/Work PB3787: the real workflow without scripts/standard_streams.py in the supersede lists is
    # flagged, naming the file and the script that imports it. The supersede job's lists (its checkout and the
    # checkout's retry) are the file's first two; the toolchain-cache and ci-gate lists name the file too.
    wf_path = ".github/workflows/build-and-test.yml"
    wf = (repo / wf_path).read_text(encoding="utf-8").replace("\r\n", "\n")
    entry = "\n            scripts/standard_streams.py\n"
    start, end = wf.find("\n  supersede:"), wf.find("\n  audits:")
    job_text = wf[start:end] if 0 <= start < end else ""
    without = wf[:start] + job_text.replace(entry, "\n") + wf[end:] if job_text else wf
    planted_files = [f for f in files if f != wf_path]
    found = problems(sparse_jobs([*(j for j in real if j.file in planted_files), *jobs(wf_path, without)]), read)
    arm("THE REAL TREE, planted: build-and-test.yml without scripts/standard_streams.py in the supersede job's two lists "
        "is flagged once, naming the job, the file and the orchestrator script that imports it",
        job_text.count(entry) == 2 and len(found) == 1 and "job `supersede`" in found[0]
        and "`scripts/standard_streams.py`" in found[0] and "scripts/orchestrator/" in found[0],
        f"{job_text.count(entry)} list entries; {found}")

    for name, passed, detail in results:
        print(f"  {'PASS' if passed else 'FAIL'}  {name}" + ("" if passed or not detail else f"\n        {detail}"))
    failed = sum(1 for _, passed, _ in results if not passed)
    print(f"=== SPARSE CHECKOUT SELF-TEST: {'FAIL — ' + str(failed) + ' of ' if failed else 'ALL GREEN — '}"
          f"{len(results)} arms ===")
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    if argv == ["--check"]:
        return check()
    print(__doc__.split("\n\n")[1])
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
