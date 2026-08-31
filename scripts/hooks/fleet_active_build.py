#!/usr/bin/env python3
"""PreToolUse hook — REFUSE to build or test while a subagent fleet is live.

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
  2. Once a fleet IS running, the tree is FROZEN. Build/test only after the completion signal, or stop the fleet.

Detection is by TRANSCRIPT MTIME, which is the one signal that cannot be faked by a stale process: a live agent
writes to its own `agent-*.jsonl` continuously, so a file touched within the window means an agent is thinking
right now. Keyed on this session's id, so another project's fleet never blocks this one.

⚠ THE CALLER IS NEVER A FLEET. The question the guard asks is "is any transcript OTHER THAN MINE live?", and the
caller's own transcript is always fresh at PreToolUse — it is being written by this very tool call. Getting that
exclusion wrong makes the guard fail CLOSED, which it has now done three separate ways (PB101 worktree agents,
PB103 main-tree agents, PB185 every subagent). The exclusion is therefore keyed on the caller's IDENTITY, not on
a path string: see `caller_identity` / `foreign_transcripts`.

FAIL-OPEN BY CONSTRUCTION. Any error, any missing path, any unreadable directory ⇒ exit 0 and allow the build. A
guard that blocks the build because IT broke would be worse than the defect it prevents.

Both branches are provable without a fleet: `python scripts/hooks/fleet_active_build.py --self-test` drives the
pure decision function through DENY and ALLOW alike (feedback_prove_the_watchdog_fails — a monitor is a gate;
fire its failure branch once before trusting its silence).
"""
import json
import os
import pathlib
import sys
import time

WINDOW_SECONDS = 120

# `dotnet clean/publish/run` rewrite or delete the same outputs a live agent is executing, so they are in scope
# too. `dotnet --version`, `dotnet tool`, `dotnet nuget` etc. are not.
BUILD_VERBS = ("build", "test", "clean", "publish", "run", "msbuild")


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


def live_agent_transcripts(session_id: str) -> list:
    cfg = os.environ.get("CLAUDE_CONFIG_DIR")
    root = pathlib.Path(cfg) if cfg else pathlib.Path.home() / ".claude"
    projects = root / "projects"
    if not projects.is_dir():
        return []

    cutoff = time.time() - WINDOW_SECONDS
    live = []
    # ~/.claude/projects/<sanitized-cwd>/<session-id>/subagents/**/agent-*.jsonl
    # Globbing on the session id rather than deriving the sanitized cwd keeps this correct whatever the
    # sanitization rule is, and scopes the guard to THIS session.
    for path in projects.glob(f"*/{session_id}/subagents/**/agent-*.jsonl"):
        try:
            if path.stat().st_mtime >= cutoff:
                live.append(path)
        except OSError:
            continue
    return live


def deny_reason(live: list) -> str:
    names = sorted({p.parent.name for p in live})
    detail = ", ".join(names[:4]) + (f" (+{len(names) - 4} more)" if len(names) > 4 else "")
    return (
        f"BLOCKED — {len(live)} subagent transcript(s) were written in the last {WINDOW_SECONDS}s, so a "
        f"fleet is LIVE: {detail}. Building or testing now changes the binary those agents are probing, "
        f"which is what made a 60-agent run unusable on 2026-08-04 (PB15). "
        f"Do ONE of: (a) wait for the completion notification, then build; "
        f"(b) stop the fleet with TaskStop if its results are no longer worth having; "
        f"(c) if you are about to fix what it measures, stop it now — you already have the answer, and a "
        f"fleet measuring a tree you are editing produces verdicts you will have to disclaim. "
        f"Meanwhile, doc / DEVLOG / design-doc / kb-note work is safe."
    )


