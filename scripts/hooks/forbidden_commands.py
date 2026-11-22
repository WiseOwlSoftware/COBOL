#!/usr/bin/env python3
"""PreToolUse hook — BLOCK the shell commands this repo's rules forbid, with the reason fed back to the agent.

Owner decision 2026-09-25 (tooling recommendation 2): prose rules are forgotten, a hook is not, and a PreToolUse hook
fires inside subagents and Workflow agents too. Each rule below was written in a skill or memory AND broken anyway:

  git stash / --autostash   the stash stack is SHARED by every linked worktree (common git dir); PB713's implementer
                            popped the registrar's stash. WIP goes into a commit (workstream skill, references/landing.md).
  bare push to main         main takes only queued pull requests (the ruleset binds administrators); the ONE route is
                            `python scripts/orchestrator/enqueue.py land <branch>` (kb/Work PB3023: main merged in, the
                            gate, the DEVLOG stamp, then one pull request into GitHub's merge queue; owner decision Q23,
                            2026-09-06; kb/Work PB3024). A push to a work branch is fine.
  a unit's push to another  inside an orchestrator unit (COBOL_COORD_DIR set) a `git push` whose remote is not THIS
  repository                repository, any flags: a unit never publishes another repo (owner 2026-10-05, kb/Work/PB2044:
                            the wave unit pushed the public skills repo's main with `git push -q origin HEAD:main`). Its
                            own work branch it pushes like any session (kb/Work PB5234: every one of 31 refusals was that).
  a backslash run in a      the Bash TOOL halves every run of two or more backslashes that does not end at a double
  Bash heredoc body         quote, after this hook and before bash runs (measured 2026-11-10, kb/Work PB5234; a single
                            backslash and the PowerShell tool are byte-exact), so a quoted heredoc body that holds one
                            arrives altered; write the file with the Write tool instead.
  bare `~X` test filter     `--filter "~X|~Y"` matches NOTHING and exits 0 — a silent green; every term needs a
                            property (FullyQualifiedName~X).
  `cd` in a backgrounded    a location change (`cd`, `pushd`, `Set-Location`, ...) inside an and-or list that a single
  list, then more           `&` sends to the background, with another command after that `&` (MANDATORY-PRACTICES
                            P14(b), kb/Work PB3095): `cd X && a & b &` runs `a` in a background subshell under X and
                            `b` in the CALLER's directory; a landing audit written that way reported a false OK into
                            the queue on 2026-10-11. Use absolute paths or `git -C <dir>`.

  a recursive delete of a   `rm -r|-rf|--recursive`, `rmdir|rd /s`, `Remove-Item -Recurse` whose target the shell expands
  variable path             (`"$rd"`, `$R/$id`, `$(…)`, `$env:X`, `%X%`): an empty variable makes it the filesystem root,
                            and the harness warned the owner for each one (owner 2026-11-15, kb/Work PB5764,
                            MANDATORY-PRACTICES P17). A probe runs through `scripts/orchestrator/run_probe.py` in a new
                            directory, so nothing needs deleting; a file that must go is removed by its literal path.

  a kill by process name    `Stop-Process -Name`, `taskkill /IM`, `pkill`, `killall`, and any command line that both
                            lists processes by name (Get-Process, ps, pgrep, pidof, tasklist, wmic, Win32_Process) and
                            kills (Stop-Process, `.Kill()`, kill, taskkill, `xargs kill`, Terminate), in any order, pipe
                            or payload (the TEXT is judged, `python -X utf8 -c "…"` and `env python -c` included):
                            a name matches every process of that image on the host, and one agent's cleanup killed
                            another agent's gate compiles (kb/Work PB6034, MANDATORY-PRACTICES P18). Stop only what you
                            started, by its pid.

  WSL lifecycle commands   `wsl --terminate/--shutdown/--update/--export/--import/--unregister/--install` (owner
                            2026-10-04): they can end the owner's open session or burn tens of GB, so only the owner runs
                            them. Commands inside a distro (`wsl -d Ubuntu -e ...`) stay allowed. This is the guard that
                            matters most under an unattended `bypassPermissions` run, where prompts no longer protect.

Exit 2 blocks the call and returns stderr to the agent. Anything unparseable passes (a hook must never wedge a session).
"""
import json
import os
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import shell_location  # noqa: E402  (the one parser of a command's location changes, kb/Work PB2599)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()


def block(reason: str) -> None:
    sys.stderr.write("BLOCKED by scripts/hooks/forbidden_commands.py: " + reason + "\n")
    sys.exit(2)


try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    sys.exit(0)

cmd = (data.get("tool_input") or {}).get("command", "") or ""

# EVERY rule reads the command line through the ONE model of it, the shared parser `shell_location.steps` (kb/Work
# PB2599, PB3198): its simple commands in order, each with its words (quoted text is one word), the heredoc bodies it
# introduces cut out beside it at their own delimiter lines, every place a shell takes a command line from walked as the
# command line it is (a `$( … )` / backquote / PowerShell expression or script block, a `bash -c` / `pwsh -Command` /
# `cmd /c` payload, `eval` / `iex`, a heredoc, here-string or pipe a shell reads as its script; the branch review's R1-1
# and R1-2), and every `git` invocation read by `shell_location.git_calls`. No rule scans the raw text: a private view of
# it reads a heredoc body as commands, a command written after a heredoc as body, or a `<<` inside quotes as the start of
# one (kb/Work PB3198: five such views let a stash, a push to main and a bare test filter through after a heredoc).
# The cwd is "." so a relative location stays relative: a relative `cd` never leaves this
# repository unless it names a registered submodule (rule 2). A parse failure leaves the rules nothing to judge.
try:
    STEPS = shell_location.steps(cmd, ".", shell_location.shell_of(data.get("tool_name")))
