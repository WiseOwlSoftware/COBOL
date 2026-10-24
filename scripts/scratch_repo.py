#!/usr/bin/env python3
"""scratch_repo.py — THE ONE FIXTURE THROUGH WHICH EVERY SCRIPT SELF-TEST BUILDS AND DRIVES A SCRATCH GIT REPOSITORY
(kb/Work PB3584).

    from scratch_repo import ScratchRepo            # a self-test under scripts/ (sys.path holds scripts/ for it)
    repo = ScratchRepo.init(tmp / "repo")           # `git init -b main`, the fixture's settings in its own config
    repo.commit("base", {"src/A.cs": "class A {}\\n"})
    origin = repo.origin(tmp / "origin.git"); repo.git("push", "-q", "origin", "main")
    wt = repo.worktree(tmp / "wt", branch="topic")
    sha = repo.git("rev-parse", "HEAD")             # a checked command: its stdout, stripped

    python scripts/scratch_repo.py --audit          # every self-test under live_tools.ROOTS: exit 1 on a finding
    python scripts/scratch_repo.py --self-test      # the fixture's guarantees and the audit's arms, then --audit

⛔ WHY ONE FIXTURE. Until PB3584 fifteen self-tests `git init`-ed their own scratch repositories, and seven defined a
git helper of their own (devlog.py `sh`, prune_worktrees.py `g`, rendered_views.py `git`, run_gate_legs.py two,
enqueue.py `sh`, landing_check.py `sh`, test_plan_wave.py `git`), each with its own identity, its own checking (some
raised with git's message, some with none, some never checked) and its own idea of the environment. A change to how a
self-test drives git reached one helper and left the others behind. Measured: run outside scripts/self_tests.py
(whose private global config turns automatic maintenance off), the prune_worktrees.py self-test started 74 background
`git maintenance` processes in the repository it was still building.

WHAT IT GUARANTEES, for every repository it creates and every command it runs:
  ISOLATION   git's repository-selection variables (`git rev-parse --local-env-vars`: GIT_DIR, GIT_WORK_TREE,
              GIT_INDEX_FILE, …) are dropped from every command's environment, so a self-test run from inside a hook or
              a linked worktree never acts on the caller's repository (kb/Work PB1719); `isolate()` drops them from the
              process's own environment for a self-test whose CODE UNDER TEST runs git itself.
  SETTINGS    `init` writes REPO_CONFIG into the new repository's own config file (no extra git process): one identity,
              no automatic maintenance or `gc --auto` (scripts/self_tests.py GIT_NO_AUTO_MAINTENANCE's reason, kb/Work
              PB1957) and no commit signing. Every git process in that repository reads it — the fixture's, the code
              under test's, a linked worktree's — whoever runs the self-test: self_tests.py, CI, or a hand run.
  CHECKING    `git` raises ScratchRepoError with the command, the directory and git's own message when git fails (an
              unchecked fixture command once left a branch where the fixture never meant it, and a lifecycle FAIL on
              that branch was the only trace: merge-group run 39193754708); `run` is the unchecked form, for a command
              whose failure the self-test asserts.
  DECODING    text as UTF-8 (errors replaced), `binary=True` for bytes in and out (a text-mode pipe on Windows writes
              CRLF into a `-z` or `--index-info` input).

THE AUDIT (`--audit`; `SelfTestFixtureDriftTests` runs it): in SELF-TEST CODE — every function whose name contains
`self_test` or `selftest` and everything nested in it, and the whole of a standalone `test_*.py` — of every tracked
Python file under live_tools.ROOTS but this one,
  SPAWN   a git argv (a list or tuple literal whose first element is the string "git") is a finding: the self-test drives
          git around the fixture;
  INIT    a list or tuple literal whose first element is the string "init", or a call whose first or second argument
          is the string "init", is a finding: a self-test creating a repository itself;
  HELPER  a function or lambda defined there whose body calls a GIT RUNNER of the same module (a top-level function
          whose git argv passes its arguments through, `["git", "-C", repo, *args]`) is a finding: a local git helper
          over the code under test's own (prune_worktrees.py's `g` over `git`); so is a standalone self-test's own
          top-level git runner. A function there that calls a subject with a fixed git command
          (devlog.py's `landing` over `commit_files`) is a case, no finding.
Calling the code under test's own git functions from a self-test's body is no finding: that is the subject under test.
"""
from __future__ import annotations

