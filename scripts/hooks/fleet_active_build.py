#!/usr/bin/env python3
"""PreToolUse hook — REFUSE to build or test while a subagent fleet is live IN THE CALLER'S OWN WORKING TREE.

⛔ WHY THIS EXISTS. On 2026-08-04 a ten-agent measurement fan-out was dispatched over the §15 result-type
population, and the fix for what it was measuring was then implemented WHILE IT RAN: ~6 `dotnet build`
invocations plus both test gates, over ~1.75 h and 60 agents. Its finders probed a binary that changed under
them and its refuters re-ran probes against a tree where the defects were already fixed, so not one of its
verdicts was usable. One build even failed outright with `MSB3027: cobol.exe locked by another process` — a live
agent — and that alarm was recorded as "transient" and the building continued.

The owner's correction: each repetition wasted tokens. Memory alone did not stop it, so the guard is mechanical.

⚖ THE RULE IT ENFORCES is the ORDER of two decisions, not just the concurrency:
  1. Measure INLINE first. A fan-out for work an inline probe already did is pure waste — PB15's entire
     population was derived and measured in three tool calls before the fleet was ever dispatched.
  2. Once a fleet IS running, THE TREE IT IS PROBING is FROZEN. Build/test only after the completion signal, or
     stop the fleet.

⚖ THE UNIT OF THE FREEZE IS THE WORKING TREE, NOT THE SESSION (2026-09-01). The guard originally denied a build
whenever ANY other agent of this session was live, which is the right harm model applied at the wrong scope: the
harm is "my build changes the binaries THAT agent is executing", and two agents in two `git worktree` checkouts
share no `bin/obj`, no `cobol.exe` and no source. Keying on the session therefore denied a build that could not
collide with anything, and with the owner's 2026-09-01 direction for MAXIMUM SUBAGENT PARALLELISM in the fix
lane that false denial became the binding constraint: N implementer agents in N worktrees serialized behind each
other's liveness. So the guard now denies iff some FOREIGN live agent is working in the SAME tree as the caller.

Both trees in that comparison are DERIVED, never listed — a hand-maintained map of agent→tree is exactly the
shape rule 5 forbids, and it would go stale the first time the host renamed a directory:
  • the CALLER's tree = the repository root of WHERE THE BUILD RUNS — the project a `dotnet` command names, else
    the directory the shell is in when it runs it, following every location change of either shell
    (`build_sites`, through the shared parser `shell_location.py`, kb/Work PB2599), else the payload's `cwd` —
    the nearest ancestor holding a `.git` entry, which is a DIRECTORY in the main checkout and a FILE in a
    worktree. Stopping at the FIRST one is what makes a worktree resolve to itself rather than to the repository
    it was cut from (`repo_root_of`). A project outside every working tree (a scratch probe) writes where its
    PROJECT FILES say: its own directory plus the directory of every project it `ProjectReference`s (a scratch
    project can reference a worktree's project, so the trees such a build writes are read off the project graph,
    `build_places`, kb/Work PB3770); a graph the guard cannot read to the end stays UNKNOWN.
  • a FOREIGN agent's tree = the tree of the directory the agent was LAUNCHED in, which its own transcript records
    on its first record (`cwd`; `launch_cwd`, `agent_place`; kb/Work PB3770). The host writes that field however
    it starts an agent, so ONE mechanism covers `Agent(isolation="worktree")`'s `agent-<id>`, a rolling wave's
    `wf_<run>-<n>`, any named `git worktree add` and the main checkout. The guard used to infer the tree from the
    EXISTENCE of `<main>/.claude/worktrees/agent-<agentId>` and read every other agent, each wave implementer
    included, as working in the main checkout: that refused a read-only adjudicator's scratch probe build for
    about 90 minutes while two lanes' implementers ran, and would have refused a main-checkout build while only
    worktree implementers were live.
Consequences, all three intended: a worktree agent may build its OWN worktree while main-tree fleets are live; a
MAIN-tree build is still denied while any main-tree agent is live (the original purpose, unchanged); and a
main-tree build is ALLOWED while only worktree agents are live, because their binaries are not the main tree's.

⚠ AND THE FALLBACK IS TOWARD THE OLD RULE, NEVER TOWARD ALLOWING. If the places a build writes cannot be located
(a directory the command line does not determine; a scratch project whose graph the guard cannot read to the end:
an unreadable or missing project file, a `$(…)` reference, an `Import`, a moved output path, a
`Directory.Build.props` above it), or an agent's launch directory cannot be read from its transcript or no longer
exists, the decision reverts to the SESSION-WIDE rule for it and the foreign live agent denies. Fail-open (below)
covers a BROKEN guard; this covers an UNKNOWN tree, where allowing would be the one outcome that reproduces
2026-08-04.

Detection is by TRANSCRIPT MTIME, which is the one signal that cannot be faked by a stale process: a live agent
writes to its own `agent-*.jsonl` continuously, so a file touched within the window means an agent is thinking
right now. Keyed on this session's id, so another project's fleet never blocks this one.

⚠ THE CALLER IS NEVER A FLEET. The question the guard asks is "is any transcript OTHER THAN MINE live?", and the
caller's own transcript is always fresh at PreToolUse — it is being written by this very tool call. Getting that
exclusion wrong makes the guard fail CLOSED, which it has now done three separate ways — PB103's TWO modes (an
agent inside its own worktree, then a main-tree agent counting its own transcript) and PB185 (every subagent,
because `transcript_path` names the SESSION transcript). ⚠ Cite PB103, not PB101: PB101 is the COLLATION
subsystem note, and it appears in this history only because its agent was the one who HIT the worktree mode
(`kb/Work/PB103.md` records the find that way). The exclusion is therefore keyed on the caller's IDENTITY, not on
a path string: see `caller_identity` / `foreign_transcripts`.

FAIL-OPEN BY CONSTRUCTION. Any error, any missing path, any unreadable directory ⇒ exit 0 and allow the build. A
guard that blocks the build because IT broke would be worse than the defect it prevents.

Both branches are provable without a fleet: `python scripts/hooks/fleet_active_build.py --self-test` drives the
pure decision functions through DENY and ALLOW alike, over REAL temporary worktrees, scratch projects and agent
transcripts so the launch-directory read, the `.git` entry and the project-graph read are exercised rather than mocked
(feedback_prove_the_watchdog_fails — a monitor is a gate; fire its failure branch once before trusting its
silence).
"""
import json
import os
import pathlib
import re
import sys
import tempfile
import time
from typing import NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "orchestrator"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import account  # noqa: E402  (the one resolver of Claude's config dir, kb/Work PB2479)
import shell_location  # noqa: E402  (the one parser of a command's location changes, kb/Work PB2599)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