except Exception:  # noqa: BLE001 — a hook must never wedge a session
    STEPS = []
GIT_CALLS = [call for st in STEPS for call in shell_location.git_calls(st)]

# 1. git stash (listing/showing is harmless) and --autostash (as a flag, or switched on through `-c <x>.autostash=…`)
for call in GIT_CALLS:
    if call.subcommand == "stash" and (call.args[:1] or ("",))[0] not in ("list", "show"):
        block("`git stash` is forbidden in this repo — the stash stack is shared by every worktree and another "
              "agent's pop takes your entry. Commit WIP on your own branch (`git commit -m \"WIP checkpoint: ...\"`); "
              "to compare against a clean tree use `git worktree add --detach <path> <sha>`.")
    if (any(a.startswith("--autostash") for a in call.args)
            or any(re.fullmatch(r"[\w.-]*\.autostash(?:=(?!(?:false|no|off|0)$).*)?", o, re.I) for o in call.options)):
        block("`--autostash` uses the shared stash stack (see `git stash`). Commit WIP first, then rebase.")

# 2. a push that targets main directly — THIS repo only (another repo reached by `cd`/`-C` has its own rules).
# "This repo" is recognized by its FOLDER NAME, derived from where this hook lives — never a hard-coded literal: the
# literal of the repo's earlier folder name silently failed OPEN when the repo became E:\COBOL (2026-09-26), so no
# earlier name is listed (kb/Work PB3837 retired them); a RELATIVE `cd` never leaves the repo.
_ROOT_DIR = pathlib.Path(__file__).resolve().parents[2]
_ROOT = _ROOT_DIR.name.lower()
_THIS_REPO = {_ROOT, "cobol"}
# A path registered in .gitmodules is ANOTHER repository nested under this one (tools/claude-skills is the public
# skills repo, whose main has no ci-gate), so a push of ITS main is not a push to this repo's main. The folder-name
# rule below would otherwise catch it, because the submodule's path contains the repo's name (2026-10-06).
_GITMODULES = _ROOT_DIR / ".gitmodules"
_SUBMODULES = [tuple(p.lower().split("/")) for p in re.findall(r"^\s*path\s*=\s*(\S+)",
               _GITMODULES.read_text(encoding="utf-8"), re.M)] if _GITMODULES.exists() else []


def _is_this_repo(target: str | None) -> bool:
    """Whether a git call's directory is THIS repository. Undetermined (None: an unknown variable, `cd -`) is this
    repository, so the rule fails closed; a relative path is too, unless it ends in a registered submodule."""
    if target is None:
        return True
    t = target.lower().replace("\\", "/")
    segs = [s for s in t.split("/") if s and s != "."]
    if any(len(segs) >= len(sub) and tuple(segs[-len(sub):]) == sub for sub in _SUBMODULES):
        return False  # a registered submodule, absolute or relative: another repository
    if not (t.startswith(("/", "~")) or re.match(r"[a-z]:", t)):
        return True
    return any(s in _THIS_REPO or any(s.startswith(n + "-") for n in _THIS_REPO) for s in segs)


def _names_main(refspec: str) -> bool:
    """A push argument whose destination is main: `main`, `HEAD:main`, `+x:refs/heads/main`."""
    return refspec.lstrip("+").rsplit(":", 1)[-1] in ("main", "refs/heads/main")


# Each push is judged in the directory IT runs in (`GitCall.cwd`: every location change either shell accepts, a `-C`,
# a `-WorkingDirectory`, read by the one shared parser, kb/Work PB2599): a `cd` to another repository elsewhere in the
# line no longer exempts a push to this one.
for call in GIT_CALLS:
    if call.subcommand == "push" and _is_this_repo(call.cwd) and any(_names_main(a) for a in call.args):
        block("a direct push to main is refused by the server (required `ci-gate` check). Land with "
              "`python scripts/orchestrator/enqueue.py land <branch>` (kb/Work PB3023): it merges origin/main in, "
              "runs the gate, stamps the DEVLOG entry and queues one pull request into GitHub's merge queue (it "
              "refuses while `ruleset.py check` does not report the queue ACTIVE). It is idempotent; run it with "
              "run_in_background and block on its `=== ENQUEUE: ` line.")
# 2b. inside an orchestrator unit, a `git push` to ANOTHER repository, whatever its flags (owner 2026-10-05, kb/Work/PB2044).
# Rule 2 guards THIS repo's main and deliberately leaves another repository to its own rules, so an attended session may
# push the sites and the public skills repo. A unit has no such need and runs under `bypassPermissions`: on 2026-10-05 the
# wave unit published the public skills repo's main with `git push -q origin HEAD:main` from a scratch clone. The
# supervisor exports COBOL_COORD_DIR into every unit's environment and hooks (also in its subagents) inherit it; attended
# and cloud sessions never have it set. Until kb/Work PB5234 this refused EVERY push in a unit; measured over three days
# (2026-11-08..10), all 31 refusals were a unit pushing its own work branch of this repository, which CLAUDE.md rule 7 asks
# of every checkpoint. So the repository is now judged by what the push publishes to, the remote's URL, never by a folder
# name: a scratch clone of another repository under `E:/COBOL-coord` (whose name starts with this repo's) is another
# repository. The destination is resolved by `_push_urls` (its docstring lists what it reads): the first positional
# word or `--repo`, else the branch's `pushRemote`, `remote.pushDefault`, the branch's remote, `origin`; a remote's PUSH
# URLs are asked of git in the directory the push runs in (`GitCall.cwd`), and EVERY one must be this repository. A push
# redirected by `--git-dir`, `GIT_DIR`-style variables in the line or a `url.*.insteadOf` is refused (`_redirected`;
# kb/Work PB5269). A push whose repository cannot be
# determined (an unknown directory, a git that does not answer) is refused: inside a unit the rule fails closed.
_HOOK_CWD = pathlib.Path(data.get("cwd") or os.getcwd())