import argparse
import ast
import os
import shutil
import subprocess
import sys
import tempfile
from collections.abc import Mapping, Sequence
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))
import live_tools  # noqa: E402  (this directory's: the one list of the live tools' trees, which the audit reads)

#: The git identity every self-test commits under: written into each scratch repository's config by `init`, and into
#: scripts/self_tests.py's private global config, from this one place.
IDENTITY = ("self-test", "self-test@localhost")

#: NO AUTOMATIC MAINTENANCE (scripts/self_tests.py `private_git_config` carries the measurement): git starts
#: `git maintenance run --auto --detach` after a commit, a merge, a fetch and a push, and in git 2.55 that background
#: process packs refs and repacks the very scratch repository a self-test is still building and surveying.
NO_AUTO_MAINTENANCE = (("maintenance", "auto", "false"), ("gc", "auto", "0"))

#: What `init` writes into a new scratch repository's own config: the identity, no automatic maintenance, no signing
#: (a developer's global `commit.gpgsign = true` would otherwise prompt or fail every fixture commit).
REPO_CONFIG = (("user", "name", IDENTITY[0]), ("user", "email", IDENTITY[1]), *NO_AUTO_MAINTENANCE,
               ("commit", "gpgsign", "false"), ("tag", "gpgsign", "false"))

_LOCAL_ENV_VARS: list[str] | None = None


class ScratchRepoError(RuntimeError):
    """A fixture git command failed: the message names the command, the directory and git's own output."""


def local_env_vars() -> list[str]:
    """git's repository-selection variables (`git rev-parse --local-env-vars`), read once per process."""
    global _LOCAL_ENV_VARS
    if _LOCAL_ENV_VARS is None:
        env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
        _LOCAL_ENV_VARS = subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                                         text=True, encoding="utf-8", env=env,
                                         cwd=tempfile.gettempdir()).stdout.split()
    return _LOCAL_ENV_VARS


def scrubbed_env(base: Mapping[str, str] | None = None) -> dict[str, str]:
    """`base` (default: this process's environment) without git's repository-selection variables (kb/Work PB1719)."""
    drop = set(local_env_vars())
    return {k: v for k, v in (os.environ if base is None else base).items() if k not in drop}


def isolate() -> None:
    """Drop git's repository-selection variables from THIS process's environment, for a self-test whose code under
    test runs git itself (in-process), so neither the fixture nor the subject can act on the caller's repository."""
    for name in local_env_vars():
        os.environ.pop(name, None)


