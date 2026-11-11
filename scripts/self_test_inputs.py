#!/usr/bin/env python3
"""self_test_inputs.py — WHAT A SCRIPT SELF-TEST READ, recorded with its PASS, so a gate re-runs a self-test only when
something it reads has changed (kb/Work PB2914; docs/rearchitecture/DESIGN-test-build-ci.md §3.14.10).

    python scripts/self_test_inputs.py <self-test path>…   # each one's static inputs on this tree
    python scripts/self_test_inputs.py --self-test

⛔ WHY. A self-test whose inputs did not change cannot change its verdict, so a gate that re-runs it learns nothing and
pays for it: every implementer gate ran all 68 (kb/Work PB2563), the slowest 4-8 minutes on a quiet host, and a fleet's
gates ran them all at once until they passed the runner's time limit and failed every gate (kb/Work PB2914).
scripts/self_tests.py therefore records, with each PASS, the CONTENT of everything the self-test read, and a
gate reuses that PASS while all of it is unchanged (every gate, kb/Work PB3022). The gate runs the self-tests at all only
for a change that TOUCHES TOOLING (scripts/inert_lane.py `Lane.tooling`, kb/Work PB4326): it is the tooling lane's, and
these recorded inputs are what keep that lane to the self-tests the change can affect. CI's merge group and the Linux
gate run every self-test and record.

THE INPUTS OF A RUN (`Deps`) are OBSERVED, not guessed — a list of what a script mentions is far wider than what its
self-test reads (a hand-derived closure over mentions reached the whole tree from most orchestrator scripts):
  TRACED   every self-test the runner RUNS runs with this module's TRACER as the `sitecustomize` of every Python process
           it starts (PYTHONPATH): through the interpreter's audit hooks (PEP 578) it records each file opened (a
           module's compiled `__pycache__` file counts as its source), each directory listed and each path handed to a
           child process, inside the repository;
  STATIC   what the trace cannot see, from the source (`Deriver`): the self-test's own file; the RUNNER (a change to how
           self-tests run re-runs them all); `# SELF-TEST-READS: <path> …` declarations on a header comment line; and,
           for a PowerShell or bash self-test, whose own reads no audit hook sees, the closure over the files its
           non-comment lines MENTION (a token naming a tracked file or directory relative to the repository root, to the
           mentioning file's directory or to a directory above it, as `Join-Path $PSScriptRoot 'stop.ps1'` does; a
           directory only as a bare word with no `/` and no `.` beside the mentioning file, so `kb/Work` in a message
           string never pulls in the register), followed through the PowerShell and bash scripts it reaches (a Python
           script it runs is traced when it runs);
  GIT      a `git` child a traced process runs inside the repository reads what `git_reads` says: nothing for its
           location, configuration, worktrees or a commit id (GIT_ENVIRONMENT); the NAMES (`ls-files`, GIT_NAMES) or
           the CONTENT (`diff`, `log`, `show`, `ls-files -s` …, GIT_CONTENT) under its pathspec (a wildcard one: under
           the directory of its literal prefix; an absolute one made relative to the repository; the magic
           PATHSPEC_MAGIC models), and the whole tree when the command's MEANING is the whole tree (no pathspec, `*.py`
           at the root). Anything else is an UNKEYED READ: a subcommand, global option or pathspec magic it does not
           model, a path outside the repository or past its top. The runner fails the self-test and records no PASS
           (scripts/self_tests.py; the owner 2026-11-10): it is never keyed as the whole tree, which would hide the
           self-test's hermeticity defect as a test that re-runs on every gate, and never as a path no tracked file can
           appear under (`Tree.under` refuses one), whose key would never change;
  IMPORTS  each module NAME an import looked for, per directory inside the repository it searched (the audit event
           `import`, raised before the search, so a failed import counts): a top-level name in every sys.path entry, a
           submodule's last name in its package's `__path__`. The import system's own directory listings
           (`FileFinder._fill_cache`) are NOT recorded as listings (kb/Work PB5227): read so, every Python self-test that
           imports from scripts/ was keyed on every NAME in scripts/ and the repository root, and any new file there
           re-ran them all. The IMPORT KEY keeps only what an import can find: the entries of the searched directory
           that answer one of its names (`Tree.importable`: `name`, a package or namespace directory, and `name<s>`
           for each suffix importlib gives, IMPORT_SUFFIXES).
A run's Deps hold, per input file, its git blob id (`-` when the run looked for an untracked path), per directory it
saw a digest of what it saw (`Tree.digest`: the names in a listed directory, the paths or the content under a git
pathspec or a STATIC directory), and per searched directory its import key. A recorded PASS is REUSED only while every
one of them is unchanged on the current tree (`Deps.changed` is empty): an edit to anything it read, a new file in a
directory it listed, a tracked file where it found none, a module, package or extension that appeared or went under an
imported name (a new scripts/json.py re-runs every self-test that imported json; a new scripts/zz.py re-runs none that
did not import zz) — each re-runs it. The blob ids are the index's for an unedited file and `git hash-object`'s
for an edited one (a gate tests the working tree), a submodule's commit for anything under it.

⛔ WHAT NOTHING HERE SEES, named rather than hidden — reads that raise no audit event, so no record exists to key or to
fail on: a PowerShell or bash process's own reads of a path it builds at run time (declare it: `SELF-TEST-READS`) and
the git commands it runs itself; `os.stat`-style probes, `importlib.util.find_spec` among them (it raises no `import`
event); a module search outside the default path finder (a meta path finder, path hook or zip importer a self-test
installs, a `.pth` file); a sys.path directory OUTSIDE the repository (its modules are the environment, as the
interpreter is). Every read the trace DOES record is keyed or fails the run (UNKEYED READ); none is widened or
dropped. One keyed read is still narrower than its content: a file or directory git IGNORES is keyed by its absence
from the tree (`-`, the tracked names), not by what it holds — see kb/Work PB5227's unresolved-read addendum. A trace that does
not contain a Python self-test's own file means the tracer did not run: the runner records no PASS for it. The same
`sitecustomize` begins with the CLOCK SHIM (scripts/self_test_clock.py, kb/Work PB4358): it moves the Python clock only
in the proof run of a self-test that can read it, and does nothing otherwise. The backstop for all of these is CI (and
the Linux gate), which never reuse a PASS.
"""
from __future__ import annotations

