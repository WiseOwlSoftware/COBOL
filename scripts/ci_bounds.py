#!/usr/bin/env python3
"""ci_bounds.py — ⛔ CI IS BOUNDED, AND ITS CACHES ARE SAVED FROM MAIN ONLY (kb/Work PB4227, PB4324, PB4348, PB4395;
DESIGN-test-build-ci.md §1.4).

    python scripts/ci_bounds.py --check       # every workflow and action; exit 1 naming each offender
    python scripts/ci_bounds.py --self-test   # each audit fires on the shape it forbids, then the real tree

Every job of every workflow under `.github/workflows/` states a `timeout-minutes` of 1 to MAX_JOB_MINUTES (a hung step
otherwise holds a merge-queue slot for GitHub's six hours); every `actions/checkout` step and every
`./.github/actions/setup-toolchain` step states one of 1 to MAX_STEP_MINUTES; every job's first step is a checkout that
is retried once with the same `with:` block; no step saves a cache from a branch ref (`actions/cache@` restores AND
saves, so a merge group, whose ref is new every time, would save a copy nothing reads again); no cache save follows a
`continue-on-error` step that produced what it saves without requiring that step's success (a failed `dotnet restore`
would otherwise store a partial package set under the exact key, kb/Work PB4348); and the toolchain action caches
Python's tool-cache directory with the rest, restored before `actions/setup-python`.

Merge-group run 41226218068 (pull request #893, 2026-10-31) sat 45 minutes in `actions/checkout` of a job whose code was
fine, and the queue removed the pull request. The workflow had no `timeout-minutes` at all. The reader is
scripts/ci_workflow.py (kb/Work PB3819). Until kb/Work PB4395 these audits were a C# class of the compiler's test assembly
(CiBoundsDriftTests), so a workflow edit was a compiler change that only a build could check; they are the pipeline's,
and run in the tooling lane. The self-test also holds this docstring's design pointer to the section that states the
bounds (kb/Work PB4349: the pointer named §3.17, the landing pipeline, while the paragraph sat in §1.4).
"""
from __future__ import annotations

import re
import subprocess
import sys
from collections.abc import Iterable, Iterator
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))  # scripts/, for the shared modules
import ci_workflow  # noqa: E402
from ci_workflow import Step  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

REPO = Path(__file__).resolve().parents[1]
#: The tracked trees the audits read: every workflow, and every composite action.
WORKFLOWS, WORKFLOW_KINDS = ".github/workflows/", (".yml", ".yaml")
ACTIONS, ACTION_KINDS = ".github/actions/", (".yml",)
TOOLCHAIN_ACTION_FILE = ".github/actions/setup-toolchain/action.yml"
#: The design doc whose section this module's docstring cites (`cited_design_section`).
DESIGN_DIR = "docs/rearchitecture/"

#: The longest a job may run: the nightly workflow's bound (kb/Work PB4332); a pull-request job is far below it.
MAX_JOB_MINUTES = 60
#: The longest a checkout or toolchain step may run before it is killed and (the checkout) retried.
MAX_STEP_MINUTES = 5
TOOLCHAIN_ACTION = "./.github/actions/setup-toolchain"
MAIN_ONLY = "github.ref == 'refs/heads/main'"


def _uses(step: Step) -> str:
    return step.value("uses") or ""


def is_checkout(step: Step) -> bool:
    return _uses(step).startswith("actions/checkout@")


def is_bounded(step: Step) -> bool:
    return is_checkout(step) or _uses(step) == TOOLCHAIN_ACTION


def is_cache_step(step: Step) -> bool:
    return _uses(step).startswith("actions/cache/")


def is_cache_save(step: Step) -> bool:
    return _uses(step).startswith("actions/cache/save@")


def in_range(value: str | None, most: int) -> bool:
    return value is not None and value.isdigit() and 1 <= int(value) <= most


def with_block(step: Step) -> str:
    """The step's `with:` block, comments dropped: what the retry must repeat."""
    lines = step.code_lines
    at = next((i for i, x in enumerate(lines) if x.strip() == "with:"), len(lines))
    return "\n".join(x.rstrip() for x in lines[at:])


