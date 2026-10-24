#!/usr/bin/env python3
"""land_lane.py — THE LOOP'S LANDING LANE: every branch the survey lists WAITING TO LAND goes through `enqueue.py land`
the moment it is ready, while whatever unit runs keeps running (kb/Work PB3103; DESIGN-orchestrator-loop.md section
4.9; MANDATORY-PRACTICES O11). Deterministic, no model: it decides nothing a script cannot, and dispatches no agent.

    python scripts/orchestrator/land_lane.py tick [--repo DIR]      reap, survey, start every eligible landing
    python scripts/orchestrator/land_lane.py status [--json]        one line per branch: running, stopped, queued
    python scripts/orchestrator/land_lane.py collect <branch> ...   the enqueue unit has handled these attempts
    python scripts/orchestrator/land_lane.py rearm <branch> --why T the enqueue unit: land it again at its CURRENT tip
    python scripts/orchestrator/land_lane.py clear-halt             the operator: the merge queue is ACTIVE again
    python scripts/orchestrator/land_lane.py run <attempt file>     (the detached runner `tick` starts; not by hand)
    python scripts/orchestrator/land_lane.py --self-test

⛔ WHY (the owner's choice 2026-10-12, "Supervisor lane (Recommended)"). Before the lane a reviewed branch landed only
when the loop's NEXT `enqueue` unit started, after the whole wave ended: wave 1069's group D was stamped Review CLEAN at
12:34 PDT and E and F by 13:05, and all three idled while G and H ran; then, while the enqueue unit gated and waited on
the merge queue, no implementer ran. The supervisor (`orchestrate.ps1`) now runs `tick` in the BACKGROUND every few
minutes while any unit runs, so a branch lands within its own gate and CI time of being ready.

STATE, all under `<coord>/land-lane/` (the coordination directory, never a worktree):
  attempts/<id>.json  one per `enqueue.py land` run, <id> = `<n>-<UTC stamp>-<branch>` (n: the attempt NUMBER, one
                      past every attempt, taken under the tick lock; the order of attempts, never the clock, kb/Work
                      PB3536): branch, head (its tip at launch), worktree, argv, log
                      (`<coord>/logs/land-lane-<id>.log`), runner pid, and once it ends rc, the log's
                      `=== ENQUEUE: ` verdict, end_head (the tip after it: the run merges main in and stamps the entry),
                      pr (the pull request it queued, from the log's `ENQUEUED pr=` line), `collected` (the enqueue
                      unit handled it) and, on an exit-0 attempt, `watch` (the queue watch's state and each stalled
                      read). Several processes write one record, each only ITS fields, through the ONE writer
                      `update` (it re-reads the record under the record's own lock and changes only the fields named):
                      the runner (rc, verdict, end_head, pr), the tick (`reap`'s DIED and halted, the queue watch's
                      `watch`), `launch` (the pid) and the enqueue unit's `collect` (`collected`). A whole-record write
                      from a copy read earlier undid a concurrent writer's field (kb/Work PB3527 review R1-3).
  survey.json         the last tick's branch survey (prune_worktrees' rows, imported, never re-parsed from its text):
                      what next_unit.py reads to decide whether the enqueue unit has AGENT work.
  rearm/<branch>.json the enqueue unit's re-arm of a branch at its tip (branch, head, why, at): a stop that was resolved
                      WITHOUT a new commit (a bounce or a red the finisher attributes to infrastructure, load timing or
                      main itself once main carries the fix; an UNREVIEWED exit 11 whose tip a reviewer has since stamped
                      CLEAN) is landed again at the SAME head, once; the launch consumes it, and a tip that moved makes
                      it stale (dropped, said). Without it a STOPPED, DONE or BOUNCED branch at an unchanged head would
                      wait forever: the lane never relaunches it there and the enqueue unit lands nothing itself.
  HALT                written when a run ends QUEUE NOT ACTIVE (exit 8): no attempt starts until the operator runs
                      `clear-halt`; the operator is named through the mailbox.
  tick.lock           an OS lock: one tick at a time (a second exits at once, saying so).

THE TICK, in order (the queue watch, step 4, runs right after the survey, ahead of the stops):
  1. REAP: an attempt whose runner is gone without an rc is DIED. A run that ended QUEUE NOT ACTIVE writes HALT.
  2. SURVEY: `git fetch`, then `prune_worktrees.live_survey` and its landing order (the most harmful lead first, kb/Work
     PB2981), written to survey.json. The survey reads these attempts back (`view`, kb/Work PB3125, PB3181): a branch
     whose newest attempt at its tip STOPPED reads STOPPED, one whose attempt there was ENQUEUED reads ENQUEUED, and
     only a tip no attempt stopped at reads WAITING TO LAND.
  3. LAUNCH, unless HALT exists or a stop does (the loop's STOP or its fleet stop scratch\\STOP-loop, or the owner's
     global scratch\\STOP; never STOP-UNIT alone, the soft context cap's signal to the unit, kb/Work PB3402; a running
     `enqueue.py land` finishes either way: it is idempotent). The candidates are the WAITING TO LAND rows in
     survey order, then the branches whose last attempt a script may retry (below) and whose tip is still that
     attempt's end head (a run that committed makes the survey read the branch IN FLIGHT, so a retry is the lane's own
     record, never the survey's), then the branches the enqueue unit re-armed at their current tip (BOUNCED ones
     included). Each candidate is decided by `decide`:
       a run in flight                                             wait
       re-armed by the enqueue unit at this head                   launch (once: the launch consumes the re-arm)
       no attempt, or a NEW HEAD (a finisher's commit, reviewed)  launch
       ENQUEUED or NOTHING TO LAND (exit 0) at this head           nothing to land: the QUEUE WATCH (below) follows its
                                                                   pull request into the merge queue
       exit 3 — the landing check's STOP: the branch shares a file with earlier-dispatched in-flight work (kb/Work
                PB2701)                                            retried at this head once `landing_check.py` passes
                                                                   on the branch (the overlap is gone); never as is
       exit 12 — GATE BUSY: another gate held the worktree's gate retried at this head once the worktree's gate lock
                lock (kb/Work PB3527; nothing ran, nothing red)   is free (`wait_free`); never the enqueue unit's
       exit 2 (main moved under every catch-up, the branch could not be read, or a GATE NOT RUN for an ENVIRONMENT
                cause or one main fails the same way, kb/Work PB3526), exit 9 (GitHub refused the push, the PR or the
                queue), or a runner that DIED                     retried ONCE at this head (enqueue.md's own rule)
       exit 8 — QUEUE NOT ACTIVE                                   HALT (above); relaunched once the halt is cleared
       anything else — CONFLICT 4, GATE RED 5 (a branch-carried GATE NOT RUN too), a DEVLOG refusal 6,
                UNREVIEWED 11, an exit
                2 or 9 retried once already                       STOPPED: never relaunched at this head unless
                                                                   re-armed; the enqueue unit dispatches its finisher
                                                                   or reviewer, and re-arms a stop resolved with no
                                                                   new commit (`rearm`)
     A launch writes the attempt and starts `land_lane.py run <attempt>` DETACHED through scripts/detached.py (the one
     launch that outlives its caller's tree kill, kb/Work PB3536), which runs `enqueue.py land <branch> --worktree <wt>` with its output in
     the log and records the outcome. (A permission refusal cannot happen here: a script has no permission layer.)
  4. THE QUEUE WATCH (kb/Work PB3527), on the facts the survey read (queue_facts.read, the one reader of GitHub's
     facts), before the stops are read: `ENQUEUED` means only that the pull request was HANDED to the queue (auto-merge
     on); on 2026-10-22 PR #385's ci-gate job completed and its check-run stayed in_progress for 50 minutes, outside
     the queue, and nothing looked. Each branch's latest exit-0 attempt is watched until its pull request is in the
     merge queue (or merged, BOUNCED, or superseded by a new head). Not in the queue QUEUE_GRACE_MIN after its required
     checks' work ended (a stuck check-run's JOB time) is a STALLED read: read 1 re-runs the stuck check's workflow run,
     read 2 (one grace after) closes, reopens and re-queues the pull request (both enqueue.py's: the only file that
     queues), read 3 sends the operator a mailbox question and the watch acts no more; each read is recorded on the
     attempt (`watch`) and in landing_measure.py's record (`stalls`). Under a stop the watch reads but acts on nothing.
`status` prints each branch's latest attempt; the enqueue unit reads it (units/enqueue.md step 3), sends each STOPPED
attempt to a finisher or a reviewer, files the leads of each ENQUEUED one, and then `collect`s them. `agent_work` is
next_unit.py's one reader: the enqueue unit starts only for agent work (WAITING FOR REVIEW, a BOUNCED branch the lane is
neither landing nor re-armed to land, a stopped attempt, an ENQUEUED one to collect), never merely because a handoff
lists DONE branches the lane is landing.

Exit: 0 done (a tick already running included) · 1 the survey or a launch failed (printed; the supervisor never treats
it as fatal) · 2 usage.
"""
from __future__ import annotations

import argparse
import dataclasses
import datetime as dt
import json
import os
import pathlib
import re
import subprocess
import sys
import time
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402
from enqueue import (EXIT_GATE_BUSY, EXIT_GITHUB, EXIT_QUEUE_NOT_ACTIVE, QUEUED_LINE, VERDICT)  # noqa: E402  (the landing's contract)
from gate_slot import ExclusiveLock  # noqa: E402  (an OS lock: the tick lock frees itself when its holder dies)
from detached import spawn_detached  # noqa: E402  (THE one way a process outlives its caller, kb/Work PB3536)

LANE_DIR = "land-lane"
ATTEMPTS = "attempts"
SURVEY = "survey.json"
HALT = "HALT"
REARM = "rearm"
TICK_LOCK = "tick.lock"
EXIT_CHECK_STOP = 3      # enqueue.py: landing_check.py's STOP (the branch shares a file with in-flight work)
EXIT_STATE = 2           # enqueue.py: usage or state, among them "main moved under every catch-up merge"
#: The exits the lane retries at the same head once what they wait on is free (wait_free asks): nothing is wrong with
#: the branch, so neither is ever the enqueue unit's (kb/Work PB2701; PB3527: a held gate lock was read as a red).
WAITS = {EXIT_CHECK_STOP: "the in-flight work it shares a file with", EXIT_GATE_BUSY: "the worktree's gate lock"}
# How old survey.json may be before next_unit.py treats the lane as not ticking (the supervisor ticks every
# -LandLaneSeconds, 300 by default, while any unit runs, and once as each unit starts).
SURVEY_MAX_AGE_MIN = 30
# The classes of a finished attempt (classify). STOPPED and DONE-to-collect are the enqueue unit's; the rest the lane's.
DONE, FREE, ONCE, HALTED, STOPPED = "done", "retry-when-free", "retry-once", "halt", "stopped"
RETRYABLE = (FREE, ONCE, HALTED)
VERDICT_LINE = re.compile(r"^" + re.escape(VERDICT) + r"(?P<outcome>.*?)(?: \((?P<branch>[^()]*)\))? ===\s*$")

Say = Callable[[str], None]


class LaneError(RuntimeError):
    """The survey could not be taken (not a repository, git failed): the tick launches nothing and says why."""


# ── the state ──────────────────────────────────────────────────────────────────────────────────────────────────────
def lane_dir(cdir: pathlib.Path) -> pathlib.Path:
    return cdir / LANE_DIR


def _now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc)


def _slug(branch: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]+", "-", branch).strip("-") or "branch"


#: An attempt's file name starts with its NUMBER (`launch`); one named before the numbers starts with its UTC stamp.
ATTEMPT_NUMBER = re.compile(r"(\d{6,})-")


def attempt_order(path: pathlib.Path) -> tuple[int, str]:
    """The order of an attempt file: its number, then its name. The number, never the clock, is the order a branch's
    attempts were launched in: a stamp orders nothing finer than the clock's resolution (on a CI runner three runs of
    the gate driver started in one microsecond, kb/Work PB3536), and two launches of one branch in one clock tick took
    ONE file name, the second record overwriting the first. A file named before the numbers is number 0, older than
    every numbered one."""
    m = ATTEMPT_NUMBER.match(path.name)
    return (int(m[1]) if m else 0), path.name