import ast
import bisect
import hashlib
import importlib.machinery
import json
import os
import posixpath
import re
import subprocess
import sys
from collections.abc import Iterable
from dataclasses import dataclass, field
from pathlib import Path
import self_test_clock as clock  # this directory's: the shim every traced process starts with (kb/Work PB4358)
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]
#: The runner itself: a change to how a self-test is run (its environment, isolation, tracer) re-runs every self-test.
RUNNER = ("scripts/self_tests.py", "scripts/self_test_inputs.py", "scripts/self_test_clock.py")
#: Scripts whose own reads the tracer cannot see: their mentions are followed (`Deriver.static`).
UNTRACED_SCRIPTS = (".ps1", ".psm1", ".sh")
READS_RE = re.compile(r"^#\s*SELF-TEST-READS:\s*(\S.*?)\s*$", re.MULTILINE)
TOKEN_RE = re.compile(r"[\w.\-/\\]+")
#: The environment the tracer reads: where its records go, and the repository whose reads it records.
TRACE_ENV = "COBOL_SELF_TEST_TRACE"
TRACE_ROOT_ENV = "COBOL_SELF_TEST_TRACE_ROOT"
UNTRACKED = "-"
#: What a run can have seen of a directory (`Tree.digest`): the names in it, every path under it, or all their content.
DIGESTS = ("entries", "paths", "content")
#: The file suffixes an import finds a module by (source, bytecode, extension) on THIS interpreter: an entry `name<s>`
#: of a searched directory can answer `import name` (`Tree.importable`). From importlib, never a hand list.
IMPORT_SUFFIXES = tuple(importlib.machinery.all_suffixes())

#: ⛔ THE TRACER — written as `sitecustomize.py` into a directory the runner puts first on PYTHONPATH, so every Python
#: process a traced self-test starts (itself, and every child that inherits its environment) records its reads. It
#: records only paths inside the traced repository, keeps them in memory and writes one file per process at exit;
#: it never prints, so no self-test's output changes. F = a file opened, D = a directory listed, X = a path argument
#: of a child process (a script it runs), G = a `git` child started inside the repository (its cwd), I = a module name
#: an import looked for in a directory inside the repository (a top-level name in each sys.path entry, a submodule's in
#: its package's `__path__`): the audit event `import` comes before the search, so a failed import is recorded too.
TRACER = r'''
import atexit, os, sys
_root, _out = os.environ.get("COBOL_SELF_TEST_TRACE_ROOT"), os.environ.get("COBOL_SELF_TEST_TRACE")
if _root and _out:
    _prefix = os.path.normcase(os.path.abspath(_root)) + os.sep
    _seen = set()

    def _inside(path, cwd=None):
        try:
            if isinstance(path, int):
                return None
            path = os.fsdecode(path)
            full = os.path.normcase(os.path.abspath(os.path.join(cwd, path) if cwd else path))
        except Exception:
            return None
        if full.startswith(_prefix):
            return full[len(_prefix):]
        return "" if full + os.sep == _prefix else None

    def _record(kind, path, cwd=None):
        rel = _inside(path, cwd)
        if rel is not None:
            _seen.add(kind + "\t" + rel)

    def _hook(event, args):
        if event == "open":
            _record("F", args[0])
        elif event == "import":
            # what an import can find: its name in each directory it searches (kb/Work PB5227, the import key)
            try:
                head, _, tail = args[0].rpartition(".")
                where = (getattr(sys.modules.get(head), "__path__", None) or ()) if head else (args[2] or ())
                for d in list(where):
                    rel = _inside(d)
                    if rel is not None:
                        _seen.add("I\t" + rel + "\t" + tail)
            except Exception:
                pass
        elif event in ("os.listdir", "os.scandir"):
            # the import system's directory cache is no read of the directory (kb/Work PB5227): an import reads the
            # module file it opens ("open") and the entries that could answer its name ("import"), never all names
            _caller = sys._getframe(1).f_code
            if _caller.co_name == "_fill_cache" and _caller.co_filename.startswith("<frozen importlib"):
                return
            _record("D", "." if args[0] is None else args[0])
        elif event == "subprocess.Popen":
            argv, cwd = args[1], args[2]
            try:
                cwd = os.fsdecode(cwd) if cwd is not None else os.getcwd()
            except Exception:
                return
            argv = argv.split() if isinstance(argv, (str, bytes)) else list(argv or ())
            argv = [os.fsdecode(a) for a in argv if isinstance(a, (str, bytes, os.PathLike))]
            for a in argv:
                _record("X", a, cwd)
            if argv and os.path.basename(argv[0]).lower() in ("git", "git.exe"):
                where, rest = cwd, argv[1:]
                while len(rest) >= 2 and rest[0] == "-C":
                    where, rest = os.path.join(where, rest[1]), rest[2:]
                rel = _inside(where)
                if rel is not None:
                    _seen.add("G\t" + rel + "\t" + chr(31).join(rest))  # every argument: the pathspec decides

    sys.addaudithook(_hook)

    def _write():
        name = os.path.join(_out, "%d-%s.trace" % (os.getpid(), os.urandom(6).hex()))
        try:
            with open(name, "w", encoding="utf-8") as fh:
                fh.write("\n".join(sorted(_seen)))
        except OSError:
            # Only an orphan that outlived its self-test's run gets here (the runner removed the trace directory after
            # reading it), and it must not print into whatever output it still has: its reads came too late to count.
            pass

    atexit.register(_write)
'''


# ── the tracked tree ───────────────────────────────────────────────────────────────────────────────────────────────


def _git(repo: Path, *args: str, stdin: str | None = None) -> str:
    return subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True, text=True, encoding="utf-8",
                          input=stdin).stdout


