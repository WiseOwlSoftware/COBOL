#!/usr/bin/env python3
"""Self-test for own_scratch.py (kb/Work PB4473): a dispatched agent's file or shell call that names the harness's session
scratchpad is blocked in every spelling, a call that names its own scratch directory passes, and EVERY agent definition
registers the hook for the file and shell tools (so the next role added cannot forget it).
Run: python scripts/hooks/test_own_scratch.py   (every gate and CI run it through scripts/self_tests.py.)"""
import json
import pathlib
import re
import subprocess
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()

HOOK = pathlib.Path(__file__).with_name("own_scratch.py")
REPO = pathlib.Path(__file__).resolve().parents[2]
UUID = "0f9b6a03-8435-40e4-aa37-7999eefeeb81"
WIN = rf"E:\Temp\claude\E--COBOL\{UUID}\scratchpad"
GIT_BASH = f"/e/Temp/claude/E--COBOL/{UUID}/scratchpad"
LINUX = f"/tmp/claude-1000/-home-u-repo/{UUID}/scratchpad"
OWN = r"E:\COBOL-coord\scratch\mythos-final\w20261845g"

#: (tool input, cwd, blocked)
CASES = [
    ({"file_path": WIN + r"\callers.py"}, None, True),                                # Write/Edit, Windows spelling
    ({"notebook_path": WIN + r"\n.ipynb"}, None, True),                               # NotebookEdit
    ({"file_path": WIN.replace("\\", "/") + "/callers.py"}, None, True),              # forward slashes
    ({"file_path": WIN.upper() + r"\x.py"}, None, True),                              # any case
    ({"file_path": GIT_BASH + "/probe.cob"}, None, True),                             # Git Bash spelling
    ({"file_path": LINUX + "/probe.cob"}, None, True),                                # the Linux spelling
    ({"file_path": f"/e/Temp/claude/E--COBOL/{UUID}/other/../scratchpad/x.py"}, None, True),  # a `..` hides the folder
    ({"file_path": "../scratchpad/x.py"}, f"/e/Temp/claude/E--COBOL/{UUID}/other", True),  # relative to the cwd
    ({"command": f"python {WIN}\\callers.py"}, None, True),                           # Bash/PowerShell: any mention
    ({"command": f"cat > {GIT_BASH}/probe.sh <<'EOF'\nx\nEOF"}, None, True),
    ({"command": f"cp a.txt {WIN}"}, None, True),
    ({"file_path": OWN + r"\callers.py"}, None, False),                               # the agent's own directory
    ({"file_path": str(REPO / "src" / "x.cs")}, None, False),                         # the worktree
    ({"command": f"python {OWN}\\callers.py"}, None, False),
    ({"command": "echo the session scratchpad is shared"}, None, False),              # the word alone is no path
    ({"command": rf"type C:\Users\u\.claude\projects\E--COBOL\{UUID}.jsonl"}, None, False),  # a .claude directory
    ({"file_path": rf"E:\Temp\claude\E--COBOL\{UUID}\other\x.py"}, None, False),       # not the scratchpad folder
    ({"file_path": rf"E:\Temp\claude\E--COBOL\not-a-uuid\scratchpad\x.py"}, None, False),
    ({"command": ""}, None, False),
    ({}, None, False),
]


def run(stdin: str) -> tuple[int, str]:
    r = subprocess.run([sys.executable, str(HOOK)], input=stdin, capture_output=True, text=True, timeout=300)
    return r.returncode, r.stderr


fails = []
for tool_input, cwd, want in CASES:
    code, why = run(json.dumps({"tool_input": tool_input, **({"cwd": cwd} if cwd else {})}))
    if (code == 2) != want or (want and "P16" not in why):
        fails.append(f"expected {'BLOCK' if want else 'PASS'}, exit {code}: {tool_input}")
# anything unparseable passes: a hook never wedges an agent
for junk in ("", "not json", "[]", '"x"', json.dumps({"tool_input": "a string"}), json.dumps({"tool_input": {"command": 5}})):
    if run(junk)[0] != 0:
        fails.append(f"unparseable input must pass: {junk!r}")

# every agent definition registers the hook for the file and shell tools, once (the registration is the "dispatched agent,
# not the operator" scope, so a role without it is a role the harness's scratchpad note still governs)
REGISTRATION = re.compile(r'matcher:\s*"([^"]*)"\s*\n\s*hooks:\s*\n\s*-\s*type:\s*command\s*\n\s*command:\s*python "\$CLAUDE_PROJECT_DIR/scripts/hooks/own_scratch\.py"')
agents = sorted((REPO / ".claude" / "agents").glob("*.md"))
if not agents:
    fails.append("no agent definition found under .claude/agents")
for agent in agents:
    front = agent.read_text(encoding="utf-8").split("\n---\n")[0]
    found = REGISTRATION.findall(front)
    if len(found) != 1:
        fails.append(f"{agent.name}: registers own_scratch.py {len(found)} times (want once)")
    elif not {"Write", "Edit", "Bash", "PowerShell"} <= set(found[0].split("|")):
        fails.append(f"{agent.name}: the matcher {found[0]!r} leaves a file or shell tool out")

for f in fails:
    print("FAIL:", f)
total = len(CASES) + 6 + len(agents)
print(f"own_scratch self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