def attempts(cdir: pathlib.Path, unreadable: list[str] | None = None) -> list[dict[str, Any]]:
    """Every attempt, oldest first (`attempt_order`). A file that is not an attempt record is left out and its path
    appended to `unreadable`, which the tick and `status` say (a lost record could relaunch its branch)."""
    d = lane_dir(cdir) / ATTEMPTS
    out = []
    for p in sorted(d.glob("*.json"), key=attempt_order) if d.is_dir() else []:
        try:
            rec = coord.read_json(p, None)
        except ValueError:
            rec = None
        if isinstance(rec, dict) and isinstance(rec.get("branch"), str) and rec.get("id"):
            rec["_path"] = str(p)
            out.append(rec)
        elif unreadable is not None:
            unreadable.append(str(p))
    return out


def by_branch(atts: list[dict[str, Any]]) -> dict[str, list[dict[str, Any]]]:
    out: dict[str, list[dict[str, Any]]] = {}
    for a in atts:
        out.setdefault(a["branch"], []).append(a)
    return out


def update(rec: dict[str, Any], fields: dict[str, Any] | Callable[[dict[str, Any]], dict[str, Any]]) -> dict[str, Any]:
    """THE ONE WRITER of an existing attempt record (module doc, STATE). Under the record's own lock it re-reads the
    record and changes only `fields` (or, given a function, the fields it returns from the FRESH record; {} writes
    nothing), so no writer undoes another's field: a tick that read the record before its slow survey no longer writes
    back over the `collected` the enqueue unit set meanwhile (kb/Work PB3527 review R1-3). `rec` is refreshed to what
    is on disk and returned."""
    path = pathlib.Path(rec["_path"])
    with coord.locked(path.parent, f"{path.stem}.lock"):
        fresh = coord.read_json(path, None)
        if not isinstance(fresh, dict):
            fresh = {k: v for k, v in rec.items() if k != "_path"}
        change = fields(fresh) if callable(fields) else fields
        if change:
            fresh.update(change)
            coord.write_json(path, fresh)
    rec.clear()
    rec.update(fresh, _path=str(path))
    return rec


def finished(a: dict[str, Any]) -> bool:
    return "rc" in a or a.get("outcome") == "DIED"


def classify(a: dict[str, Any]) -> str:
    """What a finished attempt asks for next (module doc's table)."""
    if a.get("outcome") == "DIED":
        return ONCE
    rc = a.get("rc")
    if rc == 0:
        return DONE
    if rc in WAITS:
        return FREE
    if rc in (EXIT_STATE, EXIT_GITHUB):
        return ONCE
    if rc == EXIT_QUEUE_NOT_ACTIVE:
        return HALTED
    return STOPPED


def trailing(atts: list[dict[str, Any]], cls: str) -> int:
    """How many of the branch's newest finished attempts, in a row, are of class `cls`."""
    n = 0
    for a in reversed(atts):
        if not finished(a) or classify(a) != cls:
            break
        n += 1
    return n


def effective(atts: list[dict[str, Any]]) -> str:
    """The class of a branch's LATEST attempt as the enqueue unit sees it: a retry-once outcome already retried is
    STOPPED (the second exit 2 or 9 at one head is named in the handoff, enqueue.md step 3)."""
    last = atts[-1]
    if not finished(last):
        return "running"
    cls = classify(last)
    return STOPPED if cls == ONCE and trailing(atts, ONCE) >= 2 else cls


def last_verdict(log: pathlib.Path) -> str | None:
    """The outcome of the log's last `=== ENQUEUE: <outcome> (<branch>) ===` line, or None."""
    try:
        text = log.read_bytes().decode("utf-8", errors="replace")
    except OSError:
        return None
    found = None
    for line in text.splitlines():
        if m := VERDICT_LINE.match(line.strip()):
            found = m.group("outcome").strip()
    return found


def queued_pr(log: pathlib.Path) -> int | None:
    """The pull request number on the log's last `ENQUEUED pr=<n> ...` line (enqueue.QUEUED_LINE), or None."""
    try:
        text = log.read_bytes().decode("utf-8", errors="replace")
    except OSError:
        return None
    found = None
    for line in text.splitlines():
        if m := QUEUED_LINE.match(line.strip()):
            found = int(m.group("pr"))
    return found


# ── the decisions ──────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass(frozen=True)
class Decision:
    launch: bool
    why: str


def decide(tip: str, atts: list[dict[str, Any]], free_now: Callable[[], tuple[bool, str]],
           rearmed: dict[str, Any] | None = None) -> Decision:
    """Whether to start `enqueue.py land` for a branch at `tip`, given its attempts (oldest first) and the enqueue
    unit's re-arm of it, if any. The global stops (HALT, STOP, the loop's fleet stop) are the caller's; `free_now` is
    asked only for a retry-when-free exit at this head (WAITS names what it waits on; wait_free says if it is free)."""
    last = atts[-1] if atts else None
    if last is not None and not finished(last):
        return Decision(False, f"being landed (attempt {last['id']}, pid {last.get('pid')})")
    if rearmed is not None and rearmed.get("head") == tip:
        return Decision(True, f"re-armed by the enqueue unit at this head ({rearmed.get('why') or 'no reason given'})")
    if last is None:
        return Decision(True, "first attempt")
    if tip not in (last.get("head"), last.get("end_head")):
        return Decision(True, f"new head {tip[:12]} (the last attempt ended at {str(last.get('end_head'))[:12]})")
    cls = classify(last)
    rc = last.get("rc")
    if cls == DONE:
        return Decision(False, f"{last.get('verdict') or 'exit 0'} at this head: the merge queue has it")
    if cls == HALTED:
        return Decision(True, "the queue halt was cleared since its exit 8")
    if cls == FREE:
        free, detail = free_now()
        return Decision(free, (f"{WAITS[rc]} is free now; retried at this head" if free else
                               f"exit {rc} at this head and it still waits on {WAITS[rc]} ({detail})"))
    if cls == ONCE and trailing(atts, ONCE) < 2:
        return Decision(True, f"{'its runner DIED' if rc is None else f'exit {rc}'} at this head: retried once")
    return Decision(False, f"STOPPED at this head ({'exit ' + str(rc) if rc is not None else 'DIED'}: "
                           f"{last.get('verdict') or 'no verdict line'}): the enqueue unit's; a new head or its "
                           "`rearm` lands it again")


def tip_of(worktree: str | None) -> str | None:
    """The worktree's HEAD commit, or None when there is no worktree or git cannot read it."""
    if not worktree:
        return None
    tip = subprocess.run(["git", "-C", worktree, "rev-parse", "HEAD"], capture_output=True, text=True,
                         encoding="utf-8", errors="replace")
    return tip.stdout.strip() if tip.returncode == 0 else None


# ── the collaborators (the self-test replaces each) ────────────────────────────────────────────────────────────────
def pid_alive(pid: int | None) -> bool:
    """The runner is alive: a live process with that id whose image is a Python (a reused id is another program)."""
    if not isinstance(pid, int):
        return False
    name = coord.process_name(pid)
    return bool(name) and "python" in name.lower()


def survey_rows(repo: pathlib.Path, cdir: pathlib.Path) -> tuple[list[dict[str, Any]], list[str], dict | None]:
    """(every surveyed row as {name, state, content, tip, worktree, why, lead}, the WAITING TO LAND names in landing
    order, the open pull requests the survey read: queue_facts.read, None when GitHub was not observed) —
    prune_worktrees.live_survey, imported: one lifecycle rule (MANDATORY-PRACTICES O10). The reports and the dispatch
    ledger are the TICK's coordination directory's, never the environment's default. The survey's reader of the review
    findings files never raises (review_findings.last_verdict, kb/Work PB3381 review R5-1, R6-1): a file it cannot
    read, or a torn one, holds only the branches that file concerns, so one findings file never stops the tick for
    every branch. The pull requests are read BEFORE the fetch, so one the queue merges in between reads open (or
    queued) and its branch then reads on main: never a merged pull request missing from both (the queue watch would
    take that for one closed by hand)."""
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports coord and plan_wave back)
    import queue_facts  # noqa: PLC0415
    probe = prune_worktrees.git("rev-parse", "--git-common-dir", cwd=repo)
    if probe.returncode != 0:
        raise LaneError(f"{repo} is not a git repository ({probe.stderr.strip()[:120]})")
    prs = queue_facts.read()
    fetch = prune_worktrees.git("fetch", "-q", "origin", cwd=repo)
    if fetch.returncode != 0:
        raise LaneError(f"git fetch origin failed in {repo}: {fetch.stderr.strip()[:200]}")
    rows = prune_worktrees.live_survey(repo, coord_dir=cdir, prs=prs)
    order = [r.name for r in prune_worktrees.landing_order(rows, prune_worktrees.note_rank())]
    return ([{"name": r.name, "state": r.state, "content": r.content, "tip": r.tip, "worktree": r.worktree,
              "why": r.why, "lead": r.report.lead if r.report else None} for r in rows], order, prs)


def landing_check_free(worktree: str) -> tuple[bool, str]:
    """`landing_check.py` on the branch's own range (from its merge base with origin/main, which `enqueue.py land` would
    merge in), as the landing runs it: exit 0 means the overlap that stopped it is gone."""
    base = subprocess.run(["git", "-C", worktree, "merge-base", "refs/remotes/origin/main", "HEAD"],
                          capture_output=True, text=True, encoding="utf-8", errors="replace")
    if base.returncode != 0:
        return False, f"no merge base with origin/main in {worktree}"
    p = subprocess.run([sys.executable, str(HERE / "landing_check.py"), "--rev", "HEAD", "--base", base.stdout.strip()],
                       cwd=worktree, capture_output=True, text=True, encoding="utf-8", errors="replace")
    stops = [ln for ln in p.stdout.splitlines() if ln.startswith("⛔")]
    return p.returncode == 0, (stops[0] if stops else (p.stdout.strip().splitlines() or ["no output"])[-1])[:240]


def gate_lock_free(worktree: str) -> tuple[bool, str]:
    """Whether the worktree's gate lock (the gate driver's own: run_gate_legs.Host.lock_path) is free now: taken and
    released at once (an OS lock, freed when its holder dies, so a lock that reads held is a live gate)."""
    import run_gate_legs  # noqa: PLC0415  (scripts/: the gate driver owns its lock's path)
    try:
        path = run_gate_legs.Host(pathlib.Path(worktree)).lock_path()
    except (OSError, subprocess.SubprocessError, ValueError) as exc:
        return False, f"the gate lock of {worktree} could not be located ({exc})"
    lock = ExclusiveLock.try_take(path, f"land_lane.py probe (pid {os.getpid()})")
    if lock is None:
        return False, f"held by {ExclusiveLock.holder(path) or '(no label)'}"
    lock.release()
    return True, ""


def wait_free(rc: int | None, worktree: str) -> tuple[bool, str]:
    """Whether what a retry-when-free exit (WAITS) waits on is free now: exit 3, landing_check.py passes on the
    branch; exit 12, the worktree's gate lock is free."""
    if rc == EXIT_GATE_BUSY:
        return gate_lock_free(worktree)
    return landing_check_free(worktree)


def notify_operator(cdir: pathlib.Path, subject: str, body: str, ref: str, say: Say, kind: str = "info") -> None:
    """One open mailbox message per subject (the supervisor's own pattern for an owed publish); never fatal. `kind`:
    `info` for what the operator should know, `question` for what it must decide (the queue watch's alert)."""
    try:
        import mailbox  # noqa: PLC0415
        mailbox.send(cdir, to="operator", kind=kind, subject=subject, body=body[:mailbox.BODY_MAX], refs=[ref],
                     sender="loop", session=f"land_lane.py pid {os.getpid()}", unless_pending=True)
    except Exception as exc:  # noqa: BLE001  (a mailbox that cannot be written must not stop the lane; it is said)
        say(f"land lane: the operator's mailbox was not written ({exc}); {subject}")


# ── the enqueue unit's re-arm ──────────────────────────────────────────────────────────────────────────────────────
def rearm_path(cdir: pathlib.Path, branch: str) -> pathlib.Path:
    return lane_dir(cdir) / REARM / f"{_slug(branch)}.json"