def self_test() -> int:
    """Fire BOTH branches of the guard without a fleet. `python scripts/hooks/fleet_active_build.py --self-test`.

    A guard is only trustworthy once its failure branch has been observed firing — a silent guard and a broken
    guard look identical (feedback_prove_the_watchdog_fails). Three of this hook's defects were the ALLOW branch
    failing CLOSED, which no amount of green silence would ever have revealed.
    """
    sess = "S1"
    # Real `Path`s, not `PurePath`s: `foreign_transcripts` resolves candidates, and a self-test that could not
    # reach that line would be exactly the well-formed-but-worthless probe the guard exists to prevent.
    mine = pathlib.Path(f"/p/x/{sess}/subagents/agent-aaa.jsonl")
    other = pathlib.Path(f"/p/x/{sess}/subagents/agent-bbb.jsonl")
    nested = pathlib.Path(f"/p/x/{sess}/subagents/workflows/wf_1/agent-aaa.jsonl")
    session_transcript = f"/p/x/{sess}.jsonl"

    # The exact payload shape measured 2026-08-31: a subagent's `transcript_path` is the SESSION transcript.
    subagent = {"agent_id": "aaa", "transcript_path": session_transcript}
    main = {"transcript_path": session_transcript}

    cases = [
        # name,                                        payload,               candidates,     expected foreign
        ("subagent alone (PB185) -> ALLOW", subagent, [mine], 0),
        ("subagent, own file nested in workflows/ -> ALLOW", subagent, [nested], 0),
        ("subagent inside a REAL fleet -> DENY", subagent, [mine, other], 1),
        ("main session + real fleet -> DENY", main, [mine, other], 2),
        ("main session, no fleet -> ALLOW", main, [], 0),
        ("agent id case-folded -> ALLOW", {"agent_id": "AAA"}, [mine], 0),
        ("no identity in payload -> DENY (the one fail-closed path)", {}, [mine], 1),
    ]

    failures = 0
    for name, payload, candidates, expected in cases:
        got = len(foreign_transcripts(payload, candidates))
        ok = got == expected
        failures += 0 if ok else 1
        print(f"  [{'ok' if ok else 'FAIL'}] {name}: foreign={got} expected={expected}")

    # The verb matcher, both ways — `dotnet --version` must stay OUT of scope or the guard blocks a bare probe.
    verb_cases = [
        ("dotnet build CobolSharp.sln", True),
        ("timeout 900 dotnet test x.csproj", True),
        ("dotnet --version", False),
        ("dotnet tool list", False),
        ("git status", False),
    ]
    for cmd, expected in verb_cases:
        got = is_build_command(cmd)
        ok = got == expected
        failures += 0 if ok else 1
        print(f"  [{'ok' if ok else 'FAIL'}] verb `{cmd}`: {got} expected={expected}")

    print("SELF-TEST " + ("PASS" if not failures else f"FAIL ({failures} case(s))"))
    return 1 if failures else 0


if __name__ == "__main__" and "--self-test" in sys.argv:
    sys.exit(self_test())

try:
    data = json.load(sys.stdin)
except Exception:  # noqa: BLE001
    bail()

command = (data.get("tool_input") or {}).get("command") or ""
if not command or not is_build_command(command):
    bail()

# An agent building INSIDE ITS OWN git worktree (`.claude/worktrees/<agent>/`, the Agent tool's `isolation:
# "worktree"`) has private bin/obj and its own checkout: nothing it builds is the binary another agent probes, and
# nothing else builds what it probes. The guard's harm model does not apply there — and applied literally it is a
# SELF-BLOCK (the agent's own transcript is one of the "live" ones, so the window never clears; found 2026-08-18 by
# the normalization-subsystem agent, kb/Work PB101). Recognized by the tool's cwd or a `cd` in the command itself.
cwd = (data.get("cwd") or "").replace("\\", "/")
if "/.claude/worktrees/" in cwd + "/" or "/.claude/worktrees/" in command.replace("\\", "/"):
    bail()

session_id = data.get("session_id") or ""
if not session_id:
    bail()

try:
    live = foreign_transcripts(data, live_agent_transcripts(session_id))
except Exception:  # noqa: BLE001
    bail()

if not live:
    bail()

json.dump(
    {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": deny_reason(live),
        }
    },
    sys.stdout,
)
