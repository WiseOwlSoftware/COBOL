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
  direct push in a unit     inside an orchestrator unit (COBOL_COORD_DIR set) ANY `git push`, any repository, any
                            flags: a unit lands through the landing script (enqueue.py) and never publishes another repo
                            (owner 2026-10-05, kb/Work/PB2044: the wave unit pushed the public skills repo's main with
                            `git push -q origin HEAD:main`, which the deny rule's text pattern missed).
  escapes in a heredoc      heredoc bodies with backslash escapes were mangled or mis-escaped in six sessions (and the
                            harness decodes some escapes in tool input); write the file with the Write tool instead.
  bare `~X` test filter     `--filter "~X|~Y"` matches NOTHING and exits 0 — a silent green; every term needs a
                            property (FullyQualifiedName~X).
  `cd` in a backgrounded    a location change (`cd`, `pushd`, `Set-Location`, ...) inside an and-or list that a single
  list, then more           `&` sends to the background, with another command after that `&` (MANDATORY-PRACTICES
                            P14(b), kb/Work PB3095): `cd X && a & b &` runs `a` in a background subshell under X and
                            `b` in the CALLER's directory; a landing audit written that way reported a false OK into
                            the queue on 2026-10-11. Use absolute paths or `git -C <dir>`.

  WSL lifecycle commands    `wsl --terminate/--shutdown/--update/--export/--import/--unregister/--install` (owner
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
# 2b. ANY direct `git push`, in ANY repository, inside an orchestrator unit (owner 2026-10-05, kb/Work/PB2044).
# Rule 2 guards THIS repo's main and deliberately leaves another repository to its own rules, so an attended session may
# push the sites and the public skills repo. A unit has no such need and runs under `bypassPermissions`: on 2026-10-05 the
# wave unit published the public skills repo's main with `git push -q origin HEAD:main` from a scratch clone, and the
# project's deny rule (a TEXT pattern, `git push origin HEAD:main`) missed it because of the extra `-q`. A unit lands
# through `python scripts/orchestrator/enqueue.py land`, whose command text contains no `git push`; the supervisor exports
# COBOL_COORD_DIR into every unit's environment and hooks (also in its subagents) inherit it. Attended sessions and
# cloud sessions never have it set, so their pushes are untouched. A call is the git SUBCOMMAND after any global options
# (`-C dir`, `-c k=v`, `--no-pager`; `shell_location.git_calls`), so flags before or after cannot dodge it, `git log
# --grep=push` is not hit, a commit message that says "git push" is one quoted word, and a `bash -c` payload is a command
# line like any other.
if os.environ.get("COBOL_COORD_DIR") and any(call.subcommand == "push" for call in GIT_CALLS):
    block("a direct `git push` is refused inside an orchestrator unit (any repository, any flags). Land this "
          "repository's work with `python scripts/orchestrator/enqueue.py land <branch>` (it queues a pull "
          "request into GitHub's merge queue). A change to ANOTHER repository (the public skills "
          "repo, a site) is never published by a unit: hand it off as an owed push in the handoff and the "
          "attended session or the owner publishes it (kb/Work/PB2044).")
# 3. backslash escapes inside a heredoc body — the bodies the model cut out at their delimiter lines, never the text after
# the first `<<` (a `<<` inside quotes is no heredoc, and a command after the body is no body)
if any(re.search(r"\\[nrtuUx0abfvdswDSW\\'\"]", body) for st in STEPS for body in st.heredocs):
    block("this heredoc body contains backslash escape sequences, which have been mangled here repeatedly. Write the "
          "script or file with the Write tool (into your own scratch directory, MANDATORY-PRACTICES P16) and run it, or express the character without an "
          "escape (chr(10), a character class).")

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

sys.exit(0)