def rearms(cdir: pathlib.Path, unreadable: list[str] | None = None) -> dict[str, dict[str, Any]]:
    """Every pending re-arm by branch. A file that is not a re-arm record is left out and its path appended to
    `unreadable` (the tick and `status` say it: a lost re-arm leaves its branch waiting)."""
    d = lane_dir(cdir) / REARM
    out: dict[str, dict[str, Any]] = {}
    for p in sorted(d.glob("*.json")) if d.is_dir() else []:
        try:
            rec = coord.read_json(p, None)
        except ValueError:
            rec = None
        if isinstance(rec, dict) and isinstance(rec.get("branch"), str) and isinstance(rec.get("head"), str):
            out[rec["branch"]] = rec
        elif unreadable is not None:
            unreadable.append(str(p))
    return out


def drop_stale_rearms(cdir: pathlib.Path, rows: list[dict[str, Any]], say: Say) -> dict[str, dict[str, Any]]:
    """The pending re-arms still at their branch's surveyed tip. One whose branch moved (a new commit: the lane treats
    it as a new head anyway) or left the survey is deleted and said; the rest are returned for `candidates`."""
    unreadable: list[str] = []
    pending = rearms(cdir, unreadable)
    for p in unreadable:
        say(f"land lane: {p} is not a re-arm record; left out (its branch waits for a new head): inspect it")
    tips = {r["name"]: r["tip"] for r in rows}
    for branch, rec in list(pending.items()):
        if tips.get(branch) != rec["head"]:
            rearm_path(cdir, branch).unlink(missing_ok=True)
            del pending[branch]
            where = f"at {str(tips[branch])[:12]}" if branch in tips else "no longer surveyed"
            say(f"land lane: {branch}: the re-arm at {rec['head'][:12]} is stale (the branch is {where}); dropped")
    return pending


def rearm(cdir: pathlib.Path, branch: str, why: str, say: Say = print,
          tip_for: Callable[[str | None], str | None] = tip_of) -> int:
    """Re-arm `branch` at its worktree's CURRENT tip: the next tick lands it again there, once. The enqueue unit's call,
    for a stop resolved with no new commit (module doc, `rearm/`). Refused while an attempt is running."""
    atts = by_branch(attempts(cdir)).get(branch, [])
    if atts and not finished(atts[-1]):
        say(f"land lane: {branch}: attempt {atts[-1]['id']} is still running; nothing to re-arm")
        return 1
    worktree = atts[-1].get("worktree") if atts else None
    if not worktree:
        try:
            s = coord.read_json(lane_dir(cdir) / SURVEY, None)
        except ValueError:
            s = None
        rows = s.get("rows", []) if isinstance(s, dict) else []
        worktree = next((r.get("worktree") for r in rows if r.get("name") == branch), None)
    tip = tip_for(worktree)
    if not tip:
        say(f"land lane: {branch}: no worktree tip to re-arm at (worktree {worktree or 'unknown'}: no attempt and no "
            "survey row names one); nothing re-armed")
        return 1
    path = rearm_path(cdir, branch)
    path.parent.mkdir(parents=True, exist_ok=True)
    coord.write_json(path, {"branch": branch, "head": tip, "why": why, "at": _now().isoformat(timespec="seconds")})
    say(f"land lane: {branch}: re-armed at {tip[:12]} ({why}); the next tick lands it again")
    return 0


# ── the queue watch (kb/Work PB3527) ───────────────────────────────────────────────────────────────────────────────
# A landing ends at `=== ENQUEUE: ENQUEUED ===`, which means only that the pull request was opened and handed to the
# merge queue (auto-merge on): GitHub queues it once its required checks pass. On 2026-10-22 PR #385's `ci-gate` job
# completed at 18:46:46Z and its check-run on the head commit stayed in_progress for 50 minutes, the queue empty and
# the pull request BLOCKED, and nothing looked. So every tick WATCHES each pull request the lane queued until the merge
# queue holds it, as a state machine over the facts the survey read on the same tick (queue_facts.read, the one
# reader): never a wait inside enqueue.py, never a second reader of GitHub.
QUEUE_GRACE_MIN = 10          # how long a pull request may stay out of the queue after its required checks' work ended
WATCHING, IN_QUEUE, MERGED, LEFT, ALERTED = "watching", "queued", "merged", "left", "alerted"
WATCH_DONE = (IN_QUEUE, MERGED, LEFT, ALERTED)
STUCK, NOT_QUEUED, CLOSED = "stuck-check-run", "not-queued", "closed"   # landing_measure.STALL_CAUSES
RERUN, REQUEUE, ALERT = "rerun", "requeue", "alert"                     # landing_measure.STALL_ACTS


@dataclasses.dataclass(frozen=True)
class WatchStep:
    """One read of the watch: its state after the read and, on a stalled read, what to do about it."""
    state: str
    why: str
    act: str = ""          # "" (nothing), RERUN, REQUEUE or ALERT
    cause: str = ""        # on a stalled read: STUCK, NOT_QUEUED or CLOSED
    run: int = 0           # RERUN: the workflow run of the stuck required check


def _iso(t: str) -> dt.datetime | None:
    try:
        v = dt.datetime.fromisoformat(str(t).replace("Z", "+00:00"))
    except ValueError:
        return None
    return v if v.tzinfo else None


def watch_step(att: dict[str, Any], row_state: str | None, pr: Any, now: dt.datetime) -> WatchStep:
    """THE QUEUE WATCH'S DECISION for the pull request a DONE attempt queued (pure: the self-test drives every arm).
    `row_state`: the branch's survey state (None: no longer surveyed); `pr`: its open pull request (queue_facts), None
    when none is open.
      merged, or in the queue                        the watch ends: the queue has it (a bounce is BOUNCED's, PB3023)
      a required check FAILED, the queue removed    the watch ends: BOUNCED, the enqueue unit's finisher (PB3096;
        it, or it conflicts with main (DIRTY)          a conflict is the branch's: no re-run or reopen clears it)
      the head moved past the attempt's              the watch ends: a new push is a new landing, watched afresh
      a required check still running (its job too)  watching: nothing is late yet
      not in the queue, the checks' work ended       a STALLED read once QUEUE_GRACE_MIN have passed since that work
        (or no check-run at all) or since the last     ended (a stuck check-run's JOB time), or since the last remedy:
        remedy                                         read 1 re-runs the stuck check's workflow run (`gh run rerun`;
                                                       a pull request with no check-run goes straight to read 2's
                                                       remedy), read 2 closes, reopens and re-queues the pull request
                                                       (enqueue.requeue), read 3 ALERTS the operator and acts no more
      no pull request open, the branch not on main   a stalled read (closed by hand?): it ALERTS at once"""
    w = att.get("watch") or {}
    reads = list(w.get("reads") or [])
    if row_state is None:
        return WatchStep(LEFT, "the branch is no longer surveyed (deleted after landing, or by hand)")
    if row_state == "LANDED":
        return WatchStep(MERGED, "the branch's work is on main")
    if pr is None:
        return WatchStep(ALERTED, f"no pull request is open for the branch and its work is not on main (closed by hand? "
                                  f"pull request #{w.get('pr') or att.get('pr') or '?'})", ALERT, CLOSED)
    end = str(att.get("end_head") or att.get("head") or "")
    if end and pr.head and not (pr.head.startswith(end) or end.startswith(pr.head)):
        return WatchStep(LEFT, f"pull request #{pr.number}'s head moved to {pr.head[:12]}: a new landing")
    if pr.in_queue:
        return WatchStep(IN_QUEUE, f"pull request #{pr.number} is in the merge queue")
    if pr.red or pr.conflicts or pr.bounced:
        why = (f"{pr.red.name} failed" if pr.red else "it conflicts with main: merge state DIRTY" if pr.conflicts
               else pr.bounced)
        return WatchStep(LEFT, f"pull request #{pr.number} is BOUNCED ({why}): the enqueue unit's finisher")
    # Running only while its job is SEEN running: a job the jobs API could not read (job_status '') is unknown, and an
    # unknown job must not hold the watch for ever, so its clock falls back to the enqueue, like no check-run at all.
    running = [c for c in pr.checks if not c.resolved and c.job_status not in ("", "completed")]
    if running:
        return WatchStep(WATCHING, f"pull request #{pr.number}: {', '.join(c.name for c in running)} still running")
    ends = [t for t in (_iso(c.done_at) for c in pr.checks) if t]
    if not ends:
        ends += [t for t in (_iso(att.get("ended", "")),) if t]
    ends += [t for t in (_iso(r.get("at", "")) for r in reads) if t]
    since = max(ends) if ends else now
    late = (now - since).total_seconds() / 60
    if late < QUEUE_GRACE_MIN:
        return WatchStep(WATCHING, f"pull request #{pr.number} not in the queue {late:.0f} min after its checks' work "
                                   f"ended (grace {QUEUE_GRACE_MIN} min)")
    stuck = next((c for c in pr.checks if c.stuck), None)
    cause = STUCK if stuck else NOT_QUEUED
    unknown = [c.name for c in pr.checks if not c.resolved and not c.stuck]
    what = (f"{stuck.name}'s check-run is {stuck.status.lower()} although its job completed at {stuck.job_completed_at}"
            if stuck else
            f"{', '.join(unknown)} unresolved and its job could not be read, and it is not in the queue "
            f"({pr.merge_state or '?'})" if unknown else
            f"its required checks resolved but it is not in the queue ({pr.merge_state or '?'})")
    n = len(reads) + 1
    if n >= 3:
        return WatchStep(ALERTED, f"stalled read {n}: {what}, after a re-run and a requeue", ALERT, cause)
    run = next((c.run for c in ([stuck] if stuck else list(pr.checks)) if c.run), 0)
    if n == 1 and run:
        return WatchStep(WATCHING, f"stalled read 1: {what}; re-running workflow run {run}", RERUN, cause, run)
    return WatchStep(WATCHING, f"stalled read {n}: {what}; closing, reopening and re-queueing it", REQUEUE, cause)


@dataclasses.dataclass(frozen=True)
class WatchEffects:
    """What a stalled read does, and its record (the self-test plants recorders): rerun(repo, run) and requeue(repo,
    pr) answer (ok, detail); alert(subject, body, ref); measure(pr, branch, read, cause, act, ok)."""
    rerun: Callable[[str, int], tuple[bool, str]]
    requeue: Callable[[str, int], tuple[bool, str]]
    alert: Callable[[str, str, str], None]
    measure: Callable[[int, str, int, str, str, bool], None]


def real_effects(cdir: pathlib.Path, say: Say) -> WatchEffects:
    import enqueue  # noqa: PLC0415  (the only file that queues a pull request: LandingPipelineDriftTests)
    import landing_measure  # noqa: PLC0415
    import ruleset  # noqa: PLC0415

    def measure(pr: int, branch: str, read: int, cause: str, act: str, ok: bool) -> None:
        try:
            landing_measure.record_stall(cdir / landing_measure.FILE, pr, branch, read, cause, act, ok)
        except (OSError, ValueError) as exc:   # the record is the instrument's, never the watch's stop; it is said
            say(f"land lane: PR #{pr}: the stall was NOT recorded in {landing_measure.FILE} ({exc})")
    return WatchEffects(
        rerun=lambda repo, run: enqueue.rerun_check(repo, run, gh=ruleset.real_gh),
        requeue=lambda repo, pr: enqueue.requeue(repo, pr, gh=ruleset.real_gh, state=enqueue.queue_state),
        alert=lambda subject, body, ref: notify_operator(cdir, subject, body, ref, say, kind="question"),
        measure=measure)


def watch_text(att: dict[str, Any]) -> str:
    """The queue watch on this attempt, in one clause (status, the survey's NOT QUEUED row)."""
    w = att.get("watch")
    if not w:
        return "the landing lane's queue watch has not read it yet (it reads on every tick)"
    reads = "; ".join(f"read {r['read']} {r['cause']}: {r['act']} {'done' if r.get('ok') else 'FAILED'} at {r['at']}"
                      for r in w.get("reads") or [])
    return (f"queue watch {w.get('state', WATCHING).upper()}"
            + (f" ({w.get('why')})" if w.get("why") else "") + (f" [{reads}]" if reads else ""))