WINDOW_SECONDS = 120

# ⛔ An agent inside a LONG TOOL CALL writes nothing to its transcript until the tool returns (kb/Work PB1702). The
# assistant line carrying the `tool_use` is flushed when the call STARTS; the `tool_result` line only when it ENDS. A
# gate that blocks on `timeout 580 … tail -f <log>` therefore leaves the transcript untouched for up to ten minutes
# while the agent is plainly live — measured 2026-09-28, wave 69: three finishers' transcripts went 20+ minutes
# without a write while their gates ran, and a watchdog keyed on mtime alone called them dead. So a transcript whose
# LAST record is an unanswered tool call is live for as long as a tool call can last: the shell tools' 600 s maximum
# plus a margin. The bound is what keeps a KILLED agent (which also leaves a dangling tool call) from denying forever.
TOOL_WINDOW_SECONDS = 660
TAIL_BYTES = 262144

# `dotnet clean/publish/run` rewrite or delete the same outputs a live agent is executing, so they are in scope
# too. `dotnet --version`, `dotnet tool`, `dotnet nuget` etc. are not.
BUILD_VERBS = ("build", "test", "clean", "publish", "run", "msbuild")

# A build writes the bin/obj of the PROJECT it builds: `dotnet build <x>.sln`, `dotnet run --project <x>.csproj`,
# `dotnet run walk.cs`. A positional argument with one of these suffixes, or a `--project` value, names it.
SOLUTION_SUFFIXES = (".sln", ".slnx")
MSBUILD_PROJECT_SUFFIXES = (".csproj", ".fsproj", ".vbproj", ".proj")
FILE_APP_SUFFIX = ".cs"  # a file-based app (`dotnet run walk.cs`); its `#:project` directives are its references
PROJECT_SUFFIXES = (*SOLUTION_SUFFIXES, *MSBUILD_PROJECT_SUFFIXES, FILE_APP_SUFFIX)

# What a build outside every working tree writes is read off its project graph (`build_places`, kb/Work PB3770). Any
# of these makes the graph unreadable, so the build stays UNKNOWN and the session-wide rule applies: an MSBuild
# import (it can set anything), a property that moves where the build writes, and a `Directory.Build.*` file in any
# ancestor directory (MSBuild imports it implicitly).
IMPLICIT_IMPORTS = ("Directory.Build.props", "Directory.Build.targets")
OUTPUT_PROPERTIES = (
    "BaseOutputPath", "OutputPath", "OutDir", "PublishDir", "BaseIntermediateOutputPath", "IntermediateOutputPath",
    "ArtifactsPath", "UseArtifactsOutput",
)
XML_COMMENT_RE = re.compile(r"<!--.*?-->", re.S)
IMPORT_RE = re.compile(r"<Import\b", re.I)
OUTPUT_PROPERTY_RE = re.compile(r"<(?:" + "|".join(OUTPUT_PROPERTIES) + r")\b", re.I)
PROJECT_REFERENCE_RE = re.compile(r"<ProjectReference\b[^>]*?\bInclude\s*=\s*[\"']([^\"']*)[\"']", re.I | re.S)
# A solution names its projects in quotes (`.sln`: `Project("{…}") = "n", "p.csproj", "{…}"`) or as `Path="p.csproj"`.
SOLUTION_PROJECT_RE = re.compile(r"[\"']([^\"']+\.(?:cs|fs|vb)?proj)[\"']", re.I)
MAX_GRAPH_FILES = 256 # a project graph this large is not a scratch probe: UNKNOWN
LAUNCH_RECORDS = 8     # the record that names an agent's launch `cwd` is among its first few
LAUNCH_RECORD_BYTES = 8 * 1024 * 1024  # a first record carries the launch prompt, so it can be large



def bail() -> None:
    """Allow the tool call. Every failure path lands here."""
    sys.exit(0)


def is_build_command(cmd: str) -> bool:
    low = cmd.lower()
    if "dotnet" not in low:
        return False
    # Match `dotnet <verb>` allowing intervening flags is overkill; the real invocations in this repo are
    # `dotnet build ...` / `dotnet test ...`, plus `timeout NNN dotnet build ...`.
    return any(f"dotnet {verb}" in low for verb in BUILD_VERBS)


def caller_identity(data: dict) -> tuple[str, "pathlib.Path | None"]:
    """The two spellings of "this transcript is MINE", read off the hook payload.

    ⛔ The failure this replaces (PB185, the third self-block of this guard): the payload's `transcript_path`
    names the SESSION transcript — `~/.claude/projects/<proj>/<session-id>.jsonl` — for a subagent exactly as it
    does for the main session. A subagent's own file is `<session-id>/subagents/**/agent-<agent_id>.jsonl`, a
    different path entirely, so the raw string equality added for PB103 could never match and EVERY subagent
    denied its own build permanently (the caller rewrites its transcript on every tool call, so the 120 s window
    never clears). Measured 2026-08-31 from the live payload: `transcript_path` = `…/17d093ad-….jsonl` while the
    caller's own transcript was `…/17d093ad-…/subagents/agent-acc9d0d75b870eb9d.jsonl`.

    So identity comes from `agent_id`, which IS the caller's transcript identity by construction — the file is
    named for it, wherever in the `subagents/` tree the host puts it, so no path string has to match and a
    fourth spelling of the same question is not one comparison away. `transcript_path` is kept as a second,
    independent spelling for hosts that DO point it at the agent file, compared on a RESOLVED path rather than
    on raw text. A payload carrying neither (the main session) excludes nothing, which is correct: the main
    session's own transcript is not under `subagents/` and never enters the candidate set at all.
    """
    agent_id = (data.get("agent_id") or "").strip().lower()

    own: "pathlib.Path | None" = None
    raw = (data.get("transcript_path") or "").strip()
    if raw:
        try:
            own = pathlib.Path(raw).resolve()
        except OSError:
            own = None
    return agent_id, own


def foreign_transcripts(data: dict, candidates: list) -> list:
    """The live transcripts that are NOT the caller's — the only ones that are evidence of a fleet."""
    agent_id, own = caller_identity(data)
    own_name = f"agent-{agent_id}.jsonl" if agent_id else None

    foreign = []
    for path in candidates:
        if own_name and path.name.lower() == own_name:
            continue
        if own is not None:
            try:
                if path.resolve() == own:
                    continue
            except OSError:
                pass
        foreign.append(path)
    return foreign