class ScratchRepo:
    """A scratch git repository (or a linked worktree of one) at `path`. `options` are git's own `-c name=value`
    arguments, given to every command THIS object runs and to no other process (line_endings.py's `core.autocrlf=false`:
    the code under test must still read the host's own setting)."""

    def __init__(self, path: Path | str, options: Sequence[str] = ()):
        self.path = Path(path)
        self.options = tuple(options)

    def __repr__(self) -> str:
        return f"ScratchRepo({str(self.path)!r})"

    # ── creating ─────────────────────────────────────────────────────────────────────────────────────────────────
    @classmethod
    def init(cls, path: Path | str, *, bare: bool = False, branch: str = "main",
             options: Sequence[str] = ()) -> "ScratchRepo":
        """`git init -q [--bare] -b <branch> <path>` (the directory is created when absent; files already in it stay
        untracked), then REPO_CONFIG appended to the new repository's own config file."""
        repo = cls(path, options)
        repo.path.mkdir(parents=True, exist_ok=True)
        repo.git("init", "-q", *(["--bare"] if bare else []), "-b", branch, str(repo.path), cwd=repo.path)
        config = repo.path / ("config" if bare else ".git/config")
        lines: list[str] = []
        for section, key, value in REPO_CONFIG:
            lines += [f"[{section}]", f"\t{key} = {value}"]
        with config.open("a", encoding="utf-8", newline="\n") as f:
            f.write("\n".join([*lines, ""]))
        return repo

    def origin(self, path: Path | str, name: str = "origin") -> "ScratchRepo":
        """A bare repository at `path` (with the fixture's settings: a push runs maintenance in the RECEIVING
        repository), added as this repository's remote `name`. Pushing is the caller's: what and when it pushes is the
        fixture's shape."""
        bare = ScratchRepo.init(path, bare=True)
        self.git("remote", "add", name, str(bare.path))
        return bare

    def worktree(self, path: Path | str, branch: str | None = None, start: str | None = None,
                 detach: bool = False) -> "ScratchRepo":
        """`git worktree add -q [-b <branch> | --detach] <path> [<start>]`: a linked worktree, with this object's
        options. With neither `branch` nor `detach` git names the new branch after the directory."""
        how = ["-b", branch] if branch else ["--detach"] if detach else []
        self.git("worktree", "add", "-q", *how, str(path), *([start] if start else []))
        return ScratchRepo(path, self.options)

    # ── running git ──────────────────────────────────────────────────────────────────────────────────────────────
    def run(self, *args: str, cwd: Path | str | None = None, input: str | bytes | None = None,
            env: Mapping[str, str] | None = None, binary: bool = False) -> subprocess.CompletedProcess:
        """THE ONE PLACE A SELF-TEST'S GIT PROCESS STARTS, unchecked: `git -C <cwd or path> <options> <args>` under the
        scrubbed environment plus `env`, output captured."""
        where = Path(cwd) if cwd is not None else self.path
        environment = scrubbed_env()
        if env:
            environment.update(env)
        argv = ["git", "-C", str(where), *self.options, *map(str, args)]
        if binary:
            return subprocess.run(argv, input=input, capture_output=True, env=environment, stdin=None
                                  if input is not None else subprocess.DEVNULL)
        return subprocess.run(argv, input=input, capture_output=True, text=True, encoding="utf-8", errors="replace",
                              env=environment, stdin=None if input is not None else subprocess.DEVNULL)

    def git(self, *args: str, cwd: Path | str | None = None, input: str | None = None,
            env: Mapping[str, str] | None = None) -> str:
        """A checked fixture command: its stdout, stripped; a failure raises ScratchRepoError with git's message."""
        r = self.run(*args, cwd=cwd, input=input, env=env)
        if r.returncode != 0:
            raise ScratchRepoError(f"git {' '.join(map(str, args))} (in {cwd if cwd is not None else self.path}) "
                                   f"exited {r.returncode}: {(r.stderr or r.stdout).strip()}")
        return r.stdout.strip()

    # ── building content ─────────────────────────────────────────────────────────────────────────────────────────
    def write(self, files: Mapping[str, str | bytes | None], cwd: Path | str | None = None) -> None:
        """Write each `{relative path: content}` under the working tree (or `cwd`): a str as UTF-8 with its newlines
        as written, bytes as they are, None deletes the file. Parent directories are created."""
        root = Path(cwd) if cwd is not None else self.path
        for rel, content in files.items():
            p = root / rel
            if content is None:
                p.unlink()
                continue
            p.parent.mkdir(parents=True, exist_ok=True)
            if isinstance(content, bytes):
                p.write_bytes(content)
            else:
                p.write_text(content, encoding="utf-8", newline="\n")

    def commit(self, message: str, files: Mapping[str, str | bytes | None] | None = None, *,
               cwd: Path | str | None = None, allow_empty: bool = False, when: int | None = None) -> None:
        """Write `files`, stage everything (`add -A`) and commit it with `message`. `when` (seconds since the epoch)
        dates the commit as its author and its committer, for a self-test that orders commits by time."""
        if files:
            self.write(files, cwd)
        self.git("add", "-A", cwd=cwd)
        dated = {"GIT_AUTHOR_DATE": f"@{when} +0000", "GIT_COMMITTER_DATE": f"@{when} +0000"} if when else None
        self.git("commit", "-q", *(["--allow-empty"] if allow_empty else []), "-m", message, cwd=cwd, env=dated)


# ── the audit: self-test code drives git only through this fixture ──────────────────────────────────────────────────

THIS = "scripts/scratch_repo.py"


def _is_str(node: ast.AST, value: str) -> bool:
    return isinstance(node, ast.Constant) and node.value == value


def _is_git_argv(node: ast.AST) -> bool:
    return isinstance(node, (ast.List, ast.Tuple)) and bool(node.elts) and _is_str(node.elts[0], "git")


def _is_spawn(node: ast.AST) -> bool:
    """A call that starts a process: `subprocess.<anything>(…)`."""
    return (isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute)
            and isinstance(node.func.value, ast.Name) and node.func.value.id == "subprocess")