@dataclass
class Tree:
    """The tracked files of one checkout and their content ids (git blob ids; a submodule's commit)."""
    repo: Path
    blobs: dict[str, str]
    gitlinks: frozenset[str]
    paths: list[str] = field(init=False)
    dirs: frozenset[str] = field(init=False)
    _folded: dict[str, str] = field(init=False)
    _digests: dict[tuple[str, str], str] = field(init=False, default_factory=dict)
    _entry_sets: dict[str, frozenset[str]] = field(init=False, default_factory=dict)

    def __post_init__(self) -> None:
        self.paths = sorted(self.blobs)
        dirs = {""}
        for p in self.paths:
            parts = p.split("/")[:-1]
            for i in range(1, len(parts) + 1):
                dirs.add("/".join(parts[:i]))
        self.dirs = frozenset(dirs)
        # Windows compares paths without case, and the trace records them normcase'd.
        self._folded = {os.path.normcase(p.replace("/", os.sep)): p for p in (*self.paths, *self.dirs)}

    @classmethod
    def load(cls, repo: Path) -> "Tree":
        """`git ls-files -s` for the index, then `git hash-object` for every file the working tree has edited (the
        marker DELETED for one it has removed) and for every UNTRACKED file git does not ignore: a gate tests the working
        tree, uncommitted edits and not-yet-added files included, so an untracked helper a self-test imports is keyed
        by its content, never by the "-" of a path that is absent (train 1052 review: a false REUSED)."""
        blobs, gitlinks = {}, set()
        for entry in _git(repo, "ls-files", "-s", "-z").split("\0"):
            if not entry:
                continue
            meta, path = entry.split("\t", 1)
            mode, blob, _stage = meta.split(" ")
            blobs[path] = blob
            if mode == "160000":
                gitlinks.add(path)
        edited = [p for p in _git(repo, "ls-files", "-m", "-z").split("\0") if p and p not in gitlinks]
        present = [p for p in edited if (repo / p).is_file()]
        blobs.update((p, "DELETED") for p in edited if p not in present)
        present += [p for p in _git(repo, "ls-files", "-o", "--exclude-standard", "-z").split("\0")
                    if p and "__pycache__/" not in p and (repo / p).is_file()]   # a .pyc is keyed by its source (owner)
        if present:
            ids = _git(repo, "hash-object", "--stdin-paths", stdin="\n".join(present) + "\n").split()
            blobs.update(zip(present, ids))
        return cls(repo, blobs, frozenset(gitlinks))

    def under(self, d: str) -> list[str]:
        """Every tracked path under directory `d` (`""`: the whole tree). ⛔ `d` must be a repository-relative POSIX path:
        anything else (absolute, a drive, a backslash, `..`) names no place a tracked file can appear, so its key would
        never change (kb/Work PB5227) — it is an error here, at the call, never an empty list."""
        if d.startswith("/") or d.endswith("/") or "\\" in d or DRIVE_RE.match(d) or ".." in d.split("/") or d == ".":
            raise ValueError(f"{d!r} is not a repository-relative path: no key may be made of it")
        if not d:
            return list(self.paths)
        return self.paths[bisect.bisect_left(self.paths, d + "/"):bisect.bisect_left(self.paths, d + "0")]

    def entries(self, d: str) -> list[str]:
        """The names directly in directory `d` that the tree tracks (files and directories)."""
        cut = len(d) + 1 if d else 0
        return sorted({p[cut:].split("/", 1)[0] for p in self.under(d)})

    def digest(self, kind: str, d: str) -> str:
        """What a run saw of directory `d` (`""`: the whole tree), as one digest — by KIND (DIGESTS): `entries`, the
        names directly in it (a directory listing); `paths`, every tracked path under it (`git ls-files`); `content`,
        every tracked path under it and its content id (a STATIC directory input, `git diff` or `ls-files -s`)."""
        if (kind, d) not in self._digests:
            if kind == "entries":
                lines = self.entries(d)
            elif kind == "paths":
                lines = self.under(d)
            elif kind == "content":
                lines = [f"{p}\0{self.blobs[p]}" for p in self.under(d)]
            else:
                raise ValueError(f"a tree digest kind is one of {DIGESTS}, not {kind!r}")
            self._digests[(kind, d)] = hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest()[:20]
        return self._digests[(kind, d)]

    def importable(self, d: str, names: Iterable[str]) -> list[str]:
        """The entries of directory `d` that could answer an import of one of `names` there: `name` itself (a package
        or namespace directory) and `name<suffix>` for each IMPORT_SUFFIXES — what the import system's directory cache
        is consulted for, and nothing else of the directory (the import key, kb/Work PB5227). Exact case, as the
        import system compares (no PYTHONCASEOK)."""
        if d not in self._entry_sets:
            self._entry_sets[d] = frozenset(self.entries(d))
        present = self._entry_sets[d]
        return sorted({e for n in names for e in (n, *(n + s for s in IMPORT_SUFFIXES)) if e in present})

    def blob(self, path: str) -> str:
        return self.blobs.get(path, UNTRACKED)

    def owner(self, rel: str) -> str | None:
        """The tracked path a repository-relative path names: itself, the gitlink of the submodule it sits in, or —
        for a compiled module (`__pycache__/m.cpython-3xx.pyc`) — its source. None when it names none."""
        if rel in self.blobs or rel in self.dirs:
            return rel
        head, _, name = rel.rpartition("/")
        if head.rpartition("/")[2] == "__pycache__" and name.endswith(".pyc"):
            return self.owner(posixpath.join(posixpath.dirname(head), name.split(".", 1)[0] + ".py"))
        for g in self.gitlinks:
            if rel.startswith(g + "/"):
                return g
        return None

    def unfold(self, folded: str) -> str:
        """A trace record's normcase'd OS path → the repository-relative path, in the tree's own case when tracked."""
        tracked = self._folded.get(folded)
        if tracked is not None:
            return tracked
        rel = folded.replace(os.sep, "/")
        head = posixpath.dirname(rel)
        while head:  # keep a tracked directory's own case, so an untracked file under it still maps (`owner`)
            known = self._folded.get(os.path.normcase(head.replace("/", os.sep)))
            if known is not None:
                return known + rel[len(head):]
            head = posixpath.dirname(head)
        return rel


# ── the static inputs: what the trace cannot see ───────────────────────────────────────────────────────────────────


def declared_reads(text: str, header_lines: int) -> list[str]:
    header = "\n".join(text.splitlines()[:header_lines])
    return [p for m in READS_RE.finditer(header) for p in m.group(1).split()]


PS_BLOCK_COMMENT_RE = re.compile(r"<#.*?#>", re.DOTALL)


def _code(line: str) -> str:
    """A PowerShell or bash line without its comment: from the first `#` that starts a word outside quotes."""
    quote = None
    for i, c in enumerate(line):
        if quote:
            if c == quote:
                quote = None
        elif c in "'\"":
            quote = c
        elif c == "#" and (i == 0 or line[i - 1].isspace()):
            return line[:i]
    return line


def _script_tokens(text: str) -> set[str]:
    """The tokens of a PowerShell or bash script's CODE: block comments (`<# … #>`) and line comments dropped."""
    text = PS_BLOCK_COMMENT_RE.sub("", text)
    return {t for line in text.splitlines() for t in TOKEN_RE.findall(_code(line))}


class Deriver:
    """The static half of a self-test's inputs (module docstring), each file's mentions resolved once."""

    def __init__(self, tree: Tree, header_lines: int):
        self.tree, self.header_lines = tree, header_lines
        self._mentions: dict[str, tuple[set[str], set[str]]] = {}

    def resolve(self, token: str, here: str, declared: bool = False) -> tuple[set[str], set[str]]:
        """The tracked files and directories one token names (module docstring, STATIC). A MENTIONED directory counts
        only as a bare word beside the mentioning file (`testdata`); a DECLARED one (`declared`) anywhere."""
        t = token.replace("\\", "/").strip("/")
        while t.startswith("./"):
            t = t[2:]
        if not t or t in (".", ".."):
            return set(), set()
        bare = "/" not in t and "." not in t
        bases, d = [""], here
        while d:
            bases.append(d)
            d = posixpath.dirname(d)
        files, dirs = set(), set()
        for b in bases:
            p = posixpath.normpath(posixpath.join(b, t))
            if p.startswith(".."):
                continue
            owner = self.tree.owner(p)
            if owner is None:
                continue
            if owner in self.tree.blobs:
                files.add(owner)
            elif declared or (bare and b == here):
                dirs.add(owner)
        return files, dirs

    def mentions(self, rel: str) -> tuple[set[str], set[str]]:
        if rel not in self._mentions:
            try:
                text = (self.tree.repo / rel).read_text(encoding="utf-8")
            except (OSError, UnicodeDecodeError):
                text = ""
            files, dirs = set(), set()
            here = posixpath.dirname(rel)
            for tok in _script_tokens(text):
                f, d = self.resolve(tok, here)
                files |= f
                dirs |= d
            self._mentions[rel] = (files, dirs)
        return self._mentions[rel]

    def closure(self, test: str) -> set[str]:
        """Every tracked file `static` reaches, a static directory's files included: what a script self-test may RUN
        that no Python tracer sees (kb/Work PB4358 uses it to find the clock reads of the scripts it runs)."""
        files, dirs = self.static(test)
        return files.union(*(self.tree.under(d) for d in dirs))

    def static(self, test: str) -> tuple[set[str], set[str]]:
        """(files, directories) the trace cannot see for self-test `test` — its own file, the runner, its declared
        reads, and for an untraced script the closure over its mentions (module docstring)."""
        files = {test, *(r for r in RUNNER if r in self.tree.blobs)}
        dirs: set[str] = set()
        text = (self.tree.repo / test).read_text(encoding="utf-8")
        for declared in declared_reads(text, self.header_lines):
            f, d = self.resolve(declared, "", declared=True)
            if not f and not d:
                raise ValueError(f"{test}: `SELF-TEST-READS: {declared}` names no tracked file or directory")
            files |= f
            dirs |= d
        if test.endswith(UNTRACED_SCRIPTS):
            queue, seen = [test], {test}
            while queue:
                f, d = self.mentions(queue.pop())
                files |= f
                dirs |= d
                for p in f - seen:
                    seen.add(p)
                    if p.endswith(UNTRACED_SCRIPTS):
                        queue.append(p)
        return files, dirs