def watch_queue(cdir: pathlib.Path, branches: dict[str, list[dict[str, Any]]], rows: list[dict[str, Any]],
                prs: dict[str, Any] | None, effects: WatchEffects, held: str | None, say: Say,
                now: dt.datetime | None = None) -> None:
    """Read each branch's latest attempt that ended ENQUEUED (exit 0) and whose watch has not ended, and act on a
    stalled read (watch_step). Unobserved facts (None) read nothing; a stop (`held`) lets it read but not act."""
    now = now or _now()
    if prs is None:
        if any(finished(a[-1]) and classify(a[-1]) == DONE and (a[-1].get("watch") or {}).get("state") not in WATCH_DONE
               for a in branches.values()):
            say("land lane: queue watch: GitHub's facts were not observed this tick; nothing read")
        return
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports this module back, lazily)
    # A branch whose work is on main reads LANDED here even while the survey still calls it IN FLIGHT (touched inside
    # --min-age, as a just-merged branch is): its pull request is merged, not closed by hand.
    states = {r["name"]: "LANDED" if r.get("content") in prune_worktrees.LANDED_CONTENT else r.get("state")
              for r in rows}
    for branch, atts in branches.items():
        att = atts[-1]
        if not finished(att) or classify(att) != DONE:
            continue
        w = dict(att.get("watch") or {})
        if w.get("state") in WATCH_DONE:
            continue
        pr = prs.get(branch)
        step = watch_step(att, states.get(branch), pr, now)
        if step.act and held:
            say(f"land lane: {branch}: queue watch: {step.why} — NOT acted on: {held}")
            continue
        w.update(state=step.state, why=step.why, pr=pr.number if pr else (w.get("pr") or att.get("pr")),
                 at=now.isoformat(timespec="seconds"))
        if step.act:
            number = int(w.get("pr") or 0)
            if step.act == RERUN:
                ok, detail = effects.rerun(pr.repo, step.run)
            elif step.act == REQUEUE:
                ok, detail = effects.requeue(pr.repo, pr.number)
            else:
                import queue_facts  # noqa: PLC0415
                prior = "; ".join(f"read {r['read']}: {r['act']} {'done' if r.get('ok') else 'FAILED'} ({r.get('detail')})"
                                  for r in w.get("reads") or [])
                effects.alert(f"Landing stalled: PR #{number or '?'} ({branch}) is not in the merge queue",
                              f"{step.why}. {queue_facts.describe(pr) if pr else 'No pull request is open.'} The "
                              f"landing lane's queue watch acts no more (kb/Work PB3527){': ' + prior if prior else ''}. "
                              "Read it on GitHub ("
                              + (f"gh pr view {number} --json mergeStateStatus,statusCheckRollup" if number else
                                 f"gh pr list --head {branch} --state all") + ") and land it or record why not.",
                              att["log"])
                ok, detail = True, "the operator alerted (a mailbox question)"
            read = len(w.get("reads") or []) + 1
            w["reads"] = [*(w.get("reads") or []), {"read": read, "cause": step.cause, "act": step.act, "ok": ok,
                                                    "detail": detail[:240], "at": now.isoformat(timespec="seconds")}]
            if number:
                effects.measure(number, branch, read, step.cause, step.act, ok)
            say(f"land lane: {branch}: queue watch: {step.why} — {step.act} {'done' if ok else 'FAILED'}: {detail}")
        elif w.get("state") != (att.get("watch") or {}).get("state"):
            say(f"land lane: {branch}: queue watch: {step.why}")
        update(att, {"watch": w})


# ── the tick ───────────────────────────────────────────────────────────────────────────────────────────────────────
def stop_reason(cdir: pathlib.Path) -> str | None:
    """Why no attempt may start now: the loop's STOP, its fleet stop `scratch\\STOP-loop` (the hard context cap and
    STOP), the owner's global stop, or HALT. Never `STOP-UNIT` alone (kb/Work PB3402): that is the soft context cap's
    signal to the running UNIT, and a landing is its own process, which adds nothing to the unit's context, so the
    unit's size must not hold finished work."""
    for path, what in ((cdir / "STOP", "the loop's STOP"),
                       (coord.fleet_stop(cdir / "scratch", "loop"), "the loop's fleet stop"),
                       (coord.global_stop(str(cdir)), "the owner's global STOP"),
                       (lane_dir(cdir) / HALT, "HALT (a run ended QUEUE NOT ACTIVE; `land_lane.py clear-halt`)")):
        if path.exists():
            return f"{what} ({path})"
    return None


def reap(cdir: pathlib.Path, atts: list[dict[str, Any]], alive: Callable[[int | None], bool], say: Say) -> None:
    for a in atts:
        if finished(a):
            continue
        if alive(a.get("pid")):
            continue
        # Its tip now is its end head: a runner that died after `enqueue.py land` committed (main merged in, the stamp)
        # leaves the branch IN FLIGHT in the survey, and only this record lets the lane retry it. A runner that wrote
        # its rc and then exited is not DIED: the fresh record decides, under the record's lock.
        update(a, lambda fresh: {} if "rc" in fresh else {
            "outcome": "DIED", "ended": _now().isoformat(timespec="seconds"),
            "verdict": last_verdict(pathlib.Path(a["log"])), "end_head": tip_of(a.get("worktree"))})
        if a.get("outcome") == "DIED":
            say(f"land lane: {a['branch']}: the runner of attempt {a['id']} is gone with no exit code (DIED; {a['log']})")
    for a in atts:
        if finished(a) and a.get("rc") == EXIT_QUEUE_NOT_ACTIVE and not a.get("halted"):
            halt = lane_dir(cdir) / HALT
            if not halt.exists():
                halt.write_text(f"{_now().isoformat(timespec='seconds')} {a['branch']} attempt {a['id']}: "
                                f"{a.get('verdict')}\n", encoding="utf-8", newline="\n")
                say(f"land lane: HALTED — {a['branch']} ended QUEUE NOT ACTIVE; no landing starts until the operator "
                    f"runs `land_lane.py clear-halt` once `ruleset.py check` reports ACTIVE")
                notify_operator(cdir, "Landing lane halted: the merge queue is not ACTIVE",
                                f"enqueue.py land {a['branch']} ended QUEUE NOT ACTIVE (exit 8): "
                                f"{a.get('verdict')}. The landing lane starts no landing until the queue is ACTIVE "
                                "(`python scripts/orchestrator/ruleset.py check`) and the operator runs "
                                "`python scripts/orchestrator/land_lane.py clear-halt`.", a["log"], say)
            update(a, {"halted": True})


def candidates(rows: list[dict[str, Any]], order: list[str], branches: dict[str, list[dict[str, Any]]],
               rearmed: dict[str, dict[str, Any]] | None = None) -> list[dict[str, Any]]:
    """The WAITING TO LAND rows in landing order, then every branch whose last attempt the lane may retry and whose tip
    is still that attempt's end head, unless the queue holds it or it landed, then every branch the enqueue unit
    re-armed at its current tip (a BOUNCED one included: `enqueue.py land` reuses its open pull request and queues it
    again), unless the queue holds it or it landed."""
    named = {r["name"]: r for r in rows}
    out = [named[n] for n in order if n in named and named[n]["state"] == "WAITING TO LAND"]
    seen = {r["name"] for r in out}
    for branch, atts in branches.items():
        r = named.get(branch)
        last = atts[-1]
        if branch in seen or r is None or not finished(last) or classify(last) not in RETRYABLE:
            continue
        if r["state"] in ("STOPPED", "ENQUEUED", "NOT QUEUED", "BOUNCED", "LANDED") or r["tip"] != last.get("end_head") \
                or not r["worktree"]:
            continue
        out.append(r)
        seen.add(branch)
    for branch, rec in (rearmed or {}).items():
        r = named.get(branch)
        if branch in seen or r is None or not r["worktree"] or r["state"] in ("ENQUEUED", "NOT QUEUED", "LANDED"):
            continue
        if r["tip"] == rec.get("head"):
            out.append(r)
    return out


def launch(cdir: pathlib.Path, repo: pathlib.Path, row: dict[str, Any], why: str,
           start: Callable[[list[str], pathlib.Path], int], argv: list[str] | None = None) -> dict[str, Any]:
    """Start one attempt. Only `tick` calls it, under the tick lock, so the attempt's NUMBER (one past every attempt
    recorded; `attempt_order`) is taken by one process at a time and names one attempt."""
    now = _now()
    d = lane_dir(cdir) / ATTEMPTS
    d.mkdir(parents=True, exist_ok=True)
    n = max((attempt_order(p)[0] for p in d.glob("*.json")), default=0) + 1
    aid = f"{n:06d}-{now:%Y%m%dT%H%M%SZ}-{_slug(row['name'])}"
    path = d / f"{aid}.json"
    log = cdir / "logs" / f"land-lane-{aid}.log"
    rec = {"id": aid, "branch": row["name"], "head": row["tip"], "worktree": row["worktree"], "why": why,
           "argv": argv or [sys.executable, str(HERE / "enqueue.py"), "land", row["name"], "--worktree", row["worktree"]],
           "cwd": str(repo), "log": str(log), "started": now.isoformat(timespec="seconds"), "pid": None,
           "collected": False}
    coord.write_json(path, rec)   # a new record: no other writer knows its path yet
    rec["_path"] = str(path)
    pid = start([sys.executable, str(pathlib.Path(__file__).resolve()), "run", str(path)], log)
    rearm_path(cdir, row["name"]).unlink(missing_ok=True)   # a re-arm buys ONE attempt: this one
    # The runner may already have finished (a fast refusal) and written its outcome: `update` keeps it, adding the pid.
    return update(rec, {"pid": pid})


def tick(cdir: pathlib.Path, repo: pathlib.Path, *,
         survey: Callable[[pathlib.Path, pathlib.Path], tuple[list, list[str], dict | None]] = survey_rows,
         start: Callable[[list[str], pathlib.Path], int] = spawn_detached,
         alive: Callable[[int | None], bool] = pid_alive,
         wait_free: Callable[[int | None, str], tuple[bool, str]] = wait_free,
         argv_for: Callable[[dict[str, Any]], list[str] | None] = lambda row: None,
         effects: WatchEffects | None = None, now: dt.datetime | None = None, say: Say = print) -> int:
    d = lane_dir(cdir)
    d.mkdir(parents=True, exist_ok=True)
    lock = ExclusiveLock.try_take(d / TICK_LOCK, f"land_lane.py tick (pid {os.getpid()}, {_now():%Y-%m-%d %H:%M:%S}Z)")
    if lock is None:
        say(f"land lane: a tick is already running ({ExclusiveLock.holder(d / TICK_LOCK)}); nothing done")
        return 0
    with lock:
        unreadable: list[str] = []
        atts = attempts(cdir, unreadable)
        for p in unreadable:
            say(f"land lane: {p} is not an attempt record; left out (its branch may be landed again): inspect it")
        reap(cdir, atts, alive, say)
        try:
            rows, order, prs = survey(repo, cdir)
        except (LaneError, OSError, subprocess.SubprocessError) as exc:
            say(f"land lane: no survey, nothing launched: {exc}")
            return 1
        coord.write_json(d / SURVEY, {"at": _now().isoformat(timespec="seconds"), "rows": rows, "landing_order": order})
        branches = by_branch(atts)
        why = stop_reason(cdir)
        # THE QUEUE WATCH (kb/Work PB3527), on the facts this survey read: under a stop it reads, and acts on nothing.
        watch_queue(cdir, branches, rows, prs, effects or real_effects(cdir, say), why, say, now)
        if why:
            say(f"land lane: {why}: no landing starts (a running one finishes)")
            return 0
        pending = drop_stale_rearms(cdir, rows, say)
        failed = 0
        for row in candidates(rows, order, branches, pending):
            dec = decide(row["tip"], branches.get(row["name"], []),
                         lambda r=row: (wait_free((branches.get(r["name"]) or [{}])[-1].get("rc"), r["worktree"])
                                        if r["worktree"] else (False, "no worktree")),
                         pending.get(row["name"]))
            if not dec.launch:
                if branches.get(row["name"]) and classify(branches[row["name"]][-1]) == FREE \
                        and finished(branches[row["name"]][-1]):
                    say(f"land lane: {row['name']}: {dec.why}")   # why an exit 3 or 12 still waits, every tick
                continue
            if not row["worktree"]:
                say(f"land lane: {row['name']}: WAITING TO LAND with no worktree; not launched (enqueue.py needs one)")
                continue
            try:
                rec = launch(cdir, repo, row, dec.why, start, argv_for(row))
            except OSError as exc:
                failed += 1
                say(f"land lane: {row['name']}: the runner did not start ({exc})")
                continue
            branches.setdefault(row["name"], []).append(rec)
            say(f"land lane: {row['name']} @{row['tip'][:12]}: enqueue.py land started ({dec.why}; pid {rec['pid']}, "
                f"{rec['log']})")
        return 1 if failed else 0