def _git_out(cwd: pathlib.Path, *args: str, config: tuple[str, ...] = ()) -> list[str] | None:
    """The lines git prints for `git -C cwd [-c k=v ...] args`; None when git fails or prints nothing."""
    import subprocess  # only a unit's push reaches git: importing it for every tool call cost ~11 ms
    try:
        r = subprocess.run(["git", "-C", str(cwd), *[w for kv in config for w in ("-c", kv)], *args],
                           capture_output=True, text=True, timeout=10)
    except (OSError, subprocess.SubprocessError):
        return None
    lines = [ln.strip() for ln in r.stdout.splitlines()] if r.returncode == 0 else []
    return [ln for ln in lines if ln] or None


def _repo_key(url: str) -> str:
    """One spelling of a remote: `https://github.com/O/R.git`, `git@github.com:O/R` and `ssh://git@github.com/O/R` are
    one key; a local path is its resolved path."""
    u = url.strip().replace("\\", "/").rstrip("/")
    u = re.sub(r"\.git$", "", u, flags=re.I)
    m = re.match(r"^(?:[a-z+]+://)?(?:[^@/]+@)?([^/:]+)[:/](.+)$", u, re.I)
    if m and not re.match(r"^[a-z]:/", u, re.I):
        return (m.group(1) + "/" + m.group(2)).lower()
    return str(pathlib.Path(u).resolve()).lower()


# `git push`'s options that take their value as the NEXT word (`--repo=x` spells it in the same word); every other option
# is a flag or spells its value with `=` (git-push(1))
_PUSH_VALUE_OPTIONS = {"--repo", "--receive-pack", "--exec", "-o", "--push-option"}


def _push_repository(args: tuple[str, ...]) -> str | None:
    """The repository a `git push` names: its first positional word, which git prefers (measured: `git push
    --repo=origin ../b.git` pushes to b.git), else `--repo=X` / `--repo X`; None when it names none (the default push
    remote is then used)."""
    repo = positional = None
    i = 0
    while i < len(args):
        a = args[i]
        if a.startswith("--repo="):
            repo = a[len("--repo="):]
        elif a in _PUSH_VALUE_OPTIONS:
            if a == "--repo" and i + 1 < len(args):
                repo = args[i + 1]
            i += 1
        elif not a.startswith("-") and ">" not in a and "<" not in a and positional is None:
            positional = a
        i += 1
    return positional if positional is not None else repo


# git's own inputs that move or rewrite a push's destination out of reach of the remote's configuration: a repository
# chosen by `--git-dir` or the environment (GIT_DIR, GIT_COMMON_DIR, GIT_CONFIG*, which can carry `url.*.insteadOf`), and
# a URL rewrite given with `-c`. A unit has no use for any of them, so a push that carries one is refused outright.
_GIT_ENV_WORD = re.compile(r"^(?:\$?env:)?GIT_(?:DIR|COMMON_DIR|CONFIG\w*)(?:=|$)", re.I)
_URL_REWRITE = re.compile(r"^url\..*\.(?:push)?insteadof=", re.I)


def _redirected(call) -> bool:
    """Whether the push's git was pointed elsewhere by `--git-dir`, a `-c url.*.insteadOf` / `pushInsteadOf`, or an
    environment variable set anywhere in the command line (`GIT_DIR=x git push`, `export GIT_DIR=x; git push`,
    `$env:GIT_DIR = 'x'; git push`). The hook's own environment is the push's too, and the git processes this hook starts
    inherit it, so what they read is what the push would read."""
    return (any(o.startswith("--git-dir") or _URL_REWRITE.match(o) for o in call.options)
            or any(_GIT_ENV_WORD.match(w) for st in STEPS for w in st.words))


def _push_urls(call, cwd: pathlib.Path) -> list[str] | None:
    """The URLs a `git push` publishes to, as far as this hook resolves them: the first positional word, else `--repo`,
    else the current branch's `pushRemote`, `remote.pushDefault`, the branch's own remote, `origin`. A remote name is read
    to its push URLs by `git remote get-url --push --all` (`remote.<name>.pushurl` over `url`, `url.*.insteadOf` and
    `pushInsteadOf` applied); a URL or path is its own, and None (refused) when the repository configures a URL rewrite,
    which git would apply to it. The call's own `-c k=v` options reach git. Not resolved, and refused instead:
    `--git-dir` and the environment (`_redirected`). None when it cannot be determined."""
    config = tuple(o for prev, o in zip(("",) + call.options, call.options) if prev == "-c")
    repo = _push_repository(call.args)
    if repo is None:
        branch = (_git_out(cwd, "symbolic-ref", "--short", "-q", "HEAD", config=config) or [""])[0]
        keys = ([f"branch.{branch}.pushremote"] if branch else []) + ["remote.pushdefault"] \
            + ([f"branch.{branch}.remote"] if branch else [])
        repo = next((v[0] for k in keys if (v := _git_out(cwd, "config", "--get", k, config=config))), "origin")
    if re.search(r"[:/\\]", repo):
        if _git_out(cwd, "config", "--get-regexp", r"^url\..*\.(push)?insteadof$", config=config):
            return None
        return [repo]
    return _git_out(cwd, "remote", "get-url", "--push", "--all", repo, config=config)