# ── the trace ──────────────────────────────────────────────────────────────────────────────────────────────────────


def install_tracer(directory: Path) -> Path:
    """Write the tracer as `sitecustomize.py` into `directory`, which `traced_env` puts first on PYTHONPATH, behind the
    clock shim (scripts/self_test_clock.py `SHIM`: it moves the clock only for a run that sets `SHIFT_ENV`)."""
    directory.mkdir(parents=True, exist_ok=True)
    (directory / "sitecustomize.py").write_text(clock.SHIM + TRACER, encoding="utf-8", newline="\n")
    return directory


def traced_env(env: dict[str, str], tracer_dir: Path, trace_out: Path, repo: Path) -> dict[str, str]:
    env = dict(env)
    prior = env.get("PYTHONPATH")
    env["PYTHONPATH"] = str(tracer_dir) + (os.pathsep + prior if prior else "")
    env[TRACE_ENV], env[TRACE_ROOT_ENV] = str(trace_out), str(repo)
    return env


#: ⛔ WHAT A GIT COMMAND READS (`git_reads`) — each subcommand a traced self-test may run inside the repository, by what
#: it reads of the tracked tree. A subcommand in none of these is NOT UNDERSTOOD, and its read is UNKEYED: the runner
#: fails the self-test (kb/Work PB5227, the owner 2026-11-10), never keys it as the whole tree.
#: ENVIRONMENT: the repository's location and configuration, the worktree list, creating a repository, a commit id.
GIT_ENVIRONMENT = frozenset({"rev-parse", "config", "init", "worktree", "version", "var", "symbolic-ref"})
#: NAMES: the tracked paths under its pathspec — `ls-files`, unless one of GIT_LS_FILES_CONTENT asks for content ids.
GIT_NAMES = frozenset({"ls-files"})
GIT_LS_FILES_CONTENT = frozenset({"-s", "--stage", "-m", "--modified", "-d", "--deleted", "--debug"})
#: CONTENT: the tracked paths under its pathspec and their content (history included: it changes with them).
GIT_CONTENT = frozenset({"diff", "log", "show", "status", "merge-base", "rev-list", "cat-file", "ls-tree", "grep",
                         "diff-tree", "diff-index", "blame"})
#: `log` whose output is a commit id only (environment, as `rev-parse HEAD` is).
GIT_LOG_ID_FORMATS = frozenset({"--format=%h", "--format=%H", "--pretty=%h", "--pretty=%H"})
#: git's own global options that change nothing a command reads; any other global option (`--git-dir`, `--work-tree`,
#: `--literal-pathspecs`, a `-C` after another option, which the tracer does not follow) is not understood.
GIT_GLOBAL_INERT = frozenset({"--no-pager", "-p", "--paginate", "-P", "--no-optional-locks", "--no-replace-objects",
                              "--no-advice"})
GIT_GLOBAL_VALUED = frozenset({"-c"})
#: Pathspec magic modelled EXACTLY (`_pathspec`): `top` (`:/`), from the root; `glob` and the default, a wildcard
#: narrowed to its literal prefix's directory; `literal`, no wildcard; `exclude` (`:!`, `:^`), which narrows nothing,
#: so the key is the positive pathspecs' (a superset of the read by meaning). Any other (`icase`, `attr:…`) is unkeyed.
PATHSPEC_MAGIC = frozenset({"top", "glob", "literal", "exclude"})
PATHSPEC_SHORT = {"/": "top", "!": "exclude", "^": "exclude"}
WILDCARDS = "*?["
DRIVE_RE = re.compile(r"[A-Za-z]:")


@dataclass(frozen=True)
class UnkeyedRead:
    """A read the trace cannot place inside the repository (`git_reads`, `read_trace`): a DEFECT of the self-test (it
    reads outside its scratch) or a gap here (a form `git_reads` does not model). Never keyed: the runner fails the
    self-test with UNKEYED_READ and records no PASS."""
    what: str     # the read as recorded: `git <argv>`, or a trace record
    where: str    # its repository directory ("." the root)
    why: str

    def __str__(self) -> str:
        return f"`{self.what}` in {self.where}: {self.why}"


#: The label of the runner's red for an unkeyed read (scripts/self_tests.py), greppable.
UNKEYED_READ = "UNKEYED READ"


def repo_path(path: str, where: str, root: Path) -> str | None:
    """The repository-relative POSIX path a git path argument names, given from repository directory `where` of the
    repository at `root`, or None when it names no place inside the repository. An absolute path (`/…`, `X:/…`,
    `X:\\…`) inside the root is made relative to it; None for one outside the root, on another drive or UNC, a
    drive-relative one (`X:x`), one that climbs past the top (`../..`), and on POSIX one with a backslash (git's glob
    escape). The caller makes None an UNKEYED read: a key no tracked file can appear under never changes."""
    if "\\" in path:
        if os.sep != "\\":
            return None
        path = path.replace("\\", "/")   # git for Windows takes either separator
    base = where
    drive = os.name == "nt" and DRIVE_RE.match(path)
    if drive or posixpath.isabs(path):
        if drive and path[2:3] != "/":
            return None
        try:
            path = os.path.relpath(os.path.normpath(path), os.path.normpath(root)).replace(os.sep, "/")
        except ValueError:
            return None
        base = ""
    p = posixpath.normpath(posixpath.join(base, path))
    if p == ".." or p.startswith("../") or posixpath.isabs(p) or DRIVE_RE.match(p):
        return None
    return "" if p == "." else p