def job_timeout_offenders(file: str, workflow: str) -> tuple[int, list[str]]:
    """(the jobs read, those that state no `timeout-minutes` of 1 to MAX_JOB_MINUTES minutes)."""
    found = ci_workflow.jobs(workflow)
    return len(found), [f"  {file}: job `{j.id}` states no `timeout-minutes` of 1 to {MAX_JOB_MINUTES} "
                        f"(it is `{j.value('timeout-minutes') or 'absent'}`)"
                        for j in found if not in_range(j.value("timeout-minutes"), MAX_JOB_MINUTES)]


def step_timeout_offenders(file: str, workflow: str) -> tuple[int, list[str]]:
    """(the checkout and toolchain steps read, those that state no `timeout-minutes` of 1 to MAX_STEP_MINUTES)."""
    seen, out = 0, []
    for job in ci_workflow.jobs(workflow):
        for step in filter(is_bounded, job.steps):
            seen += 1
            if not in_range(step.value("timeout-minutes"), MAX_STEP_MINUTES):
                out.append(f"  {file}: job `{job.id}` step `{step.label}` states no `timeout-minutes` of 1 to "
                           f"{MAX_STEP_MINUTES}")
    return seen, out


def checkout_retry_offenders(file: str, workflow: str) -> list[str]:
    """Jobs whose first two steps are not a checkout that continues on error and the same checkout, run only when the
    first did not succeed (`if: steps.<id>.outcome != 'success'`), with the same `with:` block."""
    out: list[str] = []
    for job in ci_workflow.jobs(workflow):
        steps = job.steps
        if len(steps) < 2 or not is_checkout(steps[0]) or not is_checkout(steps[1]):
            out.append(f"  {file}: job `{job.id}` does not begin with a checkout and its one retry")
            continue
        sid = steps[0].value("id")
        if sid is None or steps[0].value("continue-on-error") != "true":
            out.append(f"  {file}: job `{job.id}`'s first checkout needs an `id` and `continue-on-error: true`, or its "
                       "failure ends the job before the retry")
        elif f"steps.{sid}.outcome != 'success'" not in (steps[1].value("if") or ""):
            out.append(f"  {file}: job `{job.id}`'s retry checkout is not `if: steps.{sid}.outcome != 'success'`")
        if with_block(steps[0]) != with_block(steps[1]):
            out.append(f"  {file}: job `{job.id}`'s retry checkout does not repeat the first checkout's `with:` block")
    return out


def cache_save_offenders(where: str, steps: Iterable[Step], job_if: str | None) -> Iterator[str]:
    """Cache steps that save from a branch: `actions/cache@` (restore and save) anywhere, and an `actions/cache/save@`
    whose own `if`, or its job's (`job_if`), does not require the main ref. `steps` are one job's (or a composite
    action's)."""
    for step in steps:
        uses = _uses(step)
        if uses.startswith("actions/cache@"):
            yield (f"  {where}: step `{step.label}` is `{uses}`, which saves on every ref: restore with "
                   "`actions/cache/restore@` and save from the toolchain-cache job")
        elif is_cache_save(step) and MAIN_ONLY not in (step.value("if") or "") and MAIN_ONLY not in (job_if or ""):
            yield f"  {where}: step `{step.label}` saves a cache without requiring `{MAIN_ONLY}` in its `if` or its job's"


def workflow_cache_offenders(file: str, workflow: str) -> list[str]:
    return [o for j in ci_workflow.jobs(workflow) for o in cache_save_offenders(f"{file}: job `{j.id}`", j.steps,
                                                                                 j.value("if"))]


def untested_producer_offenders(where: str, steps: list[Step]) -> Iterator[str]:
    """Cache saves whose producer continues on error and is not tested. The producer is the nearest earlier step that
    is not itself a cache step (the one that wrote what the save stores); when it is `continue-on-error`, the save's `if`
    must require `steps.<its id>.outcome == 'success'`. A failed producer otherwise leaves a partial set that the save
    stores under the exact key, and a run that hits an exact key never saves it again (kb/Work PB4348)."""
    for i, save in enumerate(steps):
        if not is_cache_save(save):
            continue
        p = i - 1
        while p >= 0 and is_cache_step(steps[p]):
            p -= 1
        if p < 0 or steps[p].value("continue-on-error") != "true":
            continue
        sid = steps[p].value("id")
        if sid is None:
            yield (f"  {where}: step `{save.label}` saves what `{steps[p].label}` produced, which continues on error "
                   "and has no `id` to test")
        elif f"steps.{sid}.outcome == 'success'" not in (save.value("if") or ""):
            yield (f"  {where}: step `{save.label}` saves what `{steps[p].label}` produced without requiring "
                   f"`steps.{sid}.outcome == 'success'`")