def _pushes_elsewhere(call) -> bool:
    """Whether a push publishes to a repository other than this one (or one this hook cannot determine)."""
    if call.cwd is None or _redirected(call):
        return True
    urls = _push_urls(call, _HOOK_CWD / call.cwd)
    # this repository's own remote is asked only here, inside a unit and for a push: asked at import it cost every
    # tool call a git process (0.070 s became 0.725 s median, kb/Work PB5234)
    this_remote = _git_out(_ROOT_DIR, "remote", "get-url", "origin")
    if urls is None or this_remote is None:
        return True
    return any(_repo_key(u) != _repo_key(this_remote[0]) for u in urls)


if os.environ.get("COBOL_COORD_DIR"):
    for call in GIT_CALLS:
        if call.subcommand == "push" and _pushes_elsewhere(call):
            block("a `git push` to another repository (or one whose remote this hook cannot read) is refused inside an "
                  "orchestrator unit. A unit pushes only its own work branch of THIS repository and lands it with "
                  "`python scripts/orchestrator/enqueue.py land <branch>`; a change to ANOTHER repository (the public "
                  "skills repo, a site) is never published by a unit: hand it off as an owed push and the attended "
                  "session or the owner publishes it (kb/Work/PB2044).")
# 3. a run of two or more backslashes, not ending at a double quote, inside a Bash heredoc body. Measured 2026-11-10
# (kb/Work PB5234, its guard-measurements addendum): the Bash tool hands bash `eval '<command>'` with every such run
# halved (`\\n` arrives as `\n`, four backslashes as two), AFTER this hook has read the command; a single
# backslash (`\n`, `\d`, `C:\Users`), a run that ends at `"`, the Write tool and the PowerShell tool all arrive
# byte-exact, and bash runs a heredoc exactly as POSIX specifies. Until 2026-11-10 this rule refused ANY backslash
# escape in a body: 576 refusals in three days, 400 of them bodies that would have arrived intact. The bodies are the
# ones the model cut out at their delimiter lines, never the text after the first `<<`.
_HALVED_RUN = re.compile(r'(?<!\\)\\{2,}(?![\\"])')
if data.get("tool_name") != "PowerShell" and any(_HALVED_RUN.search(body) for st in STEPS for body in st.heredocs):
    block("this heredoc body holds a run of two or more backslashes, and the Bash tool halves such a run before bash "
          "runs it (kb/Work PB5234: `\\\\n` arrives as `\\n`; a single backslash and a run that ends at a "
          "double quote arrive intact). Write the file with the Write tool (into your own scratch directory, "
          "MANDATORY-PRACTICES P16) and run it, or use the PowerShell tool, which is byte-exact.")

# 4. a property-less dotnet test filter term, per `dotnet test` command: its own `--filter` value
for st in STEPS:
    w = st.words
    at = next((k for k in range(len(w) - 1) if shell_location.program(w[k]) == "dotnet" and w[k + 1] == "test"), None)
    if at is None:
        continue
    args = w[at + 2:]
    value = next((a[9:] for a in args if a[:9] in ("--filter=", "--filter:")), None)
    if value is None and "--filter" in args[:-1]:
        value = args[args.index("--filter") + 1]
    if value is None:
        continue
    bare = [t for t in re.split(r"[|&]", value) if t.strip().startswith(("~", "!~", "="))]
    if bare:
        block(f"filter term(s) {bare} have no property, so they match NOTHING and the run exits 0 — a silent "
              "green. Write `FullyQualifiedName~X` for every term. (A GATE never filters: it is "
              "`scripts/build-local.ps1`, the ordered gate.)")

# 5. WSL lifecycle commands (owner 2026-10-04): `wsl --terminate/--shutdown/--update/--export/--import/--unregister/--install`
# can end the owner's open session, race a procedure they run by hand, or burn tens of GB of disk, so NO agent or unattended
# unit runs them; the owner does. Reads and commands INSIDE a distro (`wsl -d Ubuntu -e ...`) stay allowed. Only a segment
# whose command IS wsl is checked, so a commit message or a grep pattern that mentions the flags passes.
WSL_LIFECYCLE = re.compile(r"^(?:\w+=\S*\s+)*(?:&\s+)?(?:\S*[\\/])?wsl(?:\.exe)?\s+(?:[^|;&]*\s)?"
                           r"(?:--terminate|-t|--shutdown|--update|--export|--import|--unregister|--install)\b")
for _st in STEPS:   # every simple command, a `-c` payload's and a pipeline's later sides too
    if WSL_LIFECYCLE.match(_st.text):
        block("WSL lifecycle commands (`--terminate`, `--shutdown`, `--update`, `--export`, `--import`, `--unregister`, "
              "`--install`) are the OWNER's to run (owner 2026-10-04: they can end an open session or burn disk). Give the "
              "owner the exact command and wait; commands inside a distro (`wsl -d Ubuntu -e ...`) are fine.")