def same_path(a, b) -> bool:
    """Path equality that survives Windows' case-insensitive filesystem.

    The two sides are built from different strings — one from the payload's `cwd`, one from a `.git` file's
    `gitdir:` line — so `E:\\COBOL` and `e:\\COBOL` are both spellings that occur in practice and a
    raw `==` would silently answer "different tree" for the same tree.
    """
    return os.path.normcase(str(a)) == os.path.normcase(str(b))


def repo_root_of(start: str) -> "pathlib.Path | None":
    """The WORKING TREE containing `start`: its nearest ancestor holding a `.git` entry, resolved.

    `.git` is a DIRECTORY in the main checkout and a FILE in a `git worktree`; both count, and stopping at the
    FIRST one is precisely what makes a worktree resolve to ITSELF rather than to the repository it was cut
    from. `None` means "no tree found", which the caller must treat as UNKNOWN, never as "no conflict".
    """
    if not start:
        return None
    try:
        here = pathlib.Path(start).resolve()
    except OSError:
        return None
    for d in (here, *here.parents):
        try:
            if (d / ".git").exists():
                return d
        except OSError:
            continue
    return None


class Place(NamedTuple):
    """Where something works or is written: a WORKING TREE (a `.git` entry's directory) or a plain directory."""

    path: pathlib.Path
    is_tree: bool


def place_of(directory) -> "Place | None":
    """The tree containing `directory`, else `directory` itself when it exists; None when it does not exist.

    A directory that is gone is UNKNOWN, never "the main checkout": `repo_root_of` would walk up from a removed
    worktree to the repository it was cut from, and a live agent's launch directory that no longer exists is a
    fact the guard cannot rely on.
    """
    try:
        here = pathlib.Path(directory).resolve()
        if not here.is_dir():
            return None
    except (OSError, ValueError):
        return None
    tree = repo_root_of(str(here))
    return Place(tree, True) if tree is not None else Place(here, False)


def launch_cwd(transcript: pathlib.Path) -> str:
    """The directory the agent was LAUNCHED in: the `cwd` its own transcript records on its first records (kb/Work
    PB3770). "" when the transcript cannot be read or carries none, which `agent_place` turns into UNKNOWN.

    Measured 2026-10-25 across every agent of this session: the first record of `agent-<id>.jsonl` is the launch
    prompt and carries `cwd` — `E:\\COBOL` for a read-only agent, `E:\\COBOL\\.claude\\worktrees\\wf_<run>-<n>` for a
    rolling-wave implementer, the named worktree of an operator-dispatched agent. Only the head is read: this runs
    before every `dotnet` call, and a live transcript can be many megabytes.
    """
    try:
        with transcript.open("rb") as f:
            for _ in range(LAUNCH_RECORDS):
                line = f.readline(LAUNCH_RECORD_BYTES)
                if not line:
                    break
                try:
                    rec = json.loads(line)
                except ValueError:
                    continue
                cwd = rec.get("cwd") if isinstance(rec, dict) else None
                if isinstance(cwd, str) and cwd.strip():
                    return cwd.strip()
    except OSError:
        pass
    return ""


def agent_place(transcript: pathlib.Path) -> "Place | None":
    """Where a live agent works — decided by CONSTRUCTION from the agent's own transcript, never by a maintained
    list and never from a directory naming convention: the tree of its launch directory (`launch_cwd`)."""
    cwd = launch_cwd(transcript)
    return place_of(cwd) if cwd else None


def is_within(child, parent) -> bool:
    return any(same_path(d, parent) for d in (pathlib.Path(child), *pathlib.Path(child).parents))


def collides(agent: Place, written: Place) -> bool:
    """Can a build writing in `written` change the binaries an agent working in `agent` executes?

    Two trees collide when they are the same tree. An agent in a plain directory (a scratch probe's agent) collides
    with a write inside that directory, or one that encloses it. An agent in a tree never collides with a plain
    directory: the plain directory has no `.git` ancestor, so it cannot lie inside the tree.
    """
    if agent.is_tree:
        return written.is_tree and same_path(agent.path, written.path)
    return is_within(written.path, agent.path) or is_within(agent.path, written.path)


def project_refs(project: pathlib.Path) -> "list[pathlib.Path] | None":
    """The projects `project` builds beyond itself, as paths; None when its text cannot be read to the end.

    A solution (`.sln`, `.slnx`) lists the projects it builds, an MSBuild project its `<ProjectReference>`s, a
    file-based app (`dotnet run walk.cs`) its `#:project` directives. Anything that could write elsewhere than
    next to the project, or that this reader cannot resolve, is None: an `<Import>`, an output-path property, a
    `$(…)` in a reference, a `Directory.Build.*` file in any ancestor directory.
    """
    for d in (project.parent, *project.parent.parents):
        try:
            if any((d / name).exists() for name in IMPLICIT_IMPORTS):
                return None
        except OSError:
            return None
    try:
        text = project.read_text(encoding="utf-8-sig", errors="replace")
    except OSError:
        return None

    suffix = project.suffix.lower()
    if suffix == FILE_APP_SUFFIX:
        found = []
        for line in text.splitlines():
            directive = line.strip()
            if not directive.startswith("#:"):
                continue
            kind, _, value = directive[2:].strip().partition(" ")
            value = value.strip()
            if kind.lower() == "project":
                found.append(value)
            elif kind.lower() == "property" and value.split("=", 1)[0].strip() in OUTPUT_PROPERTIES:
                return None
    elif suffix in SOLUTION_SUFFIXES:
        found = [m.group(1) for m in SOLUTION_PROJECT_RE.finditer(text)]
    else:
        xml = XML_COMMENT_RE.sub("", text)
        if IMPORT_RE.search(xml) or OUTPUT_PROPERTY_RE.search(xml):
            return None
        found = [m.group(1) for m in PROJECT_REFERENCE_RE.finditer(xml)]

    refs = []
    for ref in found:
        if not ref or "$(" in ref or "%" in ref:
            return None
        target = project.parent / ref.replace("\\", "/")
        if target.is_dir():
            inside = project_files_in(target)
            if not inside:
                return None
            refs += inside
        else:
            refs.append(target)
    return refs


def project_files_in(directory: pathlib.Path) -> list:
    """The solutions and projects `dotnet build` looks for in `directory` (it takes the only one; all are listed, a
    superset, when there are several)."""
    try:
        return sorted(
            p for p in directory.iterdir()
            if p.is_file() and p.suffix.lower() in (*SOLUTION_SUFFIXES, *MSBUILD_PROJECT_SUFFIXES)
        )
    except OSError:
        return []