def python_cache_offenders(action: str) -> list[str]:
    """What keeps the toolchain action from caching Python's tool-cache directory: it must restore (and save) the
    directory the manifest step names, and restore before `actions/setup-python` looks for it."""
    out: list[str] = []
    steps = ci_workflow.steps(action)
    restore = next((i for i, s in enumerate(steps) if _uses(s).startswith("actions/cache/restore@")), -1)
    python = next((i for i, s in enumerate(steps) if _uses(s).startswith("actions/setup-python@")), -1)
    if restore < 0 or python < 0 or restore > python:
        out.append("  the cache restore does not come before actions/setup-python: Python is downloaded in every job")
    out += [f"  step `{s.label}` does not cache Python's directory (`steps.manifest.outputs.python-dir`)"
            for s in steps if is_cache_step(s) and "steps.manifest.outputs.python-dir" not in s.text]
    if not any("$TOOL_CACHE/Python/" in s.text for s in steps):
        out.append("  the manifest step names no `$TOOL_CACHE/Python/<version>` directory, the one actions/setup-python "
                   "finds")
    if not any(is_cache_save(s) and MAIN_ONLY in (s.value("if") or "") for s in steps):
        out.append(f"  no `actions/cache/save@` step requires `{MAIN_ONLY}`: a queue branch would save a copy nothing "
                   "reads again")
    return out


def cited_design_section(source: str) -> tuple[str, str]:
    """The `DESIGN-*.md §N.M` pointer of `source`'s first such citation."""
    m = re.search(r"(DESIGN-[\w-]+\.md) §(\d+(?:\.\d+)*)", source)
    if not m:
        raise ValueError("the text cites no DESIGN-*.md §N.M section")
    return m.group(1), m.group(2)


_HEADING = re.compile(r"^(#+) (\d+(?:\.\d+)*)[ .]")


def design_section(document: str, number: str) -> str:
    """The text of the heading numbered `number` and everything below it, up to the next heading of the same or a
    shallower level."""
    lines = document.split("\n")
    start = next((i for i, x in enumerate(lines) if (m := _HEADING.match(x)) and m.group(2) == number), -1)
    if start < 0:
        raise ValueError(f"the document has no heading numbered {number}")
    depth = len(_HEADING.match(lines[start]).group(1))
    end = next((i for i in range(start + 1, len(lines)) if (m := _HEADING.match(lines[i])) and len(m.group(1)) <= depth),
               len(lines))
    return "\n".join(lines[start:end])


# ── the real tree ────────────────────────────────────────────────────────────────────────────────────────────────

def tracked(repo: Path, under: str, kinds: tuple[str, ...]) -> list[str]:
    """The tracked files under `under` ending in one of `kinds` (`git ls-files`: an untracked scratch file is never
    audited)."""
    out = subprocess.run(["git", "-c", "core.quotePath=false", "ls-files", "-z", "--", under], cwd=repo,
                         capture_output=True, text=True, encoding="utf-8", check=True).stdout
    return sorted(p for p in out.split("\0") if p.endswith(kinds))


def audit(repo: Path = REPO) -> dict[str, tuple[int, list[str]]]:
    """Each audit over the real tree: {its name: (the population it read, its offenders)}."""
    def text(rel: str) -> str:
        return (repo / rel).read_text(encoding="utf-8")

    flows = {f: text(f) for f in tracked(repo, WORKFLOWS, WORKFLOW_KINDS)}
    actions = {f: ci_workflow.steps(text(f)) for f in tracked(repo, ACTIONS, ACTION_KINDS)}
    jobs = {f: ci_workflow.jobs(t) for f, t in flows.items()}
    out: dict[str, tuple[int, list[str]]] = {}
    found = [job_timeout_offenders(f, t) for f, t in flows.items()]
    out["job timeouts"] = (sum(n for n, _ in found), [o for _, os in found for o in os])
    found = [step_timeout_offenders(f, t) for f, t in flows.items()]
    out["checkout and toolchain step timeouts"] = (sum(n for n, _ in found), [o for _, os in found for o in os])
    out["checkout retries"] = (sum(map(len, jobs.values())),
                               [o for f, t in flows.items() for o in checkout_retry_offenders(f, t)])
    saves = sum(1 for js in jobs.values() for j in js for s in j.steps if is_cache_save(s))
    out["caches saved from a branch"] = (
        saves + sum(1 for ss in actions.values() for s in ss if is_cache_save(s)),
        [o for f, t in flows.items() for o in workflow_cache_offenders(f, t)]
        + [o for f, ss in actions.items() for o in cache_save_offenders(f, ss, None)])
    out["saves of a failed producer"] = (
        saves, [o for f, js in jobs.items() for j in js for o in untested_producer_offenders(f"{f}: job `{j.id}`", j.steps)]
        + [o for f, ss in actions.items() for o in untested_producer_offenders(f, ss)])
    out["python in the toolchain cache"] = (1, python_cache_offenders(text(TOOLCHAIN_ACTION_FILE)))
    return out


