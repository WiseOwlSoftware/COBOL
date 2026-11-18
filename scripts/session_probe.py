#!/usr/bin/env python3
"""session_probe.py — the §0 SESSION BOOTSTRAP step-③ mechanizer (plan §0; first written 2026-07-19 as scripts/session-probe.ps1,
DEVLOG 922; ported to this concurrent driver by kb/Work PB5939).

Prints the live repo state a session must confirm before touching code. Mechanical, because manual state ledgers drift
(the COBOL1573 collision class). Read-only; safe to run any time.

HOW IT RUNS (kb/Work PB5939). The SessionStart hook runs this at every start, resume, clear and compaction, and ran the
sections one after another: 32.7 s typical and 107 s worst over 51 hooks, the sum of about twenty independent reads.
Each section below is one function that returns its lines; `run()` starts every section at once and prints the lines in
SECTIONS' order, so the wall time is the slowest section and the output is the same text the sequential probe printed.
A section that raises or runs past SECTION_TIMEOUT_S prints one UNREAD line of its own and never costs the others
theirs. Adding a section is one entry in SECTIONS, in the place its lines should print.

Each rule the probe states stays beside the line that applies it: the rule itself lives in the script the section calls
(alloc.py, work.py, prune_worktrees.py, ...) and the probe prints, never recomputes.
"""
from __future__ import annotations

import datetime as dt
import json
import os
import pathlib
import shutil
import subprocess
import sys
import threading
import time
from concurrent.futures import Future, ThreadPoolExecutor
from typing import Callable

