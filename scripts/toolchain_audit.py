#!/usr/bin/env python3
"""toolchain_audit.py — ⛔ EVERY PLACE A TOOL IS INSTALLED OR RUN TAKES IT FROM THE ONE MANIFEST (kb/Work PB4007, PB3308,
PB1637, PB3197, PB4395; DESIGN-test-build-ci.md §1.4 and §3.14.11).

    python scripts/toolchain_audit.py              # audit the tree: exit 0 clean, 1 naming every offender
    python scripts/toolchain_audit.py --self-test  # each audit fires on every planted shape it forbids, then the tree

The owner, 2026-10-28: "CI tooling should match local tooling versions - always", then "CI and local should always be
using the latest version". toolchain.env names every tool once and scripts/toolchain.py's TOOLS is the one table of them
(its own self-test holds the manifest's shape, its cloud copy and install-tool.sh's TOOL_INSTALLABLE). This audit holds
every SITE to that table:
  THE CI JOBS: every job of every workflow runs ./.github/actions/setup-toolchain as the step right after its checkout
    (the checkout's one retry, kb/Work PB4324, is part of the checkout) and runs on one of the manifest's two images; no
    workflow or action installs a tool any other way (no `actions/setup-*` but the toolchain action's own Python step,
    which reads its version from the manifest, no package manager, no `dotnet tool install`); and every command a CI step
    runs is a manifest tool, a repository script, or the shell and the utilities of the pinned runner image, so a new tool
    in CI is red here, on the day it is added, until the manifest names it.
  THE INSTALLERS: setup-toolchain installs, and puts first on PATH, exactly what `toolchain.py ci-installs` prints; no
    workflow, action or host setup loops over a literal list of tools; the host setups install from the manifest and
    write no version of their own.
  THE .NET SDK (kb/Work PB3308): the pin is an exact SDK version global.json's `sdk.version` and `rollForward` accept;
    install-tool.sh lists its feeds builds.dotnet.microsoft.com first and no aka.ms link among them; every job that runs
    `dotnet` has the toolchain action before its first `dotnet` command; the action restores, installs, saves and proves
    the SDK; the `toolchain-cache` job saves it under main on both OSes; no workflow, action or setup script names
    `actions/setup-dotnet`, `dotnet-install` or an aka.ms link; setup-wsl.sh calls install-tool.sh, and the cloud setup
    carries install-tool.sh's install block byte for byte.
  THE INTERPRETER (kb/Work PB1637, PB3197): every bash script under scripts/ that runs Python runs "$PY", resolved by
    sourcing scripts/python-resolve.sh; every PowerShell twin of a bash script runs $PY, resolved by dot-sourcing
    scripts/python-resolve.ps1; the two resolvers probe the same candidates in the same order and accept exactly the
    manifest's PYTHON_VERSION; only a provisioner that installs the interpreter (`uv python install`) names one directly.

Why: merge-group run 40754265548 (pull request #734) went red on a self-test that was green on every local leg: CI's
Windows jobs ran the runner image's Python 3.12 while every local host ran 3.14 (kb/Work PB4007). From about 18:50Z on
2026-10-25 every code merge group failed `Setup .NET 10`, which resolved "the newest 10.0" through the aka.ms redirect,
while builds.dotnet.microsoft.com served the same file (kb/Work PB3308). A name on PATH is not an interpreter: the
Windows Store alias, a python3-only Ubuntu, a python3 below the version in the cloud image (kb/Work PB1637), and the
gate's own entry points once bypassed the resolver (kb/Work PB3197). That the manifest is at each tool's LATEST release
is the nightly workflow's (`toolchain.py latest`), since a publisher's newest release is not a fact of the tree.

⛔ WHY A SCRIPT OF ITS OWN (kb/Work PB4395 item 1): these were four C# classes in the compiler's test assembly
(ToolchainManifestDriftTests, DotnetSdkInstallDriftTests, PythonInterpreterDriftTests), so a workflow edit could only be
checked by a build. toolchain.py, the nearest owner, runs in every CI job under a sparse checkout, and this audit's
workflow reader (scripts/ci_workflow.py) would join every one of those lists; so the audit imports toolchain.py, never
the reverse.
"""
from __future__ import annotations

import dataclasses
import json
import re
import sys
from collections.abc import Iterable, Iterator
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ci_workflow  # noqa: E402  (scripts/: the one reader of a workflow's shape)
import toolchain  # noqa: E402  (scripts/: the one table of the tools)
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
SCRIPTS = REPO / "scripts"
ACTION = "./.github/actions/setup-toolchain"
ACTION_FILE = REPO / ".github" / "actions" / "setup-toolchain" / "action.yml"
INSTALLER = toolchain.INSTALLER
SETUP_WSL = SCRIPTS / "wsl" / "setup-wsl.sh"
SETUP_ENV = SCRIPTS / "cloud" / "setup-env.sh"
VERDICT = "=== TOOLCHAIN AUDIT: "


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8").replace("\r\n", "\n")


def _rel(path: Path) -> str:
    return path.relative_to(REPO).as_posix()


def workflow_files() -> list[Path]:
    return sorted((REPO / ".github" / "workflows").glob("*.yml"), key=lambda p: p.name)


def action_files() -> list[Path]:
    return sorted((REPO / ".github" / "actions").rglob("action.yml"), key=_rel)


def tool_commands(tools: Iterable[toolchain.Tool] = toolchain.TOOLS) -> frozenset[str]:
    """The commands a CI step may run a manifest tool by; the interpreter running the scripts (a row with no command)
    answers to `python` and `python3`."""
    return frozenset(c for t in tools for c in ((t.command[0],) if t.command else ("python", "python3")))


# ── THE INSTALLERS ────────────────────────────────────────────────────────────────────────────────────────────────

def action_install_offenders(action: str) -> list[str]:
    """What keeps setup-toolchain from installing, and putting first on PATH, exactly the list `toolchain.py
    ci-installs` prints."""
    out = []
    if 'tools="$(python scripts/toolchain.py ci-installs)"' not in action:
        out.append("  setup-toolchain does not read its tools from `python scripts/toolchain.py ci-installs`")
    if not re.search(r'(?m)^\s+for tool in \$tools; do bash scripts/install-tool\.sh "\$tool" "\$DIR/\$tool"; done$', action):
        out.append("  setup-toolchain's install loop is not `for tool in $tools` over install-tool.sh")
    if (not re.search(r"(?m)^\s+TOOLS: \$\{\{ steps\.install\.outputs\.tools \}\}$", action)
            or not re.search(r"(?m)^\s+for tool in \$TOOLS; do$", action)):
        out.append("  setup-toolchain's PATH step does not loop over the install step's `tools` output")
    return out