#: Each audit's population floor: an audit that read fewer saw a moved reader or moved workflows, not a clean tree.
FLOORS = {"job timeouts": 15, "checkout and toolchain step timeouts": 30, "checkout retries": 15,
          "caches saved from a branch": 3, "saves of a failed producer": 2, "python in the toolchain cache": 1}
#: Why each audit's offender is a defect (the console's reason line).
WHY = {
    "job timeouts": "a CI job with no time box holds a merge-queue slot until GitHub's limit when a step hangs (kb/Work "
                    "PB4227: merge-group run 41226218068 sat 45 minutes in a checkout)",
    "checkout and toolchain step timeouts": "a checkout or toolchain step with no short time box hangs its whole job "
                                            "(kb/Work PB4324)",
    "checkout retries": "a checkout that fails once fails the job and the queue removes a branch whose code is fine "
                        "(kb/Work PB4324)",
    "caches saved from a branch": "a cache saved from a branch ref is never read again (a merge group's ref is new every "
                                  "time) and fills the 10 GB budget that main's own copies need (kb/Work PB4324)",
    "saves of a failed producer": "a cache saved after its producing step failed stores a partial set under the exact "
                                  "key, and no later run saves that key again (kb/Work PB4348)",
    "python in the toolchain cache": "actions/setup-python downloads Python from GitHub in every job (48.9 s in each of "
                                     "the eight Windows jobs) unless the toolchain cache holds its directory (kb/Work "
                                     "PB4324)",
}


def findings(results: dict[str, tuple[int, list[str]]]) -> list[str]:
    """Every offender, under its reason, and every audit that read less than its floor."""
    out: list[str] = []
    for name, (seen, offenders) in results.items():
        if offenders:
            out += [f"{name}: {WHY[name]}:", *offenders]
        if seen < FLOORS[name]:
            out.append(f"{name}: the audit read {seen} (at least {FLOORS[name]} expected): the reader or the workflows "
                       "moved")
    return out


def check(repo: Path = REPO) -> int:
    bad = findings(audit(repo))
    for line in bad:
        print(line)
    print(f"=== CI BOUNDS: {'RED' if bad else 'GREEN'} ===")
    return 1 if bad else 0


# ── the self-test: each audit fires on the shape it forbids, on a fixture ─────────────────────────────────────────

BUILD = """\
jobs:
  build:
    runs-on: ubuntu-26.04
    timeout-minutes: 15
    steps:
      - id: checkout
        uses: actions/checkout@v7
        timeout-minutes: 5
        continue-on-error: true  # one retry follows
        with:
          submodules: false
          # a comment only the first carries
          fetch-depth: 0
      - if: steps.checkout.outcome != 'success'
        uses: actions/checkout@v7
        timeout-minutes: 5
        with:
          submodules: false
          fetch-depth: 0
      - name: Setup the toolchain
        uses: ./.github/actions/setup-toolchain
        timeout-minutes: 5
      - uses: actions/cache/restore@v6
      - if: ${{ github.ref == 'refs/heads/main' }}
        uses: actions/cache/save@v6
"""

WARM = """\
jobs:
  warm:
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    runs-on: ubuntu-26.04
    timeout-minutes: 20
    steps:
      - uses: actions/cache/save@v6
"""

PRODUCE = """\
jobs:
  warm:
    steps:
      - id: restore
        continue-on-error: true
        run: dotnet restore
      - uses: actions/cache/restore@v6
      - if: ${{ steps.restore.outcome == 'success' }}
        uses: actions/cache/save@v6
      - run: fetch
      - uses: actions/cache/save@v6
"""