def _is_self_test_name(name: str) -> bool:
    low = name.lower()
    return "self_test" in low or "selftest" in low


def _runs_any_git(node: ast.AST) -> bool:
    """A GIT RUNNER: a function whose git argv passes its caller's arguments through (`["git", "-C", repo, *args]`),
    so it runs whatever git command it is handed; one with a fixed command (`devlog.commit_files`) is a subject."""
    return any(_is_git_argv(n) and any(isinstance(e, ast.Starred) for e in n.elts) for n in ast.walk(node))


def _self_test_scopes(tree: ast.Module, standalone: bool) -> tuple[list[ast.AST], list[ast.AST]]:
    """(the SELF-TEST scopes, the scopes ONLY self-test code reaches). The first: the module itself for a standalone
    `test_*.py`, else every function whose name says it is a self-test (not one nested in another, whose walk already
    covers it). The second: every top-level function that only self-test code refers to, to a fixed point
    (file_census_notes.py's `stage`, which builds the scratch tree its self-test reads); such a function may be the
    subject itself (sharedfile.py's `second_copies`, which only the self-test runs), so only the INIT arm reads it."""
    if standalone:
        return [tree], []
    named: list[ast.AST] = []
    stack: list[ast.AST] = [tree]
    while stack:
        for child in ast.iter_child_nodes(stack.pop()):
            if isinstance(child, (ast.FunctionDef, ast.AsyncFunctionDef)) and _is_self_test_name(child.name):
                named.append(child)
            else:
                stack.append(child)
    tops = {n.name: n for n in tree.body if isinstance(n, (ast.FunctionDef, ast.AsyncFunctionDef))}
    refs: dict[str, list[ast.Name]] = {name: [] for name in tops}
    for node in ast.walk(tree):
        if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Load) and node.id in refs:
            refs[node.id].append(node)
    reached: list[ast.AST] = []
    while True:
        inside = {id(n) for s in (*named, *reached) for n in ast.walk(s)}
        joined = [f for name, f in tops.items() if id(f) not in inside and refs[name]
                  and all(id(r) in inside for r in refs[name])]
        if not joined:
            return named, reached
        reached += joined


def _inits(node: ast.AST) -> bool:
    """A repository created: a git argv holding `init`, an argv list beginning with `init`
    (`for argv in (["init", "-q"], …)`), or a call handing `init` as its first or second argument (`sh(root, "init")`)."""
    if _is_git_argv(node):
        return any(_is_str(e, "init") for e in node.elts)
    if isinstance(node, (ast.List, ast.Tuple)):
        return bool(node.elts) and _is_str(node.elts[0], "init")
    return isinstance(node, ast.Call) and any(_is_str(a, "init") for a in node.args[:2])


def audit_source(rel: str, source: str) -> list[str]:
    """The audit's findings for ONE file (module docstring): `<rel>:<line>: <ARM>: <what>`."""
    tree = ast.parse(source, filename=rel)
    standalone = Path(rel).name.startswith("test_")
    runners = {n.name for n in tree.body if isinstance(n, (ast.FunctionDef, ast.AsyncFunctionDef))
               and _runs_any_git(n)}
    # a name the module binds to a git argv (`git = ["git", "-c", …]`), so `subprocess.run(git + argv)` spawns git
    git_names = {t.id for n in ast.walk(tree) if isinstance(n, ast.Assign) and any(_is_git_argv(v)
                 for v in ast.walk(n.value)) for t in n.targets if isinstance(t, ast.Name)}
    named, reached = _self_test_scopes(tree, standalone)
    found: set[tuple[int, str]] = set()
    for scope in reached:
        for node in ast.walk(scope):
            if _inits(node):
                found.add((node.lineno, "INIT: a self-test creates a repository itself; use ScratchRepo.init"))
    for scope in named:
        for node in ast.walk(scope):
            if _inits(node):
                found.add((node.lineno, "INIT: a self-test creates a repository itself; use ScratchRepo.init"))
            if _is_spawn(node):
                argv = node.args[0] if node.args else next((k.value for k in node.keywords if k.arg == "args"), None)
                if argv is not None and any(_is_git_argv(n) or (isinstance(n, ast.Name) and n.id in git_names)
                                            for n in ast.walk(argv)):
                    found.add((node.lineno, "SPAWN: self-test code starts git itself; drive git through ScratchRepo"))
            if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda)) and node is not scope:
                own = node.name if not isinstance(node, ast.Lambda) else "a lambda"
                body = node.body if isinstance(node.body, list) else [node.body]
                for call in (c for b in body for c in ast.walk(b) if isinstance(c, ast.Call)):
                    if isinstance(call.func, ast.Name) and call.func.id in runners and call.func.id != own:
                        found.add((node.lineno, f"HELPER: {own} wraps the module's own git runner "
                                                f"{call.func.id}(); drive git through ScratchRepo"))
                        break
            if standalone and isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)) and node in tree.body \
                    and node.name in runners:
                found.add((node.lineno, f"HELPER: {node.name} runs git in a standalone self-test; drive git "
                                        f"through ScratchRepo"))
    return [f"{rel}:{line}: {what}" for line, what in sorted(found)]