TOOL_LOOP = re.compile(r"^\s*for tool in (?P<list>[^;\n]*?)\s*(?:;|$)")


def literal_tool_loops(file: str, text: str, tools: Iterable[toolchain.Tool] = toolchain.TOOLS) -> list[str]:
    """A `for tool in …` loop over literal tool names: the list belongs to toolchain.py's TOOLS."""
    names = {t.name for t in tools}
    out = []
    for i, line in enumerate(text.replace("\r\n", "\n").split("\n"), 1):
        m = TOOL_LOOP.match(line)
        if m and any(w in names for w in m["list"].split()):
            out.append(f"  {file}:{i}: `{line.strip()}` lists tools itself")
    return out


HOST_LOOPS = {SETUP_WSL: re.compile(r'(?m)^for tool in \$\(bash "\$SCRIPTS/install-tool\.sh" --list\); do$'),
              SETUP_ENV: re.compile(r'(?m)^for tool in \$TOOL_INSTALLABLE; do install_tool "\$tool"')}
LITERAL_VERSION = re.compile(r"\b(?:install|--version)\b[^#\n]*?(?<![\w$.{-])\d+\.\d+(?:\.\d+)?\b")


def literal_versions(file: str, text: str) -> list[str]:
    """An install command in a host setup that names a version itself (`uv python install 3.14`), outside a comment:
    every version comes from toolchain.env's variables."""
    out = []
    for i, line in enumerate(text.replace("\r\n", "\n").split("\n"), 1):
        code = line.lstrip()
        if not code.startswith("#") and LITERAL_VERSION.search(code):
            out.append(f"  {file}:{i}: `{code}`")
    return out


def installer_offenders() -> list[str]:
    action = _read(ACTION_FILE)
    out = action_install_offenders(action)
    for f in [*workflow_files(), *action_files(), SETUP_WSL, SETUP_ENV]:
        out += literal_tool_loops(_rel(f), _read(f))
    for f, loop in HOST_LOOPS.items():
        if not loop.search(_read(f)):
            out.append(f"  {_rel(f)} does not install the tools install-tool.sh lists (`{loop.pattern}`)")
    for f in (SETUP_WSL, SETUP_ENV):
        out += literal_versions(_rel(f), _read(f))
    if not re.search(r'(?m)^\. "\$SCRIPTS/\.\./toolchain\.env"$', _read(SETUP_WSL)):
        out.append("  scripts/wsl/setup-wsl.sh does not source toolchain.env, so its versions come from elsewhere")
    if 'python="$(bash scripts/install-tool.sh --version python)"' not in action or not re.search(
            r"(?m)^\s+python-version:\s*\$\{\{\s*steps\.manifest\.outputs\.python\s*\}\}\s*$", action):
        out.append("  setup-toolchain's actions/setup-python step does not take its version from the manifest")
    return out


# ── THE CI JOBS ───────────────────────────────────────────────────────────────────────────────────────────────────

def _is_checkout(step: ci_workflow.Step) -> bool:
    return (step.value("uses") or "").startswith("actions/checkout@")


def setup_offenders(file: str, workflow: str) -> tuple[int, list[str]]:
    """(the jobs with steps, the jobs whose step after their checkout is not the toolchain action)."""
    out, seen = [], 0
    for job in ci_workflow.jobs(workflow):
        steps = job.steps
        if not steps:
            continue
        seen += 1
        checkout = next((i for i, s in enumerate(steps) if _is_checkout(s)), -1)
        if checkout < 0:
            out.append(f"  {file}: job `{job.id}` has no actions/checkout step, so it cannot run {ACTION}")
            continue
        after = checkout + 1
        while after < len(steps) and _is_checkout(steps[after]):
            after += 1  # the checkout's one retry (kb/Work PB4324) is part of the checkout
        if after >= len(steps) or steps[after].value("uses") != ACTION:
            out.append(f"  {file}: job `{job.id}`'s step after its checkout is not `uses: {ACTION}`")
    return seen, out


RUNS_ON = re.compile(r"(?m)^\s+runs-on:\s*(?P<v>\S.*?)\s*$")
MATRIX_OS = re.compile(r"(?m)^\s+os:\s*\[(?P<v>[^\]]*)\]\s*$")


def image_offenders(file: str, workflow: str, images: tuple[str, ...]) -> tuple[int, list[str]]:
    """(the `runs-on:` lines read, the jobs on an image the manifest does not name: a `-latest` label moves under the
    job)."""
    out, seen = [], 0
    for job in ci_workflow.jobs(workflow):
        body = job.code
        for m in RUNS_ON.finditer(body):
            seen += 1
            v = m["v"]
            if v.startswith("${{"):
                os_ = MATRIX_OS.search(body)
                listed = [x.strip() for x in os_["v"].split(",") if x.strip()] if os_ else []
                if not listed:
                    out.append(f"  {file}: job `{job.id}` runs on `{v}` with no `os:` matrix list to check")
                out += [f"  {file}: job `{job.id}`'s matrix runs on `{x}`, not one of {', '.join(images)}"
                        for x in listed if x not in images]
            elif v not in images:
                out.append(f"  {file}: job `{job.id}` runs on `{v}`, not one of {', '.join(images)}")
    return seen, out


FOREIGN_INSTALL = re.compile(
    r"(?:uses:\s*actions/setup-[\w-]+@|\bapt(?:-get)?\s+install\b|\bchoco\s+install\b|\bwinget\s+install\b"
    r"|\bbrew\s+install\b|\bdotnet\s+tool\s+install\b|\bpip\s+install\b[^\n]*\b(?:uv|semgrep)\b|\bnpm\s+install\s+-g\b)")


def foreign_installs(file: str, text: str) -> list[str]:
    """A step that installs a tool by any route but the toolchain action (whose own Python step is the one allowed)."""
    out = []
    own = file.endswith("setup-toolchain/action.yml")
    for i, line in enumerate(text.replace("\r\n", "\n").split("\n"), 1):
        code = line.lstrip()
        if code.startswith("#") or not FOREIGN_INSTALL.search(code):
            continue
        if own and code.startswith("- uses: actions/setup-python@"):
            continue
        out.append(f"  {file}:{i}: `{code}` installs a tool outside {ACTION}")
    return out