# 6. a location change inside a BACKGROUNDED and-or list that another command follows (MANDATORY-PRACTICES P14(b),
# kb/Work PB3095). A single `&` ends an and-or list and runs THE WHOLE LIST in a background subshell (bash) or job
# (PowerShell 7), so a `cd` joined to it by `&&`/`||` moves only that subshell: `cd X && a & b &` runs `a` under X and
# `b` in the caller's directory. On 2026-10-11 a landing audit written that way reported a false OK into the queue. A
# `cd` in its OWN list (`cd X; a & b`) moves the calling shell and stays allowed, as do `cd X && a` and `cd X && a &`
# with nothing after the `&`. The list structure is the shared parser's (`shell_location.steps`, kb/Work PB2599): it
# knows quotes, escapes, comments, heredocs, redirections and PowerShell's call operator, and it marks the first step
# after such a list with the list's location changes (`Step.stranded`).
for _after in STEPS:
    if _after.stranded:
        _moves = " ".join(t or "(home)" for t in _after.stranded)
        block(f"a location change to `{_moves[:60]}` sits in an and-or list that a single `&` sends to the background, "
              f"and `{_after.text[:50]}` follows the `&`: the backgrounded list ends there, so the location change "
              "binds only that background subshell and every command after the `&` runs in the CALLER's directory "
              "(2026-10-11: a landing audit written this way reported a false OK). Use absolute paths or `git -C <dir>` "
              "instead of `cd` (MANDATORY-PRACTICES P14(b), kb/Work PB3095); independent commands go as parallel tool "
              "calls in one turn.")

# 7. a RECURSIVE DELETE whose target the shell EXPANDS at run time (owner 2026-11-15, kb/Work PB5764, MANDATORY-PRACTICES
# P17): `rd=$R/$id; rm -rf "$rd"` is `rm -rf /` when a variable in it is empty, and the harness raised a warning to the
# owner for every one the probing agents wrote. A target is judged by the shared model's `Step.expands` (`_scan`): a `$`
# inside single quotes is literal, and a variable in ANOTHER command of the line is no target of this one. Each shell's
# recursive delete: Bash `rm` with a recursive flag in any spelling (`-r`, `-R`, `-rf`, `-fr`, `--recursive`, before a
# `--`), cmd.exe `rmdir|rd /s`, PowerShell `Remove-Item` and its aliases with `-Recurse` (any prefix of it). NOT refused:
# a literal target, and a delete that is not recursive (`rm "$f"`, one file). The command word is the step's first, past
# assignments and the leaders that run the rest of the words as the command (`sudo`, a `do` of a loop, `&`); a delete
# whose target arrives through a pipe (`xargs rm -rf`, `find … -exec rm -rf {}`, `… | Remove-Item -Recurse`) names no
# target in the line, and a recursive flag that is itself a variable (`rm $opts "$d"`) cannot be told: neither is judged.
_LEADERS = {"sudo", "doas", "command", "builtin", "exec", "nohup", "time", "&", "!",
            "if", "then", "else", "elif", "while", "until", "do"}
_BASH_RECURSIVE = re.compile(r"^(?:-[A-Za-z]*[rR][A-Za-z]*|--recursive)$")
_PS_DELETE = {"remove-item", "rm", "ri", "del", "rd", "rmdir", "erase"}
_PS_RECURSE = re.compile(r"^-r(?:e(?:c(?:u(?:r(?:s(?:e)?)?)?)?)?)?(?::\S*)?$", re.I)
_PS_PATH = re.compile(r"^-(?:path|literalpath|lp|pspath)(?::(.*))?$", re.I)
_PS_VALUED = {"-filter", "-include", "-exclude", "-credential", "-stream"}   # their value is no target


def _command_word(words: tuple[str, ...]) -> int | None:
    """The index of the step's command word: past `NAME=value` assignments and the leaders (`_LEADERS`)."""
    for k, w in enumerate(words):
        if w.lower() not in _LEADERS and not re.match(r"^[A-Za-z_][A-Za-z0-9_]*=", w):
            return k
    return None


def _recursive_delete_targets(st, shell: str) -> list[int]:
    """The word indexes of the targets of the step's recursive delete; [] when the step is none."""
    w = st.words
    k = _command_word(w)
    if k is None:
        return []
    name, rest = shell_location.program(w[k]), range(k + 1, len(w))
    if shell == shell_location.BASH and name == "rm":
        recursive, targets, operands = False, [], False
        for i in rest:
            if not operands and w[i] == "--":
                operands = True
            elif not operands and w[i].startswith("-") and w[i] != "-":
                recursive = recursive or bool(_BASH_RECURSIVE.match(w[i]))
            else:
                targets.append(i)
        return targets if recursive else []
    if shell == shell_location.CMD and name in ("rmdir", "rd"):
        return [i for i in rest if not w[i].startswith("/")] if any(w[i].lower().startswith("/s") for i in rest) else []
    if shell == shell_location.POWERSHELL and name in _PS_DELETE:
        recursive, targets, i = False, [], k + 1
        while i < len(w):
            a = w[i]
            if (m := _PS_PATH.match(a)) is not None:
                if m.group(1) is None:
                    targets.append(i + 1)
                    i += 1
                else:
                    targets.append(i)
            elif _PS_RECURSE.match(a):
                recursive = recursive or not a.lower().endswith(":$false")
            elif a.lower() in _PS_VALUED:
                i += 1
            elif not a.startswith("-"):
                targets.append(i)
            i += 1
        return [t for t in targets if t < len(w)] if recursive else []
    return []


for _st in STEPS:   # every simple command, in the shell that runs it (`Step.shell`: a `cmd /c` payload is cmd.exe's)
    _hit = [_st.words[i] for i in _recursive_delete_targets(_st, _st.shell) if _st.expands[i]]
    if _hit:
        block(f"a recursive delete of `{_hit[0][:60]}`, a path the shell expands at run time (`{_st.text[:60]}`): "
              "when a variable in it is empty the target is the filesystem root, and the owner is warned for every "
              "one (owner 2026-11-15, kb/Work PB5764, MANDATORY-PRACTICES P17). Run a probe program through "
              "`python scripts/orchestrator/run_probe.py --root <your scratch directory, written literally> ...`, "
              "which gives each run a NEW directory, so nothing needs deleting; a file that must go is removed by "
              "its one literal path.")