# ── the runner ─────────────────────────────────────────────────────────────────────────────────────────────────────
def run_attempt(path: pathlib.Path) -> int:
    """Run the attempt's argv to completion, its output appended to the attempt's log (handed over explicitly: a
    detached Windows process's own standard handles are not inherited by a child it starts without redirection), then
    record rc, the verdict and the branch's tip after it."""
    rec = coord.read_json(path, None)
    if not isinstance(rec, dict):
        print(f"land lane: {path} is not an attempt record")
        return 2
    with open(rec["log"], "ab") as log:
        rc = subprocess.run(rec["argv"], cwd=rec.get("cwd") or None, stdin=subprocess.DEVNULL, stdout=log,
                            stderr=subprocess.STDOUT).returncode
    current = update({**rec, "_path": str(path)},
                     {"rc": rc, "ended": _now().isoformat(timespec="seconds"), "end_head": tip_of(rec.get("worktree")),
                      "verdict": last_verdict(pathlib.Path(rec["log"])), "pr": queued_pr(pathlib.Path(rec["log"]))})
    print(f"land lane: attempt {rec['id']} ended exit {rc}: {current['verdict'] or 'no verdict line'}", flush=True)
    return rc


# ── what the enqueue unit and next_unit.py read ────────────────────────────────────────────────────────────────────
ACTION = {STOPPED: "the enqueue unit's: step 4 finisher (UNREVIEWED: step 2 reviewer; a repeated exit 2 or 9: handoff)",
          DONE: "the lane's queue watch follows its pull request into the merge queue; the enqueue unit's: file its "
                "leads (step 5), complete its landing record (step 6b), then collect it",
          FREE: "the lane retries it at this head once what it waits on is free (exit 3: landing_check.py passes, the "
                "in-flight work it shares a file with is gone; exit 12: the worktree's gate lock is free)",
          ONCE: "the lane retries it once at this head",
          HALTED: "HALT: the operator clears it once the merge queue is ACTIVE",
          "running": "being landed"}


@dataclasses.dataclass
class Lane:
    survey_at: dt.datetime | None
    rows: list[dict[str, Any]]
    latest: list[tuple[dict[str, Any], str]]   # each branch's latest attempt and its effective class
    halt: str | None
    unreadable: list[str]                      # attempt and re-arm files that are not records
    rearms: dict[str, dict[str, Any]]          # the enqueue unit's pending re-arms, by branch

    def survey_age_min(self, now: dt.datetime | None = None) -> float | None:
        return None if self.survey_at is None else ((now or _now()) - self.survey_at).total_seconds() / 60


def view(cdir: pathlib.Path) -> Lane:
    d = lane_dir(cdir)
    try:
        s = coord.read_json(d / SURVEY, None)
    except ValueError:
        s = None
    at = dt.datetime.fromisoformat(s["at"]) if isinstance(s, dict) and s.get("at") else None
    unreadable: list[str] = []
    latest = [(atts[-1], effective(atts)) for atts in by_branch(attempts(cdir, unreadable)).values()]
    halt = (d / HALT).read_text(encoding="utf-8").strip() if (d / HALT).exists() else None
    pending = rearms(cdir, unreadable)
    return Lane(at, (s or {}).get("rows", []) if isinstance(s, dict) else [], latest, halt, unreadable, pending)


@dataclasses.dataclass(frozen=True)
class AgentWork:
    reasons: list[str]
    survey_age_min: float | None   # None: the lane has never surveyed


def agent_work(cdir: pathlib.Path, now: dt.datetime | None = None) -> AgentWork:
    """What only the enqueue unit's agents (or its clerk and its records) can do: rows WAITING FOR REVIEW or BOUNCED in
    a survey younger than SURVEY_MAX_AGE_MIN (a BOUNCED one only while the lane is neither landing it nor re-armed to:
    its finisher already resolved it), STOPPED attempts not yet collected, and ENQUEUED attempts not yet collected
    (their leads and landing records). A row the survey reads STOPPED is counted through its attempt, never by its
    state: uncollected, it is the unit's work; collected, the unit has already dispatched its owner (kb/Work PB3125)."""
    lane = view(cdir)
    age = lane.survey_age_min(now)
    reasons = []
    landing = {a["branch"] for a, c in lane.latest if c == "running"} | set(lane.rearms)
    if age is not None and age <= SURVEY_MAX_AGE_MIN:
        for state in ("WAITING FOR REVIEW", "BOUNCED"):
            names = [r["name"] for r in lane.rows if r.get("state") == state
                     and not (state == "BOUNCED" and r["name"] in landing)]
            if names:
                reasons.append(f"{state.lower()}: {', '.join(names)}")
    stopped = [f"{a['branch']} ({outcome(a)})" for a, c in lane.latest if c == STOPPED and not a.get("collected")]
    queued = [a["branch"] for a, c in lane.latest if c == DONE and not a.get("collected")]
    if stopped:
        reasons.append(f"stopped landings: {', '.join(stopped)}")
    if queued:
        reasons.append(f"landed by the lane, leads and records to complete: {', '.join(queued)}")
    return AgentWork(reasons, age)


def outcome(a: dict[str, Any]) -> str:
    """`exit N`, `DIED` or the running runner's pid: how an attempt stands, in one word."""
    if "rc" in a:
        return f"exit {a['rc']}"
    return "DIED" if a.get("outcome") == "DIED" else f"pid {a.get('pid')}"


def status_lines(cdir: pathlib.Path, now: dt.datetime | None = None) -> list[str]:
    lane = view(cdir)
    age = lane.survey_age_min(now)
    n = {c: sum(1 for _, k in lane.latest if k == c) for c in ("running", STOPPED, DONE, FREE, ONCE, HALTED)}
    survey = ("no survey yet: the lane has not ticked" if age is None else
              f"survey {age:.0f} min old" + (f" — STALE (over {SURVEY_MAX_AGE_MIN} min): the lane is not ticking; run "
                                              "`land_lane.py tick`" if age > SURVEY_MAX_AGE_MIN else ""))
    watches = [(a.get("watch") or {}).get("state") for a, k in lane.latest if k == DONE]
    stalled = sum(1 for a, k in lane.latest if k == DONE and (a.get("watch") or {}).get("state") == WATCHING
                  and (a.get("watch") or {}).get("reads"))
    out = [f"=== LAND LANE: {n['running']} running · {n[STOPPED]} stopped · {n[DONE]} enqueued "
           f"({watches.count(IN_QUEUE) + watches.count(MERGED)} in the queue or merged, {stalled} stalled, "
           f"{watches.count(ALERTED)} alerted) · {n[FREE] + n[ONCE]} to retry · {survey}"
           f"{' · HALTED: ' + lane.halt if lane.halt else ''} ==="]
    out += [f"UNREADABLE       {p}: not a lane record (inspect it; its branch may be landed again, or wait)"
            for p in lane.unreadable]
    out += [f"{'REARMED':16} {b} @{str(r.get('head'))[:12]}: {r.get('why') or '-'} — the next tick lands it again at "
            f"this head (re-armed {r.get('at')})" for b, r in sorted(lane.rearms.items())]
    order = {"running": 0, STOPPED: 1, HALTED: 2, FREE: 3, ONCE: 3, DONE: 4}
    for a, cls in sorted(lane.latest, key=lambda t: (order.get(t[1], 9), t[0]["branch"])):
        mark = " [collected]" if a.get("collected") else ""
        watch = f" [{watch_text(a)}]" if cls == DONE else ""
        out.append(f"{cls.upper():16} {a['branch']} @{str(a.get('head'))[:12]} {outcome(a)}: {a.get('verdict') or '-'} — "
                   f"{ACTION.get(cls, '')}{mark}{watch} (attempt {a['id']}; {a['log']})")
    return out


def collect(cdir: pathlib.Path, branches: list[str], say: Say = print) -> int:
    """Mark each named branch's finished attempts handled by the enqueue unit; a running one is never collected."""
    by = by_branch(attempts(cdir))
    missing = 0
    for b in branches:
        done = [a for a in by.get(b, []) if finished(a) and not a.get("collected")]
        if not done:
            say(f"land lane: {b}: no finished, uncollected attempt")
            missing += 1
            continue
        for a in done:
            update(a, {"collected": True})
        say(f"land lane: {b}: {len(done)} attempt(s) collected")
    return 1 if missing else 0