def audit(repo: Path = REPO) -> list[str]:
    """Every finding of every tracked Python file under live_tools.ROOTS but this one."""
    out = subprocess.run(["git", "ls-files", "-z", "--", *live_tools.ROOTS], cwd=repo, check=True,
                         capture_output=True, env=scrubbed_env()).stdout
    findings: list[str] = []
    for rel in sorted(p for p in out.decode("utf-8").split("\0") if p.endswith(".py") and p != THIS):
        path = repo / rel
        if not path.is_file():      # deleted in the working tree, not yet in the index: nothing to read
            continue
        findings += audit_source(rel, path.read_text(encoding="utf-8"))
    return findings


# ── the self-test: the fixture's guarantees, and the audit's arms ──────────────────────────────────────────────────

def self_test() -> int:
    fails: list[str] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        print(f"  {'ok  ' if ok else 'FAIL'} {name}" + ("" if ok else f": {detail}"))
        if not ok:
            fails.append(name)

    tmp = Path(tempfile.mkdtemp(prefix="scratch-repo-selftest-"))
    saved = {k: os.environ.get(k) for k in ("GIT_DIR", "GIT_WORK_TREE")}
    try:
        # A caller's GIT_DIR (a hook, a linked worktree's environment) must never reach a fixture command.
        decoy = ScratchRepo.init(tmp / "decoy")
        os.environ["GIT_DIR"] = str(decoy.path / ".git")
        repo = ScratchRepo.init(tmp / "repo")
        repo.commit("base", {"a.txt": "one\n", "dir/b.bin": b"\x00\r\n"})
        check("isolation: a caller's GIT_DIR never reaches a fixture command",
              decoy.run("rev-parse", "--verify", "-q", "HEAD").returncode != 0
              and repo.git("log", "--format=%s") == "base", repo.git("log", "--format=%s"))
        isolate()
        check("isolate() drops git's repository-selection variables from this process", "GIT_DIR" not in os.environ)

        cfg = {k: repo.git("config", "--get", k) for k in
               ("user.name", "user.email", "maintenance.auto", "gc.auto", "commit.gpgsign")}
        check("settings: the identity, no automatic maintenance and no signing are the repository's own config",
              cfg == {"user.name": IDENTITY[0], "user.email": IDENTITY[1], "maintenance.auto": "false",
                      "gc.auto": "0", "commit.gpgsign": "false"}, str(cfg))
        check("settings: a commit is authored by the fixture's identity",
              repo.git("log", "-1", "--format=%an <%ae>") == f"{IDENTITY[0]} <{IDENTITY[1]}>")
        check("write: bytes are written as they are, str as UTF-8 with its own newlines",
              (repo.path / "dir/b.bin").read_bytes() == b"\x00\r\n" and (repo.path / "a.txt").read_bytes() == b"one\n")

        try:
            repo.git("rev-parse", "--verify", "-q", "refs/heads/no-such-branch")
            check("checking: a failing fixture command raises", False, "no exception")
        except ScratchRepoError as e:
            check("checking: a failing fixture command raises, naming the command and the directory",
                  "rev-parse" in str(e) and str(repo.path) in str(e), str(e))
        check("run: the unchecked form returns git's exit code",
              repo.run("rev-parse", "--verify", "-q", "refs/heads/no-such-branch").returncode == 1)

        origin = repo.origin(tmp / "origin.git")
        repo.git("push", "-q", "origin", "main")
        check("origin: a bare repository with the fixture's settings, added as the remote, receives a push",
              origin.git("rev-parse", "main") == repo.git("rev-parse", "main")
              and origin.git("config", "--get", "maintenance.auto") == "false"
              and repo.git("remote", "get-url", "origin") == str(origin.path))

        wt = repo.worktree(tmp / "wt", branch="topic")
        wt.commit("on topic", {"c.txt": "c\n"}, when=1_000_000)
        check("worktree: a linked worktree on its own branch, its commit dated by `when`",
              repo.git("log", "-1", "--format=%s %at %ct", "topic") == "on topic 1000000 1000000"
              and wt.git("rev-parse", "--abbrev-ref", "HEAD") == "topic")
        det = repo.worktree(tmp / "pin", detach=True, start="main")
        check("worktree: a detached pin", det.git("rev-parse", "--abbrev-ref", "HEAD") == "HEAD")
        z = repo.run("ls-files", "-z", binary=True)
        check("binary: bytes out", z.returncode == 0 and z.stdout.split(b"\0")[:2] == [b"a.txt", b"dir/b.bin"],
              repr(z.stdout))
        opt = ScratchRepo(repo.path, ("-c", "core.autocrlf=false"))
        check("options: given to every command of the object that holds them",
              opt.git("config", "core.autocrlf") == "false")

        # The audit's arms, on planted self-test code.
        def arms(rel: str, src: str) -> list[str]:
            return [f.split(": ")[1] for f in audit_source(rel, src)]
        check("audit SPAWN: a git argv in a self-test function",
              arms("scripts/x.py", "def self_test():\n    subprocess.run(['git', 'status'])\n") == ["SPAWN"])
        check("audit INIT: an `init` argv list and an `init` helper call in a self-test",
              set(arms("scripts/x.py", "def self_test():\n    for a in (['init', '-q'],): sh(*a)\n"
                                       "    sh(root, 'init')\n")) == {"INIT"})
        check("audit HELPER: a self-test's nested helper over the module's own git function",
              arms("scripts/x.py", "import subprocess\ndef git(*a):\n    return subprocess.run(['git', *a])\n"
                                   "def self_test():\n    def g(*a):\n        return git(*a)\n") == ["HELPER"])
        check("audit HELPER: a standalone test_*.py's own top-level git function",
              set(arms("scripts/test_x.py", "def git(*a):\n    subprocess.run(['git', *a])\n")) == {"SPAWN", "HELPER"})
        check("audit INIT: a top-level function only self-test code reaches is read by the INIT arm; one the "
              "module's own code also calls, and the subject's own git, are not",
              arms("scripts/x.py", "def stage(r):\n    subprocess.run(['git', '-C', r, 'init', '-q'])\n"
                                   "def subject(r):\n    subprocess.run(['git', '-C', r, 'ls-files'])\n"
                                   "def tool(r):\n    subprocess.run(['git', 'init', r])\n"
                                   "def main():\n    tool('.')\n"
                                   "def self_test():\n    stage(p)\n    subject(p)\n    tool(p)\n") == ["INIT"])
        check("audit: the code under test's git, and a self-test CALLING it, are no finding",
              arms("scripts/x.py", "def git(*a):\n    subprocess.run(['git', *a])\n"
                                   "def self_test():\n    head = git('rev-parse', 'HEAD')\n") == [])
        check("audit: a self-test using the fixture is no finding",
              arms("scripts/x.py", "def self_test():\n    r = ScratchRepo.init(p)\n    r.git('status')\n") == [])
    finally:
        for k, v in saved.items():
            if v is None:
                os.environ.pop(k, None)
            else:
                os.environ[k] = v
        shutil.rmtree(tmp, ignore_errors=True)

    findings = audit(REPO)
    for f in findings:
        print(f"  FINDING {f}")
    check("the tree: no self-test drives git around the fixture", not findings, f"{len(findings)} finding(s)")
    print(f"scratch_repo self-test: {'FAIL ' + str(len(fails)) if fails else 'all checks OK'}")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    ap.add_argument("--audit", action="store_true", help="every self-test under live_tools.ROOTS; exit 1 on a finding")
    ap.add_argument("--self-test", action="store_true", help="the fixture's guarantees and the audit's arms")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.audit:
        findings = audit(REPO)
        for f in findings:
            print(f)
        print(f"=== SCRATCH-REPO AUDIT: {'RED — ' + str(len(findings)) + ' finding(s)' if findings else 'GREEN'} ===")
        return 1 if findings else 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