# 8. a KILL BY PROCESS NAME (owner-controller 2026-11-21, kb/Work PB6034, MANDATORY-PRACTICES P18): a name matches EVERY
# process of that image on the host, other agents' included. `Get-Process dotnet-stack,cobol | Stop-Process -Force`, run
# by one implementer to clean up its own sampling, killed the cobol.exe compiles of another agent's gate (exit 4294967295,
# empty streams), and that gate read them as failed cases. A by-name kill has two INGREDIENTS, and the rule looks for
# them in the TEXT of the whole command line, never in a parse of how a program was started (the branch reviews of
# train-1985-c R1-1 and R2-1 and of train-1985-d R1-1 each found a form that parse missed: a `Where-Object` stage between
# the halves, a `python -c` payload, then `python -X utf8 -c`, `env python -c` and `timeout 60 python -c`; a list of
# invocations is never complete, so the rule reads no invocation at all):
#   ENUMERATION (`_ENUMERATES`) - a listing by name or of every process: Get-Process / gps / PowerShell's ps without -Id,
#     Bash ps without -p, pgrep, pidof, tasklist not filtered to one PID, wmic process or a Win32_Process query not
#     filtered to one ProcessId alone, .NET's GetProcesses / GetProcessesByName, psutil's process_iter / pids;
#   KILL (`_KILLS`) - Stop-Process / spps / kill, taskkill, tskill, pkill, killall, pskill, a `.Kill()`, `.kill()`,
#     `.terminate()` or `.send_signal()` call, os.kill, TerminateProcess, `ForEach-Object Kill`, Invoke-CimMethod /
#     Invoke-WmiMethod Terminate, Remove-CimInstance / Remove-WmiObject, wmic's delete or call terminate;
#   and a step that is both at once (`_BY_NAME`: Stop-Process -Name, PowerShell's kill -Name, taskkill /IM or an image
#     /FI, pkill, killall).
# The text judged (`_code_text`) is every word of every step, quoted payloads whole (whatever program they are handed to:
# python, node, perl, bash -c, pwsh -c, a here-string, a runner such as env, timeout, nice, xargs or sudo, with any
# options before them), plus the heredoc bodies a step introduces; an ENUMERATION and a KILL anywhere in it, in any order,
# pipe, substitution, loop, script block or payload, is refused. Three things are text and never judged: the arguments of a
# program that only SEARCHES or PRINTS text (`_TEXT_ONLY`: grep, rg, Select-String, findstr, git, echo, Write-Output, ...),
# so `git grep -n 'Get-Process'` and `git log --grep 'pgrep cobol | xargs kill'` pass, but ONLY WHILE ITS OUTPUT IS
# TEXT (`_output_stays_text`, the branch review of train-1985-d R2-1: `echo "<kill>" | python`, `iex (Write-Output
# '<kill>')`, `echo '<kill>' > k.sh && bash k.sh` and git's `!` alias each RUN the printed words). Its output is text
# unless something RUNS it (R4-1: a stderr redirect, a search piped into head, sed or awk, or text written to a file
# nothing runs, runs nothing): a program that runs its input (`_RUNS_INPUT`: an interpreter, a shell, iex, xargs of
# any but a text-only program, a ForEach-Object block that does more than read properties) on its pipeline; a file it
# writes (a stdout redirect, tee, Out-File, Set-Content) that another step of the line names; a git `!` alias; or, for
# a step inside a substitution or an expression, an evaluator anywhere in the line (`_EVALUATOR`: `iex ( ... )` is no
# step of the model). A runner (xargs, env, timeout, nice, sudo ...) of a text-only program is text-only too
# (`_runs`: `git grep -l X | xargs grep -n Y`); a string literal a payload only
# prints (`_PRINTED`: `print('kill tasklist')`); and a signal a runner chooses for the command it starts (`timeout -s
# KILL 600 python ...` kills nothing). Allowed: a kill of a pid the agent started, with
# no enumeration in the line (`$p = Start-Process ... -PassThru; Stop-Process -Id $p.Id`, `$p.Kill()`, Bash `kill $pid`,
# `taskkill /PID n`), and every listing that kills nothing. A line that lists by name AND kills its own pid is refused
# too: list in one call, kill in the next. A script FILE an agent writes is outside the hook (it reads the command line,
# not files): P18 binds it.
_TEXT_ONLY = {"grep", "egrep", "fgrep", "zgrep", "rg", "ag", "ack", "findstr", "select-string", "sls", "git", "echo",
              "printf", "write-output", "write-host", "write", "write-error", "write-warning", "write-verbose",
              "write-information", "write-debug"}
#: The programs that RUN what is piped into them (a script on stdin, or arguments to a command): a text-only step piped
#: into one is judged. awk, sed, head and the like run their own program, never their input, so they are not here.
_RUNS_INPUT = re.compile(r"^(?:py|pythonw?(?:\d+(?:\.\d+)?)?|pwsh|powershell|sh|bash|zsh|dash|ksh|fish|node|deno|bun|perl"
                         r"|ruby|php|lua|tclsh|cmd|iex|invoke-expression|invoke-command|icm|eval|source|\.|xargs|parallel"
                         r"|osascript|wscript|cscript|foreach-object|%)$")
#: The words in a line that can run a substitution's or an expression's output (`iex (Write-Output ...)`, `& (...)`).
_EVALUATOR = re.compile(r"(?<![\w$.-])(?:iex|invoke-expression|invoke-command|icm|eval|source)(?![\w-])|(?:^|[\s;|(])[&.]\s*[($]",
                        re.I)