# ── the commands a CI step runs ───────────────────────────────────────────────────────────────────────────────────

#: What a CI step may run that no manifest names: the shell's keywords and builtins, and the utilities of the OS
#: release the image label pins (coreutils, grep, sed, awk, curl, tar, unzip, Git for Windows' cygpath).
SHELL_AND_IMAGE = frozenset("""
    if then else elif fi for while until do done case esac in function time exec ! { } [ [[ ]] true false :
    echo printf cd export set unset read exit return test local declare shift trap wait eval source . command type
    mapfile readarray let shopt continue break
    bash sh cat grep sed awk sort uniq head tail tr cut wc tee xargs find ls mkdir rm cp mv ln chmod touch basename
    dirname date sleep sha256sum sha512sum curl tar unzip cygpath mktemp env realpath diff comm seq timeout nproc df du
    stat uname""".split())

#: A command position: a line's start, after a separator (`; | && || ( {`, a command substitution) or after a keyword
#: that starts a command, past any NAME=value assignments.
COMMAND_AT = re.compile(r"(?:^|[;|&({]|\$\(|`|\b(?:then|do|else|elif|if|while|until|time|exec|!)\s)\s*(?:\w+=\S*\s+)*"
                        r"(?P<cmd>[A-Za-z_./][\w./+-]*)(?=$|[\s;|&)`])")
EXPRESSION = re.compile(r"\$\{\{.*?\}\}")
ARITHMETIC = re.compile(r"\$\(\((?:[^()]|\([^()]*\))*\)\)")
FUNCTION_DEFINITION = re.compile(r"(?m)^\s*(?P<name>[A-Za-z_][\w-]*)\s*\(\)\s*\{")
HEREDOC = re.compile(r"""<<-?\s*['"]?(?P<end>[A-Za-z_]+)['"]?""")
CMDLET = re.compile(r"^[A-Z][a-z]+-[A-Z]\w+$")
#: A `case` arm's patterns at a line's start (`pull_request|merge_group)`, `*)`): words, but no command.
CASE_ARM = re.compile(r"^\s*[^\s()|;]+(?:\s*\|\s*[^\s()|;]+)*\)")
RUN_KEY = re.compile(r"^(?P<at>\s*(?:- )?)run:\s*(?P<rest>.*)$")
PWSH_SHELL = re.compile(r"^\s*(?:- )?shell:\s*pwsh\s*$")


def run_blocks(text: str) -> Iterator[tuple[str, bool, list[str]]]:
    """The `run:` text of every step, with its job (an action's one step list is `(action)`) and whether its shell is
    pwsh: each a list of lines."""
    units = ([(j.id, j.steps) for j in ci_workflow.jobs(text)] if ci_workflow.has_jobs(text)
             else [("(action)", ci_workflow.steps(text))])
    for jid, steps in units:
        for step in steps:
            lines = list(step.lines)
            pwsh = any(PWSH_SHELL.match(x) for x in lines)
            for i, line in enumerate(lines):
                run = RUN_KEY.match(line)
                if not run:
                    continue
                rest = run["rest"].strip()
                if rest not in ("|", ">", "|-", ">-"):
                    yield jid, pwsh, [rest]
                    continue
                at, block = len(run["at"]), []
                for x in lines[i + 1:]:
                    t = x.lstrip()
                    if t and len(x) - len(t) <= at:
                        break
                    block.append(x)
                yield jid, pwsh, block


def unquote(text: str) -> str:
    """The shell text with its quoting read left to right, as the shell reads it: a single-quoted string is empty, a
    double-quoted one keeps only its command substitutions (each set off as commands of its own, read the same way),
    and a backslash escapes the next character (a continued line joins the next)."""
    out: list[str] = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == "\\":
            out.append(" ")
            i += 2
        elif c == "'":
            end = text.find("'", i + 1)
            i = n if end < 0 else end + 1
            out.append("''")
        elif c == '"':
            out.append('""')
            i += 1
            while i < n and text[i] != '"':
                if text[i] == "\\":
                    i += 2
                elif text[i] == "$" and i + 1 < n and text[i + 1] == "(":
                    depth, j = 0, i + 1
                    while j < n:
                        if text[j] == "(":
                            depth += 1
                        elif text[j] == ")":
                            depth -= 1
                            if depth == 0:
                                break
                        j += 1
                    out.append(" ; " + unquote(text[i + 2:min(j, n)]) + " ; ")
                    i = j + 1
                else:
                    i += 1
            i += 1
        else:
            out.append(c)
            i += 1
    return "".join(out)


def commands(lines: list[str]) -> Iterator[str]:
    """The words at the command positions of a step's run text: no comment, no heredoc body, no quoted text but a
    command substitution's, no expression; a continued line joined to the next."""
    kept, heredoc_end = [], None
    for raw in lines:  # line by line first: comments and heredoc bodies go (a heredoc's text may hold an unbalanced quote)
        line = raw.strip()
        if heredoc_end is not None:
            if line == heredoc_end:
                heredoc_end = None
            continue
        if not line or line.startswith("#"):
            continue
        h = HEREDOC.search(line)
        if h:
            heredoc_end = h["end"]
        kept.append(line)
    # Then the whole text, since a quoted string (a jq program, a message) may span lines.
    code = unquote(ARITHMETIC.sub("0", EXPRESSION.sub("x", "\n".join(kept))))
    functions = {f["name"] for f in FUNCTION_DEFINITION.finditer(code)}
    for line in code.split("\n"):
        for m in COMMAND_AT.finditer(CASE_ARM.sub("", line, count=1)):
            if m["cmd"] not in functions:
                yield m["cmd"]


def unnamed_commands(file: str, text: str, tools: frozenset[str] | None = None) -> tuple[int, list[str]]:
    """(the commands read, each one a CI step runs that is no manifest tool, repository script, shell word or image
    utility)."""
    tools = tool_commands() if tools is None else tools
    out: dict[str, None] = {}
    count = 0
    for jid, pwsh, lines in run_blocks(text):
        for cmd in commands(lines):
            count += 1
            if not (cmd in tools or cmd in SHELL_AND_IMAGE or cmd.startswith(("scripts/", "./"))
                    or (pwsh and CMDLET.match(cmd))):
                out[f"  {file}: job `{jid}` runs `{cmd}`, a tool toolchain.env does not name"] = None
    return count, list(out)