# ── self-test ──────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    root = scratch_dir("land-lane-selftest-")
    try:
        def fresh(name: str) -> pathlib.Path:
            c = root / name
            (c / "scratch").mkdir(parents=True)
            return c

        def row(name: str, state: str, tip: str) -> dict[str, Any]:
            return {"name": name, "state": state, "tip": tip, "worktree": str(root / f"wt-{name}"), "why": "",
                    "lead": None}

        class Fake:
            """A planted survey, a recording launcher and a liveness oracle: no git, no GitHub, no process."""
            def __init__(self, rows: list[dict[str, Any]]):
                self.rows, self.started, self.live, self.free = rows, [], set(), False
                self.asked: list[str] = []
                self.prs: dict[str, Any] | None = None     # GitHub's facts: unobserved unless a case plants them
                self.now: dt.datetime | None = None
                self.effects: list[tuple] = []

            def survey(self, repo: pathlib.Path, cdir: pathlib.Path) -> tuple[list, list[str], dict | None]:
                return self.rows, [r["name"] for r in self.rows if r["state"] == "WAITING TO LAND"], self.prs

            def watch_effects(self) -> WatchEffects:
                rec = self.effects.append
                return WatchEffects(rerun=lambda repo, run: rec(("rerun", repo, run)) or (True, "re-run requested"),
                                    requeue=lambda repo, pr: rec(("requeue", repo, pr)) or (True, "requeued"),
                                    alert=lambda subject, body, ref: rec(("alert", subject, body)),
                                    measure=lambda pr, b, read, cause, act, ok: rec(("measure", pr, b, read, cause, act)))

            def start(self, argv: list[str], log: pathlib.Path) -> int:
                self.started.append(json.loads(pathlib.Path(argv[-1]).read_text(encoding="utf-8"))["branch"])
                pid = 40000 + len(self.started)
                self.live.add(pid)
                return pid

            def alive(self, pid: int | None) -> bool:
                return pid in self.live

            def overlap(self, rc: int | None, wt: str) -> tuple[bool, str]:
                self.asked.append(wt)
                return self.free, "shares scripts/x.py with w1069g (dispatched earlier)"

            def tick(self, cdir: pathlib.Path) -> list[str]:
                before = len(self.started)
                out: list[str] = []
                tick(cdir, root, survey=self.survey, start=self.start, alive=self.alive, wait_free=self.overlap,
                     effects=self.watch_effects(), now=self.now, say=out.append)
                self.said = out
                return self.started[before:]

        def finish(cdir: pathlib.Path, branch: str, rc: int, verdict: str, end_head: str | None = None) -> None:
            """The runner's own completion of the branch's running attempt (the record `run_attempt` writes)."""
            for a in attempts(cdir):
                if a["branch"] == branch and not finished(a):
                    update(a, {"rc": rc, "verdict": verdict, "ended": _now().isoformat(),
                               "end_head": end_head or a["head"]})

        # 1. planted survey rows: exactly the WAITING TO LAND rows start, in survey order; a second tick starts none.
        c = fresh("survey")
        f = Fake([row("w1a", "WAITING TO LAND", "a1"), row("w1b", "WAITING FOR REVIEW", "b1"),
                  row("w1c", "WAITING TO LAND", "c1"), row("w1d", "ENQUEUED", "d1"), row("w1e", "IN FLIGHT", "e1")])
        expect("planted survey: the WAITING TO LAND rows start, in survey order, and nothing else", f.tick(c) == ["w1a", "w1c"],
               str(f.started))
        s = coord.read_json(lane_dir(c) / SURVEY, {})
        expect("the tick writes the survey it decided from (survey.json)",
               [r["name"] for r in s.get("rows", [])] == ["w1a", "w1b", "w1c", "w1d", "w1e"], str(s))
        expect("a second tick while both run starts nothing (being landed)", f.tick(c) == [], str(f.started))
        w = agent_work(c)
        expect("agent work: the row WAITING FOR REVIEW, and nothing for the runs in flight",
               w.reasons == ["waiting for review: w1b"], str(w.reasons))

        # 2. exit 5 (GATE RED) is never relaunched at the same head; a new head (a finisher's commit) is eligible.
        f.rows[0] = row("w1a", "DECISION", "a2")                  # enqueue merged main in: the survey no longer lists it
        finish(c, "w1a", 5, "GATE RED (Windows) on a2", end_head="a2")
        f.live.discard(40001)
        f.rows[0] = row("w1a", "WAITING TO LAND", "a2")           # even listed again at that head
        expect("exit 5 is never relaunched at the same head", f.tick(c) == [] and f.tick(c) == [], str(f.started))
        w = agent_work(c)
        expect("a stopped attempt is the enqueue unit's agent work until collected",
               any(r.startswith("stopped landings: w1a (exit 5)") for r in w.reasons), str(w.reasons))
        expect("status names the stopped attempt and its finisher", any(
            ln.startswith("STOPPED") and "w1a" in ln and "step 4 finisher" in ln for ln in status_lines(c)),
            "\n".join(status_lines(c)))
        collect(c, ["w1a"], say=lambda s: None)
        expect("collect clears it from the agent work, and it stays unlaunched at that head",
               not any("w1a" in r for r in agent_work(c).reasons) and f.tick(c) == [], str(agent_work(c).reasons))
        f.rows[0] = row("w1a", "STOPPED", "a2")                   # the survey's own reading of that stop (PB3125)
        expect("a row the survey reads STOPPED is not launched, and once collected is no agent work again",
               f.tick(c) == [] and not any("w1a" in r for r in agent_work(c).reasons), str(agent_work(c).reasons))
        f.rows[0] = row("w1a", "WAITING TO LAND", "a3")           # the finisher's fix, reviewed CLEAN
        expect("a new head is eligible again", f.tick(c) == ["w1a"], str(f.started))

        # 3. exit 3 (the landing check's overlap): retried at the same head only once landing_check passes.
        c3 = fresh("overlap")
        f3 = Fake([row("w2a", "WAITING TO LAND", "h1")])
        f3.tick(c3)
        finish(c3, "w2a", 3, "NOT QUEUED (exit 3, above)", end_head="h2")
        f3.live.clear()
        f3.rows[0] = row("w2a", "IN FLIGHT", "h2")                # it committed (main merged, the stamp): IN FLIGHT
        expect("exit 3 while the overlap remains: not relaunched, and the check is asked",
               f3.tick(c3) == [] and f3.asked == [str(root / "wt-w2a")], f"{f3.started} {f3.asked}")
        expect("exit 3 is the lane's to retry, not the enqueue unit's", agent_work(c3).reasons == [],
               str(agent_work(c3).reasons))
        f3.free = True
        expect("exit 3 once the overlap is gone: retried at the same head", f3.tick(c3) == ["w2a"], str(f3.started))

        # 3b. exit 12 (GATE BUSY: another gate held the worktree's gate lock, kb/Work PB3527): never the enqueue unit's
        #     and never a red; retried at the same head once the lock is free.
        c3b = fresh("busy")
        f3b = Fake([row("w2b", "WAITING TO LAND", "g1")])
        f3b.tick(c3b)
        finish(c3b, "w2b", EXIT_GATE_BUSY, "GATE BUSY (Windows) on g2 — another gate holds the worktree's gate lock",
               end_head="g2")
        f3b.live.clear()
        f3b.rows[0] = row("w2b", "IN FLIGHT", "g2")
        expect("exit 12 while the gate lock is held: not relaunched, not the enqueue unit's work, and status says the "
               "lane retries it once the lock is free",
               f3b.tick(c3b) == [] and agent_work(c3b).reasons == []
               and any("w2b" in ln and "gate lock is free" in ln for ln in status_lines(c3b)), str(status_lines(c3b)))
        f3b.free = True
        expect("exit 12 once the gate lock is free: retried at the same head", f3b.tick(c3b) == ["w2b"], str(f3b.started))
        lock_wt = ScratchRepo.init(root / "wt-lock").path
        import run_gate_legs  # noqa: PLC0415
        held = ExclusiveLock.try_take(run_gate_legs.Host(lock_wt).lock_path(), "gate (pid 356316)")
        try:
            while_held = wait_free(EXIT_GATE_BUSY, str(lock_wt))
        finally:
            held.release()
        after = wait_free(EXIT_GATE_BUSY, str(lock_wt))
        expect("wait_free for exit 12 reads the gate driver's own lock of the worktree: held names its holder, then free",
               while_held[0] is False and "pid 356316" in while_held[1] and after == (True, ""), f"{while_held} {after}")

        # 4. exit 2 / 9 retried ONCE at the same head; the second is STOPPED (the handoff's).
        c4 = fresh("once")
        f4 = Fake([row("w3a", "WAITING TO LAND", "k1")])
        f4.tick(c4)
        finish(c4, "w3a", 9, "NOT QUEUED (exit 9, above)")
        f4.live.clear()
        retried = f4.tick(c4)
        finish(c4, "w3a", 2, "NOT QUEUED — main moved under every one of 3 catch-up merges; re-run")
        f4.live.clear()
        expect("exit 9 is retried once, and a second exit 2 at that head is not", retried == ["w3a"] and f4.tick(c4) == [],
               str(f4.started))
        expect("the repeated exit is STOPPED: the enqueue unit's", any(
            r.startswith("stopped landings: w3a") for r in agent_work(c4).reasons), str(agent_work(c4).reasons))

        # 4b. a branch's attempts are ordered by their NUMBERS, never the clock (kb/Work PB3536): with the clock STOPPED,
        #     a launch and its retry in one clock tick are two records, in launch order. A stamp-named id gave both ONE
        #     file name, and the retry's record overwrote the first.
        c4b = fresh("one-tick")
        stopped_at = dt.datetime(2026, 10, 23, 2, 21, 9, 187360, dt.timezone.utc)
        real_now = _now
        globals()["_now"] = lambda: stopped_at
        try:
            f4b = Fake([row("w3b", "WAITING TO LAND", "m1")])
            first4b = f4b.tick(c4b)
            finish(c4b, "w3b", 9, "NOT QUEUED (exit 9, above)")
            f4b.live.clear()
            again4b = f4b.tick(c4b)
        finally:
            globals()["_now"] = real_now
        (lane_dir(c4b) / ATTEMPTS / "20261022T225440187360Z-w3b.json").write_text(
            json.dumps({"id": "20261022T225440187360Z-w3b", "branch": "w3b", "head": "m0", "rc": 2}),
            encoding="utf-8", newline="\n")   # an attempt named before the numbers
        atts4b = [(a["id"], a.get("rc")) for a in attempts(c4b)]
        expect("a launch and its retry in one stopped clock tick are two attempts, ordered by their numbers; one named "
               "before the numbers is the oldest",
               first4b == ["w3b"] and again4b == ["w3b"] and [i for i, _ in atts4b] == [
                   "20261022T225440187360Z-w3b", "000001-20261023T022109Z-w3b", "000002-20261023T022109Z-w3b"]
               and [rc for _, rc in atts4b] == [2, 9, None], str(atts4b))

        # 5. a runner that died with no rc is DIED, then retried once.
        c5 = fresh("died")
        f5 = Fake([row("w4a", "WAITING TO LAND", "m1")])
        f5.tick(c5)
        f5.live.clear()
        expect("a runner gone with no exit code is reaped DIED and retried once at its head",
               f5.tick(c5) == ["w4a"] and any(a.get("outcome") == "DIED" for a in attempts(c5)),
               f"{f5.started} {[a.get('outcome') for a in attempts(c5)]}")

        # 6. exit 8 halts every attempt and names the operator; clear-halt re-arms the lane.
        c6 = fresh("halt")
        f6 = Fake([row("w5a", "WAITING TO LAND", "q1")])
        f6.tick(c6)
        finish(c6, "w5a", 8, "QUEUE NOT ACTIVE (ruleset: DRIFT — x)")
        f6.live.clear()
        pathlib.Path(attempts(c6)[0]["log"]).parent.mkdir(parents=True, exist_ok=True)
        pathlib.Path(attempts(c6)[0]["log"]).write_text("", encoding="utf-8", newline="\n")
        f6.rows.append(row("w5b", "WAITING TO LAND", "r1"))
        expect("exit 8 stops all attempts: none starts, the other ready branch included", f6.tick(c6) == [] and
               (lane_dir(c6) / HALT).exists(), f"{f6.started} {f6.said}")
        import mailbox  # noqa: PLC0415  (this directory's: the sessions' mailbox)
        inbox_files = list(mailbox.inbox(c6, "operator").glob("*.json"))
        expect("exit 8 is named to the operator (one mailbox message)", len(inbox_files) == 1,
               f"{inbox_files} {f6.said}")
        expect("a later tick while halted still starts nothing", f6.tick(c6) == [], str(f6.started))
        (lane_dir(c6) / HALT).unlink()
        expect("after clear-halt both branches start, the halted one again at its head",
               sorted(f6.tick(c6)) == ["w5a", "w5b"], str(f6.started))

        # 7. The loop's STOP, the hard cap's pair (STOP-UNIT with the loop's fleet stop) and the global stop: nothing
        #    starts, yet the tick still reaps and surveys.
        for name, paths in (("STOP", [lambda c: c / "STOP"]),
                            ("the hard cap's STOP-UNIT and fleet stop",
                             [lambda c: coord.stop_unit(str(c)), lambda c: coord.fleet_stop(c / "scratch", "loop")]),
                            ("global STOP", [lambda c: coord.global_stop(str(c))])):
            cs = fresh(f"stop-{name.split()[0]}-{len(paths)}")
            fs = Fake([row("w6a", "WAITING TO LAND", "s1")])
            for path in paths:
                path(cs).parent.mkdir(parents=True, exist_ok=True)
                path(cs).write_text("", encoding="utf-8", newline="\n")
            expect(f"{name}: no attempt starts, and the survey is still written",
                   fs.tick(cs) == [] and (lane_dir(cs) / SURVEY).exists(), str(fs.said))
        # 7b. STOP-UNIT alone is the SOFT context cap's signal to the running unit (kb/Work PB3402): the lane lands on.
        cu = fresh("stop-unit-soft")
        fu = Fake([row("w6b", "WAITING TO LAND", "s2")])
        coord.stop_unit(str(cu)).write_text("", encoding="utf-8", newline="\n")
        expect("STOP-UNIT alone (the soft cap): the WAITING TO LAND branch starts", fu.tick(cu) == ["w6b"], str(fu.said))

        # 8. ENQUEUED: nothing more at that head; its leads and record are the enqueue unit's until collected.
        c8 = fresh("enqueued")
        f8 = Fake([row("w7a", "WAITING TO LAND", "t1")])
        f8.tick(c8)
        finish(c8, "w7a", 0, "ENQUEUED", end_head="t2")
        f8.live.clear()
        f8.rows[0] = row("w7a", "WAITING TO LAND", "t2")
        expect("ENQUEUED at this head: never relaunched", f8.tick(c8) == [], str(f8.started))
        expect("an ENQUEUED attempt is agent work (leads, the landing record) until collected",
               agent_work(c8).reasons == ["landed by the lane, leads and records to complete: w7a"],
               str(agent_work(c8).reasons))
        collect(c8, ["w7a"], say=lambda s: None)
        expect("collected: no agent work is left", agent_work(c8).reasons == [], str(agent_work(c8).reasons))
        old = coord.read_json(lane_dir(c8) / SURVEY, {})
        old["at"] = (_now() - dt.timedelta(minutes=SURVEY_MAX_AGE_MIN + 5)).isoformat()
        coord.write_json(lane_dir(c8) / SURVEY, old)
        expect("a survey older than SURVEY_MAX_AGE_MIN is reported stale (the lane is not ticking)",
               agent_work(c8).survey_age_min > SURVEY_MAX_AGE_MIN and "STALE" in status_lines(c8)[0], status_lines(c8)[0])

        # 9. one tick at a time: a held tick lock makes a second tick a no-op.
        c9 = fresh("lock")
        f9 = Fake([row("w8a", "WAITING TO LAND", "u1")])
        held = ExclusiveLock.try_take(lane_dir(c9) / TICK_LOCK, "another tick")
        try:
            expect("a second tick while one holds the lock does nothing", f9.tick(c9) == [] and
                   "already running" in " ".join(f9.said), str(f9.said))
        finally:
            held.release()
        (lane_dir(c9) / ATTEMPTS).mkdir(parents=True, exist_ok=True)
        (lane_dir(c9) / ATTEMPTS / "torn.json").write_text("{not json", encoding="utf-8", newline="\n")
        f9.tick(c9)
        expect("an attempt file that is not a record is said by the tick and by status, never dropped in silence",
               any("torn.json" in s and "not an attempt record" in s for s in f9.said)
               and any(ln.startswith("UNREADABLE") and "torn.json" in ln for ln in status_lines(c9)),
               f"{f9.said} {status_lines(c9)}")

        # 10. the real launch path: a DETACHED runner records rc, the verdict and the end head of its argv.
        c10 = fresh("runner")
        runner_wt = ScratchRepo.init(root / "wt-runner")
        runner_wt.commit("x", allow_empty=True)
        wt, head = runner_wt.path, runner_wt.git("rev-parse", "HEAD")
        fake = [sys.executable, "-c", "print('=== ENQUEUE: GATE RED (Windows) on x (w9a) ==='); raise SystemExit(5)"]
        r = {"name": "w9a", "state": "WAITING TO LAND", "tip": head, "worktree": str(wt), "why": "", "lead": None}
        out: list[str] = []
        tick(c10, root, survey=lambda repo, cdir: ([r], ["w9a"], None), argv_for=lambda row: fake, say=out.append)
        deadline = time.monotonic() + 120
        while time.monotonic() < deadline and not all(finished(a) for a in attempts(c10)):
            time.sleep(0.2)
        a = (attempts(c10) or [{}])[0]
        expect("a detached runner runs the attempt and records rc 5, the verdict and the end head",
               a.get("rc") == 5 and a.get("verdict") == "GATE RED (Windows) on x" and a.get("end_head") == head,
               f"{a} {out}")

        # 11. a runner that died AFTER enqueue.py committed: the survey reads the branch IN FLIGHT at its new tip, and
        #     the lane retries it from its own record (the DIED attempt's end head is the worktree's tip at the reap).
        c11 = fresh("died-after-commit")
        f11 = Fake([{"name": "w9b", "state": "WAITING TO LAND", "tip": "before-merge", "worktree": str(wt), "why": "",
                     "lead": None}])
        f11.tick(c11)
        f11.live.clear()
        f11.rows[0] = {**f11.rows[0], "state": "IN FLIGHT", "tip": head}
        expect("a runner that died after committing is retried from its record at the branch's new tip",
               f11.tick(c11) == ["w9b"] and attempts(c11)[0].get("end_head") == head,
               f"{f11.started} {attempts(c11)[0]}")

        # 12. a stop resolved WITHOUT a new commit (review R1-1): the enqueue unit's `rearm` lands it again at the same
        #     head, once; a BOUNCED branch is the enqueue unit's only until it is re-armed or being landed.
        c12 = fresh("rearm")
        f12 = Fake([row("w10a", "WAITING TO LAND", "v1"), row("w10b", "WAITING TO LAND", "x1")])
        f12.tick(c12)
        finish(c12, "w10a", 5, "GATE RED (Windows) on v1: a red already on main")
        finish(c12, "w10b", 0, "ENQUEUED", end_head="x2")
        f12.live.clear()
        f12.rows = [row("w10a", "DECISION", "v1"), row("w10b", "BOUNCED", "x2")]   # the merge group removed w10b
        collect(c12, ["w10a", "w10b"], say=lambda s: None)
        expect("a collected exit 5 at an unchanged head, and a BOUNCED branch, are not relaunched by themselves",
               f12.tick(c12) == [] and agent_work(c12).reasons == ["bounced: w10b"], f"{f12.started} {agent_work(c12).reasons}")
        tips = {str(root / "wt-w10a"): "v1", str(root / "wt-w10b"): "x2"}
        said: list[str] = []
        expect("rearm records a finished, collected exit 5 at its worktree's tip",
               rearm(c12, "w10a", "main carries the fix", say=said.append, tip_for=tips.get) == 0
               and coord.read_json(rearm_path(c12, "w10a"), {}).get("head") == "v1", str(said))
        expect("rearm records the BOUNCED branch at its tip, and it stops being the enqueue unit's work",
               rearm(c12, "w10b", "bounce: infrastructure (runner lost)", say=said.append, tip_for=tips.get) == 0
               and agent_work(c12).reasons == [] and any(ln.startswith("REARMED") and "w10b" in ln
                                                         for ln in status_lines(c12)),
               f"{said} {agent_work(c12).reasons}")
        expect("a re-armed branch is landed again at the SAME head, the BOUNCED one included",
               sorted(f12.tick(c12)) == ["w10a", "w10b"] and not rearm_path(c12, "w10a").exists()
               and not rearm_path(c12, "w10b").exists(), f"{f12.started} {f12.said}")
        running = [a for a in attempts(c12) if not finished(a)]
        expect("rearm is refused while the branch's attempt is running (nothing written)",
               bool(running) and rearm(c12, "w10a", "x", say=said.append, tip_for=tips.get) == 1
               and not rearm_path(c12, "w10a").exists(), str(said))
        finish(c12, "w10a", 5, "GATE RED (Windows) on v1: main still red")
        f12.live.clear()
        expect("a re-arm buys ONE attempt: the same stop at the same head again is not relaunched",
               "w10a" not in f12.tick(c12), str(f12.started))
        rearm(c12, "w10a", "main carries the fix", say=said.append, tip_for=tips.get)
        f12.rows[0] = row("w10a", "WAITING FOR REVIEW", "v2")                   # a finisher committed after all
        out12 = f12.tick(c12)
        expect("a re-arm whose branch moved is stale: dropped and said (the new tip goes through review first)",
               "w10a" not in out12 and not rearm_path(c12, "w10a").exists()
               and any("stale" in s and "w10a" in s for s in f12.said), f"{out12} {f12.said}")
        c12b = fresh("rearm-unreviewed")
        f12b = Fake([row("w11a", "WAITING TO LAND", "y1")])
        f12b.tick(c12b)
        finish(c12b, "w11a", 11, "UNREVIEWED (the tip is not covered by its CLEAN stamp)")
        f12b.live.clear()
        stopped_first = f12b.tick(c12b) == []
        rearm(c12b, "w11a", "reviewed CLEAN at y1", say=said.append, tip_for=lambda wt: "y1")
        expect("an UNREVIEWED exit 11 waits, then a reviewer's CLEAN stamp plus rearm lands it at the same head",
               stopped_first and f12b.tick(c12b) == ["w11a"], f"{f12b.started} {f12b.said}")

        # 14. THE QUEUE WATCH (kb/Work PB3527): each pull request the lane queued, followed into the merge queue from a
        #     stubbed reader of GitHub's facts, on a planted clock; PR #385's shape is a ci-gate check-run in progress
        #     whose job completed at 18:46:46Z.
        import queue_facts  # noqa: PLC0415
        t = lambda hm: dt.datetime.fromisoformat(f"2026-10-22T{hm}:00+00:00")   # noqa: E731
        job_done = "2026-10-22T18:46:46Z"
        stuck_gate = queue_facts.CheckRun("ci-gate", "IN_PROGRESS", "", 120018552758, 39834070835,
                                          job_status="completed", job_completed_at=job_done)
        running_gate = queue_facts.CheckRun("ci-gate", "IN_PROGRESS", "", 120018552759, 39834070835,
                                            job_status="in_progress")
        green_gate = queue_facts.CheckRun("ci-gate", "COMPLETED", "SUCCESS", 120043423624, 39834070835, job_done)
        red_gate = queue_facts.CheckRun("ci-gate", "COMPLETED", "FAILURE", 120043423625, 39834070836, job_done)

        def pr(number: int, *checks, in_queue: bool = False, head: str = "z1") -> Any:
            return queue_facts.PullRequest(number, f"https://github.com/o/r/pull/{number}", head, in_queue,
                                           "2026-10-22T18:44:50Z", "BLOCKED", "", tuple(checks), "o/r")

        c14 = fresh("watch")
        names = ["q", "m", "rerun", "reopen", "alert", "red", "closed", "held"]
        f14 = Fake([row(f"w14{n}", "WAITING TO LAND", "z0") for n in names])
        f14.tick(c14)
        for n in names:
            finish(c14, f"w14{n}", 0, "ENQUEUED", end_head="z1")
        f14.live.clear()
        f14.rows = [row(f"w14{n}", "IN FLIGHT", "z1") for n in names]
        f14.rows[1]["content"] = "MERGED"                      # merged by the queue, the survey still calls it fresh
        n_of = {f"w14{n}": 380 + i for i, n in enumerate(names)}

        def facts(**over) -> dict[str, Any]:
            base = {b: pr(n_of[b], stuck_gate) for b in n_of if b not in ("w14q", "w14m", "w14red", "w14closed")}
            base.update(w14q=pr(n_of["w14q"], green_gate, in_queue=True), w14red=pr(n_of["w14red"], red_gate))
            base.update(over)
            return {k: v for k, v in base.items() if v is not None}

        def watch(b: str) -> dict[str, Any]:
            return next(a for a in attempts(c14) if a["branch"] == b).get("watch") or {}

        def acts(b: str) -> list[str]:
            """What the watch did for branch b, in order: its recorded stalls (each act is measured), and an alert
            on a pull request whose number is unknown (closed), which has no record to write."""
            return [e[5] if e[0] == "measure" else e[0] for e in f14.effects
                    if (e[0] == "measure" and e[2] == b) or (e[0] == "alert" and f"({b})" in e[1] and "#?" in e[1])]

        (lane_dir(c14).parent / "STOP").write_text("", encoding="utf-8", newline="\n")
        f14.prs, f14.now = facts(w14held=pr(n_of["w14held"], stuck_gate)), t("18:57")
        f14.tick(c14)
        held_said = list(f14.said)
        (lane_dir(c14).parent / "STOP").unlink()
        expect("queue watch: under a stop it reads but acts on nothing (no re-run, no requeue, no alert)",
               not f14.effects and any("NOT acted on" in s for s in held_said), f"{f14.effects} {held_said}")
        f14.prs, f14.now = None, t("18:58")
        f14.tick(c14)
        expect("queue watch: GitHub's facts unobserved this tick: nothing is read and nothing is done",
               not f14.effects and any("not observed" in s for s in f14.said), str(f14.said))

        f14.prs, f14.now = facts(w14closed=None), t("18:50")
        f14.tick(c14)
        expect("queue watch: a pull request in the queue ends the watch QUEUED; one whose branch's work is on main "
               "ends it MERGED; a required check that failed ends it LEFT (BOUNCED: the finisher's); none acted on",
               watch("w14q").get("state") == IN_QUEUE and watch("w14m").get("state") == MERGED
               and watch("w14red").get("state") == LEFT and not acts("w14q") and not acts("w14m") and not acts("w14red"),
               f"{watch('w14q')} {watch('w14m')} {watch('w14red')} {f14.effects}")
        expect("queue watch: a stuck check-run inside the grace is only watched; a pull request with no open pull "
               "request and its work not on main ALERTS at once (closed by hand)",
               watch("w14rerun").get("state") == WATCHING and not watch("w14rerun").get("reads")
               and watch("w14closed").get("state") == ALERTED
               and acts("w14closed") == ["alert"], f"{watch('w14rerun')} {watch('w14closed')} {f14.effects}")
        f14.prs, f14.now = facts(w14closed=None, w14rerun=pr(n_of["w14rerun"], running_gate)), t("18:57")
        f14.tick(c14)
        expect("queue watch: past the grace, read 1 of a stuck check-run re-runs its workflow run and records the "
               "stall (cause stuck-check-run, read 1); a check whose job still runs is not stalled",
               ("rerun", "o/r", 39834070835) in f14.effects and acts("w14reopen") == [RERUN]
               and ("measure", n_of["w14reopen"], "w14reopen", 1, STUCK, RERUN) in f14.effects
               and not acts("w14rerun") and watch("w14reopen")["reads"][0]["act"] == RERUN, str(f14.effects))
        f14.prs, f14.now = facts(w14closed=None, w14rerun=pr(n_of["w14rerun"], green_gate, in_queue=True)), t("19:05")
        f14.tick(c14)
        expect("queue watch: stuck, then cleared by the re-run: the pull request enters the queue and the watch ends; "
               "a stuck one only 8 min after its re-run is still inside the grace",
               watch("w14rerun").get("state") == IN_QUEUE and len(watch("w14reopen")["reads"]) == 1,
               f"{watch('w14rerun')} {watch('w14reopen')}")
        f14.prs, f14.now = facts(w14closed=None, w14rerun=None), t("19:08")
        f14.tick(c14)
        expect("queue watch: read 2 (still stuck one grace after the re-run) closes, reopens and re-queues the pull "
               "request, recorded as read 2",
               ("requeue", "o/r", n_of["w14reopen"]) in f14.effects and acts("w14reopen") == [RERUN, REQUEUE]
               and ("measure", n_of["w14reopen"], "w14reopen", 2, STUCK, REQUEUE) in f14.effects, str(f14.effects))
        f14.prs, f14.now = facts(w14closed=None, w14rerun=None,
                                 w14reopen=pr(n_of["w14reopen"], green_gate, in_queue=True)), t("19:12")
        f14.tick(c14)
        expect("queue watch: stuck, then cleared by the reopen: in the queue, the watch ends with no third read",
               watch("w14reopen").get("state") == IN_QUEUE and len(watch("w14reopen")["reads"]) == 2,
               str(watch("w14reopen")))
        f14.prs, f14.now = facts(w14closed=None, w14rerun=None, w14reopen=None), t("19:19")
        f14.tick(c14)
        f14.now = t("19:45")
        f14.tick(c14)
        alerts = [e for e in f14.effects if e[0] == "alert" and "(w14alert)" in e[1]]
        expect("queue watch: a third stalled read ALERTS the operator (one mailbox question naming the pull request and "
               "the remedies tried) and the watch acts no more, however long it stays out",
               acts("w14alert") == [RERUN, REQUEUE, ALERT] and len(alerts) == 1
               and f"PR #{n_of['w14alert']}" in alerts[0][1] and "re-run" in alerts[0][2]
               and watch("w14alert").get("state") == ALERTED and len(watch("w14alert")["reads"]) == 3,
               f"{acts('w14alert')} {watch('w14alert')}")
        expect("queue watch: status and the survey's NOT QUEUED row say the watch's state and each read",
               any("w14alert" in ln and "queue watch ALERTED" in ln and "read 1 stuck-check-run: rerun done" in ln
                   for ln in status_lines(c14))
               and "read 2 stuck-check-run: requeue done" in watch_text(watch_holder := next(
                   a for a in attempts(c14) if a["branch"] == "w14alert")), str(watch_holder))
        unread_gate = queue_facts.CheckRun("ci-gate", "IN_PROGRESS", "", 120018552760, 39834070835)
        enq = {"end_head": "z1", "ended": "2026-10-22T18:45:00Z"}
        early, late = (watch_step(enq, "IN FLIGHT", pr(397, unread_gate), t(hm)) for hm in ("18:50", "18:56"))
        expect("queue watch: a check-run whose job the jobs API could not read never holds the watch for ever: its "
               "clock is the enqueue, and past the grace it is a stalled read (not-queued) that names the unread job",
               early.state == WATCHING and not early.act and late.act == RERUN and late.cause == NOT_QUEUED
               and "could not be read" in late.why, f"{early} {late}")
        noq = watch_step({"end_head": "z1", "ended": "2026-10-22T18:45:00Z"}, "IN FLIGHT", pr(399), t("18:56"))
        expect("queue watch: with no check-run at all the clock starts at the enqueue, and read 1 goes straight to the "
               "requeue (there is no run to re-run); a new head on the pull request ends the watch LEFT",
               noq.act == REQUEUE and noq.cause == NOT_QUEUED
               and watch_step({"end_head": "z1"}, "IN FLIGHT", pr(398, head="z9"), t("19:00")).state == LEFT, str(noq))
        dirty = watch_step(enq, "IN FLIGHT", dataclasses.replace(pr(396, green_gate), merge_state="DIRTY"), t("19:30"))
        expect("queue watch: a pull request that conflicts with main (merge state DIRTY), its checks green, ends the "
               "watch LEFT (BOUNCED, the finisher's) with no remedy: a re-run or a reopen cannot clear a conflict "
               "(kb/Work PB3527 review R1-5)",
               dirty.state == LEFT and not dirty.act and "DIRTY" in dirty.why, str(dirty))

        # 12b. ONE WRITER PER FIELD (kb/Work PB3527 review R1-3): the tick reads the attempts before its slow survey (git
        #      fetch, the GraphQL read, the jobs API); the enqueue unit's `collect` that lands in between is kept, and the
        #      watch's own field is written beside it. And a runner that writes its rc after reap read the record is
        #      never marked DIED.
        c15 = fresh("race")
        f15 = Fake([row("w15", "WAITING TO LAND", "y0")])
        f15.tick(c15)
        finish(c15, "w15", 0, "ENQUEUED", end_head="y1")
        f15.live.clear()
        f15.rows = [row("w15", "IN FLIGHT", "y1")]
        f15.prs, f15.now = {"w15": pr(415, running_gate, head="y1")}, t("18:50")
        planted = f15.survey
        f15.survey = lambda repo, cdir: (collect(cdir, ["w15"], say=lambda s: None), planted(repo, cdir))[1]
        f15.tick(c15)
        raced = next(a for a in attempts(c15) if a["branch"] == "w15")
        expect("one writer per field: a `collect` between the tick's read and its queue watch's write is kept (collected "
               "stays true, nothing asks the enqueue unit again), and the watch's state is written beside it",
               raced.get("collected") is True and (raced.get("watch") or {}).get("state") == WATCHING
               and not agent_work(c15, t("18:51")).reasons, f"{raced} {agent_work(c15, t('18:51')).reasons}")
        f15.survey = planted
        f15.rows = [row("w15b", "WAITING TO LAND", "x0")]
        f15.prs = None
        f15.tick(c15)
        stale = next(a for a in attempts(c15) if a["branch"] == "w15b")
        late = next(a for a in attempts(c15) if a["branch"] == "w15b")
        update(late, {"rc": 0, "verdict": "ENQUEUED", "ended": _now().isoformat(), "end_head": "x1"})
        reap(c15, [stale], lambda pid: False, say=lambda s: None)
        expect("one writer per field: a runner that wrote its rc after reap read the record is not marked DIED (the "
               "fresh record decides, under the record's lock)",
               "outcome" not in stale and stale.get("rc") == 0
               and "outcome" not in next(a for a in attempts(c15) if a["branch"] == "w15b"), str(stale))

        # 12c. A HUNG GITHUB CALL NEVER STOPS THE TICK (kb/Work PB3527 review R1-4): the real remedy (enqueue.rerun_check)
        #      on a gh that gives no answer (real_gh's timeout raises ruleset.Unobserved) is a FAILED read, recorded and
        #      measured; the tick still launches the branch WAITING TO LAND; the next stalled read escalates.
        import enqueue  # noqa: PLC0415
        import ruleset  # noqa: PLC0415

        def silent(args: list[str]) -> tuple[int, str, str]:
            raise ruleset.Unobserved(f"`gh {' '.join(args[:2])}` gave no answer in 120 s")
        c16 = fresh("hang")
        f16 = Fake([row("w16", "WAITING TO LAND", "v0")])
        f16.tick(c16)
        finish(c16, "w16", 0, "ENQUEUED", end_head="v1")
        f16.live.clear()
        f16.rows = [row("w16", "IN FLIGHT", "v1"), row("w16next", "WAITING TO LAND", "u0")]
        f16.prs, f16.now = {"w16": pr(416, stuck_gate, head="v1")}, t("18:57")
        recorded: list[tuple] = []
        f16.watch_effects = lambda: WatchEffects(
            rerun=lambda repo, run: enqueue.rerun_check(repo, run, gh=silent),
            requeue=lambda repo, number: enqueue.requeue(repo, number, gh=silent, state=lambda: (ruleset.ACTIVE, "x")),
            alert=lambda subject, body, ref: recorded.append(("alert", subject)),
            measure=lambda number, b, read, cause, act, ok: recorded.append(("measure", number, read, act, ok)))
        try:
            launched = f16.tick(c16)
            hung = None
        except ruleset.Unobserved as exc:
            launched, hung = [], exc
        reads16 = (next(a for a in attempts(c16) if a["branch"] == "w16").get("watch") or {}).get("reads") or []
        f16.rows = [row("w16", "IN FLIGHT", "v1")]
        f16.now = t("19:08")
        f16.tick(c16)
        reads16b = (next(a for a in attempts(c16) if a["branch"] == "w16").get("watch") or {}).get("reads") or []
        expect("a remedy GitHub does not answer (gh's timeout) is a FAILED read, recorded and measured, and the tick "
               "still launches the branch WAITING TO LAND; the next stalled read escalates to the requeue, never "
               "repeats the re-run",
               hung is None and launched == ["w16next"] and [r["ok"] for r in reads16] == [False]
               and "GitHub gave no answer" in reads16[0]["detail"] and ("measure", 416, 1, RERUN, False) in recorded
               and [r["act"] for r in reads16b] == [RERUN, REQUEUE], f"{hung} {launched} {reads16b} {recorded}")

        # 13. the runner's launch (no window, outliving the tick) is scripts/detached.py's, with its own self-test
        #     arms (kb/Work PB3117, PB3536): the tick starts every runner through it.
        expect("the tick starts every runner through the one detached launch (scripts/detached.py)",
               tick.__kwdefaults__["start"] is spawn_detached)
    finally:
        for dirpath, _, files in os.walk(root):  # git writes read-only objects, which rmtree cannot delete on Windows
            for name in files:
                try:
                    os.chmod(os.path.join(dirpath, name), 0o600)
                except OSError:
                    pass
        remove_tree(root)
    print(f"=== land_lane.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--coord", help=f"the coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    sub = ap.add_subparsers(dest="verb")
    t = sub.add_parser("tick", help="reap, survey, start every eligible landing")
    t.add_argument("--repo", type=pathlib.Path, default=coord.REPO, help="the repository (default: this checkout)")
    st = sub.add_parser("status", help="each branch's latest attempt, one line each")
    st.add_argument("--json", action="store_true")
    co = sub.add_parser("collect", help="the enqueue unit handled these branches' finished attempts")
    co.add_argument("branches", nargs="+")
    rr = sub.add_parser("rearm", help="the enqueue unit: land the branch again at its current tip (a stop resolved "
                                      "with no new commit)")
    rr.add_argument("branch")
    rr.add_argument("--why", required=True, help="what resolved the stop (the finisher's or reviewer's finding)")
    sub.add_parser("clear-halt", help="the operator: the merge queue is ACTIVE again; the lane may start landings")
    ru = sub.add_parser("run", help=argparse.SUPPRESS)
    ru.add_argument("attempt", type=pathlib.Path)
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    cdir = coord.coord_dir(a.coord)
    if a.verb == "tick":
        return tick(cdir, a.repo.resolve())
    if a.verb == "status":
        if a.json:
            lane = view(cdir)
            print(json.dumps({"survey_at": lane.survey_at.isoformat() if lane.survey_at else None, "halt": lane.halt,
                              "attempts": [{**{k: v for k, v in at.items() if k != "_path"}, "class": cls}
                                           for at, cls in lane.latest],
                              "rearms": list(lane.rearms.values()), "agent_work": agent_work(cdir).reasons}, indent=1))
        else:
            print("\n".join(status_lines(cdir)))
        return 0
    if a.verb == "collect":
        return collect(cdir, a.branches)
    if a.verb == "rearm":
        return rearm(cdir, a.branch, a.why)
    if a.verb == "clear-halt":
        halt = lane_dir(cdir) / HALT
        if not halt.exists():
            print("land lane: not halted")
            return 0
        print(f"land lane: halt cleared ({halt.read_text(encoding='utf-8').strip()}); the next tick starts landings")
        halt.unlink()
        return 0
    if a.verb == "run":
        return run_attempt(a.attempt)
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