#: The commands that run another: their own options and values are skipped to find what they run (`_runs`).
_RUNNERS = {"xargs", "env", "sudo", "doas", "nice", "ionice", "nohup", "time", "timeout", "command", "exec", "stdbuf"}
_RUNNER_VALUED = {"-I", "-d", "-a", "-E", "-L", "-n", "-P", "-s", "-u", "-k", "--signal", "--kill-after"}
_WRITERS = {"tee", "out-file", "set-content", "add-content", "sc", "ac"}
_STDOUT_REDIRECT = re.compile(r"^(?:1|&)?>>?\|?(.*)$")      # `>`, `>>`, `1>`, `&>`, `>|`; never `2>`
_NO_FILE = {"/dev/null", "$null", "nul", "null", "/dev/stdout", "/dev/stderr"}
_PROPERTY_READ = re.compile(r"^\{\s*\$_(?:\.\w+)*\s*\}$")       # `{ $_.Line }`: reads, runs nothing
_W = r"(?<![\w$.-])"                      # a word starts here: not inside a name, a variable or a member access
_PS_ID_PARAM = r"\s-(?:id|pid|i(?:n(?:p(?:u(?:t(?:o(?:b(?:j(?:e(?:c(?:t)?)?)?)?)?)?)?)?)?)?)\b"   # -Id, -InputObject
_COMMAND_POSITION = r"(?:^|[|;&({}\[,=`'\"]|\$\()\s*"
_ENUMERATES = [
    re.compile(_W + r"(?:get-process|gps)\b(?![^|;)}\n]*" + _PS_ID_PARAM + ")", re.I),
    re.compile(_COMMAND_POSITION + r"ps\b(?![^|;)}\n]*(?:" + _PS_ID_PARAM + r"|\s(?:-p|--pid|-q|--quick-pid)\b|\s-p\d|\s--pid=))",
               re.I),
    re.compile(_W + r"(?:pgrep|pidof)\b", re.I),
    re.compile(_W + r"tasklist\b(?![^|;&\n]*[/-]+fi\s+['\"]?pid\b)", re.I),
    re.compile(r"::getprocesses(?:byname)?\s*\(|\bprocess_iter\b|\bpsutil\.pids\b", re.I),
]
_PROCESS_QUERY = re.compile(r"\bwmic\b[^|;&\n]*\bprocess\b[^|;&\n]*|\bwin32_process\b[^|;\n]*", re.I)
_ONE_PID = re.compile(r"\b(?:processid|handle)\s*=", re.I)
_NAME_FILTER = re.compile(r"\b(?:name|caption|commandline|executablepath)\s*(?:=|like\b|-eq\b|-like\b|-match\b)", re.I)
_KILLS = [
    re.compile(_W + r"(?:stop-process|spps|taskkill|tskill|pkill|killall|pskill|remove-ciminstance|remove-wmiobject"
               r"|rcim|rwmi)\b", re.I),
    re.compile(_W + r"(?<!-s )(?<!--signal )(?<!--signal=)kill\b", re.I),   # `-s KILL` is a runner's signal choice
    re.compile(r"\.(?:kill|terminate|send_signal)\s*\(|\bterminateprocess\b", re.I),
    re.compile(_W + r"(?:foreach-object|%)\s+(?:-m\w*\s+)?terminate\b", re.I),
    re.compile(_W + r"(?:invoke-cimmethod|invoke-wmimethod|icim|iwmi)\b[^|;\n]*\bterminate\b", re.I),
    re.compile(r"\bwmic\b[^|;&\n]*\b(?:delete|terminate)\b", re.I),
]
_BY_NAME = [
    re.compile(_W + r"(?:pkill|killall)\b", re.I),
    re.compile(_W + r"taskkill\b[^|;&\n]*?(?<!\w)[/-]+(?:im\b|fi\s+['\"]?(?!pid\b))", re.I),
    re.compile(_W + r"(?:stop-process|spps)\b[^|;)}\n]*\s-(?:n|na|nam|name|processname)(?![\w-])", re.I),
]
# A string literal a payload only prints is text too (`python -c "print('kill tasklist')"`), like a `_TEXT_ONLY` step's words.
_PRINTED = re.compile(r"\b(?:print|echo|printf|puts|say|console\.log|write-output|write-host)\b\s*\(?\s*[rbfu]?"
                      r"(['\"])(?:\\.|(?!\1).)*?\1", re.I | re.S)
_PS_KILL_BY_NAME = re.compile(_W + r"kill\b[^|;)}\n]*\s-(?:n|na|nam|name|processname)(?![\w-])", re.I)  # Bash's kill -n is a signal


def _program(st) -> str | None:
    k = _command_word(st.words)
    return None if k is None else shell_location.program(st.words[k])


def _runs(st) -> str | None:
    """The program a step runs in the end: its command word, or past a runner's (`_RUNNERS`) own options, their values,
    a duration and assignments, the program the runner starts (`xargs -0 grep -n x` runs grep)."""
    k = _command_word(st.words)
    w = st.words
    while k is not None and k < len(w) and shell_location.program(w[k]) in _RUNNERS:
        k += 1
        while k < len(w) and (w[k].startswith("-") or re.fullmatch(r"\d+(?:\.\d+)?[smhd]?|[A-Za-z_]\w*=.*", w[k])):
            k += 2 if w[k] in _RUNNER_VALUED else 1
    return shell_location.program(w[k]) if k is not None and k < len(w) else None