def ci_job_offenders() -> tuple[dict[str, int], list[str]]:
    """The four CI-job audits over every workflow and action: (what each read, the offenders)."""
    manifest = toolchain.load()
    images = (manifest["CI_LINUX_IMAGE"], manifest["CI_WINDOWS_IMAGE"])
    counts, out = {"jobs": 0, "runs-on": 0, "commands": 0}, []
    for f in workflow_files():
        text, rel = _read(f), _rel(f)
        n, bad = setup_offenders(rel, text)
        counts["jobs"] += n
        out += bad
        n, bad = image_offenders(rel, text, images)
        counts["runs-on"] += n
        out += bad
    for f in [*workflow_files(), *action_files()]:
        text, rel = _read(f), _rel(f)
        out += foreign_installs(rel, text)
        n, bad = unnamed_commands(rel, text)
        counts["commands"] += n
        out += bad
    return counts, out


# ── THE .NET SDK ──────────────────────────────────────────────────────────────────────────────────────────────────

SDK_VERSION = re.compile(r"^(?P<major>\d+)\.(?P<minor>\d+)\.(?P<band>\d)(?P<patch>\d{2})$")
FEEDS_LINE = re.compile(r'(?m)^DOTNET_SDK_FEEDS="(?P<f>[^"]*)"$')
INSTALL_BLOCK = ("# >>> tool-install", "# <<< tool-install")
#: The install routes that resolve "the newest SDK" through the aka.ms redirect: none may come back.
REDIRECT_ROUTES = ("actions/setup-dotnet", "dotnet-install", "aka.ms")
#: A line that runs an SDK command, at the start of a step's command or after a separator (the inert watch runs
#: `… watch -- dotnet test …`); the word alone, in prose, is not one.
DOTNET_COMMAND = re.compile(r"(?:^|[\s;&|(])dotnet\s+(?:build|test|restore|run|publish|pack|tool)\b")


def accepts(floor: str, roll_forward: str, candidate: str) -> bool:
    """Whether global.json's `floor` under `roll_forward` accepts the SDK `candidate`: the policies of the global.json
    reference (an SDK version is major.minor.FPP, F its feature band, PP its patch)."""
    f, c = SDK_VERSION.match(floor), SDK_VERSION.match(candidate)
    if not f or not c:
        return False
    fv, cv = (tuple(int(m[g]) for g in ("major", "minor", "band", "patch")) for m in (f, c))
    at_least = cv >= fv
    return {"disable": cv == fv,
            "patch": at_least and cv[:3] == fv[:3], "latestPatch": at_least and cv[:3] == fv[:3],
            "feature": at_least and cv[:2] == fv[:2], "latestFeature": at_least and cv[:2] == fv[:2],
            "minor": at_least and cv[0] == fv[0], "latestMinor": at_least and cv[0] == fv[0],
            "major": at_least, "latestMajor": at_least}.get(roll_forward, False)


def feeds(script: str) -> list[str]:
    m = FEEDS_LINE.search(script)
    return m["f"].split() if m else []


def install_block(text: str) -> str | None:
    """The install block, its two marker lines included (each compared whole), or None without both markers."""
    lf = text.replace("\r\n", "\n")
    begin, end = lf.find(INSTALL_BLOCK[0]), lf.find(INSTALL_BLOCK[1])
    if begin < 0 or end < begin:
        return None
    stop = lf.find("\n", end)
    return lf[begin:stop if stop >= 0 else len(lf)]


def _strip_item(line: str) -> str:
    t = line.strip()
    return t[2:].lstrip() if t.startswith("- ") else t


def dotnet_offenders(file: str, workflow: str) -> tuple[int, list[str]]:
    """(the jobs that run dotnet, those whose first dotnet command comes before the toolchain action, or with none)."""
    out, seen = [], 0
    for job in ci_workflow.jobs(workflow):
        code = job.code_lines
        first = next((i for i, x in enumerate(code) if DOTNET_COMMAND.search(x)), -1)
        if first < 0:
            continue
        seen += 1
        setup = next((i for i, x in enumerate(code) if _strip_item(x) == f"uses: {ACTION}"), -1)
        if setup < 0 or setup > first:
            out.append(f"  {file}: job `{job.id}` runs `{code[first].strip()}` with no `uses: {ACTION}` step before it")
    return seen, out


def redirect_offenders(file: str, lines: list[str]) -> list[str]:
    out = []
    for i, line in enumerate(lines, 1):
        code = line.lstrip()
        if code.startswith("#"):
            continue
        cut = code.find(" #")
        code = (code[:cut] if cut >= 0 else code).replace(ACTION, "")  # the repository's own action is not the raw one
        out += [f"  {file}:{i}: `{line.strip()}` names {r}" for r in REDIRECT_ROUTES if r.lower() in code.lower()]
    return out


def sdk_action_offenders(action: str, tools: Iterable[toolchain.Tool] = toolchain.TOOLS) -> list[str]:
    """What keeps setup-toolchain from restoring, installing, saving and proving the SDK. The action installs the tools
    `toolchain.py ci-installs` names, so the SDK is among them when its row of TOOLS is an install-tool row run in CI."""
    out = [f"  no `{text}`: {why}" for text, why in (
        ("actions/cache/restore@", "a cached SDK must be restored before any feed is asked"),
        ("toolchain.env | sha256sum", "the cache is keyed on the manifest's versions, the OS and the architecture"),
        ('bash scripts/install-tool.sh "$tool" "$DIR/$tool"', "the SDK is installed by the one install script"),
        ('tools="$(python scripts/toolchain.py ci-installs)"', "the action installs the tools TOOLS names for CI"),
        ("python scripts/toolchain.py check --scope ci",
         "the step proves `dotnet` (with every other tool) reports the manifest's version")) if text not in action]
    if not any(t.name == "dotnet-sdk" and t.source == toolchain.INSTALL_TOOL and t.runs == toolchain.CI for t in tools):
        out.append("  scripts/toolchain.py's dotnet-sdk row is not an install-tool row that runs in CI: the action would "
                   "not install the SDK")
    if not re.search(r"(?m)^\s+continue-on-error: true\n\s+uses: actions/cache/save@", action):
        out.append("  no `continue-on-error: true` on an `actions/cache/save@` step: the SDK is saved, and a failed save "
                   "never fails the job")
    return out