def build_places(site: str) -> "list[Place] | None":
    """Every place a build at `site` writes (kb/Work PB3770); None when the line does not determine them.

    Inside a working tree the build writes that tree. Outside every tree (a scratch probe) it writes next to each
    project of its graph — the project or solution named, or the only one in the directory, and the closure of what
    they reference — and a reference into a worktree makes that worktree one of the places. A graph the guard
    cannot read to the end is None (`project_refs`), which reverts to the session-wide rule.
    """
    if not site:
        return None
    tree = repo_root_of(site)
    if tree is not None:
        return [Place(tree, True)]
    try:
        start = pathlib.Path(site).resolve()
    except (OSError, ValueError):
        return None
    if start.is_dir():
        queue = project_files_in(start)
    else:
        queue = [start] if start.is_file() else []
    if not queue:
        return None
    seen: dict = {}
    while queue:
        project = queue.pop()
        key = os.path.normcase(str(project))
        if key in seen:
            continue
        if len(seen) >= MAX_GRAPH_FILES:
            return None
        seen[key] = project
        if repo_root_of(str(project.parent)) is not None or not project.exists():
            # A project inside a tree makes that tree a place, and what that tree's own projects reference is that
            # tree's business (the in-tree arm above reads no graph either); a missing one fails the build.
            continue
        refs = project_refs(project)
        if refs is None:
            return None
        queue += refs
    places = []
    for project in seen.values():
        place = place_of(project.parent)
        if place is None:
            return None
        if not any(same_path(place.path, q.path) for q in places):
            places.append(place)
    return places


def build_sites(data: dict) -> list:
    """Every place the tool call builds: for each `dotnet <verb>` command, the project it names, else the directory it
    runs in. `None` is a place the command line does not determine (an UNKNOWN tree, so the session-wide rule).

    The directory each command runs in comes from `shell_location.steps`, the one parser of the two shells' location
    changes (kb/Work PB2599): a PowerShell `Set-Location <worktree>; dotnet build`, a Bash `git worktree add … && cd
    <worktree> && dotnet build`, a `pushd`, a `( … )` subshell or `pwsh -WorkingDirectory` each build where the shell
    does, not in the payload's cwd. `cwd` is a documented PreToolUse payload field (since PB103); `os.getcwd()` stands
    in for a host that omits it, because the hook is launched from the caller's directory.
    """
    cwd = (data.get("cwd") or "").strip()
    if not cwd:
        try:
            cwd = os.getcwd()
        except OSError:
            cwd = ""
    command = (data.get("tool_input") or {}).get("command") or ""
    sites = []
    for step in shell_location.steps(command, cwd or None, shell_location.shell_of(data.get("tool_name"))):
        if not is_build_command(step.text):
            continue
        words = list(step.words)
        named = [words[i + 1] for i, w in enumerate(words[:-1]) if w in ("--project", "--solution")]
        named += [w.split("=", 1)[1] for w in words if w.startswith(("--project=", "--solution="))]
        named += [w for w in words if w.lower().endswith(PROJECT_SUFFIXES) and not w.startswith("-")]
        sites += [shell_location.resolve(n, step.cwd) for n in named] or [step.cwd]
    return sites


def agents_sharing_any(sites: list, foreign: list) -> tuple[list, "pathlib.Path | None"]:
    """The live foreign agents that work where the build at ANY of `sites` writes — the only builds that can collide
    with this one — and the first tree that made one of them share (None when none did, or when the line left the
    answer UNKNOWN).

    An UNKNOWN answer reverts to the session-wide rule rather than guessing: a site whose written places are not
    determined (`build_places`) is shared with every foreign agent, and an agent whose launch directory is not
    determined (`agent_place`) is shared with every site. The failure the guard exists to prevent is a build that
    went ahead.
    """
    places = {p: agent_place(p) for p in foreign}
    sharing, tree = [], None
    for site in sites:
        written = build_places(site or "")
        for transcript in foreign:
            if transcript in sharing:
                continue
            agent = places[transcript]
            if written is None or agent is None:
                sharing.append(transcript)
                continue
            hit = next((w for w in written if collides(agent, w)), None)
            if hit is not None:
                sharing.append(transcript)
                if tree is None and hit.is_tree:
                    tree = hit.path
    return sharing, tree


def live_agent_transcripts(session_id: str) -> list:
    projects = account.config_dir() / "projects"
    if not projects.is_dir():
        return []

    now = time.time()
    live = []
    # ~/.claude/projects/<sanitized-cwd>/<session-id>/subagents/**/agent-*.jsonl
    # Globbing on the session id rather than deriving the sanitized cwd keeps this correct whatever the
    # sanitization rule is, and scopes the guard to THIS session.
    for path in projects.glob(f"*/{session_id}/subagents/**/agent-*.jsonl"):
        try:
            if is_live(path, now):
                live.append(path)
        except OSError:
            continue
    return live


def is_live(path: pathlib.Path, now: float) -> bool:
    """Written within WINDOW_SECONDS, or ending on an unanswered tool call written within TOOL_WINDOW_SECONDS."""
    age = now - path.stat().st_mtime
    if age <= WINDOW_SECONDS:
        return True
    return age <= TOOL_WINDOW_SECONDS and pending_tool_call(path)


def pending_tool_call(path: pathlib.Path) -> bool:
    """Does the transcript END on a tool call that has no result yet — i.e. is the agent inside a tool right now?

    Reads only the tail. The last record that is either an assistant message or a tool result decides it: an
    assistant message carrying `tool_use` ⇒ pending; a user record carrying `tool_result`, or an assistant message
    without `tool_use` (the turn ended) ⇒ not pending. Any parse problem ⇒ False, which leaves the plain mtime rule
    in force: this can only ADD liveness the old rule missed, never remove liveness it found.
    """
    try:
        with path.open("rb") as f:
            f.seek(0, os.SEEK_END)
            size = f.tell()
            f.seek(max(0, size - TAIL_BYTES))
            tail = f.read().decode("utf-8", errors="replace")
    except OSError:
        return False
    for line in reversed(tail.splitlines()):
        try:
            rec = json.loads(line)
        except ValueError:
            continue
        content = (rec.get("message") or {}).get("content")
        if not isinstance(content, list):
            continue
        kinds = {c.get("type") for c in content if isinstance(c, dict)}
        if rec.get("type") == "assistant":
            return "tool_use" in kinds
        if rec.get("type") == "user" and "tool_result" in kinds:
            return False
    return False