def _pathspec(spec: str) -> tuple[frozenset[str], str] | None:
    """A pathspec's magic words and pattern (`:(top,glob)x`, `:/x`, `:!x`, `x`), or None for a form not parsed."""
    if not spec.startswith(":"):
        return frozenset(), spec
    if spec.startswith(":("):
        end = spec.find(")")
        if end < 0:
            return None
        return frozenset(m.strip() for m in spec[2:end].split(",")), spec[end + 1:]
    i, magic = 1, set()
    while i < len(spec) and spec[i] in PATHSPEC_SHORT:
        magic.add(PATHSPEC_SHORT[spec[i]])
        i += 1
    if i < len(spec) and spec[i] == ":":
        i += 1
    return frozenset(magic), spec[i:]


def _spec_read(spec: str, where: str, root: Path) -> tuple[str | None, str]:
    """(the directory a positive pathspec reads under, or None for an exclusion; "" when unkeyed with the reason)."""
    parsed = _pathspec(spec)
    if parsed is None:
        return None, f"pathspec {spec!r} is not a form git_reads parses"
    magic, pattern = parsed
    if magic - PATHSPEC_MAGIC or {"glob", "literal"} <= magic:
        return None, f"pathspec magic {', '.join(sorted(magic - PATHSPEC_MAGIC) or ['glob+literal'])} of {spec!r} is not modelled"
    if not pattern and not magic:
        return None, "an empty pathspec is not one git accepts"
    if "exclude" in magic:
        return None, ""
    if "literal" not in magic and (wild := min((pattern.find(c) for c in WILDCARDS if c in pattern), default=-1)) >= 0:
        pattern = posixpath.dirname(pattern[:wild])
    p = repo_path(pattern or ".", "" if "top" in magic else where, root)
    return (p, "") if p is not None else (None, f"pathspec {spec!r} names no place inside the repository")


def git_reads(args: list[str], where: str, root: Path) -> list[tuple[str, str]] | UnkeyedRead:
    """What a git command run in directory `where` (its arguments after the leading `-C <dir>`s) of the repository at
    `root` reads of the tracked tree: (kind, path) pairs, kind `paths` (the names under path) or `content` (the names and
    their content), path "" the whole tree — or an UnkeyedRead for a command it cannot place, never a guess.

    THREE OUTCOMES. RESOLVED: the pathspecs after `--` (or, before it, an argument naming an existing path, or one with
    a wildcard: git's own rule, an argument that is no revision must be a path in the working tree) each key the
    directory they read under (`_spec_read`: an absolute path made relative to the root; a wildcard narrowed to the
    directory of its literal prefix, `scripts/spec/work*.py` reading under scripts/spec; kb/Work PB5227), and
    `<rev>:<path>` its path (from the root; `./` and `../` from `where`, as git resolves them). THE WHOLE TREE BY MEANING:
    a command with no pathspec, a wildcard at the root (`*.py`), only exclusions — that IS its read set, not a fallback.
    UNKEYED: a subcommand, global option or pathspec magic not modelled here, and a path outside the repository or past
    its top. An environment command reads nothing."""
    unkeyed = lambda why: UnkeyedRead("git " + " ".join(args), where or ".", why)  # noqa: E731
    i = 0
    while i < len(args) and args[i].startswith("-"):
        if args[i] in GIT_GLOBAL_VALUED:
            i += 2
        elif args[i] in GIT_GLOBAL_INERT:
            i += 1
        elif i == len(args) - 1 and args[i] in ("--version", "--exec-path", "--help"):
            return []
        else:
            return unkeyed(f"global option {args[i]} is not modelled")
    if i >= len(args):
        return []
    sub, rest = args[i], args[i + 1:]
    dashdash = "--" in rest
    opts, specs = (rest[:rest.index("--")], rest[rest.index("--") + 1:]) if dashdash else (rest, [])
    if sub in GIT_ENVIRONMENT and not any(":" in a and not a.startswith("-") for a in opts):
        return []
    if sub == "log" and any(a in GIT_LOG_ID_FORMATS for a in opts):
        return []
    if sub in GIT_NAMES:
        kind = "content" if GIT_LS_FILES_CONTENT.intersection(opts) else "paths"
    elif sub in GIT_CONTENT or sub in GIT_ENVIRONMENT:   # `rev-parse <rev>:<path>` reads that path's content id
        kind = "content"
    else:
        return unkeyed(f"subcommand {sub!r} is not one git_reads models (GIT_ENVIRONMENT, GIT_NAMES, GIT_CONTENT)")
    reads: set[str] = set()
    specs = list(specs or [])
    for a in (a for a in opts if not a.startswith("-")):
        # ls-files takes no revision: every positional is a pathspec. Elsewhere, before `--`, git's own rule: `<rev>:<path>`
        # names a path; an argument that is no revision must name a path of the working tree (or carry a wildcard or
        # magic); anything else is a revision, which reads history and is keyed by what the command reads under.
        if sub in GIT_NAMES or (not dashdash and (a.startswith(":") or any(c in a for c in WILDCARDS))):
            specs.append(a)
        elif ":" in a and not DRIVE_RE.match(a):
            shown = a.split(":", 1)[1]
            p = repo_path(shown, where if shown.startswith(("./", "../")) else "", root)
            if p is None:
                return unkeyed(f"{a!r} names no place inside the repository")
            reads.add(p)
        elif not dashdash and os.path.exists(os.path.join(root, where, a)):
            specs.append(a)
    for spec in specs:
        p, why = _spec_read(spec, where, root)
        if why:
            return unkeyed(why)
        if p is not None:   # None: an exclusion, which narrows nothing
            reads.add(p)
    if not reads:   # no pathspec, or only exclusions: the whole tree, by meaning
        reads.add("")
    return [(kind, p) for p in sorted(reads)]


@dataclass(frozen=True)
class Trace:
    files: frozenset[str]          # repository-relative paths opened or run (tracked or not)
    dirs: frozenset[str]           # tracked directories listed ("" is the repository root)
    git_in_repo: frozenset[str]    # `<dir>: git <command>` for each git child run inside the repository
    git_read: frozenset[tuple[str, str]] = frozenset()   # what those commands read (`git_reads`)
    imports: frozenset[tuple[str, str]] = frozenset()    # (directory, module name) an import looked for there
    unkeyed: frozenset[UnkeyedRead] = frozenset()        # what it read that no key can hold: the runner fails it

    def saw(self, path: str) -> bool:
        return path in self.files


def _in_git_dir(rel: str) -> bool:
    return rel == ".git" or rel.startswith(".git/") or "/.git/" in rel


