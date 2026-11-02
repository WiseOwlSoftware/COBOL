#!/usr/bin/env python3
"""test_linux_gate.py — ⛔ THE LINUX REPRODUCTION TOOL RUNS WHAT CI'S LINUX JOBS RUN, AND A CI TEST STEP LEAVES ITS HARNESS
LOG WHERE IT CAN BE READ (kb/Work PB1732, PB1955, PB2880, PB4327, PB3108, PB4395).

    python scripts/test_linux_gate.py     # each audit fires on every planted shape it forbids, then the real tree

scripts/linux-gate.sh is the tool that reproduces a Linux CI red, run by hand under WSL (DESIGN-test-build-ci.md section
3.15). Its own `--self-test` measures its behaviour (the host hand-off, the footprint, one gate per tree); this test holds
the FACTS of its text against the workflows, which a shell self-test cannot read without a second workflow reader:
  THE LEGS (kb/Work PB1732, PB1955): every test project a Linux job of build-and-test.yml runs with `dotnet test`, and
    every repository script a Linux job of build-and-test.yml or nightly.yml runs as `run: bash scripts/<x>.sh`, is a
    leg of linux-gate.sh, the guard leg among its DEFAULT legs. Train 71b's first push went red in CI's Linux unit job on
    a Windows path literal that was green on Windows, and CI's guard job ran the one script no local gate ran (train
    1013), so a Linux job that gains a test the tool does not know is red here until it learns it.
  THE TREE'S OWN GIT (kb/Work PB2880): every git call of linux-gate.sh that names the tree goes through its one `wgit`,
    defined once per tree kind, each arm passing `--no-optional-locks`, a Windows drive read by Windows git (git.exe):
    Linux git reading a Windows tree through /mnt re-hashed every tracked file across 9P and wrote the index back.
  NO GATE STARTS IT (kb/Work PB4327, owner 2026-10-31): the per-change gate runs nothing on Linux, so no gate script
    (gate.py, enqueue.py, land_lane.py, build-local.{ps1,sh}) starts linux-gate.sh, the driver has no Linux leg and no
    `--plan-leg`, and linux-gate.sh has no mode but its legs.
  THE HARNESS LOG (kb/Work PB3108; DESIGN-test-build-ci.md §3.10): every `dotnet test` step of build-and-test.yml sets
    WISEOWL_COBOL_HARNESS_LOG to `harness-observations.log` in its own `--results-directory`, and its job uploads that
    file (the directory, or the file by name) through ./.github/actions/upload-results, so a CI retry or non-observation
    can be read after the run (on a hosted runner the temp directory's file was gone with the runner).

⛔ WHY HERE (kb/Work PB4395 item 1): these were two C# classes in the compiler's test assembly (LinuxGateDriftTests,
CiHarnessLogDriftTests), so a workflow or gate-script edit could only be checked by a build.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ci_workflow  # noqa: E402  (scripts/: the one reader of a workflow's shape)
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
SCRIPTS = REPO / "scripts"
WORKFLOWS = REPO / ".github" / "workflows"
GATE = SCRIPTS / "linux-gate.sh"
UPLOAD_ACTION = "./.github/actions/upload-results"
LOG_NAME = "harness-observations.log"
#: The scripts that start a gate or a landing: none may start the Linux tool.
CALLERS = ("gate.py", "orchestrator/enqueue.py", "orchestrator/land_lane.py", "build-local.ps1", "build-local.sh")


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8").replace("\r\n", "\n")


def _code(text: str) -> list[str]:
    return [x for x in text.split("\n") if not x.lstrip().startswith("#")]


def linux_jobs(workflow: str) -> list[str]:
    """The code (no comment line) of each job of `workflow` that runs on an Ubuntu image."""
    return [j.code for j in ci_workflow.jobs(workflow) if (j.value("runs-on") or "").startswith("ubuntu")]


# ── THE LEGS ──────────────────────────────────────────────────────────────────────────────────────────────────────

#: A `dotnet test` run, and the test project it names (written with `\b` first, so test_population.py's caller audit
#: never reads the pattern as an unscrubbed instruction).
DOTNET_TEST = re.compile(r"\bdotnet test ")
DOTNET_TEST_PROJECT = re.compile(r"\bdotnet test (tests/\S+\.csproj)")


def project_offenders(workflow: str, gate: str) -> tuple[int, list[str]]:
    """(the test projects CI's Linux jobs run, those linux-gate.sh has no leg for)."""
    ran = sorted({m[1] for job in linux_jobs(workflow) for m in DOTNET_TEST_PROJECT.finditer(job)})
    legs = set(re.findall(r'proj="(tests/[^"]+\.csproj)"', gate))
    return len(ran), [f"  CI's Linux jobs run {p}, which scripts/linux-gate.sh has no leg for" for p in ran if p not in legs]


def script_offenders(workflows: list[str], gate: str) -> tuple[list[str], list[str]]:
    """(the scripts CI's Linux jobs run as `run: bash scripts/<x>.sh`, the offenders: one linux-gate.sh never runs, or a
    guard leg outside its default legs, which no gate run reaches)."""
    ran = sorted({m[1] for w in workflows for job in linux_jobs(w)
                  for m in re.finditer(r"(?m)^\s+run:\s*bash (scripts/\S+\.sh)\s*$", job)})
    out = [f"  CI's Linux jobs run {s}, which scripts/linux-gate.sh never runs" for s in ran
           if not re.search(r"(?m)^[^#\n]*\bbash " + re.escape(s) + r"\b", gate)]
    defaults = re.search(r'(?m)^legs="([^"]+)"', gate)
    if not defaults:
        out.append('  scripts/linux-gate.sh no longer declares its default legs as legs="…"')
    elif "guard" not in defaults[1].split(","):
        out.append(f"  the guard leg is not among linux-gate.sh's default legs ({defaults[1]})")
    return ran, out


# ── THE TREE'S OWN GIT ────────────────────────────────────────────────────────────────────────────────────────────

def wgit_offenders(gate: str) -> list[str]:
    code = _code(gate)
    arms = [x for x in code if re.match(r"^\s*wgit\(\)\s*\{", x)]
    out = []
    if len(arms) != 2:
        out.append(f"  scripts/linux-gate.sh defines wgit {len(arms)} time(s), not once per tree kind (a Windows drive, a "
                   "Linux filesystem)")
    out += [f"  a wgit arm without --no-optional-locks: `{a.strip()}`" for a in arms if "--no-optional-locks" not in a]
    if not any(re.search(r"\bgit\.exe\b", a) and "$winroot" in a for a in arms):
        out.append("  no wgit arm reads a Windows drive's tree with git.exe")
    # A git command word outside an echoed message (a message that NAMES git runs none), on a line naming the tree.
    out += [f"  scripts/linux-gate.sh runs git on the tree outside wgit: `{x.strip()}`" for x in code
            if x not in arms and re.search(r"(^|[\s;(|&])git(\.exe)?\s", re.sub(r'\becho\s+"[^"]*"', "", x))
            and re.search(r"\$\{?(tree|winroot)\b", x)]
    return out


# ── NO GATE STARTS IT ─────────────────────────────────────────────────────────────────────────────────────────────

def caller_offenders(callers: dict[str, str], driver: str, gate: str) -> list[str]:
    out = []
    for name, text in callers.items():
        code = "\n".join(_code(text))
        out += [f"  scripts/{name} starts the Linux tool (`{s}`)" for s in ('"scripts/linux-gate.sh"', "bash scripts/linux-gate.sh")
                if s in code]
    out += [f"  scripts/gate.py names `{s}`: the per-change gate runs nothing on Linux" for s in
            ("--plan-leg", "class LinuxLeg", "runs_linux_leg") if s in driver]
    out += [f"  scripts/linux-gate.sh has the mode `{m}`: it has no mode but its legs" for m in
            ("--plan)", "--plan-legs)", "--probe)", "targeted|inert)", "--plan-leg ") if m in gate]
    return out


# ── THE HARNESS LOG ───────────────────────────────────────────────────────────────────────────────────────────────

#: A path as the workflow spells it: a run of non-blank characters and `${{ … }}` expressions, which hold blanks.
WORKFLOW_PATH = r"((?:\$\{\{[^}]*\}\}|[^\s$])+)"
RESULTS_DIRECTORY = re.compile(r"--results-directory\s+" + WORKFLOW_PATH)
HARNESS_LOG = re.compile(r"^\s*WISEOWL_COBOL_HARNESS_LOG:\s*\$\{\{\s*github\.workspace\s*\}\}/" + WORKFLOW_PATH + r"\s*$")


def upload_paths(job: ci_workflow.Job) -> list[str]:
    """The upload paths of a job's upload steps: the `path:` value, or each line of a `path: |` block."""
    out = []
    for step in job.steps:
        if UPLOAD_ACTION not in (step.value("uses") or ""):
            continue
        lines = step.code_lines
        at = next((i for i, x in enumerate(lines) if x.lstrip().startswith("path:")), -1)
        if at < 0:
            continue
        value = lines[at].lstrip()[len("path:"):].strip()
        if value != "|":
            out.append(value)
            continue
        deep = ci_workflow.indent(lines[at])
        for x in lines[at + 1:]:
            if ci_workflow.indent(x) <= deep:
                break
            out.append(x.strip())
    return out


def harness_log_offenders(workflow: str) -> tuple[int, list[str]]:
    """(the `dotnet test` steps, those whose harness log is not written into their results directory or not
    uploaded)."""
    seen, out = 0, []
    for job in ci_workflow.jobs(workflow):
        uploaded = upload_paths(job)
        for step in job.steps:
            code = step.code_lines
            if not any(DOTNET_TEST.search(x) for x in code):
                continue
            seen += 1
            name = step.lines[0].strip()
            results = next((m[1] for m in map(RESULTS_DIRECTORY.search, code) if m), None)
            log = next((m[1] for m in map(HARNESS_LOG.search, code) if m), None)
            if results is None:
                out.append(f"  {name}: names no --results-directory")
            elif log is None:
                out.append(f"  {name}: sets no WISEOWL_COBOL_HARNESS_LOG")
            elif log != f"{results}/{LOG_NAME}":
                out.append(f"  {name}: WISEOWL_COBOL_HARNESS_LOG is {log}, not {results}/{LOG_NAME}")
            elif f"{results}/" not in uploaded and f"{results}/{LOG_NAME}" not in uploaded:
                out.append(f"  {name}: its job uploads neither {results}/ nor {results}/{LOG_NAME}")
    return seen, out


# ── the test ──────────────────────────────────────────────────────────────────────────────────────────────────────

PLANTED_GATE = """legs="unit,characterization,conformance,guard"
if [ -n "$winroot" ]; then
    wgit() { git.exe --no-optional-locks -C "$winroot" "$@"; }
else
    wgit() { git -c safe.directory='*' --no-optional-locks -C "$tree" "$@"; }
fi
# git -C "$tree" status   (a comment runs nothing)
echo "git status of $tree is read through wgit" && wgit status
    unit)  proj="tests/U/U.csproj" ;;
( bash scripts/guard-fast.sh )
"""

PLANTED_WORKFLOW = """jobs:
  linux:
    runs-on: ubuntu-26.04
    steps:
      - name: Unit
        run: dotnet test tests/U/U.csproj --no-build
      - name: Guard
        run: bash scripts/guard-fast.sh
  windows:
    runs-on: windows-2025-vs2026
    steps:
      - run: dotnet test tests/W/W.csproj
"""

PLANTED_LOG = """jobs:
  a:
    steps:
      - name: tests
        env:
          WISEOWL_COBOL_HARNESS_LOG: ${{ github.workspace }}/TestResults/x/harness-observations.log
        run: dotnet test p.csproj --results-directory TestResults/x
      - name: Upload
        uses: ./.github/actions/upload-results
        with:
          path: |
            TestResults/x/*.trx
            TestResults/x/harness-observations.log
"""


def main() -> int:
    fails: list[str] = []

    def expect(what: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'}  {what}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            fails.append(what)

    def n(r) -> tuple[int, int]:
        return (len(r[0]) if isinstance(r[0], list) else r[0]), len(r[1])

    g, w = PLANTED_GATE, PLANTED_WORKFLOW
    expect("a Linux job's test project with a leg is clean, a Windows job's is not read", n(project_offenders(w, g)), (1, 0))
    expect("a Linux job's test project with no leg is an offender",
           n(project_offenders(w.replace("tests/U/U.csproj", "tests/V/V.csproj"), g)), (1, 1))
    expect("a Linux job's script the gate runs is clean", n(script_offenders([w], g)), (1, 0))
    expect("a Linux job's script the gate never runs (or runs only in a comment) is an offender",
           (n(script_offenders([w], g.replace("( bash scripts/guard-fast.sh )", "# bash scripts/guard-fast.sh"))),
            n(script_offenders([w.replace("guard-fast.sh", "guard-slow.sh")], g))), ((1, 1), (1, 1)))
    expect("a guard leg outside the default legs, or no default legs, is an offender",
           (n(script_offenders([w], g.replace(",guard\"", "\""))), n(script_offenders([w], g.replace("legs=", "LEGS=")))),
           ((1, 1), (1, 1)))
    expect("two wgit arms with the flag, the Windows one git.exe's, and the tree read only through them", wgit_offenders(g), [])
    expect("an arm without the flag, a single arm, and a git call on the tree outside wgit are each an offender",
           [len(wgit_offenders(x)) for x in (
               g.replace('git.exe --no-optional-locks -C "$winroot"', 'git.exe -C "$winroot"'),
               g.replace("    wgit() { git -c safe.directory='*' --no-optional-locks -C \"$tree\" \"$@\"; }\n", ""),
               g + 'git -C "$tree" status\n')], [1, 1, 1])
    callers = {"gate.py": "# bash scripts/linux-gate.sh in a comment\nrun(['x'])\n"}
    expect("no caller, no Linux leg in the driver and no mode is clean", caller_offenders(callers, "", g), [])
    expect("a caller that starts the tool, a Linux leg in the driver and a mode are each an offender",
           [len(caller_offenders(*a)) for a in (
               ({"build-local.sh": "bash scripts/linux-gate.sh --legs unit\n"}, "", g),
               ({"gate.py": 'subprocess.run(["bash", "scripts/linux-gate.sh"])\n'}, "", g),
               (callers, "class LinuxLeg:\n", g), (callers, "", g + "  --probe) probe ;;\n"))], [1, 1, 1, 1])

    log = PLANTED_LOG
    expect("a test step that writes its log into its results directory, which its job uploads, is clean",
           harness_log_offenders(log), (1, []))
    expect("a step with no log, a log in another directory and a log not uploaded are each an offender",
           [n(harness_log_offenders(x)) for x in (
               log.replace("WISEOWL_COBOL_HARNESS_LOG: ${{ github.workspace }}/TestResults/x/harness-observations.log",
                           "OTHER: x"),
               log.replace("/TestResults/x/harness-observations.log", "/TestResults/y/harness-observations.log"),
               log.replace("\n            TestResults/x/harness-observations.log", ""))], [(1, 1), (1, 1), (1, 1)])
    expect("an upload of the whole results directory carries the log", harness_log_offenders(log.replace(
        "path: |\n            TestResults/x/*.trx\n            TestResults/x/harness-observations.log",
        "path: TestResults/x/")), (1, []))
    expect("a step with no results directory is an offender",
           n(harness_log_offenders(log.replace(" --results-directory TestResults/x", ""))), (1, 1))

    # The real tree. A reader that read nothing is a failure, never a clean tree.
    gate = _read(GATE)
    build, nightly = _read(WORKFLOWS / "build-and-test.yml"), _read(WORKFLOWS / "nightly.yml")
    projects, bad = project_offenders(build, gate)
    expect("every test project CI's Linux jobs run is a leg of linux-gate.sh", (projects > 0, bad), (True, []))
    scripts, bad = script_offenders([build, nightly], gate)
    expect("every script CI's Linux jobs run is run by linux-gate.sh, the guard among its default legs",
           ("scripts/guard-fast.sh" in scripts, bad), (True, []))
    expect("linux-gate.sh reads the tree with the tree's own git and never refreshes its index", wgit_offenders(gate), [])
    expect("no gate script starts linux-gate.sh, and it has no mode but its legs", caller_offenders(
        {c: _read(SCRIPTS / c) for c in CALLERS}, _read(SCRIPTS / "gate.py"), gate), [])
    steps, bad = harness_log_offenders(build)
    expect("every CI test step writes the harness log where its job uploads it", (steps > 0, bad), (True, []))
    print(f"=== LINUX GATE FACTS: {'PASS' if not fails else f'FAIL — {len(fails)}'} "
          f"({projects} Linux test project(s), {len(scripts)} Linux script(s), {steps} CI test step(s)) ===")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