sys.path.append(str(pathlib.Path(__file__).resolve().parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[1]

#: One command's hang guard. The slowest honest section is about ten seconds; the SessionStart hook allows 300 s in all, and
#: the sections run side by side, so a hung one costs this much once and the rest still print.
SECTION_TIMEOUT_S = 120


class Cmd:
    """One finished command: its exit code and its output, stdout then stderr as `2>&1` printed them, as lines."""

    def __init__(self, rc: int, out: str):
        self.rc, self.out = rc, out

    @property
    def lines(self) -> list[str]:
        return self.out.splitlines()

    @property
    def text(self) -> str:
        return " ".join(self.lines)


def run_cmd(argv: list[str]) -> Cmd:
    """`argv` in the repository root, output merged (`2>&1`) and read as UTF-8; a missing executable, a timeout or any
    other failure to RUN is raised (the section's own UNREAD line), never read as an empty answer."""
    env = {**os.environ, "PYTHONIOENCODING": "utf-8"}
    p = subprocess.run(argv, cwd=str(REPO), stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                       encoding="utf-8", errors="replace", timeout=SECTION_TIMEOUT_S, env=env)
    return Cmd(p.returncode, p.stdout or "")


def py(*args: str) -> Cmd:
    return run_cmd([sys.executable, *args])


def git(*args: str) -> Cmd:
    return run_cmd(["git", *args])


def gh(*args: str) -> Cmd:
    return run_cmd([shutil.which("gh") or "gh", *args])


def have_gh() -> bool:
    return shutil.which("gh") is not None


Section = Callable[[], list[str]]


# ── 0. THE NIGHTLY, FIRST (kb/Work PB5235; PB5207: "a red one is shown by the session probe as the first line") ────────────
def nightly() -> list[str]:
    """The nightly (.github/workflows/nightly.yml) runs the slow work no landing waits for — the NIST corpus through the
    CLI, the GnuCOBOL differential, every self-test on both OSes — and blocks nothing, so its red is read here or nowhere.
    Its record is the workflow's own run history; this prints the latest scheduled run's verdict, naming each red job."""
    unread = "nightly: gh unavailable — the nightly verdict is UNREAD (do not infer green)"
    if not have_gh():
        return [unread]
    try:
        listing = gh("run", "list", "--workflow", "nightly.yml", "--event", "schedule", "--limit", "1", "--json",
                     "databaseId,conclusion,status,createdAt")
        runs = json.loads(listing.out) if listing.rc == 0 and listing.out.strip() else []
        if not runs:
            return ["nightly: no scheduled run found — the nightly verdict is UNREAD (do not infer green)"]
        r = runs[0]
        when = gh_time(r.get("createdAt"))
        if r.get("status") != "completed":
            return [f"nightly: the run of {when} is {r.get('status')} — no verdict yet"]
        if r.get("conclusion") == "success":
            return [f"nightly: the run of {when} = success"]
        red = gh("run", "view", str(r["databaseId"]), "--json", "jobs", "--jq",
                 '[.jobs[] | select(.conclusion != "success" and .conclusion != "skipped") | .name] | join(", ")')
        names = red.out.strip() if red.rc == 0 else ""
        return [f"nightly: ⛔ the run of {when} = {r.get('conclusion')} — red: {names} "
                f"(gh run view {r['databaseId']} --log-failed)"]
    except Exception:  # noqa: BLE001 - a probe never breaks the session
        return [unread]


def gh_time(stamp: str | None) -> str:
    """A GitHub timestamp as the probe has always shown it (`MM/dd/yyyy HH:mm:ss`, the UTC clock)."""
    try:
        return dt.datetime.strptime(stamp or "", "%Y-%m-%dT%H:%M:%SZ").strftime("%m/%d/%Y %H:%M:%S")
    except ValueError:
        return str(stamp)


# ── 1. Branch + HEAD + cleanliness, the mailbox and the memory index ────────────────────────────────────────────────────
def tree_state() -> list[str]:
    branch = git("rev-parse", "--abbrev-ref", "HEAD").text
    head = git("rev-parse", "--short", "HEAD").text
    dirty = git("status", "--porcelain").lines
    unexpected = "⚠ UNEXPECTED BRANCH" if branch not in ("phase-14", "main") else ""
    out = [f"branch : {branch} @ {head} {unexpected}"]
    out.append(f"⚠ DIRTY TREE ({len(dirty)} paths) — commit or explain before proceeding" if dirty else "tree   : clean")
    unpushed = git("log", "--oneline", "@{u}..HEAD")
    pushed = unpushed.rc == 0 and unpushed.out.strip()
    out.append(f"⚠ UNPUSHED commits: {len(unpushed.lines)}" if pushed else "push   : up to date")
    return out


def mailbox() -> list[str]:
    """The two lanes' mailbox (kb/Work PB2814): this session's open messages and whether IT has an inbox watcher armed.
    Only the session can arm one (a background Bash task, whose exit wakes it), so the probe — which the SessionStart hook
    runs at every start, resume, clear and compaction — prints a ⛔ with the command whenever none is armed."""
    return py("scripts/orchestrator/mailbox.py", "status").lines


def memory_index() -> list[str]:
    """The auto-memory index rides EVERY turn of every session and agent (kb/Work PB3779: 19.5K chars, about 4.9K tokens a
    turn, $75 a week). The cap and the key of its path live once, in account.py; the probe prints its line and flags it
    over the cap, so the drift is seen when it starts instead of measured once."""
    r = py("scripts/orchestrator/account.py", "--memory-index")
    return [f"{'⛔ ' if r.rc != 0 else ''}{r.text}"]


# ── 2. Diagnostic band ──────────────────────────────────────────────────────────────────────────────────────────────────
def diag() -> list[str]:
    """The next-free rule (the CEILING of the src scan, the catalog scan and every code already reserved for an in-flight
    worktree) lives ONCE, in scripts/orchestrator/alloc.py (`code_scans`, which carries the 1573/1518 and 1999/2096
    collision history). The probe prints its line and never recomputes it."""
    return [f"diag   : {py('scripts/orchestrator/alloc.py', 'peek', 'code', '--probe').text}"]


# ── 3. VCR todo count (the P14 burn-down instrument) ────────────────────────────────────────────────────────────────────
def vcr() -> list[str]:
    text = (REPO / "docs" / "VERSION_CHANGE_REFERENCE.md").read_text(encoding="utf-8", errors="replace")
    return [f"VCR    : {text.count('<!-- todo -->')} todo anchors remaining (P14 Step 1 drives to zero)"]


# ── 4. Conformance suite counts (drift signal, not a gate): the tests of the clause folders and WiseOwl's refusals ───────
def suite() -> list[str]:
    tests = 0
    for cases in ("conformance-suite/cases", "conformance-suite/wiseowl/cases"):
        with os.scandir(REPO / cases) as clauses:
            for clause in clauses:
                if clause.is_dir():
                    with os.scandir(clause.path) as files:
                        tests += sum(1 for f in files if f.is_file() and f.name.lower().endswith(".cob"))
    with os.scandir(REPO / "conformance-suite" / "wiseowl" / "diagnostics") as files:
        refusals = sum(1 for f in files if f.is_file() and f.name.lower().endswith(".json"))
    return [f"suite  : {tests} tests · {refusals} WiseOwl refusals (conformance-suite/)"]


# ── 5. Traceability inventory (exists after P14 Step 0) ─────────────────────────────────────────────────────────────────
def inventory_built() -> bool:
    return (REPO / "tests" / "version-matrix" / "inventory-schema.json").exists()


def inventory() -> list[str]:
    """Each edition's inventory is a VIEW, its catalog joined with its store and each rule's state computed (kb/Work
    PB4992, scripts/spec/inventory_view.py): the probe prints the view's own summary, each edition's rules and GAP count
    side by side, then each edition's verdicts and its CONFORMS rows still a spec-derived test from closing."""
    if not inventory_built():
        return ["invent : not built yet (P14 Step 0)"]
    return py("scripts/spec/inventory_view.py", "--summary").lines


def work_next() -> list[str]:
    """⛔ THE WORK LIST, PRINTED, FROM THE ONE REGISTER. Deriving "what should I fix next" from a 2,400-line prose queue is
    what led to picking PB39 (rule-id cosmetics, zero wrong answers) over PB24 (FUNCTION LENGTH silently wrong) — both are
    [MAJOR], so severity could not separate them. kb/Work/ replaced FIVE overlapping registers; this ranks on what a
    defect DOES to a user's program."""
    if not inventory_built() or not (REPO / "kb" / "Work").is_dir():
        return []
    return py("scripts/spec/work.py", "next").lines


def work_check() -> list[str]:
    """`work.py check` only to learn whether the register has malformed items (`next` stops on an unreadable note, but a
    note that parses and breaks a rule only `check` finds). It runs beside `next`, not after it."""
    if not inventory_built() or not (REPO / "kb" / "Work").is_dir():
        return []
    if py("scripts/spec/work.py", "check").rc != 0:
        return ["work   : the kb/Work register has malformed items - run: python scripts/spec/work.py check"]
    return []


def rendered_views() -> list[str]:
    """⛔ THE VAULT'S BURN-DOWN IS A RENDERED VIEW (kb/Work PB3026; scripts/rendered_views.py): kb/Conformance/ is gitignored
    and was once STALE for days because it was a file someone had to remember to regenerate (five of twelve clause notes
    wrong, §15 claiming 533 GAP where the live number was 481). A copy cannot be stale if it is RENDERED here, from the
    inventory read above, at every session start; the render is cheap and the directory is gitignored, so the checkout
    stays clean. The conformance statement is a rendered view too (kb/Work PB5430): docs/CONFORMANCE.md from
    docs/conformance/. A render refuses to overwrite a hand-edited copy (rendered_views rule (e)), so a failure here names
    an edit to carry. The two renders run one after the other: they share the inventory's caches."""
    if not inventory_built():
        return []
    out = []
    if py("scripts/rendered_views.py", "conformance-burn-down").rc != 0:
        out.append("vault  : the kb/Conformance burn-down did not render - run: python scripts/rendered_views.py "
                   "conformance-burn-down")
    if py("scripts/rendered_views.py", "conformance").rc != 0:
        out.append("conform: docs/CONFORMANCE.md did not render - run: python scripts/rendered_views.py conformance")
    return out


# ── THE FUZZ LEDGER (owner 2026-10-28, kb/Work PB4010) ──────────────────────────────────────────────────────────────────
def fuzz() -> list[str]:
    """The Artifact and FUZZ-LEDGER.md must show the same ledger. The rule lives in fuzz_history.py; the probe prints its
    one status line (current, PUBLISH OWED or REGENERATE OWED)."""
    return [f"fuzz   : {l}" for l in py("scripts/fuzz_history.py", "status").lines]


# ── ⛔ NO BRANCH IS LEFT UNDECIDED (owner 2026-10-08 00:00 PDT, kb/Work PB2600; MANDATORY-PRACTICES O10) ──────────────────
def branches() -> list[str]:
    """Every local branch and detached worktree is IN FLIGHT, WAITING FOR REVIEW, WAITING TO LAND, STOPPED, ENQUEUED, NOT
    QUEUED, BOUNCED, LANDED or needs a DECISION; the rows someone must act on print here every session, so an undecided
    branch can no longer be carried forward from handoff to handoff unseen. The rule lives once, in prune_worktrees.py;
    the probe prints its --brief form and never recomputes it."""
    return [f"branch : {l}" for l in py("scripts/prune_worktrees.py", "--brief").lines]


# ── ⛔ A GATE SLOT HELD FAR PAST ITS KIND'S MEDIAN IS NAMED, NOT DISCOVERED (kb/Work PB3310) ──────────────────────────────
def slots() -> list[str]:
    """On 2026-10-16 a hung Linux gate held one of the three slots for 6 h 40 min and the operator's peer session noticed
    it, not a tool. `gate_slot.py status` flags a hold older than HOLD_LONG_FACTOR times the median of its kind's recorded
    holds (the gates' own history); the probe only prints those lines. SURFACED, never a kill: verify the holder's pid
    first (feedback_verify_pid_before_kill)."""
    r = py("scripts/gate_slot.py", "status")
    if r.rc != 0:
        first = r.lines[0] if r.lines else ""
        return [f"slots  : gate_slot.py status failed (exit {r.rc}) - long holds UNREAD: {first}"]
    return [f"slots  : ⛔ {l.strip()}" for l in r.lines if "LONG HOLD" in l or "NO LONG-HOLD FLAGS" in l]


def growth() -> list[str]:
    """THE GATE'S GROWTH TRIGGER (PB4323 section 3.4, kb/Work PB4333): the whole-population gate grows with the suite, and
    the design builds the emitted-program differential when the weekly median of the code lane's test phase passes 6 min
    or Conformance passes 1.5 times its 20,406 cases. The rule lives in gate_growth.py; the probe prints its one verdict
    line (FIRED is the only one that asks for action: build step 10, measuring the changed-program fraction first)."""
    return [f"growth : {l}" for l in py("scripts/gate_growth.py", "status").lines]


def ci_disabled() -> list[str]:
    """⛔ A DISABLED GATE MUST NOT BE INVISIBLE. Reported here because the probe is the one thing every session reads, and
    "CI is off" is exactly the kind of live state that rots into a false sense of coverage (feedback_green_gates_arent_
    evidence) — the battery is NOT a superset of CI: it is Windows/DEBUG only, so the Linux legs and the Release
    configuration are verified by CI and by nothing else. EARNED 2026-08-06, when the workflow was turned off during
    GitHub's Actions outage and five pushes queued nothing. It prints ONLY while some workflow is non-active, so it clears
    itself the moment CI comes back. Re-enabling is an OWNER decision; the re-entry sequence is in plan §0. Network-
    tolerant: any failure to reach GitHub is silent, so the probe still works offline. The repository is read from the
    remote, never hard-coded (kb/Work PB3078)."""
    try:
        repo = gh("repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner")
        if repo.rc != 0 or not repo.text:
            return []
        wf = gh("api", f"repos/{repo.text}/actions/workflows", "--jq",
                '.workflows[] | select(.state != "active") | "\\(.name) [\\(.state)]"')
        if wf.rc == 0 and wf.text:
            return [f"ci     : ⛔ DISABLED — {wf.text}  (re-enable: gh workflow enable 'Build and Test')",
                    "         every gate is LOCAL until then: bash scripts/battery.sh"]
    except Exception:  # noqa: BLE001
        pass
    return []


def ci_main() -> list[str]:
    """⛔ AND IS THE ENABLED WORKFLOW ACTUALLY PASSING? The check above answers "is CI running at all" and says nothing about
    the VERDICT, so a red main is invisible to a session that reads only this probe: main was red for 29 hours across 16
    consecutive completed runs (2026-09-05 → 2026-09-06) while every landing in that window reported green, because no step
    of the landing protocol looked at CI after the push and the battery cannot see the Linux legs (kb/Work/PB796). Printed
    unconditionally — a green line is the positive evidence a silence never is. Degrades to one quiet line with no gh, no
    network and no runs."""
    unread = "ci     : gh unavailable — CI verdict UNREAD (do not infer green)"
    if not have_gh():
        return [unread]
    try:
        r = gh("run", "list", "--branch", "main", "--limit", "1", "--json", "conclusion,headSha,status,displayTitle")
        runs = json.loads(r.out) if r.rc == 0 and r.out.strip() else []
        if not runs:
            return ["ci     : no runs found on main"]
        run, sha = runs[0], str(runs[0]["headSha"])[:8]
        if run.get("status") != "completed":
            return [f"ci     : last run on main @ {sha} is {run.get('status')} — no verdict yet"]
        if run.get("conclusion") == "success":
            return [f"ci     : last run on main @ {sha} = success"]
        return [f"ci     : ⛔ last run on main @ {sha} = {run.get('conclusion')} — attribute it before landing: "
                "gh run view --branch main --log-failed"]
    except Exception:  # noqa: BLE001
        return [unread]


def merge_queue() -> list[str]:
    """⛔ AND IS THE REQUIRED CHECK STILL IN PLACE? `main` requires the `ci-gate` status check with `enforce_admins: true`
    (owner decision 2026-09-06, question 23; kb/Work/PB796), and that rule lives on the SERVER, outside this repository —
    so it is the ONE part of the landing gate no test in this tree can pin, and nothing but this line would notice it being
    turned off. Printed unconditionally, green included, and a 404 is told apart from an unreadable API: "I could not look"
    must never render as "it is off", nor as silence.
    ⭐ THE MERGE QUEUE FIRST (kb/Work PB3024): `ruleset.py check` compares main's live ruleset with the committed
    scripts/orchestrator/ruleset-main.json. ACTIVE: landings are pull requests queued by enqueue.py and the classic
    protection is gone (its lookup would only report the 404 the ruleset replaced). DRIFT: the line names what differs, and
    enqueue.py refuses every landing until it is fixed. ABSENT (no ruleset on main): enqueue.py refuses every landing too
    (exit 8; kb/Work PB3023 review R1-1), and the classic protection is read. SKIPPED: UNREAD, never a pass. The exit code
    is the child's own, never a pipeline's (a stale code once printed an ABSENT queue as ACTIVE)."""
    q = py("scripts/orchestrator/ruleset.py", "check")
    line = q.lines[0] if q.lines else ""
    out = []
    if q.rc == 0:
        out.append(f"gate   : {line} — land with: python scripts/orchestrator/enqueue.py land <branch> "
                   "(it queues the pull request)")
        return out   # the classic protection is gone by design; nothing more to read
    if q.rc == 1:
        out.append(f"gate   : ⛔ {line} — every landing is refused until it is fixed (python scripts/orchestrator/ruleset.py check)")
    elif q.rc == 4:
        out.append(f"gate   : merge queue state UNREAD ({line}) — do not infer either path")
    elif q.rc != 3:
        out.append(f"gate   : ruleset.py check failed (exit {q.rc}): {line}")
    if not have_gh():
        return out + ["gate   : gh unavailable — main's required-check state UNREAD (do not infer it is on)"]
    prot = gh("api", "repos/{owner}/{repo}/branches/main/protection")
    if prot.rc != 0:
        if "Branch not protected" in prot.out or "HTTP 404" in prot.out:
            return out + ["gate   : ⛔ main has NO branch protection — the required ci-gate check is GONE (kb/Work/PB796)"]
        return out + ["gate   : gh could not read main's protection — required-check state UNREAD (do not infer it is on)"]
    try:
        j = json.loads(prot.out)
        ctx = list(j["required_status_checks"]["contexts"] or [])
        adm = bool(j["enforce_admins"]["enabled"])
    except (ValueError, KeyError, TypeError):
        return out + ["gate   : main's protection did not parse — required-check state UNREAD (do not infer it is on)"]
    if "ci-gate" in ctx and adm:
        return out + ["gate   : ⛔ main requires ci-gate (enforce_admins on) but the merge queue is not applied (ruleset.py "
                      "apply, on the owner's go) — enqueue.py land refuses every landing until ruleset.py check reports ACTIVE"]
    return out + [f"gate   : ⛔ main protection DRIFTED — contexts=[{','.join(ctx)}] enforce_admins={adm} "
                  "(expected ci-gate + True)"]


# THE ORDER THE LINES PRINT IN is this list's, the order the sequential probe printed them; the sections run at once.
# (name for the UNREAD line, section). The inventory block is one group: its four reads start together.
SECTIONS: list[tuple[str, Section]] = [
    ("nightly", nightly),
    ("tree", tree_state),
    ("mailbox", mailbox),
    ("memory index", memory_index),
    ("diag", diag),
    ("VCR", vcr),
    ("suite", suite),
    ("inventory", inventory),
    ("work next", work_next),
    ("work check", work_check),
    ("rendered views", rendered_views),
    ("fuzz", fuzz),
    ("branches", branches),
    ("slots", slots),
    ("growth", growth),
    ("ci disabled", ci_disabled),
    ("ci main", ci_main),
    ("merge queue", merge_queue),
]
PROBE_BANNER = "=== next: confirm the battery green (plan §9) before code changes; the worklist is kb/Work (work.py next, above); plan 0 is live state ==="


def guarded(name: str, section: Section) -> list[str]:
    try:
        return section()
    except Exception as exc:  # noqa: BLE001 - a probe never breaks the session; the section says it could not read
        return [f"{name:7}: UNREAD ({type(exc).__name__}: {str(exc)[:200]}) — do not infer it is fine"]


def run(pool: ThreadPoolExecutor | None = None, sections: list[tuple[str, Section]] | None = None) -> str:
    """The probe's whole text. `pool`: the caller's (the SessionStart hook runs its tooling check beside it), else one
    sized for the sections; `sections`: a self-test's own."""
    sections = SECTIONS if sections is None else sections
    own = pool is None
    pool = pool or ThreadPoolExecutor(max_workers=len(sections))
    try:
        started = time.strftime("%Y-%m-%d %H:%M")
        futures: list[Future[list[str]]] = [pool.submit(guarded, n, s) for n, s in sections]
        out = [f"=== SESSION PROBE ({started}) ==="]
        for f in futures:
            out += f.result()
        out.append(PROBE_BANNER)
        return "\n".join(out)
    finally:
        if own:
            pool.shutdown()


def self_test() -> int:
    fails: list[str] = []

    def check(what: str, ok: bool) -> None:
        if not ok:
            fails.append(what)

    def boom() -> list[str]:
        raise RuntimeError("no")

    # Three sections that each wait for the other two: they can only all finish if they run at the same time (a
    # structural proof of concurrency, never a measured duration); the first finishes last, so the order is the list's.
    meet = threading.Barrier(3, timeout=10)

    def together(tag: str, pause: float) -> Section:
        def section() -> list[str]:
            meet.wait()
            time.sleep(pause)
            return [tag]
        return section

    text = run(sections=[("a", together("first", 0.2)), ("b", boom), ("c", together("third", 0.0)),
                         ("d", together("fourth", 0.0))])
    lines = text.splitlines()
    check("the lines print in the sections' order, not in the order they finish", [lines[1], lines[3], lines[4]] ==
          ["first", "third", "fourth"])
    check("a raising section says UNREAD and the others still print", "b      : UNREAD (RuntimeError: no)" in lines[2])
    check("the banner opens and closes the text", lines[0].startswith("=== SESSION PROBE (") and lines[-1] == PROBE_BANNER)
    check("a GitHub timestamp reads as the sequential probe printed it",
          gh_time("2026-11-18T18:01:56Z") == "11/18/2026 18:01:56")
    names = [n for n, _ in SECTIONS]
    check("every section is listed once", len(names) == len(set(names)))
    for f in fails:
        print("FAIL:", f)
    print(f"=== SESSION PROBE SELF-TEST: {'GREEN' if not fails else f'RED ({len(fails)})'} ===")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(run())