ACTION = """\
runs:
  using: composite
  steps:
    - id: manifest
      run: echo "python-dir=$TOOL_CACHE/Python/$python"
    - id: cache
      uses: actions/cache/restore@v6
      with:
        path: ${{ steps.manifest.outputs.dir }} ${{ steps.manifest.outputs.python-dir }}
    - uses: actions/setup-python@v7
    - if: ${{ github.ref == 'refs/heads/main' }}
      uses: actions/cache/save@v6
      with:
        path: ${{ steps.manifest.outputs.dir }} ${{ steps.manifest.outputs.python-dir }}
"""


def _swap(text: str, old: str, new: str = "") -> str:
    """`text` with `old` replaced; refuses an `old` the fixture does not hold, so a mutation that no longer applies
    cannot pass by mutating nothing."""
    if old not in text:
        raise ValueError(f"the fixture no longer holds {old!r}")
    return text.replace(old, new)


def self_test(repo: Path = REPO) -> int:
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: object = "") -> None:
        results.append((name, bool(ok), "" if ok else str(detail)))

    def offenders(f, text) -> int:
        return len(f("w.yml", text)[1])

    both = BUILD + WARM.replace("jobs:\n", "")
    arm("a job's timeout: present and in range is clean; absent, zero, too long or an expression is one offender each",
        (job_timeout_offenders("w.yml", both)[0], offenders(job_timeout_offenders, both),
         offenders(job_timeout_offenders, _swap(BUILD, "    timeout-minutes: 15\n")),
         offenders(job_timeout_offenders, _swap(BUILD, "timeout-minutes: 15", "timeout-minutes: 0")),
         offenders(job_timeout_offenders, _swap(BUILD, "timeout-minutes: 15", "timeout-minutes: 361")),
         offenders(job_timeout_offenders, _swap(BUILD, "timeout-minutes: 15", "timeout-minutes: ${{ vars.T }}")))
        == (2, 0, 1, 1, 1, 1))

    no_toolchain_timeout = _swap(_swap(BUILD, "uses: ./.github/actions/setup-toolchain\n        timeout-minutes: 5\n"),
                                 "- name: Setup the toolchain\n",
                                 "- name: Setup the toolchain\n        uses: ./.github/actions/setup-toolchain\n")
    arm("the checkout and toolchain steps' timeouts: a job's own is not a step's; a step over the bound and a toolchain "
        "step with none are offenders",
        (step_timeout_offenders("w.yml", BUILD), offenders(step_timeout_offenders,
                                                           _swap(BUILD, "        timeout-minutes: 5",
                                                                 "        timeout-minutes: 6")),
         offenders(step_timeout_offenders, no_toolchain_timeout)) == ((3, []), 3, 1))

    retry = ("      - if: steps.checkout.outcome != 'success'\n        uses: actions/checkout@v7\n        timeout-minutes: 5\n"
             "        with:\n          submodules: false\n          fetch-depth: 0\n")
    arm("the checkout and its one retry: a missing retry, a retry not tied to the first's outcome, a first that does not "
        "continue on error and a retry with another `with:` block are one offender each; a job with no checkout is one",
        (checkout_retry_offenders("w.yml", BUILD), any("`warm`" in o for o in checkout_retry_offenders("w.yml", WARM)),
         len(checkout_retry_offenders("w.yml", _swap(BUILD, retry))),
         len(checkout_retry_offenders("w.yml", _swap(BUILD, "if: steps.checkout.outcome != 'success'", "if: always()"))),
         len(checkout_retry_offenders("w.yml", _swap(BUILD, "        continue-on-error: true  # one retry follows\n"))),
         len(checkout_retry_offenders("w.yml", _swap(BUILD, "          fetch-depth: 0\n      - name: Setup",
                                                     "          fetch-depth: 1\n      - name: Setup"))))
        == ([], True, 1, 1, 1, 1))

    arm("caches saved from a branch: `actions/cache@`, a save with no main-ref `if` and a job `if` that lost the main ref "
        "are one offender each; a save under a main-only job is clean",
        (workflow_cache_offenders("w.yml", BUILD), workflow_cache_offenders("w.yml", WARM),
         len(workflow_cache_offenders("w.yml", _swap(BUILD, "actions/cache/restore@v6", "actions/cache@v6"))),
         len(workflow_cache_offenders("w.yml", _swap(BUILD, "- if: ${{ github.ref == 'refs/heads/main' }}\n",
                                                     "- name: save\n"))),
         len(workflow_cache_offenders("w.yml", _swap(WARM, "github.ref == 'refs/heads/main' && "))))
        == ([], [], 1, 1, 1))

    def producer(text: str) -> int:
        return len(list(untested_producer_offenders("w.yml", ci_workflow.jobs(text)[0].steps)))
    arm("a save after a producer that continues on error: tested is clean; untested, a producer with no id, and a second "
        "untested producer are one offender each (kb/Work PB4348)",
        (producer(PRODUCE), producer(_swap(PRODUCE, "steps.restore.outcome == 'success'", "always()")),
         producer(_swap(PRODUCE, "- id: restore", "- name: restore")),
         producer(_swap(PRODUCE, "- run: fetch", "- continue-on-error: true\n        run: fetch"))) == (0, 1, 1, 1))

    python_step = "    - uses: actions/setup-python@v7\n"
    restore_step = ("    - id: cache\n      uses: actions/cache/restore@v6\n      with:\n"
                    "        path: ${{ steps.manifest.outputs.dir }} ${{ steps.manifest.outputs.python-dir }}\n")
    arm("Python's directory in the toolchain cache: restored after setup-python, a cache step without the directory, a "
        "save from any ref and no restore at all are offenders",
        (python_cache_offenders(ACTION), len(python_cache_offenders(_swap(ACTION, restore_step + python_step,
                                                                          python_step + restore_step))),
         any("python-dir" in o for o in python_cache_offenders(_swap(ACTION, "python-dir", "dir"))),
         len(python_cache_offenders(_swap(ACTION, "github.ref == 'refs/heads/main'", "true"))),
         bool(python_cache_offenders(_swap(ACTION, "actions/cache/restore@v6", "actions/cache/other@v6"))))
        == ([], 1, True, 1, True))

    doc = "# D\n\n## 1 One\n\ntext Pinned\n\n### 1.1 Sub\n\nnested\n\n## 2 Two\n\nother\n"
    sec1 = design_section(doc, "1")
    try:
        design_section(doc, "3")
        missing = ""
    except ValueError as e:
        missing = str(e)
    arm("the design-section reader: a section holds its subsections and stops at its sibling; a missing section is "
        "refused; the pointer reader reads `DESIGN-x.md §N.M`",
        ("Pinned" in sec1 and "nested" in sec1 and "other" not in sec1 and "Pinned" not in design_section(doc, "2")
         and "no heading numbered 3" in missing
         and cited_design_section("(kb/Work PB1; DESIGN-x.md §3.17): text") == ("DESIGN-x.md", "3.17")), sec1)

    # THE REAL TREE: each audit clean, over at least its floor.
    for name, result in audit(repo).items():
        bad = findings({name: result})
        arm(f"THE REAL TREE, {name}: clean over {result[0]} (at least {FLOORS[name]})", not bad, "\n".join(bad))

    # The pointer this module's docstring carries leads to the paragraph that states the bounds and names this module.
    name, number = cited_design_section(__doc__)
    section = design_section((repo / DESIGN_DIR / name).read_text(encoding="utf-8"), number)
    arm(f"THE REAL TREE: the docstring cites {name} §{number}, which states the bounds and names scripts/ci_bounds.py "
        "(kb/Work PB4349)", "scripts/ci_bounds.py" in section and "CI is bounded" in section,
        f"§{number} does not name scripts/ci_bounds.py or state the bounds: cite the section that holds the paragraph, "
        "or add the paragraph there")

    for label, passed, detail in results:
        print(f"  {'PASS' if passed else 'FAIL'}  {label}" + ("" if passed or not detail else f"\n        {detail}"))
    failed = sum(1 for _, passed, _ in results if not passed)
    print(f"=== CI BOUNDS SELF-TEST: {'FAIL — ' + str(failed) + ' of ' if failed else 'ALL GREEN — '}"
          f"{len(results)} arms ===")
    return 1 if failed else 0


def main(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        return self_test()
    if argv == ["--check"]:
        return check()
    print(__doc__.split("\n\n")[1])
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