def _written(st) -> list[str]:
    """The files a step writes its standard output to: a stdout redirect's target (never `2>`, `>&2` or the null device)
    and a writer's (`_WRITERS`: tee, Out-File, Set-Content) path arguments."""
    w, out = st.words, []
    for j, x in enumerate(w):
        if (m := _STDOUT_REDIRECT.match(x)) is not None:
            target = m.group(1) or (w[j + 1] if j + 1 < len(w) else "")
            if target and not target.startswith("&"):
                out.append(target)
    if _program(st) in _WRITERS:
        out += [x for x in w[(_command_word(w) or 0) + 1:] if not x.startswith("-")]
    return [t for t in out if t.lower() not in _NO_FILE]


def _run_later(files: list[str], writers: set[int]) -> bool:
    """Whether another step of the line names one of `files` (by its path or its file name) and is no text-only program:
    `echo … > k.sh && bash k.sh` runs what it wrote; `echo … >> notes.md` alone does not."""
    names = {f.replace("\\", "/").rsplit("/", 1)[-1].lower() for f in files}
    return any(j not in writers and _runs(st) not in _TEXT_ONLY and any(
        x.replace("\\", "/").rsplit("/", 1)[-1].lower() in names for x in st.words) for j, st in enumerate(STEPS))


def _output_stays_text(i: int) -> bool:
    """Whether what step `STEPS[i]` prints reaches nothing that runs it (the branch reviews of train-1985-d R2-1 and
    R4-1): no program on its pipeline runs its input (`_RUNS_INPUT`, but xargs of a text-only program and a ForEach-Object
    that only reads properties), no file it or a writer on its pipeline writes is named by another step, it is no git `!`
    alias, and, inside a substitution or an expression, the line holds no evaluator (`_EVALUATOR`) and no step that runs
    its input (`python -c "$(echo …)"`)."""
    st = STEPS[i]
    if st.depth and (_EVALUATOR.search(cmd) or any(_RUNS_INPUT.match(_runs(x) or "") for x in STEPS)):
        return False             # a substitution's output may be the program an interpreter of the line runs
    if _program(st) == "git" and any(w.startswith("!") or "=!" in w for w in st.words):
        return False             # `git -c alias.k='!…' k`: git hands a `!` alias to the shell
    written, writers = _written(st), {i}
    while st.sep == "|":
        i = next((j for j in range(i + 1, len(STEPS)) if STEPS[j].depth == st.depth), None)
        if i is None:
            return False
        st = STEPS[i]
        name = _program(st) or ""
        if _RUNS_INPUT.match(name) and not (
                name == "xargs" and _runs(st) in _TEXT_ONLY
                or name in ("foreach-object", "%") and len(st.words) == 2 and _PROPERTY_READ.match(st.words[1])):
            return False
        written += _written(st)
        writers.add(i)
    return not (written and _run_later(written, writers))


def _code_text(i: int) -> str:
    """The text of step `STEPS[i]` that a shell or an interpreter may run: every word (a quoted payload whole) and the
    bodies of the heredocs it introduces; empty for a program that only searches or prints text (`_TEXT_ONLY`) while its
    output stays text (`_output_stays_text`)."""
    st = STEPS[i]
    if _runs(st) in _TEXT_ONLY and _output_stays_text(i):
        return ""
    return _PRINTED.sub(" ", "\n".join((" ".join(st.words), *st.heredocs)))


def _enumerates(text: str) -> bool:
    """Whether the text lists processes by name or all of them: what a by-name kill reads its targets from."""
    return any(p.search(text) for p in _ENUMERATES) or any(
        not _ONE_PID.search(m.group(0)) or _NAME_FILTER.search(m.group(0)) for m in _PROCESS_QUERY.finditer(text))


def _kills(text: str) -> bool:
    """Whether the text stops a process, by whatever it was handed."""
    return any(p.search(text) for p in _KILLS)


def _kills_by_name(st, text: str) -> bool:
    """Whether one step is both ingredients: a kill whose own arguments name the image."""
    return any(p.search(text) for p in _BY_NAME) or (
        st.shell == shell_location.POWERSHELL and bool(_PS_KILL_BY_NAME.search(text)))


_TEXTS = [(st, _code_text(i)) for i, st in enumerate(STEPS)]
_BY_NAME_STEP = next((st for st, t in _TEXTS if _kills_by_name(st, t)), None)
_ENUMERATION = next((st for st, t in _TEXTS if _enumerates(t)), None)
_KILL = next((st for st, t in _TEXTS if _kills(t)), None)
if _BY_NAME_STEP is not None or (_ENUMERATION is not None and _KILL is not None):
    _what = (f"`{_BY_NAME_STEP.text[:60]}`" if _BY_NAME_STEP is not None
             else f"`{_KILL.text[:50]}` stops what `{_ENUMERATION.text[:50]}` lists")
    block(f"a kill by process NAME ({_what}): a name matches every process of that image on the host, including "
          "other agents' gates and builds (kb/Work PB6034: a `Get-Process cobol | Stop-Process` killed another gate's "
          "compiles, MANDATORY-PRACTICES P18). A command line that lists processes by name (Get-Process, ps, pgrep, "
          "pidof, tasklist, wmic, Win32_Process, psutil.process_iter) and kills (Stop-Process, .Kill(), kill, taskkill, "
          "xargs kill, Terminate, os.kill) is refused in any order, pipe or payload. Stop only what you started, by its "
          "pid: keep the object `Start-Process -PassThru` returns and run `Stop-Process -Id $p.Id` (Bash: `kill $pid`); "
          "list in one call and kill in another if you must look first.")


sys.exit(0)
