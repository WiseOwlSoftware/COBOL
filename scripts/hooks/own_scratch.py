#!/usr/bin/env python3
"""PreToolUse hook of every DISPATCHED agent role (.claude/agents/*.md) — BLOCK a Write/Edit/NotebookEdit target, or a
Bash/PowerShell command, that names the harness's session scratchpad (kb/Work PB4473; MANDATORY-PRACTICES P16, PB4466).

The session scratchpad is the one folder every agent of a session shares, and the harness's own system prompt tells each
agent to use it: two agents of wave 20261834 each wrote a `callers.py` there, and one ran the other's copy against its own
worktree. P16 and the dispatch spec's `Scratch:` line override the harness by instruction, and an instruction is forgotten
(the owner's reason for readonly_repo.py and dispatch_guard.py before this). This hook is the structural half: a
dispatched agent's file and shell calls never name that folder, so its files can only go to its own scratch directory.

WHO IT BINDS. It is registered in the frontmatter of each agent definition, as readonly_repo.py is for the read-only
roles, so it runs for an agent started under that role (a subagent, a workflow agent, a `claude -p --agent` session of the
review lane) and NEVER for the operator's own session, which has no agent definition and whom the harness's note is
written for. The hook therefore needs no test of "dispatched or not".

WHAT IT RECOGNISES, by shape and not by one session's id: `<...>/claude<...>/<project slug>/<session uuid>/scratchpad`,
the Windows (`E:\\Temp\\claude\\E--COBOL\\<uuid>\\scratchpad`), the Git Bash and the Linux (`/tmp/claude-1000/-p/<uuid>/...`)
spellings alike, in any case and with either slash. A shell command is refused for any mention (an agent has no use for the
folder, reading another agent's file in it is the harm P16 names, and a write cannot be told from a read in a command line).
Anything unparseable passes: a hook never wedges an agent, and a refusal is re-issued with the agent's own directory.
"""
import json
import os
import pathlib
import re
import sys

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: The folder's shape: a `claude*` directory, the project's slug, the session's uuid, then `scratchpad`.
SESSION_FOLDER = re.compile(
    r"(?:^|[\\/])claude[^\\/]*[\\/]+[^\\/]+[\\/]+[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}[\\/]+scratchpad(?![\w-])",
    re.IGNORECASE)
#: The tool inputs that carry a path to write, and the one that carries a command line.
PATH_FIELDS = ("file_path", "notebook_path")


def names_session_folder(text: str) -> bool:
    return bool(SESSION_FOLDER.search(text))


def refusal(data: dict) -> str | None:
    """Why the call is refused, or None when it passes."""
    args = data.get("tool_input") or {}
    for field in PATH_FIELDS:
        target = args.get(field)
        if isinstance(target, str) and target:
            path = pathlib.Path(target)
            if not path.is_absolute():
                path = pathlib.Path(data.get("cwd") or ".") / path
            if names_session_folder(target) or names_session_folder(os.path.normpath(str(path))):
                return target
    command = args.get("command")
    if isinstance(command, str) and names_session_folder(command):
        return command if len(command) <= 120 else command[:120] + "..."
    return None


def main() -> int:
    try:
        data = json.load(sys.stdin)
        if not isinstance(data, dict):
            return 0
        named = refusal(data)
    except Exception:  # noqa: BLE001 — unparseable: pass, a hook never wedges an agent
        return 0
    if named is None:
        return 0
    sys.stderr.write("BLOCKED by scripts/hooks/own_scratch.py: this call names the harness's session scratchpad "
                     f"({named}), the one folder every agent of the session shares. Your files go in YOUR OWN scratch "
                     "directory, the `Scratch:` line of your dispatch spec or the directory your prompt names, and nowhere "
                     "else (MANDATORY-PRACTICES P16, kb/Work PB4466, PB4473): create it, and re-issue the call there.\n")
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