def cache_job_offenders(workflow: str) -> list[str]:
    """What keeps build-and-test.yml's `toolchain-cache` job from saving the tools under main on both OSes, a failed
    warm-up never failing main's run: a merge group's ref is new every time, so only a save under main is restored."""
    job = next((j for j in ci_workflow.jobs(workflow) if j.id == "toolchain-cache"), None)
    if job is None:
        return ["  build-and-test.yml has no `toolchain-cache` job"]
    code = job.code
    out = [f"  the toolchain-cache job has no `{s}`" for s in
           ("github.event_name == 'push'", f"uses: {ACTION}", "continue-on-error: true") if s not in code]
    if not re.search(r"os:\s*\[[^\]]*ubuntu[^\]]*windows[^\]]*\]", code):
        out.append("  the toolchain-cache job does not run on both an ubuntu and a windows image")
    return out


def sdk_offenders() -> tuple[int, list[str]]:
    """The .NET SDK's audits over the tree: (the jobs that run dotnet, the offenders)."""
    out = []
    pin = toolchain.load()["DOTNET_SDK_VERSION"]
    sdk = json.loads((REPO / "global.json").read_text(encoding="utf-8"))["sdk"]
    floor, roll = sdk["version"], sdk.get("rollForward", "patch")
    if not accepts(floor, roll, pin):
        out.append(f"  toolchain.env pins SDK {pin}, which global.json (version {floor}, rollForward {roll}) does not "
                   "accept: CI would install an SDK the build then refuses")
    installer = _read(INSTALLER)
    fs = feeds(installer)
    if len(fs) < 2 or fs[0] != "https://builds.dotnet.microsoft.com/dotnet" or not all(
            f.startswith("https://") and "aka.ms" not in f.lower() for f in fs):
        out.append(f"  scripts/install-tool.sh's DOTNET_SDK_FEEDS is {fs}: builds.dotnet.microsoft.com first, a next feed "
                   "for a refusing one, every one https and none through aka.ms")
    jobs = 0
    for f in workflow_files():
        n, bad = dotnet_offenders(_rel(f), _read(f))
        jobs += n
        out += bad
    for f in [*sorted((REPO / ".github").rglob("*.yml"), key=_rel), SETUP_WSL, SETUP_ENV, INSTALLER]:
        out += redirect_offenders(_rel(f), _read(f).split("\n"))
    out += sdk_action_offenders(_read(ACTION_FILE))
    out += cache_job_offenders(_read(REPO / ".github" / "workflows" / "build-and-test.yml"))
    if not re.search(r'(?m)^[^#\n]*bash "\$SCRIPTS/install-tool\.sh" dotnet-sdk "\$HOME/\.dotnet"', _read(SETUP_WSL)):
        out.append("  scripts/wsl/setup-wsl.sh does not install the SDK with install-tool.sh")
    script, cloud = install_block(installer), install_block(_read(SETUP_ENV))
    if script is None or script != cloud:
        out.append("  scripts/cloud/setup-env.sh's install block differs from scripts/install-tool.sh's (or one has none): "
                   "it runs before the clone, so copy the script's block, markers included, then paste setup-env.sh into "
                   "the cloud environment")
    return jobs, out


# ── THE INTERPRETER ───────────────────────────────────────────────────────────────────────────────────────────────

#: A Python interpreter named as a COMMAND: `python`, `python3` or `python3.14`, optionally quoted, where a command
#: starts — the line's start, after `; & | ( { ! \`` or `$(` (PowerShell's call operator is `&`), after a keyword, after
#: a wrapper that runs or looks up its argument (`command [-v]`, `which`, `env`, `nice [-n N]`, a `"${prefix[@]}"` array)
#: or after an environment assignment. A word inside a longer name or path, a string argument and a stand-in's
#: definition (`python3() { … }`) run nothing.
BARE_INTERPRETER = re.compile(
    r"""(?:^|[;&|({!`]|\$\(|\b(?:then|do|else|exec|time|if|while|until|command(?:\s+-v)?|which|env|nice(?:\s+-n\s*-?\d+)?)\s"""
    r"""|"\$\{\w+\[@\]\}"\s|\b\w+=\S*\s)\s*['"]?python(?:3(?:\.\d+)?)?['"]?(?=$|[\s;&|)`])""")
COMMENT = re.compile(r"(?:^|\s)#.*$")
PROVISIONER = re.compile(r"\buv python install\b")
SOURCES_BASH_RESOLVER = re.compile(r"(?:^|[\s(;])(?:\.|source)\s+.*python-resolve\.sh")
DOT_SOURCES_PWSH_RESOLVER = re.compile(r"(?:^|[\s({;])\.\s+.*python-resolve\.ps1")
USES_PY = re.compile(r"\$\{?PY\b")
BASH_CANDIDATES = re.compile(r"for __py_cand in (?P<list>.+?); do")
PWSH_CANDIDATES = re.compile(r"foreach \(\$candidate in @\((?P<list>[^)]+)\)\)")
PROBE = re.compile(r"-c '(?P<probe>[^']*platform\.python_version\(\) == sys\.argv\[1\][^']*)'")


def bare_interpreter_lines(file: str, text: str) -> list[str]:
    """Each non-comment line that names an interpreter directly, as `file:line: text`."""
    return [f"{file}:{i}: {line.strip()}" for i, line in enumerate(text.replace("\r\n", "\n").split("\n"), 1)
            if BARE_INTERPRETER.search(COMMENT.sub("", line))]


def _any_code_line(text: str, pattern: re.Pattern[str]) -> bool:
    return any(pattern.search(COMMENT.sub("", x)) for x in text.split("\n"))


def bash_interpreter(file: str, text: str) -> tuple[str, list[str]]:
    """(`provisioner`, `resolver` or `none`: what the bash script is; its offenders)."""
    if PROVISIONER.search(text):
        return "provisioner", []
    out = bare_interpreter_lines(file, text)
    if _any_code_line(text, SOURCES_BASH_RESOLVER):
        return "resolver", out
    if USES_PY.search(text):
        out.append(f"{file}: runs $PY but never sources scripts/python-resolve.sh")
    return "none", out


def pwsh_interpreter(file: str, text: str) -> list[str]:
    out = bare_interpreter_lines(file, text)
    if USES_PY.search(text) and not _any_code_line(text, DOT_SOURCES_PWSH_RESOLVER):
        out.append(f"{file}: runs $PY but never dot-sources scripts/python-resolve.ps1")
    return out


def candidates(text: str, pattern: re.Pattern[str]) -> list[str]:
    """The candidates a resolver probes, in order, with the override spelled `$PYTHON` in both shells."""
    m = pattern.search(text)
    words = re.split(r"[ ,]+", m["list"].strip()) if m else []
    return ["$PYTHON" if w.strip("'\"") in ('${PYTHON:+"$PYTHON"}', "$env:PYTHON") else w.strip("'\"")
            for w in words if w]