def deny_reason(live: list, tree: "pathlib.Path | None" = None) -> str:
    # ⛔ `p.stem`, not `p.parent.name`: the parent of `…/subagents/agent-bbb.jsonl` is `subagents`, so the
    # message used to name the CONTAINING DIRECTORY for every agent and printed "subagents" as the culprit.
    names = sorted({p.stem for p in live})
    detail = ", ".join(names[:4]) + (f" (+{len(names) - 4} more)" if len(names) > 4 else "")
    where = (
        f"the SAME working tree as this build ({tree})" if tree is not None
        else "a working tree that could not be determined, so the session-wide rule applies"
    )
    return (
        f"BLOCKED — {len(live)} subagent(s) are LIVE (transcript written in the last {WINDOW_SECONDS}s, or "
        f"inside a tool call started in the last {TOOL_WINDOW_SECONDS}s), so a fleet is running in {where}: "
        f"{detail}. Building or testing now changes the binary those agents are "
        f"probing, which is what made a 60-agent run unusable on 2026-08-04 (PB15). "
        f"Do ONE of: (a) wait for the completion notification, then build; "
        f"(b) stop the fleet with TaskStop if its results are no longer worth having; "
        f"(c) if you are about to fix what it measures, stop it now — you already have the answer, and a "
        f"fleet measuring a tree you are editing produces verdicts you will have to disclaim; "
        f"(d) re-dispatch the agent with `isolation: \"worktree\"` — an agent in its own worktree builds its "
        f"own binaries and neither blocks nor is blocked by this tree. "
        f"Meanwhile, doc / DEVLOG / design-doc / kb-note work is safe."
    )


def _build_fixture(root: pathlib.Path) -> dict:
    """REAL trees and scratch projects on disk, for the self-test.

    Mocking the filesystem here would defeat the point: the facts this guard now turns on are a transcript's
    recorded launch directory, a `.git` entry's existence and a project file's text, and a fake for any of them is
    a probe that cannot fail for the reason production would. The worktrees are named the way the host and the
    rolling wave name them (`agent-<id>`, `wf_<run>-<n>`) and the way an operator does (`notes-x`): the guard must
    not care.
    """
    main = root / "main"
    (main / ".git").mkdir(parents=True)
    (main / "scripts" / "hooks").mkdir(parents=True)

    def worktree(name: str) -> pathlib.Path:
        wt = main / ".claude" / "worktrees" / name
        wt.mkdir(parents=True)
        (wt / ".git").write_text(f"gitdir: {(main / '.git' / 'worktrees' / name).as_posix()}\n", encoding="utf-8", newline="\n")
        return wt.resolve()

    def write(path: pathlib.Path, text: str) -> pathlib.Path:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8", newline="\n")
        return path.resolve()

    sdk = '<Project Sdk="Microsoft.NET.Sdk">\n{0}</Project>\n'
    wt_a, wt_b, wt_c = worktree("agent-aaa"), worktree("wf_run-1"), worktree("notes-x")
    for tree in (main, wt_a, wt_b):
        write(tree / "src" / "App" / "App.csproj", sdk.format(""))
        write(tree / "Directory.Build.props", "<Project/>\n")  # the real repository has one: a tree's own business
    scratch = root / "scratch"

    def scratch_project(name: str, body: str = "") -> pathlib.Path:
        return write(scratch / name / "probe.csproj", sdk.format(body)).parent

    def ref(project: pathlib.Path) -> str:
        return f'  <ItemGroup><ProjectReference Include="{str(project).replace("/", chr(92))}" /></ItemGroup>\n'

    plain = scratch_project("plain", "  <!-- no <ProjectReference>s: a comment is not one -->\n")
    other_plain = scratch_project("other-plain")
    write(scratch / "sln" / "probe.sln", 'Project("{FAE04EC0}") = "probe", "p\\probe.csproj", "{1}"\n')
    write(scratch / "sln" / "p" / "probe.csproj", sdk.format(""))
    write(scratch / "slnref" / "probe.slnx", f'<Solution><Project Path="{wt_a.as_posix()}/src/App/App.csproj" /></Solution>\n')
    cs = scratch / "cs"
    write(cs / "plain" / "walk.cs", "// a file-based app\nConsole.WriteLine(1);\n")
    write(cs / "ref" / "walk.cs", f"#:project {main.as_posix()}/src/App\nConsole.WriteLine(1);\n")
    write(cs / "out" / "walk.cs", "#:property OutputPath=../../x\nConsole.WriteLine(1);\n")
    (scratch / "empty").mkdir(parents=True)
    write(scratch / "dbp" / "Directory.Build.props", "<Project/>\n")
    write(scratch / "dbp" / "probe.csproj", sdk.format(""))
    return {
        "main": main.resolve(),
        "nested": (main / "scripts" / "hooks").resolve(),
        "wtA": wt_a,
        "wtB": wt_b,
        "wtC": wt_c,
        "outside": root.resolve(),
        "scratch": scratch.resolve(),
        "plain": plain,
        "otherPlain": other_plain,
        "refWave": scratch_project("ref-wave", ref(wt_b / "src" / "App" / "App.csproj")),
        "refMain": scratch_project("ref-main", ref(main / "src" / "App" / "App.csproj")),
        "transitive": scratch_project("transitive", ref(other_plain / "probe.csproj")),
        "variable": scratch_project("variable", '  <ItemGroup><ProjectReference Include="$(Root)/App.csproj" /></ItemGroup>\n'),
        "imports": scratch_project("imports", '  <Import Project="x.targets" />\n'),
        "moved": scratch_project("moved", "  <PropertyGroup><OutputPath>..\\..\\main\\bin\\</OutputPath></PropertyGroup>\n"),
        "sln": (scratch / "sln").resolve(),
        "slnRef": (scratch / "slnref").resolve(),
        "csPlain": (cs / "plain" / "walk.cs").resolve(),
        "csRef": (cs / "ref" / "walk.cs").resolve(),
        "csOut": (cs / "out" / "walk.cs").resolve(),
        "empty": (scratch / "empty").resolve(),
        "dbp": (scratch / "dbp").resolve(),
    }