def read_trace(trace_out: Path, tree: Tree) -> Trace:
    """The reads of one run's trace, each placed in the tree (TRACER's record kinds). A record this cannot place — a
    git command `git_reads` does not model, a record of no known kind — is an UnkeyedRead, never dropped."""
    files, dirs, git, git_read, imports, unkeyed = set(), set(), set(), set(), set(), set()
    for f in sorted(trace_out.glob("*.trace")):
        for line in f.read_text(encoding="utf-8").splitlines():
            kind, tab, folded = line.partition("\t")
            if kind == "I" and tab:
                where, _, name = folded.partition("\t")
                where = tree.unfold(where.rstrip(os.sep)) if where not in ("", ".") else ""
                if not _in_git_dir(where):  # kept when empty: a file can appear there
                    imports.add((where, name))
            elif kind == "G" and tab:
                where, _, command = folded.partition("\t")
                where = tree.unfold(where) if where else ""
                args = command.split(chr(31))  # the tracer joins a command's arguments with the unit separator
                git.add(f"{where or '.'}: git {' '.join(args)}")
                read = git_reads(args, where, tree.repo)
                if isinstance(read, UnkeyedRead):
                    unkeyed.add(read)
                else:
                    git_read.update(read)
            elif kind in ("F", "X", "D") and tab:
                rel = tree.unfold(folded.rstrip(os.sep)) if folded not in ("", ".") else ""
                if _in_git_dir(rel):
                    continue
                owner = tree.owner(rel) if rel else ""
                if owner is not None and owner in tree.blobs:
                    files.add(owner)   # a tracked file, or anything inside a submodule (its gitlink)
                elif kind == "D":
                    dirs.add(owner if owner is not None else rel)   # a tracked file appearing there changes its names
                elif "__pycache__" not in rel:
                    files.add(rel)  # looked for and not tracked: a tracked file appearing there must re-run it
            else:
                unkeyed.add(UnkeyedRead(f"trace record {line[:120]!r}", ".", "a record of no kind the tracer writes"))
    return Trace(frozenset(files), frozenset(dirs), frozenset(git), frozenset(git_read), frozenset(imports),
                 frozenset(unkeyed))


# ── the deps of one run ────────────────────────────────────────────────────────────────────────────────────────────


@dataclass(frozen=True)
class Deps:
    """Everything one run read, with what it read: {path: blob id, or UNTRACKED where it found none} and
    {`<kind>:<directory>`: Tree.digest} — `entries:d` for a directory it listed, `paths:d` for the names a git command
    listed, `content:d` for a STATIC directory input or a git command that read content (`content:` is the whole
    tree) — and {directory: {"names": the module names an import looked for there, "found": `Tree.importable`}}, the
    IMPORT KEY: the entries of a searched directory that could answer one of those names, so a new or removed module,
    package or extension of an imported name re-runs the run, and an unrelated new file beside them does not."""
    files: dict[str, str]
    trees: dict[str, str]
    imports: dict[str, dict[str, list[str]]]

    @classmethod
    def of(cls, tree: Tree, files: set[str], seen: set[tuple[str, str]] = frozenset(),
           imported: set[tuple[str, str]] = frozenset()) -> "Deps":
        """`files` read, directories `seen` as (kind, directory) pairs — a `content` read of a single tracked file
        is that file — and modules `imported` as (directory, name) pairs."""
        files = set(files) | {d for kind, d in seen if kind == "content" and d in tree.blobs}
        trees = {f"{kind}:{d}": tree.digest(kind, d) for kind, d in seen if not (kind == "content" and d in tree.blobs)}
        names: dict[str, set[str]] = {}
        for d, name in imported:
            names.setdefault(d, set()).add(name)
        imports = {d: {"names": sorted(n), "found": tree.importable(d, n)} for d, n in sorted(names.items())}
        return cls({p: tree.blob(p) for p in sorted(files)}, dict(sorted(trees.items())), imports)

    def changed(self, tree: Tree) -> list[str]:
        """What differs on `tree` from what the run read (empty: the PASS still holds). An import key names each
        entry that appeared or went: `imports:<directory>/<entry>`."""
        out = [p for p, blob in self.files.items() if tree.blob(p) != blob]
        for key, digest in self.trees.items():
            kind, _, d = key.partition(":")
            if tree.digest(kind, d) != digest:
                out.append(key)
        for d, seen in self.imports.items():
            now = tree.importable(d, seen["names"])
            out += [f"imports:{posixpath.join(d, e)}" for e in sorted(set(now).symmetric_difference(seen["found"]))]
        return out

    def to_json(self) -> dict:
        return {"files": self.files, "trees": self.trees, "imports": self.imports}

    @classmethod
    def from_json(cls, data: dict) -> "Deps":
        """A record without the import key (written before it existed) raises KeyError: its run is keyed on less than
        a run now is, so it is no PASS (self_tests.py `lookup` counts it unusable and re-runs)."""
        return cls(dict(data["files"]), dict(data["trees"]),
                   {d: {"names": list(v["names"]), "found": list(v["found"])} for d, v in data["imports"].items()})


def run_deps(test: str, tree: Tree, deriver: Deriver, trace: Trace) -> Deps:
    """The Deps of one PASSED run: its trace plus its static inputs (a static directory is read whole)."""
    files, dirs = deriver.static(test)
    seen = {("entries", d) for d in trace.dirs} | {("content", d) for d in dirs} | set(trace.git_read)
    return Deps.of(tree, files | set(trace.files), seen, set(trace.imports))


def main(argv: list[str] | None = None) -> int:
    args = sys.argv[1:] if argv is None else argv
    if args[:1] == ["--self-test"]:
        return self_test()
    sys.path.insert(0, str(REPO / "scripts"))
    import self_tests  # noqa: E402 — the runner owns HEADER_LINES
    tree = Tree.load(REPO)
    deriver = Deriver(tree, self_tests.HEADER_LINES)
    for test in args:
        files, dirs = deriver.static(test)
        print(f"{test}: {len(files)} static files, {len(dirs)} directories")
        for p in sorted(files):
            print(f"  {p}")
        for d in sorted(dirs):
            print(f"  {d}/ ({len(tree.under(d))} files)")
    return 0