def resolver_offenders(bash: str, pwsh: str) -> list[str]:
    """What keeps the two resolvers from probing the same candidates for exactly the manifest's PYTHON_VERSION."""
    out = []
    b, p = candidates(bash, BASH_CANDIDATES), candidates(pwsh, PWSH_CANDIDATES)
    if b != ["$PYTHON", "python3", "python"] or b != p:
        out.append(f"  the resolvers probe {b} (bash) and {p} (PowerShell): both must probe $PYTHON, python3, python")
    bp, pp = PROBE.search(bash), PROBE.search(pwsh)
    if not bp or not pp or bp["probe"] != pp["probe"]:
        out.append("  the two resolvers' probes differ (or one has none): both run "
                   "`platform.python_version() == sys.argv[1]` on the manifest's version")
    for name, text in (("python-resolve.sh", bash), ("python-resolve.ps1", pwsh)):
        if "toolchain.env" not in text or "PYTHON_VERSION=" not in text or re.search(r"version_info\s*>=", text):
            out.append(f"  scripts/{name} does not take exactly toolchain.env's PYTHON_VERSION (a floor is not a version)")
    return out


#: The scan sees its own population: the gate's entry points and the battery source the resolver, and the two
#: environment provisioners are the only exemption. A scan that stopped seeing them would pass over nothing.
RESOLVER_USERS = ("scripts/battery.sh", "scripts/build-local.sh", "scripts/linux-gate.sh",
                  "scripts/measure-battery-determinism.sh")
PROVISIONERS = ["scripts/cloud/setup-env.sh", "scripts/wsl/setup-wsl.sh"]


def interpreter_offenders() -> tuple[int, list[str]]:
    """The interpreter's audits over every bash script and PowerShell twin under scripts/: (the scripts read, the
    offenders)."""
    out: list[str] = []
    kinds: dict[str, list[str]] = {"provisioner": [], "resolver": [], "none": []}
    shells = sorted((p for p in SCRIPTS.rglob("*.sh") if p.name != "python-resolve.sh"), key=_rel)
    for f in shells:
        kind, bad = bash_interpreter(_rel(f), _read(f))
        kinds[kind].append(_rel(f))
        out += bad
    twins = sorted((p for p in SCRIPTS.rglob("*.ps1") if p.name != "python-resolve.ps1" and p.with_suffix(".sh").exists()),
                   key=_rel)
    for f in twins:
        out += pwsh_interpreter(_rel(f), _read(f))
    missing = [u for u in RESOLVER_USERS if u not in kinds["resolver"]]
    if missing or kinds["provisioner"] != PROVISIONERS or "scripts/build-local.ps1" not in [_rel(t) for t in twins]:
        out.append(f"  the scan no longer sees its own population: resolver users missing {missing}, provisioners "
                   f"{kinds['provisioner']} (want {PROVISIONERS}), twins {[_rel(t) for t in twins]}")
    out += resolver_offenders(_read(SCRIPTS / "python-resolve.sh"), _read(SCRIPTS / "python-resolve.ps1"))
    return len(shells) + len(twins), out


# ── the audit and its self-test ───────────────────────────────────────────────────────────────────────────────────

def audit() -> tuple[int, str]:
    """(exit code, report): every audit over the tree. A reader that read nothing is a failure, never a clean tree."""
    counts, out = ci_job_offenders()
    out += installer_offenders()
    dotnet_jobs, bad = sdk_offenders()
    out += bad
    scripts, bad = interpreter_offenders()
    out += bad
    floors = {"jobs": (counts["jobs"], 10), "runs-on lines": (counts["runs-on"], 10),
              "step commands": (counts["commands"], 50), "jobs that run dotnet": (dotnet_jobs, 1),
              "scripts": (scripts, 10)}
    out += [f"  the audit read {n} {what} (at least {least} expected): the reader or the tree moved"
            for what, (n, least) in floors.items() if n < least]
    read = ", ".join(f"{n} {what}" for what, (n, _) in floors.items())
    verdict = f"{VERDICT}{'RED — ' + str(len(out)) + ' offender(s)' if out else 'CLEAN'} ({read}) ==="
    return (1 if out else 0), "\n".join([*out, verdict])


PLANTED_WORKFLOW = """jobs:
  build:
    runs-on: ubuntu-26.04
    steps:
      - uses: actions/checkout@v7
        with:
          submodules: false
      - name: Setup the toolchain
        uses: ./.github/actions/setup-toolchain
      - name: Build
        run: |
          set -euo pipefail
          n="$(gh api x --jq .n)" && echo "$n" | jq . > out.txt
          if [ -f out.txt ]; then python3 scripts/x.py --check; fi
          cat > f <<'EOF'
          anything at all
          EOF
          bash scripts/y.sh 'quoted node words' "and ( parens"
  matrix:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-26.04, windows-2025-vs2026]
    steps:
      - uses: actions/checkout@v7
      - uses: ./.github/actions/setup-toolchain
      - shell: pwsh
        run: dotnet build WiseOwl.Cobol.sln
"""

PLANTED_DOTNET = """jobs:
  build:
    runs-on: ubuntu-26.04
    steps:
      - uses: actions/checkout@v7
      - name: Setup .NET SDK
        uses: ./.github/actions/setup-toolchain
      - name: Build
        run: dotnet build WiseOwl.Cobol.sln
  docs:
    runs-on: ubuntu-26.04
    steps:
      - name: The word dotnet in prose
        run: echo "the dotnet word"

  last:
    runs-on: ubuntu-26.04
"""