def self_test() -> int:
    """Fire BOTH branches of the guard without a fleet. `python scripts/hooks/fleet_active_build.py --self-test`.

    A guard is only trustworthy once its failure branch has been observed firing — a silent guard and a broken
    guard look identical (feedback_prove_the_watchdog_fails). Three of this hook's defects were the ALLOW branch
    failing CLOSED, which no amount of green silence would ever have revealed; the 2026-09-01 tree-scoping is the
    same shape one level up (the whole guard failing closed for every parallel worktree agent), so its ALLOW
    cases are asserted here as hard as its DENY cases.
    """
    failures = 0

    def check(name: str, got, expected) -> None:
        nonlocal failures
        ok = got == expected
        failures += 0 if ok else 1
        print(f"  [{'ok' if ok else 'FAIL'}] {name}: {got!r} expected={expected!r}")

    # ── 1. The caller-identity exclusion (PB103's two modes / PB185), unchanged ────────────────────────────
    sess = "S1"
    # Real `Path`s, not `PurePath`s: `foreign_transcripts` resolves candidates, and a self-test that could not
    # reach that line would be exactly the well-formed-but-worthless probe the guard exists to prevent.
    mine = pathlib.Path(f"/p/x/{sess}/subagents/agent-aaa.jsonl")
    other = pathlib.Path(f"/p/x/{sess}/subagents/agent-bbb.jsonl")
    nested = pathlib.Path(f"/p/x/{sess}/subagents/workflows/wf_1/agent-aaa.jsonl")
    session_transcript = f"/p/x/{sess}.jsonl"

    # The exact payload shape measured 2026-08-31: a subagent's `transcript_path` is the SESSION transcript.
    subagent = {"agent_id": "aaa", "transcript_path": session_transcript}
    main_payload = {"transcript_path": session_transcript}

    for name, payload, candidates, expected in [
        ("subagent alone (PB185) -> ALLOW", subagent, [mine], 0),
        ("subagent, own file nested in workflows/ -> ALLOW", subagent, [nested], 0),
        ("subagent inside a REAL fleet -> DENY", subagent, [mine, other], 1),
        ("main session + real fleet -> DENY", main_payload, [mine, other], 2),
        ("main session, no fleet -> ALLOW", main_payload, [], 0),
        ("agent id case-folded -> ALLOW", {"agent_id": "AAA"}, [mine], 0),
        ("no identity in payload -> DENY (the one fail-closed path)", {}, [mine], 1),
    ]:
        check(f"identity: {name}", len(foreign_transcripts(payload, candidates)), expected)

    # ── 2. Where agents work and where builds write, over REAL temp trees, projects and transcripts ─────────
    with tempfile.TemporaryDirectory() as tmp:
        base = pathlib.Path(tmp)
        fx = _build_fixture(base)
        main, wt = fx["main"], fx["wtA"]

        check("tree: main checkout resolves to itself", repo_root_of(str(main)), main)
        check("tree: a nested dir walks UP to the main checkout", repo_root_of(str(fx["nested"])), main)
        check("tree: a worktree resolves to ITSELF, not to main", repo_root_of(str(wt)), wt)
        check("tree: a scratch dir outside every tree has none", repo_root_of(str(fx["plain"])), None)
        check("place: a missing directory is UNKNOWN, not its ancestor tree", place_of(wt / "gone"), None)

        # Every agent is a REAL transcript whose first record names the directory it was launched in (`launch_cwd`).
        # The names are the roles: main1/main2 work in the main checkout (a read-only fleet), aaa in a host-named
        # worktree, bbb in a rolling wave's `wf_<run>-<n>`, ccc in an operator's named worktree, nest in a
        # subdirectory of main, scr in a scratch directory outside every tree.
        launched = {
            "main1": main, "main2": main, "aaa": wt, "bbb": fx["wtB"], "ccc": fx["wtC"], "nest": fx["nested"],
            "scr": fx["plain"], "gone": wt / "removed",
        }

        def t(agent: str) -> pathlib.Path:
            """A live transcript for `agent`, filed the way the host files it."""
            p = base / "transcripts" / "subagents" / f"agent-{agent}.jsonl"
            p.parent.mkdir(parents=True, exist_ok=True)
            first = {"type": "user", "message": {"content": "go"}}
            second = {"type": "assistant", "message": {"content": [{"type": "text", "text": "x"}]}}
            if agent in launched:
                first["cwd"] = str(launched[agent])
                second["cwd"] = str(fx["outside"])  # a later record that differs: only the LAUNCH record decides
            p.write_text(json.dumps(first) + "\n" + json.dumps(second) + "\n", encoding="utf-8", newline="\n")
            return p

        check("launch: an agent's recorded launch directory", launch_cwd(t("bbb")), str(fx["wtB"]))
        check("launch: a transcript with no cwd is UNKNOWN", launch_cwd(t("nocwd")), "")
        check("launch: a missing transcript is UNKNOWN", launch_cwd(base / "no-such-agent.jsonl"), "")
        check("launch: a wf_<run>-<n> implementer works in ITS worktree", agent_place(t("bbb")), Place(fx["wtB"], True))
        check("launch: a subdirectory launch resolves to its tree", agent_place(t("nest")), Place(main, True))
        check("launch: a scratch launch is a plain directory", agent_place(t("scr")), Place(fx["plain"], False))
        check("launch: a removed launch directory is UNKNOWN", agent_place(t("gone")), None)

        def shared(site, *agents: str) -> list:
            """The agents among `agents` that share the places a build at `site` writes."""
            sharing, _ = agents_sharing_any([str(site)], [t(a) for a in agents])
            return sorted(p.stem[len("agent-"):] for p in sharing)

        fleet = ("main1", "aaa", "bbb", "ccc")
        for name, site, agents, expected in [
            # N implementer agents in N worktrees, all building at once (the case of the 2026-09-01 tree scoping).
            ("worktree caller + main-tree fleet -> ALLOW", wt, ("main1", "main2"), []),
            ("worktree caller + ANOTHER worktree's agent -> ALLOW", wt, ("bbb",), []),
            ("worktree caller + every other tree's agents -> ALLOW", wt, ("bbb", "ccc", "main1", "nest"), []),
            ("worktree caller + live agent in the SAME worktree -> DENY", wt, ("aaa",), ["aaa"]),
            ("worktree caller, same tree + a foreign tree -> DENY on the one", wt, ("aaa", "bbb"), ["aaa"]),
            ("wf_<run>-<n> caller + its own implementer -> DENY, the others ALLOW", fx["wtB"], fleet, ["bbb"]),
            ("operator-named worktree caller + its agent -> DENY, the others ALLOW", fx["wtC"], fleet, ["ccc"]),
            # The original purpose, unchanged — and the wave implementers no longer read as main-tree agents.
            ("main caller + main-tree agent -> DENY", main, ("main1",), ["main1"]),
            ("main caller (nested cwd) + main-tree agent -> DENY", fx["nested"], ("main1",), ["main1"]),
            ("main caller + an agent launched in a subdirectory of main -> DENY", main, ("nest",), ["nest"]),
            ("main caller + worktree-only agents (host, wave, operator names) -> ALLOW", main, ("aaa", "bbb", "ccc"), []),
            ("main caller + the whole fleet -> DENY on the main-tree agent only", main, fleet, ["main1"]),
            ("main caller, no fleet -> ALLOW", main, (), []),
            # UNKNOWN reverts to the session-wide rule, never to an allow.
            ("an agent whose launch directory was removed -> DENY any build", wt, ("gone",), ["gone"]),
            ("an agent whose transcript names no launch directory -> DENY any build", main, ("nocwd",), ["nocwd"]),
            ("cwd outside any repo and no project -> DENY session-wide (old rule)", fx["outside"], ("main1",), ["main1"]),
            ("empty cwd -> DENY session-wide (old rule)", "", ("main1",), ["main1"]),
        ]:
            check(f"scope: {name}", shared(site, *agents), expected)

        # kb/Work PB3770: a build OUTSIDE every working tree writes where its project graph says.
        for name, site, agents, expected in [
            ("scratch probe with no reference + the whole fleet -> ALLOW (the adjudicator's 90 minutes)", fx["plain"], fleet, []),
            ("scratch probe naming its project file -> ALLOW", fx["plain"] / "probe.csproj", fleet, []),
            ("scratch probe referencing a worktree project -> DENY on that worktree's agent only", fx["refWave"], fleet, ["bbb"]),
            ("scratch probe referencing the main project -> DENY on the main-tree agent only", fx["refMain"], fleet, ["main1"]),
            ("scratch probe referencing a scratch probe with no reference -> ALLOW", fx["transitive"], fleet, []),
            ("scratch solution listing its own project -> ALLOW", fx["sln"], fleet, []),
            ("scratch solution (slnx) listing a worktree project -> DENY on that worktree's agent", fx["slnRef"], fleet, ["aaa"]),
            ("file-based app with no reference -> ALLOW", fx["csPlain"], fleet, []),
            ("file-based app with `#:project <main>/src/App` -> DENY on the main-tree agent", fx["csRef"], fleet, ["main1"]),
            ("a scratch agent in the probe's directory -> DENY it, nobody else", fx["plain"], fleet + ("scr",), ["scr"]),
            ("a scratch agent in ANOTHER scratch directory -> ALLOW", fx["otherPlain"], fleet + ("scr",), []),
            # The graph the guard cannot read to the end stays UNKNOWN: every foreign agent denies.
            ("a `$(…)` in a ProjectReference -> UNKNOWN -> DENY all", fx["variable"], fleet, sorted(fleet)),
            ("an <Import> -> UNKNOWN -> DENY all", fx["imports"], fleet, sorted(fleet)),
            ("a moved <OutputPath> -> UNKNOWN -> DENY all", fx["moved"], fleet, sorted(fleet)),
            ("a Directory.Build.props above the project -> UNKNOWN -> DENY all", fx["dbp"], fleet, sorted(fleet)),
            ("a file-based app moving its output -> UNKNOWN -> DENY all", fx["csOut"], fleet, sorted(fleet)),
            ("a scratch dir with no project -> UNKNOWN -> DENY all", fx["empty"], fleet, sorted(fleet)),
            ("a project file that does not exist -> UNKNOWN -> DENY all", fx["plain"] / "gone.csproj", fleet, sorted(fleet)),
        ]:
            check(f"graph: {name}", shared(site, *agents), expected)

        # The transitive arm: a scratch project whose reference is a scratch project that references a worktree, so
        # the closure, not the first hop, reaches the worktree.
        far = fx["scratch"] / "far" / "probe.csproj"
        far.parent.mkdir()
        far.write_text(
            '<Project Sdk="Microsoft.NET.Sdk">\n  <ItemGroup><ProjectReference Include="'
            + str(fx["refWave"] / "probe.csproj").replace("/", chr(92))
            + '" /></ItemGroup>\n</Project>\n',
            encoding="utf-8", newline="\n",
        )
        check("graph: a scratch project referencing a scratch project that references a worktree -> DENY (transitive)",
              shared(far.parent, *fleet), ["bbb"])

        # A build is judged where it runs (kb/Work PB2599): every location change either shell accepts, and the project
        # a build names. The shapes are the operator's six false denials of 2026-10-07/08, plus the harmful direction.
        def sites(command: str, cwd, tool: str = "Bash") -> list:
            return build_sites({"cwd": str(cwd), "tool_name": tool, "tool_input": {"command": command}})

        def denies(command: str, cwd, tool: str = "Bash", fleet=("main1",)) -> int:
            return len(agents_sharing_any(sites(command, cwd, tool), [t(a) for a in fleet])[0])

        for name, command, tool, expected in [
            ("`cd <main> && dotnet build` from a worktree -> DENY on the main fleet", f'cd "{main}" && dotnet build', "Bash", 1),
            ("no location change -> the payload cwd (main) -> DENY", "dotnet build WiseOwl.Cobol.sln", "Bash", 1),
            ("PowerShell `Set-Location <wt>; dotnet build` -> ALLOW", f"Set-Location {wt}; dotnet build -c Debug", "PowerShell", 0),
            ("PowerShell `Set-Location <wt>; (Measure-Command {{ dotnet publish }})` -> ALLOW",
             f"Set-Location {wt}; (Measure-Command {{ dotnet publish x.csproj -c Release }}).TotalSeconds", "PowerShell", 0),
            ("PowerShell `Push-Location -Path <wt>` -> ALLOW", f"Push-Location -Path '{wt}'; dotnet build; Pop-Location", "PowerShell", 0),
            ("PowerShell `pwsh -WorkingDirectory <wt> -Command` -> ALLOW",
             f'pwsh -NoProfile -WorkingDirectory "{wt}" -Command "dotnet build"', "PowerShell", 0),
            ("Bash `git worktree add … && cd <wt> && dotnet build` -> ALLOW",
             f"git worktree add -q --detach x abc && cd {wt.as_posix()} && dotnet build WiseOwl.Cobol.sln", "Bash", 0),
            ("Bash `mkdir -p … && cd <wt> && dotnet build` -> ALLOW", f"mkdir -p /tmp/q && cd {wt.as_posix()} && dotnet build", "Bash", 0),
            ("Bash `ls …; cd <wt> && dotnet build` -> ALLOW", f"ls STOP; cd {wt.as_posix()} && dotnet build", "Bash", 0),
            ("Bash `pushd <wt> && dotnet build` -> ALLOW", f"pushd {wt.as_posix()} && dotnet build", "Bash", 0),
            ("Bash `(cd <wt> && dotnet build) && dotnet test` -> DENY: the test runs in main",
             f"(cd {wt.as_posix()} && dotnet build) && dotnet test", "Bash", 1),
            ("Bash `cd <wt> & dotnet build` -> DENY: a background cd does not move the shell", f"cd {wt.as_posix()} & dotnet build", "Bash", 1),
            ("a target the line does not determine -> UNKNOWN -> DENY session-wide", "cd $NOWHERE && dotnet build", "Bash", 1),
            ("from main, `dotnet build <wt>/x.sln` builds the worktree -> ALLOW",
             f"dotnet build {wt.as_posix()}/WiseOwl.Cobol.sln", "Bash", 0),
            ("`cd <scratch probe> && dotnet build` (PB3770: the adjudicator's command) -> ALLOW",
             f"cd {fx['plain'].as_posix()} && dotnet build -c Release", "Bash", 0),
            ("PowerShell `Set-Location <scratch probe>; dotnet run -c Release` -> ALLOW",
             f"Set-Location {fx['plain']}; dotnet run -c Release", "PowerShell", 0),
            ("`cd <scratch> && dotnet run walk.cs` with no reference -> ALLOW",
             f"cd {fx['csPlain'].parent.as_posix()}; timeout 600 dotnet run walk.cs", "Bash", 0),
            ("`dotnet build <scratch>/probe.csproj --no-restore` from main -> ALLOW",
             f"dotnet build {(fx['plain'] / 'probe.csproj').as_posix()} --no-restore", "Bash", 0),
        ]:
            check(f"scope: {name}", denies(command, main, tool), expected)
        # The harmful direction: from a worktree, a build of the MAIN checkout's solution must see the main fleet.
        check("scope: from a worktree, `dotnet build <main>/x.sln` -> DENY on the main fleet",
              denies(f"dotnet build {main.as_posix()}/WiseOwl.Cobol.sln", wt), 1)
        check("scope: from a worktree, `dotnet run --project <main>/x.csproj` -> DENY on the main fleet",
              denies(f"dotnet run --project {main.as_posix()}/x.csproj", wt), 1)
        check("scope: a scratch probe referencing a worktree, built from main -> DENY on that worktree's agent",
              denies(f"cd {fx['refWave'].as_posix()} && dotnet build", main, fleet=("bbb",)), 1)
        check("scope: a scratch dir outside every tree with no project stays UNKNOWN -> DENY (2026-10-07 21:41 refuter)",
              denies(f"cd {fx['outside'].as_posix()}; timeout 600 dotnet run walk.cs", main), 1)

        # PB474: a Git-Bash `cd /e/…` target must land on the worktree, not on `E:\e\…` and thence UNKNOWN.
        if os.name == "nt":
            drive, rest = os.path.splitdrive(str(wt))
            msys = "/" + drive[0].lower() + rest.replace("\\", "/")
            for spelled in (msys, "/cygdrive" + msys):
                got = sites(f"cd {spelled} && dotnet build", main)
                check(f"scope: MSYS `cd {spelled[:14]}…` resolves to the worktree (PB474)", same_path(got[0], wt), True)
                sharing, tree = agents_sharing_any(got, [t("main1")])
                check("scope: …and a main-tree fleet then does NOT deny that worktree build", (len(sharing), tree), (0, None))

        # The DENY message must name the agents AND the shared tree (it named `subagents` for every one).
        sharing, tree = agents_sharing_any([str(main)], [t("main1"), t("main2")])
        msg = deny_reason(sharing, tree)
        check("message: names the live agents", "agent-main1" in msg and "agent-main2" in msg, True)
        check("message: names the shared tree", str(main) in msg, True)

    # ── 2b. Liveness (PB1702): an agent INSIDE a long tool call writes nothing until the call returns ──────────
    with tempfile.TemporaryDirectory() as tmp:
        base = pathlib.Path(tmp)
        tool_use = {"type": "assistant", "message": {"content": [{"type": "tool_use", "id": "t1", "name": "Bash"}]}}
        tool_res = {"type": "user", "message": {"content": [{"type": "tool_result", "tool_use_id": "t1"}]}}
        ended = {"type": "assistant", "message": {"content": [{"type": "text", "text": "done"}]}}
        now = time.time()

        def transcript(name: str, records: list, age: float) -> pathlib.Path:
            p = base / f"agent-{name}.jsonl"
            p.write_text("\n".join(json.dumps(r) for r in records) + "\n", encoding="utf-8", newline="\n")
            os.utime(p, (now - age, now - age))
            return p

        for name, records, age, expected in [
            ("fresh write -> LIVE", [tool_use, tool_res], 30, True),
            # THE CASE THIS EXISTS FOR: wave 69's finishers, blocked 5+ minutes on `timeout 580 … tail -f gate.log`.
            ("in a tool call for 5 min -> LIVE", [tool_res, tool_use], 300, True),
            ("tool call answered, quiet 5 min -> not live", [tool_use, tool_res], 300, False),
            ("turn ended, quiet 5 min -> not live", [tool_use, tool_res, ended], 300, False),
            # A KILLED agent also leaves a dangling tool call: bounded, so it cannot deny forever.
            ("dangling tool call older than any tool can run -> not live", [tool_res, tool_use], 900, False),
            ("unparseable tail, quiet 5 min -> not live (old rule stands)", [], 300, False),
        ]:
            p = transcript(name.split()[0] + str(age), records, age)
            if not records:
                p.write_text("{not json\n", encoding="utf-8", newline="\n")
                os.utime(p, (now - age, now - age))
            check(f"liveness: {name}", is_live(p, now), expected)

    # ── 3. The verb matcher, both ways — `dotnet --version` must stay OUT of scope ─────────────────────────
    for cmd, expected in [
        ("dotnet build WiseOwl.Cobol.sln", True),
        ("timeout 900 dotnet test x.csproj", True),
        ("dotnet --version", False),
        ("dotnet tool list", False),
        ("git status", False),
    ]:
        check(f"verb `{cmd}`", is_build_command(cmd), expected)

    print("SELF-TEST " + ("PASS" if not failures else f"FAIL ({failures} case(s))"))
    return 1 if failures else 0


def main() -> None:
    try:
        data = json.load(sys.stdin)
    except Exception:  # noqa: BLE001
        bail()

    command = (data.get("tool_input") or {}).get("command") or ""
    if not command or not is_build_command(command):
        bail()

    session_id = data.get("session_id") or ""
    if not session_id:
        bail()

    try:
        foreign = foreign_transcripts(data, live_agent_transcripts(session_id))
        live, caller_tree = agents_sharing_any(build_sites(data), foreign)
    except Exception:  # noqa: BLE001
        bail()

    if not live:
        bail()

    json.dump(
        {
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "deny",
                "permissionDecisionReason": deny_reason(live, caller_tree),
            }
        },
        sys.stdout,
    )


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(self_test()) if "--self-test" in sys.argv else main()