def self_test() -> int:
    import tempfile
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        print(f"  {'ok  ' if got == want else 'FAIL'} {name}" + ("" if got == want else f": got {got!r}, want {want!r}"))
        if got != want:
            fails.append(name)

    def _raises(call) -> bool:
        try:
            call()
        except (KeyError, TypeError, ValueError):
            return True
        return False

    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        planted = {
            "scripts/o/test_x.ps1": "<#\nkb/Work in a block comment\n#>\n. (Join-Path $Here 'testdata/lib.ps1')\n"
                                    "$s = Join-Path $Here 'stop.ps1'   # docs/ignored.md in a line comment\n"
                                    "Write-Host 'no kb/Work note names it'\n$t = Join-Path $Here testdata\n",
            "scripts/o/testdata/lib.ps1": "& python scripts/o/runner.py\n$d = 'data.json'\n",
            "scripts/o/stop.ps1": "exit 0\n",
            "scripts/o/runner.py": "print(1)\n",
            "scripts/o/testdata/data.json": "{}\n",
            "scripts/o/test_y.py": "#!/usr/bin/env python3\n# SELF-TEST-READS: kb/Work docs/ignored.md\nprint(1)\n",
            "scripts/o/test_z.py": "# SELF-TEST-READS: no/such/path\n",
            "scripts/o/pk/__init__.py": "",
            "docs/ignored.md": "x\n",
            "kb/Work/n.md": "x\n",
        }
        for rel, text in planted.items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(text, encoding="utf-8", newline="\n")
        blobs = {rel: f"blob-{i}" for i, rel in enumerate(planted)}
        blobs["tools/sub"] = "commit-1"
        tree = Tree(root, blobs, frozenset({"tools/sub"}))
        deriver = Deriver(tree, 5)
        files, dirs = deriver.static("scripts/o/test_x.ps1")
        check("arm: a PowerShell self-test's static inputs follow what its CODE mentions — beside it, below it, from "
              "the root, through the scripts it reaches — never a comment, and a mentioned directory only as a bare "
              "word beside it", (sorted(files), sorted(dirs)),
              (["scripts/o/runner.py", "scripts/o/stop.ps1", "scripts/o/test_x.ps1", "scripts/o/testdata/data.json",
                "scripts/o/testdata/lib.ps1"], ["scripts/o/testdata"]))
        files, dirs = deriver.static("scripts/o/test_y.py")
        check("arm: a Python self-test's static inputs are its own file and its declared reads (the trace sees the "
              "rest)", (sorted(files), sorted(dirs)), (["docs/ignored.md", "scripts/o/test_y.py"], ["kb/Work"]))
        try:
            deriver.static("scripts/o/test_z.py")
            check("arm: a declared read that names nothing tracked is an error, never an empty set", "derived", "raised")
        except ValueError as e:
            check("arm: a declared read that names nothing tracked is an error, never an empty set",
                  "no/such/path" in str(e), True)
        check("arm: a compiled module counts as its source, and a path inside a submodule as its gitlink",
              (tree.owner("scripts/o/__pycache__/runner.cpython-314.pyc"), tree.owner("tools/sub/skills/x.py")),
              ("scripts/o/runner.py", "tools/sub"))

        deps = Deps.of(tree, {"scripts/o/runner.py", "scripts/o/new.txt"}, {("entries", "kb/Work"), ("content", "docs")})
        whole = Deps.of(tree, set(), {("content", "")})
        names = Deps.of(tree, set(), {("paths", "kb")})
        check("arm: an unchanged tree changes nothing", (deps.changed(tree), whole.changed(tree), names.changed(tree)),
              ([], [], []))
        planted_tree = lambda **more: Tree(root, {**blobs, **more}, frozenset({"tools/sub"}))  # noqa: E731
        edited = planted_tree(**{"scripts/o/runner.py": "blob-edited"})
        added = planted_tree(**{"kb/Work/m.md": "blob-new"})
        appeared = planted_tree(**{"scripts/o/new.txt": "blob-new"})
        deeper = planted_tree(**{"docs/sub/new.md": "blob-new"})
        check("arm: an edited input, a new file in a listed directory, a tracked file where the run found none and a "
              "new file anywhere under a directory read whole each invalidate the PASS",
              (deps.changed(edited), deps.changed(added), deps.changed(appeared), deps.changed(deeper)),
              (["scripts/o/runner.py"], ["entries:kb/Work"], ["scripts/o/new.txt"], ["content:docs"]))
        check("arm: a git command that read the whole tree's content re-runs on any edit; one that listed names "
              "re-runs on a new name under its pathspec, never on an edit",
              (whole.changed(edited), names.changed(edited), names.changed(added)),
              (["content:"], [], ["paths:kb"]))
        check("arm: what a git command reads: nothing for the repository's location, worktrees or a commit id; the "
              "names or the content under its pathspec (from its directory); a shown file; the whole tree otherwise",
              [git_reads(c.split(), w, root) for c, w in (
                  ("rev-parse --show-toplevel", ""), ("--exec-path", ""), ("worktree list --porcelain", ""),
                  ("log -1 --format=%h", ""), ("log -5", ""), ("ls-files -z", ""), ("ls-files -z -- scripts", ""),
                  ("ls-files -s -- src/ tests/", ""), ("diff --name-only -- x", "docs"), ("diff HEAD", ""),
                  ("show HEAD:a/b.json", ""), ("ls-files -- *.py", ""),
                  ("ls-files -z -- :(glob)scripts/spec/work*.py", ""), ("diff -- docs/*.md", ""),
                  ("ls-files -- *.ps1", "scripts"), ("ls-files -- :(exclude)docs", ""))],
              [[], [], [], [], [("content", "")], [("paths", "")], [("paths", "scripts")],
               [("content", "src"), ("content", "tests")], [("content", "docs/x")], [("content", "")],
               [("content", "a/b.json")], [("paths", "")], [("paths", "scripts/spec")], [("content", "docs")],
               [("paths", "scripts")], [("paths", "")]])
        posix_root, outside = root.as_posix(), (root.parent / "elsewhere").as_posix()

        def outcome(argv: list[str], where: str):
            got = git_reads(argv, where, root)
            return ("UNKEYED", got.what, got.where) if isinstance(got, UnkeyedRead) else got
        check("arm: THREE OUTCOMES, RESOLVED — an absolute path inside the repository is made relative to it, literal "
              "or wildcard, with either separator; `:(top)`/`:/` read from the root, `:(literal)` keeps a wildcard "
              "literal, an exclusion narrows nothing; before `--`, a positional of ls-files and a path that exists are "
              "pathspecs and `<rev>:<path>` reads that path (kb/Work PB5227)",
              [outcome(c, w) for c, w in (
                  (["ls-files", "--", f"{posix_root}/scripts/*.py"], ""),
                  (["diff", "--", str(root / "docs")], "scripts"),
                  (["ls-files", "--", ":(top)scripts/o"], "docs"),
                  (["ls-files", "--", ":/docs"], "scripts"),
                  (["ls-files", "--", ":(literal)scripts/o*"], ""),
                  (["ls-files", "--", "scripts", ":!scripts/o"], ""),
                  (["ls-files", "-z", "scripts/o"], ""),
                  (["diff", "--name-only", "HEAD", "docs"], ""),
                  (["show", "HEAD:./x"], "docs"))],
              [[("paths", "scripts")], [("content", "docs")], [("paths", "scripts/o")], [("paths", "docs")],
               [("paths", "scripts/o*")], [("paths", "scripts")], [("paths", "scripts/o")], [("content", "docs")],
               [("content", "docs/x")]])
        check("arm: THREE OUTCOMES, THE WHOLE TREE BY MEANING — no pathspec, a wildcard at the root, only exclusions, "
              "a shown root: that is the read set, not a fallback",
              [outcome(c, w) for c, w in (
                  (["ls-files", "--", f"{posix_root}/*.md"], "docs"), (["ls-files", "--", "../*.py"], "docs"),
                  (["ls-files", "--", ":!docs"], ""), (["diff", "HEAD"], ""), (["show", "HEAD:"], ""))],
              [[("paths", "")], [("paths", "")], [("paths", "")], [("content", "")], [("content", "")]])
        unkeyed_rows = [
            (["ls-files", "--", f"{outside}/scripts"], ""), (["ls-files", "--", f"{outside}/*.py"], ""),
            (["diff", "--", "../../x"], "docs"), (["ls-files", "--", "/*.py"], ""),
            (["ls-files", "--", "//server/share/x"], ""), (["show", "HEAD:../x"], ""),
            (["ls-files", "--", ":(icase)docs"], ""), (["ls-files", "--", ":(attr:x)docs"], ""),
            (["ls-files", "--", ":(glob,literal)docs"], ""), (["ls-files", "--", ""], ""),
            (["fetch", "origin", "main"], ""), (["--git-dir=x", "status"], ""), (["-c", "a=b", "-C", "x", "status"], ""),
            (["ls-files", "--", "a\\b"] if os.sep == "/" else ["ls-files", "--", "E:x"], "")]
        check("arm: THREE OUTCOMES, UNKEYED — a path outside the repository, past its top or UNC, a shown path past "
              "the top, pathspec magic not modelled (icase, attr, glob with literal), an empty pathspec, a subcommand "
              "or global option not modelled, a drive-relative path (Windows) or a backslash (POSIX) is an UnkeyedRead "
              "carrying its command and directory, never a key (the owner 2026-11-10)",
              [outcome(c, w) for c, w in unkeyed_rows],
              [("UNKEYED", "git " + " ".join(c), w or ".") for c, w in unkeyed_rows])
        check("arm: Tree.under refuses a path that is not repository-relative (an outside path's key would never "
              "change), at the call", [_raises(lambda d=d: tree.under(d)) for d in ("../x", posix_root + "/scripts",
                                                                                     "a\\b", "/x", "x/")],
              [True] * 5)

        out = root / "trace-out"
        out.mkdir()
        tracer = install_tracer(root / "tracer-dir")
        child = ("import os, subprocess, sys\nopen(os.path.join('scripts', 'o', 'run' + 'ner.py')).read()\n"
                 "os.listdir('kb/Work')\nsubprocess.run([sys.executable, '-c', 'pass', 'scripts/o/stop.ps1'])\n")
        subprocess.run([sys.executable, "-c", child], cwd=root, check=True,
                       env=traced_env(dict(os.environ), tracer, out, root))
        seen = read_trace(out, tree)
        check("arm: the tracer records a file opened by a built name, a directory listed and a path handed to a child "
              "process, in every Python process the self-test starts",
              (seen.saw("scripts/o/runner.py"), "kb/Work" in seen.dirs, seen.saw("scripts/o/stop.ps1"),
               len(list(out.glob("*.trace"))) >= 2), (True, True, True, True))
        imports = root / "trace-imports"
        imports.mkdir()
        subprocess.run([sys.executable, "-c", "import sys\nsys.path.insert(0, 'scripts/o')\nimport runner\n"
                        "import os\nos.listdir('scripts/o/testdata')\n"], cwd=root, check=True, capture_output=True,
                       env=traced_env(dict(os.environ), tracer, imports, root))
        seen = read_trace(imports, tree)
        check("arm: an import records the module file it opens, never the sys.path directory the import system "
              "caches a listing of; a listing the code makes itself is still one (kb/Work PB5227)",
              (seen.saw("scripts/o/runner.py"), sorted(seen.dirs)), (True, ["scripts/o/testdata"]))

        # THE IMPORT KEY (kb/Work PB5227): the names an import looked for, and the entries that could answer them
        keyed = root / "trace-import-key"
        keyed.mkdir()
        subprocess.run([sys.executable, "-c", "import sys\nsys.path.insert(0, 'scripts/o')\nimport runner, colorsys\n"
                        "try:\n    import zz_absent\nexcept ImportError:\n    pass\n"
                        "try:\n    from pk import sub_absent\nexcept ImportError:\n    pass\n"],
                       cwd=root, check=True, capture_output=True, env=traced_env(dict(os.environ), tracer, keyed, root))
        seen = read_trace(keyed, tree)
        check("arm: the tracer records each module name an import looked for in a directory inside the repository — "
              "found or not, a top-level name in each sys.path entry, a submodule in its package's directory",
              sorted(i for i in seen.imports if i[0].startswith("scripts/")
                     and i[1] in ("runner", "colorsys", "zz_absent", "pk", "sub_absent")),
              [("scripts/o", "colorsys"), ("scripts/o", "pk"), ("scripts/o", "runner"), ("scripts/o", "zz_absent"),
               ("scripts/o/pk", "sub_absent")])
        keyed_deps = run_deps("scripts/o/test_y.py", tree, deriver, seen)
        check("arm: the import key holds only the entries that answer an imported name",
              keyed_deps.imports["scripts/o"]["found"], ["pk", "runner.py"])
        suffix = next(s for s in IMPORT_SUFFIXES if s != ".py")
        cases = {
            "(a) an unrelated new file in a searched directory": planted_tree(**{"scripts/o/unrelated.py": "b"}),
            "(b) a new module shadowing an imported standard-library one": planted_tree(**{"scripts/o/colorsys.py": "b"}),
            "(c) a new module answering a failed import": planted_tree(**{"scripts/o/zz_absent" + suffix: "b"}),
            "(c) a new submodule answering a failed import from a package": planted_tree(
                **{"scripts/o/pk/sub_absent.py": "b"}),
            "(d) a new package directory with an imported module's name": planted_tree(
                **{"scripts/o/colorsys/__init__.py": "b"}),
            "(e) the imported module's file removed": Tree(root, {k: v for k, v in blobs.items()
                                                                if k != "scripts/o/runner.py"}, frozenset({"tools/sub"})),
        }
        check("arm: an unrelated new file leaves the Deps unchanged; a new shadowing module, a new module or package "
              "answering an imported name and a removed module each change them (kb/Work PB5227)",
              {name: keyed_deps.changed(t) for name, t in cases.items()},
              {"(a) an unrelated new file in a searched directory": [],
               "(b) a new module shadowing an imported standard-library one": ["imports:scripts/o/colorsys.py"],
               "(c) a new module answering a failed import": [f"imports:scripts/o/zz_absent{suffix}"],
               "(c) a new submodule answering a failed import from a package": ["imports:scripts/o/pk/sub_absent.py"],
               "(d) a new package directory with an imported module's name": ["imports:scripts/o/colorsys"],
               "(e) the imported module's file removed": ["scripts/o/runner.py", "imports:scripts/o/runner.py"]})
        check("arm: the import key survives its record (to_json → from_json), and a record without it is no PASS",
              (Deps.from_json(json.loads(json.dumps(keyed_deps.to_json()))) == keyed_deps,
               _raises(lambda: Deps.from_json({"files": {}, "trees": {}}))), (True, True))
    print(f"self_test_inputs.py --self-test: {'ALL GREEN' if not fails else f'{len(fails)} FAILED'}")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