def self_test() -> int:
    fails: list[str] = []

    def expect(what: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'}  {what}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            fails.append(what)

    def counted(r: tuple[int, list[str]]) -> tuple[int, int]:
        return r[0], len(r[1])

    w = PLANTED_WORKFLOW
    images = ("ubuntu-26.04", "windows-2025-vs2026")
    checkout_then_action = "      - uses: actions/checkout@v7\n      - uses: ./.github/actions/setup-toolchain\n"
    expect("every job with the toolchain action right after its checkout is clean", counted(setup_offenders("w.yml", w)),
           (2, 0))
    expect("the checkout's one retry sits between the checkout and the action", counted(setup_offenders("w.yml", w.replace(
        checkout_then_action, "      - uses: actions/checkout@v7\n      - if: steps.checkout.outcome != 'success'\n"
        "        uses: actions/checkout@v7\n      - uses: ./.github/actions/setup-toolchain\n"))), (2, 0))
    expect("a job with no toolchain action is an offender", counted(setup_offenders("w.yml", w.replace(
        "      - name: Setup the toolchain\n        uses: ./.github/actions/setup-toolchain\n", ""))), (2, 1))
    expect("a step between the checkout and the action runs on the image's tools", counted(setup_offenders("w.yml", w.replace(
        "      - uses: ./.github/actions/setup-toolchain\n",
        "      - run: python3 scripts/x.py\n      - uses: ./.github/actions/setup-toolchain\n"))), (2, 1))
    expect("a job with no checkout is an offender", counted(setup_offenders("w.yml", w.replace(
        checkout_then_action, "      - run: echo no checkout\n"))), (2, 1))

    expect("jobs on the manifest's two images are clean", counted(image_offenders("w.yml", w, images)), (2, 0))
    expect("a `-latest` label is an offender", counted(image_offenders(
        "w.yml", w.replace("runs-on: ubuntu-26.04", "runs-on: ubuntu-latest"), images)), (2, 1))
    expect("a matrix image the manifest does not name is an offender", counted(image_offenders(
        "w.yml", w.replace("windows-2025-vs2026]", "windows-latest]"), images)), (2, 1))

    expect("the toolchain action is no foreign install", foreign_installs("w.yml", w), [])
    for what, old, new in [
            ("an actions/setup-* step", "uses: ./.github/actions/setup-toolchain\n      - shell",
             "uses: actions/setup-node@v5\n      - shell"),
            ("apt-get install", "set -euo pipefail", "sudo apt-get install -y nodejs"),
            ("pip install uv", "set -euo pipefail", "python3 -m pip install uv"),
            ("dotnet tool install", "set -euo pipefail", "dotnet tool install PowerShell")]:
        expect(f"{what} is a foreign install", len(foreign_installs("w.yml", w.replace(old, new))), 1)
    expect("a pinned Python package is no tool install",
           foreign_installs("w.yml", w.replace("set -euo pipefail", "python3 -m pip install pyyaml==6.0.3")), [])
    expect("only the toolchain action's own setup-python step is allowed",
           (foreign_installs(".github/actions/setup-toolchain/action.yml", "    - uses: actions/setup-python@v7\n"),
            len(foreign_installs("a/action.yml", "    - uses: actions/setup-python@v7\n"))), ([], 1))

    n, unnamed = unnamed_commands("w.yml", w)
    expect("every command of the planted steps is named: tools, scripts, shell words, a heredoc and quoted text skipped",
           (unnamed, n >= 10), ([], True))
    expect("an unnamed tool is named, with its job",
           unnamed_commands("w.yml", w.replace("set -euo pipefail", "node scripts/x.js"))[1],
           ["  w.yml: job `build` runs `node`, a tool toolchain.env does not name"])
    expect("a tool after a pipe and one in a command substitution are read",
           (len(unnamed_commands("w.yml", w.replace('echo "$n" | jq .', 'echo "$n" | yq .'))[1]),
            len(unnamed_commands("w.yml", w.replace('n="$(gh api', 'n="$(az api'))[1])), (1, 1))
    expect("a composite action's one step list is read, a pwsh cmdlet named",
           unnamed_commands("a.yml", "runs:\n  steps:\n    - shell: pwsh\n      run: Get-ChildItem x | zz\n")[1],
           ["  a.yml: job `(action)` runs `zz`, a tool toolchain.env does not name"])
    expect("the quoting is read as the shell reads it",
           (unquote("echo 'a; b' \"x $(jq .) y\" \\\nnext"), unquote('"a \\" b"')),
           ("echo '' \"\" ; jq . ;   next", '""'))

    action = _read(ACTION_FILE)
    expect("the live action installs exactly toolchain.py ci-installs' list", action_install_offenders(action), [])
    expect("an action with a list of its own is an offender",
           (len(action_install_offenders(action.replace("python scripts/toolchain.py ci-installs", "echo dotnet-sdk jdk"))),
            len(action_install_offenders(action.replace("for tool in $TOOLS; do",
                                                        "for tool in dotnet-sdk jdk pwsh gh jq; do")))), (1, 1))
    expect("a loop over literal tool names is an offender, one over the table's list is not",
           (len(literal_tool_loops("a.yml", "        for tool in dotnet-sdk jdk pwsh gh jq; do\n")),
            len(literal_tool_loops("s.sh", "for tool in jdk pwsh; do x; done\n")),
            literal_tool_loops("s.sh", "for tool in $TOOL_INSTALLABLE; do x; done\nfor tool in $(bash x.sh --list); do\n")),
           (1, 1, []))
    expect("a host setup's literal version is an offender, the manifest's variable and a comment are not",
           (literal_versions("s.sh", 'uv python install "$PYTHON_VERSION" --default\n# uv python install 3.14 in a comment\n'),
            len(literal_versions("s.sh", "uv python install 3.14 --default\n")),
            len(literal_versions("s.sh", "dotnet tool install PowerShell --version 7.6.9\n"))), ([], 1, 1))

    expect("global.json's rollForward policies, at their edges",
           [accepts(*a) for a in [("10.0.100", "latestMinor", "10.0.403"), ("10.0.100", "latestMinor", "10.2.101"),
                                  ("10.0.100", "latestMinor", "11.0.100"), ("10.0.400", "latestMinor", "10.0.399"),
                                  ("10.0.401", "latestPatch", "10.0.403"), ("10.0.401", "patch", "10.0.500"),
                                  ("10.0.401", "latestFeature", "10.0.500"), ("10.0.401", "feature", "10.1.100"),
                                  ("10.0.100", "latestMajor", "11.0.100"), ("10.0.403", "disable", "10.0.404"),
                                  ("10.0.100", "latestMinor", "10.0.x")]],
           [True, True, False, False, True, False, True, False, True, False, False])
    d = PLANTED_DOTNET
    expect("a job whose dotnet command follows the toolchain action is clean", counted(dotnet_offenders("w.yml", d)), (1, 0))
    expect("a job with actions/setup-dotnet in its place is an offender", counted(dotnet_offenders(
        "w.yml", d.replace("uses: ./.github/actions/setup-toolchain", "uses: actions/setup-dotnet@v6"))), (1, 1))
    expect("the action after the first dotnet command is too late", counted(dotnet_offenders("w.yml", d.replace(
        "      - name: Setup .NET SDK\n        uses: ./.github/actions/setup-toolchain\n"
        "      - name: Build\n        run: dotnet build WiseOwl.Cobol.sln\n",
        "      - name: Build\n        run: dotnet build WiseOwl.Cobol.sln\n"
        "      - name: Setup .NET SDK\n        uses: ./.github/actions/setup-toolchain\n"))), (1, 1))
    expect("a `- uses:` toolchain step counts as the action (a step with no name)", counted(dotnet_offenders(
        "w.yml", d.replace("      - name: Setup .NET SDK\n        uses: ./", "      - uses: ./"))), (1, 0))
    expect("a multi-line run block's dotnet command counts, and one behind a wrapper (the inert watch)",
           (counted(dotnet_offenders("w.yml", d.replace('run: echo "the dotnet word"', "run: |\n          dotnet build p.csproj"))),
            counted(dotnet_offenders("w.yml", d.replace('run: echo "the dotnet word"',
                                                        "run: python3 scripts/inert_lane.py watch -- dotnet test p.csproj")))),
           ((2, 1), (2, 1)))
    expect("a blank line between jobs swallows neither", ([j.id for j in ci_workflow.jobs(d)],
           counted(dotnet_offenders("w.yml", d + "    steps:\n      - run: dotnet test q.csproj\n"))),
           (["build", "docs", "last"], (2, 1)))
    expect("each redirect route is named, a comment and the repository's own action are not",
           [len(redirect_offenders("a", [x])) for x in (
               "curl -fsSL https://dot.net/v1/dotnet-install.sh -o x", "        uses: actions/setup-dotnet@v6",
               "curl https://aka.ms/dotnet/10.0/dotnet-sdk-linux-x64.tar.gz",
               "# once actions/setup-dotnet resolved 10.0.x through aka.ms", "        uses: ./.github/actions/setup-toolchain")],
           [1, 1, 1, 0, 0])
    expect("the live action restores, installs, saves and proves the SDK", sdk_action_offenders(action), [])
    expect("each missing piece of the SDK's install is an offender", [len(sdk_action_offenders(action.replace(a, b))) for a, b in (
        ('bash scripts/install-tool.sh "$tool" "$DIR/$tool"', "true"),
        ("python scripts/toolchain.py ci-installs", "echo jdk"), ("actions/cache/restore@", "actions/other/restore@"),
        ("toolchain.env | sha256sum", "global.json | sha256sum"),
        ("python scripts/toolchain.py check --scope ci", "true"))], [1, 1, 1, 1, 1])
    host_sdk = [dataclasses.replace(t, runs=toolchain.HOST) if t.name == "dotnet-sdk" else t
                for t in toolchain.TOOLS]
    expect("a dotnet-sdk row that is not run in CI is an offender", len(sdk_action_offenders(action, host_sdk)), 1)
    script = ("x\n# >>> tool-install (kept)\nDOTNET_SDK_VERSION=10.0.403\nDOTNET_SDK_FEEDS=\"https://a https://b\"\n"
              "# <<< tool-install\ny\n")
    expect("the feeds and the install block are read",
           (feeds(script), install_block(script) == install_block("other\n" + script.replace("x\n", "")),
            install_block(script) == install_block(script.replace("10.0.403", "10.0.404")), install_block("no markers")),
           (["https://a", "https://b"], True, False, None))
    cache = ("jobs:\n  toolchain-cache:\n    if: github.event_name == 'push'\n    continue-on-error: true\n"
             "    strategy:\n      matrix:\n        os: [ubuntu-26.04, windows-2025-vs2026]\n    steps:\n"
             "      - uses: ./.github/actions/setup-toolchain\n")
    expect("the toolchain-cache job saves under main on both OSes, a failed warm-up never failing main",
           (cache_job_offenders(cache), len(cache_job_offenders(cache.replace("windows-2025-vs2026", "ubuntu-24.04"))),
            len(cache_job_offenders(cache.replace("continue-on-error: true\n", ""))),
            len(cache_job_offenders(cache.replace("toolchain-cache", "other")))), ([], 1, 1, 1))

    expect("a bare interpreter is named where a command starts, never inside a name, a string or a stand-in",
           [len(bare_interpreter_lines("s.sh", x)) for x in (
               "python3 x.py", "x && python -c 1", 'env FOO=1 python3 y', '"${prefix[@]}" python3 z',
               "nice -n 10 python3 x", "VAR=1 python3 x", "source scripts/python-resolve.sh",
               'check "python" "$got"', "python3() { return 1; }", "  # python3 in a comment",
               '"$PY" scripts/x.py')], [1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0])
    expect("a bash script that runs $PY sources the resolver, a provisioner is exempt",
           (bash_interpreter("a.sh", '. "$here/python-resolve.sh"\n"$PY" x.py\n'),
            bash_interpreter("b.sh", '"$PY" x.py\n')[1],
            bash_interpreter("c.sh", "uv python install 3.14\npython3 x\n")),
           (("resolver", []), ["b.sh: runs $PY but never sources scripts/python-resolve.sh"], ("provisioner", [])))
    expect("a PowerShell twin that runs $PY dot-sources the resolver",
           (pwsh_interpreter("a.ps1", ". $PSScriptRoot/python-resolve.ps1\n& $PY x.py\n"), pwsh_interpreter("b.ps1", "& $PY x\n")),
           ([], ["b.ps1: runs $PY but never dot-sources scripts/python-resolve.ps1"]))
    bash, pwsh = _read(SCRIPTS / "python-resolve.sh"), _read(SCRIPTS / "python-resolve.ps1")
    expect("the two live resolvers probe the same candidates for exactly the manifest's version",
           (candidates(bash, BASH_CANDIDATES), resolver_offenders(bash, pwsh)), (["$PYTHON", "python3", "python"], []))
    expect("a resolver that drops a candidate, or that probes a floor, is an offender",
           (len(resolver_offenders(bash, pwsh.replace("'python3', ", ""))),
            len(resolver_offenders(bash.replace("PYTHON_VERSION=", "PYTHON_FLOOR="), pwsh))), (1, 1))

    code, report = audit()
    print(report)
    expect("the tree is clean", code, 0)
    print(f"=== TOOLCHAIN AUDIT SELF-TEST: {'PASS' if not fails else f'FAIL — {len(fails)}'} ===")
    return 1 if fails else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    if argv:
        print(__doc__.split("\n\n")[0] if __doc__ else "", file=sys.stderr)
        return 2
    code, report = audit()
    print(report)
    return code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
