#!/usr/bin/env python3
"""land_lane.py — THE LOOP'S LANDING LANE: every branch the survey lists WAITING TO LAND goes through `enqueue.py land`
the moment it is ready, while whatever unit runs keeps running (kb/Work PB3103; DESIGN-orchestrator-loop.md section
4.9; MANDATORY-PRACTICES O11). Deterministic, no model: it decides nothing a script cannot, and dispatches no agent.

    python scripts/orchestrator/land_lane.py tick [--repo DIR]      reap, survey, start every eligible landing
    python scripts/orchestrator/land_lane.py status [--json]        one line per branch: running, stopped, queued
    python scripts/orchestrator/land_lane.py collect <branch> ...   the enqueue unit has handled these attempts
    python scripts/orchestrator/land_lane.py rearm <branch> --why T the enqueue unit: land it again at its CURRENT tip
    python scripts/orchestrator/land_lane.py clear-halt             the operator: the merge queue is ACTIVE again
    python scripts/orchestrator/land_lane.py conflict-brief <branch> the brief its CONFLICT finisher starts from
    python scripts/orchestrator/land_lane.py run <attempt file>     (the detached runner `tick` starts; not by hand)
    python scripts/orchestrator/land_lane.py --self-test

⛔ WHY (the owner's choice 2026-10-12, "Supervisor lane (Recommended)"). Before the lane a reviewed branch landed only
when the loop's NEXT `enqueue` unit started, after the whole wave ended: wave 1069's group D was stamped Review CLEAN at
12:34 PDT and E and F by 13:05, and all three idled while G and H ran; then, while the enqueue unit gated and waited on
the merge queue, no implementer ran. The supervisor (`orchestrate.ps1`) now runs `tick` in the BACKGROUND every few
minutes for as long as it lives (while a unit runs, between units, through a budget hold and a failure backoff: the lane spends no
tokens, kb/Work PB3639), so a branch lands within its own gate and CI time of being ready.

STATE, all under `<coord>/land-lane/` (the coordination directory, never a worktree):
  attempts/<id>.json  one per `enqueue.py land` run, <id> = `<n>-<UTC stamp>-<branch>` (n: the attempt NUMBER, one
                      past every attempt, taken under the tick lock; the order of attempts, never the clock, kb/Work
                      PB3536): branch, head (its tip at launch), worktree, argv, log
                      (`<coord>/logs/land-lane-<id>.log`), runner pid (the runner is ALIVE only while that pid's
                      process is a Python whose command line names this record: `runner_alive`, so a pid the OS
                      reused for another program never reads RUNNING, kb/Work PB3113), and once it ends rc, the log's
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
  conflicts/<id>.md   the CONFLICT BRIEF of an attempt that stopped exit 4 (kb/Work PB3640; the section doc above
                      `conflict_brief`), written by its runner after its outcome and named on the attempt (`brief`, or
                      `brief_error` when it could not be written): the conflicted files, the commits main gained on
                      them since the branch's merge base, the notes those commits and the branch name, each note's
                      newest report. `conflict-brief <branch>` prints it, or computes one now (a DIRTY bounce).
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
       exit 3 — the landing check's STOP: a never-carried path   STOPPED: the enqueue unit's finisher (only a new
                or a duplicate public-API symbol (an edit meeting  commit clears it); never retried as is
                another branch's only warns, kb/Work PB4452)
       exit 13 — another `land` of the branch runs (kb/Work     retried at this head once the worktree's land lock
                PB3310; nothing ran, nothing red)           is free (`wait_free`); never the enqueue unit's
       exit 15 — PREDECESSOR QUEUED: the branch carries the stamped DEVLOG entry of a branch it merged, whose pull
                request is still open (kb/Work PB3145; nothing queued, nothing red)  retried at this head once no open
                                                                   pull request into main owns an entry the branch
                                                                   carries (`wait_free`); never the enqueue unit's
       exit 12 — GATE BUSY: a gate of the worktree was running or retried at this head once the worktree's gate lock
                starting when the landing would write the worktree   is free (`wait_free`; a gate holds it until
                (kb/Work PB3527, PB3662, PB4204); nothing written,   its verdict is written); never the enqueue
                nothing red                                          unit's
       exit 2 (main moved under every catch-up, the branch could not be read, or a clean merge of main could not be
                completed for a cause in the ENVIRONMENT, kb/Work PB3673), exit 9 (GitHub refused the push, the PR or
                the queue), exit 16 (STORE NOT PUBLISHED: the passes could not reach origin's store ref, kb/Work
                PB5235), or a runner that DIED                   retried ONCE at this head (enqueue.md's own rule)
       exit 8 — QUEUE NOT ACTIVE                                   HALT (above); relaunched once the halt is cleared
       anything else — CONFLICT 4, MERGE NOT COMPLETED BRANCH-CARRIED 5, a DEVLOG/register/prefix refusal 6,
                UNREVIEWED 11, UNTESTED 14 (no recorded GREEN local gate covers the tip, kb/Work PB3612), an exit
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
     merge queue (or merged, BOUNCED, or superseded by a new head). While ANY check on its head runs, required or not
     (ci-gate has no check-run until the jobs it needs end, kb/Work PB4052), nothing is late. Not in the queue
     QUEUE_GRACE_MIN after its checks' work ended (the newest check-run's end; a stuck one's JOB time) is a STALLED read:
     read 1 re-runs the stuck check's workflow run, read 2 (one grace after) closes, reopens and re-queues the pull
     request, refused while a run on its head is in progress (both enqueue.py's: the only file that queues), read 3
     sends the operator a mailbox question and the watch acts no more; each read is recorded on the
     attempt (`watch`) and in landing_measure.py's record (`stalls`). A pull request the queue REMOVED (a merge-group
     red) is CLASSIFIED before anyone is blamed (the class section above `watch_step`; kb/Work PB4271, PB4331): INFRA
     and RUNNER are re-queued once by the lane, no agent woken; GENUINE, MERGE, UNCLASSIFIED and a second red of the
     landing end the watch BOUNCED with the class on its line, the enqueue unit's finisher; each red is recorded on the
     attempt (`watch.reds`) and in landing_measure.py's record (`reds`). Under a stop the watch reads but acts on nothing.
`status` prints each branch's latest attempt; the enqueue unit reads it (units/enqueue.md step 3), sends each STOPPED
attempt to a finisher or a reviewer, files the leads of each ENQUEUED one, and then `collect`s them. `agent_work` is
next_unit.py's one reader: the enqueue unit starts only for agent work (a BOUNCED branch the lane is neither landing nor
re-armed to land, a stopped attempt, an ENQUEUED one to collect), never merely because a handoff lists DONE branches the
lane is landing. THE REVIEWS ARE NOT HERE: a branch WAITING FOR REVIEW, and the CONFLICT finisher of an exit 4 or of a
pull request bounced DIRTY, are the REVIEW LANE's (review_lane.py, kb/Work PB3608), which the supervisor ticks right
after this tick, on the survey this tick wrote (`survey.json`, whose rows carry the `report` and `conflicts` it reads).

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
from enqueue import (EXIT_CONFLICT, EXIT_GATE_BUSY, EXIT_GITHUB, EXIT_LAND_BUSY,
                     EXIT_NOT_JUMPED, EXIT_PREDECESSOR_QUEUED, EXIT_QUEUE_NOT_ACTIVE, EXIT_STORE_UNPUBLISHED, PR_LIST_FIELDS,
                     QUEUED_LINE, land_lock_path)  # noqa: E402  (the landing's contract)
from gate_slot import ExclusiveLock  # noqa: E402  (an OS lock: the tick lock frees itself when its holder dies)
from detached import spawn_detached  # noqa: E402  (THE one way a process outlives its caller, kb/Work PB3536)
import verdict_line  # noqa: E402  (scripts/: the one home of the `=== ENQUEUE: ` verdict line, kb/Work PB3714)
from standard_streams import ensure_utf8  # noqa: E402

LANE_DIR = "land-lane"
ATTEMPTS = "attempts"
SURVEY = "survey.json"
HALT = "HALT"
REARM = "rearm"
TICK_LOCK = "tick.lock"
EXIT_STATE = 2           # enqueue.py: usage or state, among them "main moved under every catch-up merge"
#: The exits the lane retries at the same head once what they wait on is free (wait_free asks): nothing is wrong with
#: the branch, so neither is ever the enqueue unit's (kb/Work PB2701; PB3527: a held gate lock was read as a red).
WAITS = {EXIT_GATE_BUSY: "the worktree's gate lock (a running gate holds it until its verdict is written)",
         EXIT_LAND_BUSY: "the worktree's land lock",
         EXIT_PREDECESSOR_QUEUED: "the open pull request of the predecessor whose DEVLOG entry it carries"}
# How old survey.json may be before next_unit.py treats the lane as not ticking (the supervisor ticks every
# -LandLaneSeconds, 300 by default, for as long as it lives, unit or no unit, and once as each unit starts).
SURVEY_MAX_AGE_MIN = 30
# The classes of a finished attempt (classify). STOPPED and DONE-to-collect are the enqueue unit's; the rest the lane's.
DONE, FREE, ONCE, HALTED, STOPPED = "done", "retry-when-free", "retry-once", "halt", "stopped"
RETRYABLE = (FREE, ONCE, HALTED)
#: The survey states in which the queue owns the branch: its pull request is IN the merge queue (ENQUEUED, at whatever
#: head the queue took: a push would eject it), handed to the queue AT the branch's tip (NOT QUEUED), or its work is on
#: main (LANDED, by content). The queue watch follows the first two and the lane starts no landing in any of them, a
#: re-arm included (rearm_hold). prune_worktrees.lifecycle reads NOT QUEUED only for a pull request at the tip, so a
#: tip past an older pull-request head reads by its report and lands here (kb/Work PB3275, PB3628; review lead R1-2).
QUEUE_OWNED = ("ENQUEUED", "NOT QUEUED", "LANDED")

Say = Callable[[str], None]


class LaneError(RuntimeError):
    """The survey could not be taken (not a repository, git failed): the tick launches nothing and says why."""


# ── the state ──────────────────────────────────────────────────────────────────────────────────────────────────────
def lane_dir(cdir: pathlib.Path) -> pathlib.Path:
    return cdir / LANE_DIR


def now_utc() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc)


def slug(branch: str) -> str:
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
    # Exit 17 (kb/Work PB5483 review R1-4): a fix-first jump GitHub refused. The pull request IS queued (at the back, with
    # auto-merge), so the landing is DONE and watched like any; `watch_holds` alerts the operator once that it did not jump.
    if rc in (0, EXIT_NOT_JUMPED):
        return DONE
    if rc in WAITS:
        return FREE
    if rc in (EXIT_STATE, EXIT_GITHUB, EXIT_STORE_UNPUBLISHED):
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
    return (verdict_line.outcomes(text) or [None])[-1]


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


# ── the conflict brief (kb/Work PB3640) ────────────────────────────────────────────────────────────────────────────
# No file-set partition removes every merge conflict (a declared set is a prediction: PB3628's site block named
# land_lane.py and its change also rewrote prune_worktrees.lifecycle, beside PB3275), so the cost to cut is the
# conflict FINISHER's: it re-derived both sides from scratch. When an attempt stops CONFLICT (exit 4) its runner writes
# the brief the finisher starts from: the conflicted files, the commits main gained on each since the branch's merge
# base, the notes those commits and the branch name, and each note's newest report. `land_lane.py conflict-brief
# <branch>` prints it (or computes one for a pull request the queue BOUNCED as DIRTY, which no attempt recorded), the
# status line points to it, and units/enqueue.md step 4 hands it to the finisher verbatim.
CONFLICTS = "conflicts"
NOTE_ID = re.compile(r"\bPB\d+\b")
MAIN = "refs/remotes/origin/main"
#: At most this many notes are read for one brief (each one is a `git show`): a brief naming more is truncated, said.
BRIEF_NOTES = 24
Git = Callable[..., Any]   # prune_worktrees.git's shape: (*args, cwd=) -> CompletedProcess


def _note_line(nid: str, worktree: str, git: Git, reports: list[Any], ref: str = MAIN) -> str:
    """`- PBnnnn (status): title — newest report: <path> (<status>)`, the note read from `ref` (origin/main, where the
    notes of main's commits are, or the queued head a brief is against), else from the worktree's own copy (a note the
    branch filed)."""
    from clusters import note_of  # noqa: PLC0415  (the one note reader, on the one frontmatter reader)
    shown = git("show", f"{ref}:kb/Work/{nid}.md", cwd=worktree)
    text = shown.stdout if shown.returncode == 0 else None
    if text is None and (local := pathlib.Path(worktree) / "kb" / "Work" / f"{nid}.md").is_file():
        text = local.read_text(encoding="utf-8", errors="replace")
    note = note_of(text, nid, {}) if text else None
    head = f"- {nid} ({note.status}): {note.title[:200]}" if note else f"- {nid}: no kb/Work note on main or the branch"
    newest = [r for r in reports if nid in r.notes or r.lead == nid]
    return head + (f" — newest report: {newest[-1].path} ({newest[-1].status})" if newest else " — no report")


def conflict_brief(branch: str, worktree: str, files: list[str], *, git: Git, reports: list[Any],
                   now: dt.datetime | None = None, against: str = MAIN, label: str = "origin/main") -> str:
    """THE CONFLICT BRIEF (section doc) for `branch` in `worktree` against origin/main, as Markdown; `against` (a ref or
    a commit) and its `label` name another side instead: the head of a queued pull request ahead of this one that the
    queue's simulation finds conflicting (kb/Work PB3723). `reports` are the
    fix-lane reports, oldest first (reports.load_reports with the dispatch ledger, so each names the notes it
    answers). Git answers three questions: the merge base, the commits main gained on `files` since it (ONE `git log`
    over every file), and the branch's own commit subjects; a question git cannot answer is said in the brief, never
    dropped."""
    who = "main" if against == MAIN else label     # how the brief's prose names the other side
    mb = git("merge-base", against, "HEAD", cwd=worktree)
    base = mb.stdout.strip() if mb.returncode == 0 else ""
    main_sha = main_of(worktree, git) if against == MAIN else \
        git("rev-parse", "--verify", "--quiet", against, cwd=worktree).stdout.strip()
    tip = git("rev-parse", "HEAD", cwd=worktree).stdout.strip()
    out = [f"# Conflict brief: {branch}", "",
           f"Written {(now or now_utc()).isoformat(timespec='seconds')} by `land_lane.py` (kb/Work PB3640) for the "
           "CONFLICT finisher, who gets it verbatim (units/enqueue.md step 4).",
           f"Bringing {label} ({main_sha[:12] or 'unreadable'}) into {branch} ({tip[:12] or 'unreadable'}, merge "
           f"base {base[:12] or 'unreadable'}) leaves {len(files)} file(s) in conflict: {', '.join(files) or 'none'}.",
           f"Merge with `git merge {against if against == MAIN else main_sha or against}` and "
           f"resolve each file against BOTH sides' intent: this branch's notes and report, and what {who} gained on the "
           "file, with its notes and reports (finisher-brief.md, CONFLICT). Never take one side wholesale."
           + ("" if against == MAIN else
              f" {label} is a pull request QUEUED ahead of this one: the queue will remove this one for a merge conflict "
              "with it (kb/Work PB3723), and bringing its head in now keeps it from being removed; this branch then carries "
              "that pull request's stamped DEVLOG entry and lands after it (`enqueue.py land` exit 15 until it is on main)."),
           ""]
    mine = [r for r in reports if r.branch == branch]
    own_ids = list(dict.fromkeys(mine[-1].notes if mine else []))
    out.append("## This branch")
    out.append(f"report: {mine[-1].path} ({mine[-1].status}, HEAD {str(mine[-1].head or '-')[:12]})" if mine
               else "report: none in the reports directory names this branch")
    theirs: dict[str, None] = {}
    if base:
        subjects = git("log", "--format=%s", f"{base}..HEAD", cwd=worktree)
        own_ids = list(dict.fromkeys(own_ids + (NOTE_ID.findall(subjects.stdout) if subjects.returncode == 0 else [])))
        out += ["", f"## What {who} gained on each file since the merge base"]
        # ONE log of the range, filtered here to the conflicted files: a pathspec of every conflicted file made the
        # command line as long as the conflict (763 files overflowed Windows' limit, WinError 206; kb/Work PB5002).
        # `--no-renames`: without a pathspec git pairs a rename across the tree and names only the new path, so a
        # conflicted file main renamed away would lose its commits (review w20261932b R1-1); each side is named instead.
        log = git("log", "--no-merges", "--no-renames", "--format=%x1e%H%x1f%s", "--name-only", f"{base}..{against}",
                  cwd=worktree) if files else None
        changes: dict[str, list[str]] = {f: [] for f in files}
        if log is not None and log.returncode != 0:
            out.append(f"git log failed ({log.stderr.strip()[:200]}): read `git log {base[:12]}..{label} -- <file>`")
        for chunk in (log.stdout.split(chr(30)) if log is not None and log.returncode == 0 else []):
            head, _, names = chunk.strip().partition(chr(10))
            sha, _, subject = head.partition(chr(31))
            touched = [f for f in (n.strip() for n in names.splitlines()) if f in changes]   # a blank line ends the header
            if not sha or not touched:
                continue
            for i in NOTE_ID.findall(subject):
                theirs.setdefault(i)
            for f in touched:
                changes[f].append(f"- {sha[:12]} {subject}")
        for f, lines in changes.items():
            out += [f"### {f}", *(lines or ["- nothing since the merge base (the conflict is the branch's edit against "
                                            "an older one)"])]
    else:
        out += ["", f"git could not read the merge base of HEAD and {label} ({mb.stderr.strip()[:200]}): the "
                    f"commits {who} gained are not listed"]
    out += ["", "## The branch's notes"]
    ids = own_ids[:BRIEF_NOTES]
    out += [_note_line(i, worktree, git, reports, against) for i in ids] or ["- none named by its report or its commits"]
    other = [i for i in theirs if i not in own_ids]
    out += ["", f"## The notes {who}'s commits name"]
    out += [_note_line(i, worktree, git, reports, against) for i in other[:max(0, BRIEF_NOTES - len(ids))]] or \
        ["- none (their subjects name no PB note)"]
    if len(own_ids) + len(other) > BRIEF_NOTES:
        out.append(f"- ... {len(own_ids) + len(other) - BRIEF_NOTES} more not read (BRIEF_NOTES = {BRIEF_NOTES})")
    return chr(10).join(out) + chr(10)


def brief_path(cdir: pathlib.Path, a: dict[str, Any]) -> pathlib.Path:
    return lane_dir(cdir) / CONFLICTS / f"{a['id']}.md"


def main_of(worktree: str, git: Git) -> str:
    """The origin/main commit the worktree reads ("" when git cannot read it): what a brief is computed against."""
    r = git("rev-parse", "--verify", "--quiet", MAIN, cwd=worktree)
    return r.stdout.strip() if r.returncode == 0 else ""


def write_conflict_brief(cdir: pathlib.Path, a: dict[str, Any], git: Git | None = None) -> dict[str, str]:
    """Write the brief of a CONFLICT attempt `a` beside the attempt records and return the fields that name it on the
    attempt: `brief` (its path) and `brief_main` (the origin/main it was computed against, so `conflict-brief` serves it
    only while main is still that commit). Its files are the ones the verdict names (verdict_line.outcome_files), or,
    when the verdict line is unreadable (a log not in UTF-8), the ones git's dry run leaves in conflict against
    origin/main (git_merge.merge_conflicts)."""
    import git_merge  # noqa: PLC0415  (scripts/: the one dry-run merge)
    from reports import load_reports  # noqa: PLC0415  (the one report order, with the dispatch ledger's notes)
    import prune_worktrees  # noqa: PLC0415
    git = git or prune_worktrees.git
    reports = load_reports(coord.reports_dir(str(cdir)), cdir)
    main_sha = main_of(a["worktree"], git)
    files = verdict_line.outcome_files(a.get("verdict")) or git_merge.merge_conflicts(a["worktree"], MAIN) or []
    text = conflict_brief(a["branch"], a["worktree"], files, git=git, reports=reports)
    path = brief_path(cdir, a)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")
    return {"brief": str(path), "brief_main": main_sha}


def show_conflict_brief(cdir: pathlib.Path, branch: str, git: Git | None = None, say: Say = print) -> int:
    """`conflict-brief <branch>`: the recorded brief of the branch's latest attempt when it stopped CONFLICT, the branch
    is still at that attempt's end head and origin/main is still the commit the brief was computed against
    (`brief_main`); otherwise a brief computed now against origin/main
    (git_merge.merge_conflicts: a pull request the queue BOUNCED as DIRTY, a branch that moved, or a main that
    moved), its worktree from the latest attempt or the last survey. Exit 0 with the brief, 1 when there is nothing to
    brief (no worktree, or nothing conflicts)."""
    import git_merge  # noqa: PLC0415  (scripts/: the one dry-run merge)
    from reports import load_reports  # noqa: PLC0415  (the one report order, with the dispatch ledger's notes)
    import prune_worktrees  # noqa: PLC0415
    git = git or prune_worktrees.git
    lane = view(cdir)
    last = next((at for at, _ in lane.latest if at["branch"] == branch), None)
    row = next((r for r in lane.rows if r.get("name") == branch), None)
    wt = (last or {}).get("worktree") or (row or {}).get("worktree")
    if not wt:
        say(f"land lane: {branch}: no attempt and no survey row names its worktree: nothing to brief")
        return 1
    # A queued pull request the queue's simulation finds conflicting with an entry ahead of it (the survey's `foreseen`,
    # kb/Work PB3723) is briefed against that entry's head, the first it conflicts with; the others follow once it is in.
    ahead = [f for f in (row or {}).get("foreseen") or [] if f.get("branch") != "origin/main"]
    against, label = (ahead[0]["head"], ahead[0]["branch"]) if ahead else (MAIN, "origin/main")
    recorded = pathlib.Path(last["brief"]) if last and last.get("brief") and not ahead else None
    if (recorded and recorded.is_file() and tip_of(wt) == last.get("end_head")
            and last.get("brief_main") and last.get("brief_main") == main_of(wt, git)):
        say(recorded.read_text(encoding="utf-8").rstrip())
        return 0
    files = git_merge.merge_conflicts(wt, against)
    if not files:
        say(f"land lane: {branch}: " + (f"git could not merge-tree it against {label}" if files is None else
                                        f"merging {label} leaves nothing in conflict now: nothing to brief"))
        return 1
    text = conflict_brief(branch, wt, files, git=git, reports=load_reports(coord.reports_dir(str(cdir)), cdir),
                          against=against, label=label).rstrip()
    if len(ahead) > 1:
        text += (f"\n\nAlso queued ahead and conflicting: {', '.join(a['branch'] for a in ahead[1:])}. Run "
                 f"`land_lane.py conflict-brief {branch}` again once {label} is merged in.")
    say(text)
    return 0


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
def runner_alive(a: dict[str, Any]) -> bool:
    """The attempt's runner is alive (kb/Work PB3113): the process with the recorded pid is a Python whose command line
    names this attempt's own record (`launch` starts `land_lane.py run <record>`). A pid the OS has reused for another
    program after the runner died, a Python one included, carries another command line, so it is not the runner: the
    lane no longer reads RUNNING until that stranger exits. The one rule is `coord.runs_python` (kb/Work PB4008)."""
    pid = a.get("pid")
    return isinstance(pid, int) and bool(a.get("_path")) and coord.runs_python(pid, str(a["_path"]))


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
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports coord and the report reader back)
    import queue_facts  # noqa: PLC0415
    probe = prune_worktrees.git("rev-parse", "--git-common-dir", cwd=repo)
    if probe.returncode != 0:
        raise LaneError(f"{repo} is not a git repository ({probe.stderr.strip()[:120]})")
    prs = queue_facts.read()
    fetch = prune_worktrees.fetch_origin(repo)
    if fetch.returncode != 0:
        raise LaneError(f"git fetch origin failed in {repo}: {fetch.stderr.strip()[:200]}")
    rows = prune_worktrees.live_survey(repo, coord_dir=cdir, prs=prs)
    # the surveyed checkout's own register ranks its notes, as live_survey reads it (kb/Work PB5227)
    order = [r.name for r in prune_worktrees.landing_order(rows, prune_worktrees.note_rank(repo / "kb" / "Work"))]
    return row_dicts(rows, prs), order, prs


def row_dicts(rows: list, prs: dict | None) -> list[dict[str, Any]]:
    """The surveyed rows (prune_worktrees.Row) as the tick and the review lane read them. Pure, so the self-test runs
    it on a planted Row: the tick's own survey is a fake there, and a field this mapping read that the survey's report
    lacks crashed every tick on main (kb/Work PB4055)."""
    return [{"name": r.name, "state": r.state, "content": r.content, "tip": r.tip, "worktree": r.worktree,
             "why": r.why, "lead": r.report.lead if r.report else None,
             "report": str(r.report.path) if r.report else None,
             # GitHub cannot create the pull request's merge commit (DIRTY), or the queue's simulation finds its head
             # conflicting with an entry ahead of it (foreseen, kb/Work PB3723): a CONFLICT finisher's, as an exit 4 is
             "conflicts": bool(prs and r.name in prs and prs[r.name].conflicts) or bool(r.foreseen),
             "foreseen": [{"branch": b, "head": h} for b, h in r.foreseen]} for r in rows]




def gate_lock_free(worktree: str) -> tuple[bool, str]:
    """Whether no gate runs in the worktree now (exit 12's wait): the worktree's gate lock (the gate driver's own:
    gate.Host.lock_path) is free, taken and released at once (an OS lock, freed when its holder dies, so a
    lock that reads held is a live gate). That lock is also `gate.live_gate`'s reader, the one the landing
    answers exit 12 by (kb/Work PB3662: a gate holds it until its verdict file is written); a gate process id is
    never read (Windows reuses them). A hand run of linux-gate.sh holds no lock and gates nothing a landing reads
    (kb/Work PB4327), so it is not waited on."""
    import gate  # noqa: PLC0415  (scripts/: the gate driver owns its lock's path)
    return lock_free(lambda: gate.Host(pathlib.Path(worktree)).lock_path(), f"the gate lock of {worktree}")


def land_lock_free(worktree: str) -> tuple[bool, str]:
    """Whether the worktree's land lock (enqueue.py's own: land_lock_path, kb/Work PB3310) is free now: no other `land`
    of its branch runs."""
    return lock_free(lambda: land_lock_path(pathlib.Path(worktree)), f"the land lock of {worktree}")


def lock_free(locate: Callable[[], pathlib.Path], what: str) -> tuple[bool, str]:
    """Whether the OS lock at `locate()` is free now: taken and released at once (an OS lock is freed when its holder
    dies, so a lock that reads held is a live holder)."""
    try:
        path = locate()
    except (OSError, subprocess.SubprocessError, ValueError) as exc:
        return False, f"{what} could not be located ({exc})"
    lock = ExclusiveLock.try_take(path, f"land_lane.py probe (pid {os.getpid()})")
    if lock is None:
        return False, f"held by {ExclusiveLock.holder(path) or '(no label)'}"
    lock.release()
    return True, ""


def predecessor_free(worktree: str, gh: Callable[[list[str]], tuple[int, str, str]]) -> tuple[bool, str]:
    """Whether exit 15's wait is over (kb/Work PB3145): no open pull request into main owns a DEVLOG entry the branch
    carries. enqueue.py's own reader (`open_predecessors`) answers, as it answered `land`, against the worktree's last
    fetched origin/main; GitHub or the range unreadable is NOT free (with why), never a launch into the same wait."""
    import enqueue  # noqa: PLC0415  (the one reader of which open pull requests own a carried entry)
    wt = pathlib.Path(worktree)
    here = enqueue.worktree_state(wt)
    if here is None or not here.branch:
        return False, f"{worktree} has no branch checked out"
    queued, problem = enqueue.open_predecessors(wt, "refs/remotes/origin/main", here.branch, gh=gh)
    if problem:
        return False, problem
    if queued:
        return False, "open: " + ", ".join(f"#{n}" for n in sorted(queued))
    return True, ""


def wait_free(rc: int | None, worktree: str, *,
              gh: Callable[[list[str]], tuple[int, str, str]] | None = None) -> tuple[bool, str]:
    """Whether what a retry-when-free exit (WAITS) waits on is free now: exit 12, the worktree's gate lock is free; exit 13, the worktree's land lock
    is free; exit 15, no open pull request owns a DEVLOG entry the branch carries (`gh`: GitHub's reader, the real one
    unless a self-test plants it)."""
    if rc == EXIT_PREDECESSOR_QUEUED:
        import ruleset  # noqa: PLC0415
        return predecessor_free(worktree, gh or ruleset.real_gh)
    if rc == EXIT_GATE_BUSY:
        return gate_lock_free(worktree)
    if rc == EXIT_LAND_BUSY:
        return land_lock_free(worktree)
    raise ValueError(f"exit {rc} is no retry-when-free exit (WAITS)")


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
    return lane_dir(cdir) / REARM / f"{slug(branch)}.json"


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
    it as a new head anyway), left the survey, or reads LANDED (its work is on main: nothing is left to land, and the
    queue owns no pull request of it, kb/Work PB3609) is deleted and said; the rest are returned for `candidates`."""
    unreadable: list[str] = []
    pending = rearms(cdir, unreadable)
    for p in unreadable:
        say(f"land lane: {p} is not a re-arm record; left out (its branch waits for a new head): inspect it")
    tips = {r["name"]: r["tip"] for r in rows}
    landed = {r["name"] for r in rows if r["state"] == "LANDED"}
    for branch, rec in list(pending.items()):
        if branch in landed:
            rearm_path(cdir, branch).unlink(missing_ok=True)
            del pending[branch]
            say(f"land lane: {branch}: its work is on main (the survey reads it LANDED); the re-arm at "
                f"{rec['head'][:12]} is dropped")
        elif tips.get(branch) != rec["head"]:
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
    coord.write_json(path, {"branch": branch, "head": tip, "why": why, "at": now_utc().isoformat(timespec="seconds")})
    say(f"land lane: {branch}: re-armed at {tip[:12]} ({why}); the next tick lands it again")
    return 0


# ── the queue watch (kb/Work PB3527) ───────────────────────────────────────────────────────────────────────────────
# A landing ends at `=== ENQUEUE: ENQUEUED ===`, which means only that the pull request was opened and handed to the
# merge queue (auto-merge on): GitHub queues it once its required checks pass. On 2026-10-22 PR #385's `ci-gate` job
# completed at 18:46:46Z and its check-run on the head commit stayed in_progress for 50 minutes, the queue empty and
# the pull request BLOCKED, and nothing looked. So every tick WATCHES each pull request the lane queued until it
# merges, leaves or is alerted on (one IN the merge queue is read again: the queue can drop it, kb/Work PB3772), as a
# state machine over the facts the survey read on the same tick (queue_facts.read, the one reader): never a wait
# inside enqueue.py, never a second reader of GitHub.
QUEUE_GRACE_MIN = 10          # how long a pull request may stay out of the queue after its head's checks' work ended
WATCHING, IN_QUEUE, MERGED, LEFT, ALERTED = "watching", "queued", "merged", "left", "alerted"
#: The states in which the watch has ENDED. IN_QUEUE is not one: a pull request in the queue leaves it again without
#: merging and without a failed run (a ruleset toggle, a `merge_conflict` removal, a dequeue), and only a watch that
#: keeps reading it sees that (kb/Work PB3772: PR #593 sat out of the queue for an hour with auto-merge off).
WATCH_DONE = (MERGED, LEFT, ALERTED)
STUCK, NOT_QUEUED, CLOSED = "stuck-check-run", "not-queued", "closed"   # landing_measure.STALL_CAUSES
RERUN, REQUEUE, ALERT = "rerun", "requeue", "alert"                     # landing_measure.STALL_ACTS


@dataclasses.dataclass(frozen=True)
class WatchStep:
    """One read of the watch: its state after the read and, on a stalled read or a bounce, what to do about it."""
    state: str
    why: str
    act: str = ""          # "" (nothing), RERUN, REQUEUE or ALERT; on a bounce CLASSIFY, then REQUEUE_BOUNCE
    cause: str = ""        # on a stalled read: STUCK, NOT_QUEUED or CLOSED
    run: int = 0           # RERUN: the workflow run of the stuck required check
    red: str = ""          # on a classified bounce: its class (landing_measure.RED_CLASSES, kb/Work PB4271)


def _iso(t: str) -> dt.datetime | None:
    try:
        v = dt.datetime.fromisoformat(str(t).replace("Z", "+00:00"))
    except ValueError:
        return None
    return v if v.tzinfo else None


# ── THE CLASS OF A MERGE-QUEUE RED (kb/Work PB4271, PB4331; the rule of CLAUDE.md Testing) ─────────────────────────────────────────────
# THE RULE (owner 2026-10-31, replacing the CI invariant of 2026-10-04): every case the merge group runs on Windows
# already ran green at the pushed head, on this host, under the same toolchain.env. The merge group is the FIRST run of
# the merged tree, of Linux, of Release with a cold compile cache, of the Windows tooling self-tests on a tooling change
# and of GitHub's runners, so a red there is one of those, a flake, or infrastructure, never an untested case. Before
# anyone is blamed the lane classifies it, comparing the failed run's red cases (its test results, downloaded by
# queue_facts.fetch_bounce_results into the worktree, where the finisher's gate reads them too) with the branch's GREEN
# gate record (enqueue.tested_locally: the run that covers the queued head, or the run a REUSED answer names):
#   INFRA         no test failed in the run (a job died before or outside its tests), or the run timed out
#   RUNNER        every red case ran GREEN in that record, or the record is a lane's that runs no compiler test (the
#                 inert or the tooling lane, inert_lane.TEST_FREE_LANES: no compiler test reads the change):
#                 a Linux, Release, host or runner difference, or a flake
#   GENUINE       a red case the record lists but did not run (skipped on Windows): the merge group ran it first
#   MERGE         a red case not in the record at all: main brought it, or the merged tree made it
#   UNCLASSIFIED  the run's results or the record could not be read
# INFRA and RUNNER are re-queued ONCE by the lane (enqueue.requeue, the only file that queues), no agent woken, and filed
# against the pipeline (landing_measure.record_red: the `infrastructure` cause, with the class). GENUINE, MERGE and
# UNCLASSIFIED end the watch BOUNCED, the enqueue unit's finisher, the class on the line; so does the second red of a
# landing the lane already re-queued (a red twice in a row is never re-queued again: the lane stops there). There is no
# flake retry inside `dotnet test`: a flake is a kb/Work defect fixed at its root, and this record holds its rate.
# ONE RE-RUN PER CAUSE, NOT PER LANDING (kb/Work PB5483; the owner, 2026-11-13: "we rerun things far too many times, often
# in CI, rather than chase down and fix the ultimate issue"). A cause's key is a red test case (the failing test is the
# same on every pull request it hits: landing_measure.red_causes). A PIPELINE_CLASSES red whose red case was ALREADY
# re-queued once, on any pull request, while no fix-first landing that names one of that cause's failed runs has merged
# (`landing_measure.requeued_unfixed`, read from the record by `WatchEffects.open_causes`, released as `watch_holds` and `open_causes_now` say), is HELD: not re-queued, the
# act `hold-for-fix` recorded with its cases, the watch ended BOUNCED with HELD on its line, and the operator alerted to
# land the root fix with `enqueue.py land <branch> --fix-first "<note> run <id>"`. PR #1607 met the main-carried flake of
# kb/Work PB5478 twice in one night (runs 43550577247 and 43553292493) and every queued landing could meet it too. An
# INFRA red has no red case and so no key: it is re-queued once per landing as before.
REQUEUE_BOUNCE = "requeue-bounce"   # the WatchStep act that re-queues a bounce of a PIPELINE_CLASSES class, once
HOLD_BOUNCE = "hold-for-fix"        # the WatchStep act that holds one whose cause was re-queued before (landing_measure.HOLD_FOR_FIX)
CLASSIFY = "classify"               # the WatchStep act that asks for a bounce's class before the watch decides
NAMED = 3                           # how many red cases a class's reason names


@dataclasses.dataclass(frozen=True)
class RedFacts:
    """What a merge-queue red is classified on (`red_facts` reads them; `classify_red` decides). Pure data, so the
    self-test plants each arm."""
    run: int = 0                          # the failed merge-group run its results came from (0: none read)
    conclusion: str = ""                  # that run's conclusion: failure or timed_out
    failed: frozenset[str] = frozenset()  # the red cases in its test results, by trx definition name
    unobserved: str = ""                  # why the run's results could not be read ("" when they were)
    record: str = ""                      # the GREEN gate run that measured the queued head ("" when there is none)
    no_record: str = ""                   # why there is none (the record's own reason, or that its results are gone)
    test_free: str = ""                   # that record's lane when it runs no compiler test (inert, tooling)
    passed: frozenset[str] = frozenset()  # the cases the record ran and passed
    listed: frozenset[str] = frozenset()  # every case the record lists, run or skipped


def classify_red(f: RedFacts) -> tuple[str, str]:
    """(class, why) of a merge-queue red (the section above): the one decision, pure."""
    import landing_measure as lm  # noqa: PLC0415  (the record's vocabulary of classes)

    def some(cases: set[str] | frozenset[str]) -> str:
        names = sorted(cases)
        return ", ".join(names[:NAMED]) + (f" and {len(names) - NAMED} more" if len(names) > NAMED else "")
    if f.unobserved:
        return lm.UNCLASSIFIED, f"the red's test results could not be read ({f.unobserved})"
    if f.conclusion == "timed_out":
        return lm.INFRA, f"merge-group run {f.run} timed out"
    if not f.failed:
        return lm.INFRA, f"no test failed in merge-group run {f.run}: a job died before or outside its tests"
    if not f.record:
        return lm.UNCLASSIFIED, f"{len(f.failed)} case(s) failed ({some(f.failed)}), and no gate record to compare: {f.no_record}"
    if f.test_free:
        return lm.RUNNER, (f"{len(f.failed)} case(s) failed ({some(f.failed)}), and the branch's gate {f.record} is the "
                           f"{f.test_free} lane's: no compiler test reads its change")
    if skipped := (f.failed & f.listed) - f.passed:
        return lm.GENUINE, (f"{len(skipped)} red case(s) the branch's gate {f.record} listed but did not run "
                            f"({some(skipped)}): the merge group ran them first")
    if absent := f.failed - f.listed:
        return lm.MERGE, (f"{len(absent)} red case(s) are not in the branch's population at its gate {f.record} "
                          f"({some(absent)}): main brought them, or the merged tree made them")
    return lm.RUNNER, (f"every red case ({len(f.failed)}: {some(f.failed)}) ran GREEN in the branch's gate {f.record}: a "
                       "Linux, Release, host or runner difference, or a flake")


def red_facts(att: dict[str, Any], pr: Any, fetch: Callable[[str, pathlib.Path], queue_facts.BounceFetch] | None = None) -> RedFacts:
    """THE FACTS OF A BOUNCE, read (the queue watch's `red_facts` effect): the failed merge-group run's red cases and the
    branch's GREEN gate record of the queued head (`end_head`), both in the attempt's worktree. `fetch(branch, dest)`
    downloads the run's results (queue_facts.fetch_bounce_results, GitHub's one reader; the self-test plants it). Never
    raises: what cannot be read is said (`unobserved`, `no_record`) and classifies UNCLASSIFIED, the finisher's, as every
    bounce was before."""
    import enqueue  # noqa: PLC0415  (the one reader of a gate record that covers a head)
    import gate  # noqa: PLC0415  (scripts/: the record, the run root, where a bounce's results land)
    import inert_lane  # noqa: PLC0415  (scripts/: the lanes whose record keeps no test result)
    import queue_facts  # noqa: PLC0415
    import test_population as tp  # noqa: PLC0415  (scripts/: THE trx reader)
    wt = pathlib.Path(str(att.get("worktree") or ""))
    if not att.get("worktree") or not wt.is_dir():
        return RedFacts(unobserved=f"the branch's worktree {att.get('worktree') or '(none recorded)'} is gone")
    dest = wt / gate.CI_BOUNCE_RESULTS
    got = (fetch or queue_facts.fetch_bounce_results)(str(att["branch"]), dest)
    src = queue_facts.bounce_source(dest)
    # The results are the newest failed run's only when the fetch says it holds them now: a download that failed, an
    # expired artifact or an unreachable GitHub leaves dest as it was, and it then holds an OLDER bounce's results of the
    # same pull request (a reused one keeps its number), which would classify this red on another run's cases.
    if not got.run or src is None or src.run != got.run or src.pr != pr.number:
        return RedFacts(unobserved=got.line)
    try:
        failed = frozenset(tp.red_cases(sorted(dest.rglob("*.trx"))))
    except tp.UnreadableTrx as e:
        return RedFacts(src.run, src.conclusion, unobserved=f"a trx of run {src.run} is unreadable: {e}")
    facts = RedFacts(src.run, src.conclusion, failed)
    try:
        proof = enqueue.tested_locally(wt, str(att.get("end_head") or att.get("head") or ""), MAIN)
    except (OSError, subprocess.SubprocessError, ValueError) as e:
        return dataclasses.replace(facts, no_record=f"the gate records of {wt} could not be read ({e})")
    if not proof.ok or proof.run is None:
        return dataclasses.replace(facts, no_record=proof.text)
    rec = gate.read_record(proof.run) or {}
    # THE RUNS THAT HOLD THE EVIDENCE (kb/Work PB5099): the run that measured the head and, for a composed verdict, the
    # earlier runs of the same code state whose passes it counts (`gate.measuring_runs`, the one resolution).
    measuring = gate.measuring_runs(proof.run)
    measured = measuring[0] if measuring else proof.run
    if rec.get("lane") in inert_lane.TEST_FREE_LANES:
        return dataclasses.replace(facts, record=measured.name, test_free=str(rec["lane"]))
    trx = sorted(t for m in measuring for t in m.glob("*.trx"))
    if not trx:
        return dataclasses.replace(facts, no_record=f"the gate run {measured.name} that measured the head keeps no test "
                                                    "results (pruned, or a reused run's source is gone)")
    try:
        cases = [tp.read_trx(p) for p in trx]
    except tp.UnreadableTrx as e:
        return dataclasses.replace(facts, no_record=f"a trx of the gate run {measured.name} is unreadable: {e}")
    listed = frozenset(n for c in cases for n in c.definitions)
    passed = frozenset(n for c in cases for n, k in c.definitions.items() if k > c.skipped[n])
    return dataclasses.replace(facts, record=measured.name, listed=listed, passed=passed)


def bounce_step(w: dict[str, Any], pr: Any, cls: str, why: str, cases: frozenset[str] = frozenset(),
                held: frozenset[str] = frozenset()) -> WatchStep:
    """THE WATCH'S DECISION ON A CLASSIFIED BOUNCE (pure): re-queue a PIPELINE_CLASSES red once per landing and once per
    CAUSE, else end the watch BOUNCED for the enqueue unit's finisher. The removal GitHub reported is the bounce's
    identity: the same one read again after the lane re-queued it means the re-queue did not hold. `cases`: the red's
    test cases; `held`: the cases already re-queued with no fix landed (`landing_measure.requeued_unfixed`): a red that
    meets one is HELD for its fix (kb/Work PB5483), never re-run again."""
    import landing_measure as lm  # noqa: PLC0415
    prior = [r for r in w.get("reds") or [] if isinstance(r, dict)]
    text = f"pull request #{pr.number} is BOUNCED ({pr.bounced}) — {cls}: {why}"
    if any(r.get("removed") == pr.removed for r in prior):
        return WatchStep(LEFT, f"{text}; the lane re-queued this bounce and it did not take: the enqueue unit's finisher",
                         red=cls)
    requeued = any(r.get("act") == REQUEUE_BOUNCE for r in prior)
    if cls == lm.RUNNER and len(cases) > lm.FLAKE_CASES:
        # Review R2-1: many cases that ran GREEN at the gate failing at once is no flake (a Linux, Release or runner
        # difference, or a broken runner): it is never re-run, opens no cause, and is the finisher's.
        return WatchStep(LEFT, f"{text}; {len(cases)} red cases at once is no flake (more than {lm.FLAKE_CASES}): never "
                               "re-run (kb/Work PB5483): the enqueue unit's finisher", red=cls)
    if cls in lm.PIPELINE_CLASSES and not requeued and (again := sorted(cases & held)):
        return WatchStep(LEFT, f"{text}; HELD for the root fix of its cause ({', '.join(again[:NAMED])}): already re-queued "
                               "once with no fix landed, so never re-run again (kb/Work PB5483); the operator is alerted",
                         HOLD_BOUNCE, red=cls)
    if cls in lm.PIPELINE_CLASSES and not requeued:
        return WatchStep(WATCHING, f"{text}; re-queued once by the landing lane, no agent woken", REQUEUE_BOUNCE, red=cls)
    return WatchStep(LEFT, f"{text}{'; the second red of a landing the lane already re-queued' if requeued else ''}: the "
                           "enqueue unit's finisher", red=cls)


def watch_step(att: dict[str, Any], row_state: str | None, pr: Any, now: dt.datetime) -> WatchStep:
    """THE QUEUE WATCH'S DECISION for the pull request a DONE attempt queued (pure: the self-test drives every arm).
    `row_state`: the branch's survey state (None: no longer surveyed); `pr`: its open pull request (queue_facts), None
    when none is open.
      the branch's work is on main                   the watch ends (MERGED)
      in the queue                                   IN_QUEUE, and read again on every tick: it can leave the queue
                                                       without merging and without a failed run (a ruleset toggle, a
                                                       dequeue), and then it is a stalled read like any pull request
                                                       out of the queue (kb/Work PB3772)
      a required check FAILED before the queue, or   the watch ends: BOUNCED, the enqueue unit's finisher (PB3096;
        it conflicts with main (DIRTY)                 a conflict is the branch's: no re-run or reopen clears it)
      the queue removed it (a merge-group red)       CLASSIFY (kb/Work PB4271, PB4331): watch_queue reads its class
                                                       (`red_facts`, `classify_red`) and decides by `bounce_step`: an
                                                       INFRA or RUNNER red is re-queued once, every other class, and a
                                                       second red of the landing, ends the watch BOUNCED
      the head moved past the attempt's              the watch ends: a new push is a new landing, watched afresh
      any check on the head still running (its job   watching: nothing is late yet. ANY check, not only a required
        too)                                           one: `ci-gate` `needs:` every other job and has no check-run
                                                       until they end, so a long `audits` read as no work running and
                                                       the requeue cancelled it (kb/Work PB4052)
      not in the queue, the checks' work ended       a STALLED read once QUEUE_GRACE_MIN have passed since that work
        (or no check-run at all) or since the last     ended (the newest check-run's end on the head; a stuck one's JOB
        remedy or the last reading in the queue        time), or since the last remedy or the last reading IN the queue:
                                                       read 1 re-runs the workflow run of the stuck check, else of any
                                                       check (`gh run rerun`; a pull request with no check-run, or one
                                                       the watch saw IN the queue, whose checks passed, goes straight
                                                       to read 2's remedy), read 2 closes, reopens and re-queues the
                                                       pull request
                                                       (enqueue.requeue, which refuses while a run on the head is still
                                                       in progress), read 3 ALERTS the operator and acts no more
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
    if pr.red or pr.conflicts:
        why = f"{pr.red.name} failed" if pr.red else "it conflicts with main: merge state DIRTY"
        return WatchStep(LEFT, f"pull request #{pr.number} is BOUNCED ({why}): the enqueue unit's finisher")
    if pr.bounced:
        return WatchStep(LEFT, f"pull request #{pr.number} is BOUNCED ({pr.bounced}): its class is read first", CLASSIFY)
    # Running only while its job is SEEN running: a job the jobs API could not read (job_status '') is unknown, and an
    # unknown job must not hold the watch for ever, so its clock falls back to the enqueue, like no check-run at all.
    work = pr.head_runs
    running = [c for c in work if not c.resolved and c.job_status not in ("", "completed")]
    if running:
        return WatchStep(WATCHING, f"pull request #{pr.number}: {', '.join(c.name for c in running)} still running")
    ends = [t for t in (_iso(c.done_at) for c in work) if t]
    if not ends:
        ends += [t for t in (_iso(att.get("ended", "")),) if t]
    ends += [t for t in (_iso(r.get("at", "")) for r in reads) if t]
    # A pull request the watch SAW in the queue and now finds out of it (no failed run, no conflict: the cases above
    # ended) was dequeued: its checks passed, so a re-run of them is no remedy, and the clock starts at the last
    # reading in the queue, so the moment between its merge and the branch reading LANDED is no stall (kb/Work PB3772).
    queued = _iso(w.get("queued_at", ""))
    ends += [queued] if queued else []
    since = max(ends) if ends else now
    late = (now - since).total_seconds() / 60
    if late < QUEUE_GRACE_MIN:
        return WatchStep(WATCHING, f"pull request #{pr.number} not in the queue {late:.0f} min after its checks' work "
                                   f"ended (grace {QUEUE_GRACE_MIN} min)")
    stuck = next((c for c in work if c.stuck), None)
    cause = STUCK if stuck else NOT_QUEUED
    unknown = [c.name for c in work if not c.resolved and not c.stuck]
    what = (f"{stuck.name}'s check-run is {stuck.status.lower()} although its job completed at {stuck.job_completed_at}"
            if stuck else
            f"it left the merge queue (in it at {queued:%Y-%m-%dT%H:%M:%SZ}) without merging and without a failed run, "
            f"auto-merge {'on' if pr.auto_merge_at else 'OFF'} ({pr.merge_state or '?'})" if queued else
            f"{', '.join(unknown)} unresolved and its job could not be read, and it is not in the queue "
            f"({pr.merge_state or '?'})" if unknown else
            f"its required checks resolved but it is not in the queue ({pr.merge_state or '?'})")
    n = len(reads) + 1
    if n >= 3:
        return WatchStep(ALERTED, f"stalled read {n}: {what}, after a re-run and a requeue", ALERT, cause)
    run = 0 if queued else next((c.run for c in ([stuck] if stuck else list(work)) if c.run), 0)
    if n == 1 and run:
        return WatchStep(WATCHING, f"stalled read 1: {what}; re-running workflow run {run}", RERUN, cause, run)
    return WatchStep(WATCHING, f"stalled read {n}: {what}; closing, reopening and re-queueing it", REQUEUE, cause)


@dataclasses.dataclass(frozen=True)
class WatchEffects:
    """What a stalled read or a bounce does, and its record (the self-test plants recorders): rerun(repo, run) and
    requeue(repo, pr, head) answer (ok, detail); alert(subject, body, ref); measure(pr, branch, read, cause, act, ok);
    red_facts(attempt, pr) reads a bounce's facts (`red_facts`); measure_red(pr, branch, run, class, why, act, ok, cases)
    records its class, red cases and the cause's note (landing_measure.record_red); open_causes() reads the causes
    already re-queued or held with no fix landed, after releasing each whose note is landed (`open_causes_now`, kb/Work
    PB5483); cause_note(cases) names the open kb/Work note on origin/main that a hold of those cases names (`cause_note`)."""
    rerun: Callable[[str, int], tuple[bool, str]]
    requeue: Callable[[str, int, str], tuple[bool, str]]
    alert: Callable[[str, str, str], None]
    measure: Callable[[int, str, int, str, str, bool], None]
    red_facts: Callable[[dict[str, Any], Any], RedFacts]
    measure_red: Callable[[int, str, int, str, str, str, bool, tuple[str, ...], str], None]
    open_causes: Callable[[], dict[str, dict]]
    cause_note: Callable[[tuple[str, ...]], str]


# ── THE RELEASES OF A HOLD (kb/Work PB5483 review R1-1): no hold outlives its cause's fix ──────────────────────────────
# A hold is released by (a) a merged fix-first landing naming a failed run of the cause (read from the landing records),
# (b) the kb/Work note the hold names reading `status: landed` (or retired) on origin/main, which the lane checks on every
# read of the open causes and records as a release, or (c) the operator: `land_lane.py release-hold <case> --reason ...`.
# So a hold NAMES a note: when the lane first holds a cause it finds the open note on origin/main whose text names the
# failing test (`cause_note`); with none, its alert asks the operator to file one. And a hold older than
# landing_measure.HOLD_ALARM_H hours is alerted again, every HOLD_ALARM_H (`hold_alarms`), so none sits unseen.
TERMINAL_NOTE = re.compile(r"^status:\s*(landed|retired)\s*$", re.M)
HOLD_ALARMS = "hold-alarms.json"     # {case: the last alarm's time}: the age alarm's own memory, in the lane directory


def note_landed(repo: pathlib.Path, note: str) -> bool:
    """Whether kb/Work/<note>.md reads `status: landed` or `retired` on origin/main (False when it cannot be read)."""
    shown = subprocess.run(["git", "show", f"{MAIN}:kb/Work/{note}.md"], cwd=repo, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
    return shown.returncode == 0 and bool(TERMINAL_NOTE.search(shown.stdout.split("\n---", 1)[0]))


#: Where a note names its defect (review R2-2): its frontmatter `title:` and its `**Sites:**` line, the register's own
#: places for what a note is about. A mention anywhere else (an example, a related note) is no claim on the test.
NOTE_CLAIM = re.compile(r"^(?:title:.*|\*\*Sites?:\*\*.*)$", re.M)


def cause_note(repo: pathlib.Path, cases: tuple[str, ...]) -> str:
    """THE note a hold of `cases` names (review R2-2): the ONE open kb/Work note on origin/main whose title or Sites line
    names one of the failing tests (by its method name); "" when none does or more than one does (ambiguous: the hold
    alert then asks for one, and `name-hold` attaches it)."""
    found: set[str] = set()
    for case in cases:
        name = case.split("(", 1)[0].rsplit(".", 1)[-1]
        if len(name) < 6:
            continue
        hits = subprocess.run(["git", "grep", "-l", "-F", name, MAIN, "--", "kb/Work/*.md"], cwd=repo,
                              capture_output=True, text=True, encoding="utf-8", errors="replace")
        for line in hits.stdout.splitlines():
            m = re.search(r"kb/Work/((?:PB|R|A)\d+)\.md$", line)
            if not m:
                continue
            shown = subprocess.run(["git", "show", f"{MAIN}:kb/Work/{m.group(1)}.md"], cwd=repo, capture_output=True,
                                   text=True, encoding="utf-8", errors="replace").stdout
            if any(name in claim for claim in NOTE_CLAIM.findall(shown)) and not note_landed(repo, m.group(1)):
                found.add(m.group(1))
    return next(iter(found)) if len(found) == 1 else ""


def open_causes_now(cdir: pathlib.Path, landed: Callable[[str], bool], say: Say,
                    find: Callable[[tuple[str, ...]], str] = lambda cases: "") -> dict[str, dict]:
    """The open causes (landing_measure.open_causes) after release (b). A HELD cause with no note is resolved again on
    every read (`find`, cause_note: review R2-3), so a note filed after the hold is attached (landing_measure.HOLD_NOTES);
    then a cause whose note `landed(note)` reads landed is released, the release recorded with its reason."""
    import landing_measure as lm  # noqa: PLC0415
    recs, rel_path, notes_path = lm.read_all(cdir / lm.FILE), cdir / lm.RELEASES, cdir / lm.HOLD_NOTES
    causes = lm.open_causes(recs, lm.read_all(rel_path), lm.read_all(notes_path))
    for case, c in list(causes.items()):
        if c["held_at"] and not c["notes"] and (note := find((case,))):
            lm.record_hold_note(notes_path, case, note)
            c["notes"].append(note)
            say(f"land lane: the hold on {case} now names {note}")
        if done := next((n for n in c["notes"] if landed(n)), ""):
            lm.record_release(rel_path, case, lm.NOTE_LANDED, f"{done} reads landed on origin/main")
            say(f"land lane: the hold on {case} is released: its note {done} is landed")
            del causes[case]
    return causes


def hold_alarms(causes: dict[str, dict], last: dict[str, str], now: dt.datetime, hours: int) -> list[str]:
    """The held causes to alert again (pure): held longer than `hours`, and not alerted in the last `hours`."""
    def older(at: str | None) -> bool:
        return at is None or (now - dt.datetime.fromisoformat(at.replace("Z", "+00:00"))).total_seconds() >= hours * 3600
    return sorted(case for case, c in causes.items() if c.get("held_at") and older(c["held_at"]) and older(last.get(case)))


def watch_holds(cdir: pathlib.Path, branches: dict[str, list[dict[str, Any]]], effects: WatchEffects, say: Say,
                now: dt.datetime | None = None) -> None:
    """Every tick (kb/Work PB5483): the age alarm on each hold (`hold_alarms`), and the one alert that a fix-first landing
    (exit 17, DONE) was NOT jumped (review R1-4)."""
    import landing_measure as lm  # noqa: PLC0415
    now = now or now_utc()
    for branch, atts in branches.items():
        a = atts[-1] if atts else {}
        if finished(a) and a.get("rc") == EXIT_NOT_JUMPED and not a.get("jump_alerted"):
            effects.alert(f"Fix first NOT jumped: {branch}",
                          f"`enqueue.py land {branch} --fix-first` was refused the jump to the front of the merge queue "
                          "(its log names why); the pull request is queued at the BACK. Run the same command again once "
                          "its pull-request check has passed: it dequeues and jumps (kb/Work PB5483).", str(a.get("log")))
            update(a, {"jump_alerted": now.isoformat(timespec="seconds")})
    causes = effects.open_causes()
    path = lane_dir(cdir) / HOLD_ALARMS
    last = coord.read_json(path, {}) or {}
    due = hold_alarms(causes, last, now, lm.HOLD_ALARM_H)
    for case in due:
        c = causes[case]
        effects.alert(f"A hold older than {lm.HOLD_ALARM_H} h: {case}",
                      f"The landing lane has held {case} since {c['held_at']} (kb/Work PB5483): no fix has released it. "
                      + (f"Its note {', '.join(c['notes'])} is not landed on origin/main. " if c["notes"] else
                         "It names no kb/Work note: file one for the failing test. ")
                      + f"If its fix is on main, `python scripts/orchestrator/land_lane.py release-hold \"{case}\" "
                        "--reason \"<the fix>\"`.", str(path))
        last[case] = now.isoformat(timespec="seconds")
    if due:
        coord.write_json(path, last)


def release_hold(cdir: pathlib.Path, case: str, reason: str, say: Say = print) -> int:
    """The operator's release (c): refused (exit 2) for a cause that is not open, so a typo releases nothing."""
    import landing_measure as lm  # noqa: PLC0415
    causes = lm.open_causes(lm.read_all(cdir / lm.FILE), lm.read_all(cdir / lm.RELEASES), lm.read_all(cdir / lm.HOLD_NOTES))
    if case not in causes:
        say(f"land lane: {case!r} is not held or re-queued-and-open; open causes: {', '.join(sorted(causes)) or 'none'}")
        return 2
    lm.record_release(cdir / lm.RELEASES, case, lm.OPERATOR, reason)
    say(f"land lane: the hold on {case} is released ({reason})")
    return 0


def name_hold(cdir: pathlib.Path, case: str, note: str, say: Say = print) -> int:
    """The operator attaches the note of an open cause (review R2-3: a hold gains its note later, and that note's landing
    then releases it). Refused (exit 2) for a cause that is not open or a note id that is not one."""
    import landing_measure as lm  # noqa: PLC0415
    causes = lm.open_causes(lm.read_all(cdir / lm.FILE), lm.read_all(cdir / lm.RELEASES), lm.read_all(cdir / lm.HOLD_NOTES))
    if case not in causes:
        say(f"land lane: {case!r} is not an open cause; open causes: {', '.join(sorted(causes)) or 'none'}")
        return 2
    try:
        lm.record_hold_note(cdir / lm.HOLD_NOTES, case, note)
    except ValueError as exc:
        say(f"land lane: {exc}")
        return 2
    say(f"land lane: the hold on {case} now names {note}; it is released when {note} lands on main")
    return 0


def real_effects(cdir: pathlib.Path, say: Say, repo: pathlib.Path = coord.REPO) -> WatchEffects:
    import enqueue  # noqa: PLC0415  (the only file that queues a pull request: landing_rules.py)
    import landing_measure  # noqa: PLC0415
    import ruleset  # noqa: PLC0415

    def measure(pr: int, branch: str, read: int, cause: str, act: str, ok: bool) -> None:
        try:
            landing_measure.record_stall(cdir / landing_measure.FILE, pr, branch, read, cause, act, ok)
        except (OSError, ValueError) as exc:   # the record is the instrument's, never the watch's stop; it is said
            say(f"land lane: PR #{pr}: the stall was NOT recorded in {landing_measure.FILE} ({exc})")

    def measure_red(pr: int, branch: str, run: int, cls: str, why: str, act: str, ok: bool,
                    cases: tuple[str, ...], cause: str) -> None:
        try:
            landing_measure.record_red(cdir / landing_measure.FILE, pr, branch, run, cls, why, act, ok, cases=cases,
                                       cause=cause)
        except (OSError, ValueError) as exc:   # the record is the instrument's, never the watch's stop; it is said
            say(f"land lane: PR #{pr}: the red's class was NOT recorded in {landing_measure.FILE} ({exc})")
    return WatchEffects(
        rerun=lambda repo, run: enqueue.rerun_check(repo, run, gh=ruleset.real_gh),
        requeue=lambda repo, pr, head: enqueue.requeue(repo, pr, head, gh=ruleset.real_gh, state=enqueue.queue_state),
        alert=lambda subject, body, ref: notify_operator(cdir, subject, body, ref, say, kind="question"),
        measure=measure, red_facts=red_facts, measure_red=measure_red,
        open_causes=lambda: open_causes_now(cdir, lambda note: note_landed(repo, note), say,
                                            lambda cases: cause_note(repo, cases)),
        cause_note=lambda cases: cause_note(repo, cases))


def watch_text(att: dict[str, Any]) -> str:
    """The queue watch on this attempt, in one clause (status, the survey's NOT QUEUED row)."""
    w = att.get("watch")
    if not w:
        return "the landing lane's queue watch has not read it yet (it reads on every tick)"
    reads = "; ".join(f"read {r['read']} {r['cause']}: {r['act']} {'done' if r.get('ok') else 'FAILED'} at {r['at']}"
                      for r in w.get("reads") or [])
    state = w.get("state", WATCHING)
    # Its reading is the watch's LAST one: a tick later it may differ, and an ENDED watch reads nothing again, so the
    # text says which, never presenting a stale reading as the current state (kb/Work PB3772).
    when = (f" — read at {w['at']}" if w.get("at") else "") + (", and the watch has ENDED: nothing reads it again"
                                                              if state in WATCH_DONE else "")
    return (f"queue watch {state.upper()}"
            + (f" ({w.get('why')})" if w.get("why") else "") + when + (f" [{reads}]" if reads else ""))


def watch_queue(cdir: pathlib.Path, branches: dict[str, list[dict[str, Any]]], rows: list[dict[str, Any]],
                prs: dict[str, Any] | None, effects: WatchEffects, held: str | None, say: Say,
                now: dt.datetime | None = None) -> None:
    """Read each branch's latest attempt that ended ENQUEUED (exit 0) and whose watch has not ended (WATCH_DONE; a
    pull request in the queue is read again on every tick, kb/Work PB3772), and act on a stalled read (watch_step).
    Unobserved facts (None) read nothing; a stop (`held`) lets it read but not act."""
    now = now or now_utc()
    if prs is None:
        if any(finished(a[-1]) and classify(a[-1]) == DONE and (a[-1].get("watch") or {}).get("state") not in WATCH_DONE
               for a in branches.values()):
            say("land lane: queue watch: GitHub's facts were not observed this tick; nothing read")
        return
    import prune_worktrees  # noqa: PLC0415  (scripts/: it imports this module back, lazily)
    # A branch whose work is on main reads LANDED here by its CONTENT, even where the survey's state is not LANDED (a
    # dirty worktree reads DECISION, a merged branch with no commit of its own IN FLIGHT while fresh: prune_worktrees.
    # lifecycle, kb/Work PB3609): its pull request is merged, not closed by hand.
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
        if step.act == CLASSIFY:
            classify_bounce(att, w, pr, branch, effects, say, now)
            continue
        w.update(state=step.state, why=step.why, pr=pr.number if pr else (w.get("pr") or att.get("pr")),
                 at=now.isoformat(timespec="seconds"))
        if step.state == IN_QUEUE:
            w["queued_at"] = w["at"]    # the last reading IN the queue: watch_step's clock for one that then leaves it
        if step.act:
            number = int(w.get("pr") or 0)
            if step.act == RERUN:
                ok, detail = effects.rerun(pr.repo, step.run)
            elif step.act == REQUEUE:
                ok, detail = effects.requeue(pr.repo, pr.number, pr.head)
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


def classify_bounce(att: dict[str, Any], w: dict[str, Any], pr: Any, branch: str, effects: WatchEffects, say: Say,
                    now: dt.datetime) -> None:
    """A merge-group red, classified and acted on (the class section above; kb/Work PB4271, PB4331): its facts read
    (`effects.red_facts`), its class decided (`classify_red`), the watch's step taken (`bounce_step`): an INFRA or
    RUNNER red is re-queued once (`effects.requeue`, enqueue.requeue: a refused or failed re-queue ends the watch
    BOUNCED, the finisher's, never silence), every other ends it BOUNCED with its class. Each red is recorded on the
    watch (`reds`, which `bounce_step` reads to re-queue a landing once) and in landing_measure.py's record."""
    import landing_measure as lm  # noqa: PLC0415  (the record's vocabulary of acts)
    facts = effects.red_facts(att, pr)
    cls, why = classify_red(facts)
    held = frozenset(effects.open_causes()) if cls in lm.PIPELINE_CLASSES and facts.failed else frozenset()
    step = bounce_step(w, pr, cls, why, facts.failed, held)
    act = {REQUEUE_BOUNCE: "requeue", HOLD_BOUNCE: lm.HOLD_FOR_FIX}.get(step.act, "finisher")   # landing_measure.RED_ACTS
    note = ""
    if step.act == HOLD_BOUNCE:   # a hold NAMES its cause's note, so that note's landing releases it (review R1-1)
        note = effects.cause_note(tuple(sorted(facts.failed & held)))
        effects.alert(f"Landing held for a root fix: PR #{pr.number} ({branch}) met a cause already re-run once",
                      f"{step.why}. Re-running it again would only meet it again. "
                      + (f"Its note is {note}: the hold ends when {note} lands on main, " if note else
                         "No open kb/Work note names the failing test in its title or Sites line: FILE ONE (the lane "
                         "attaches it on its next read, or `land_lane.py name-hold <case> <note>`), ")
                      + "or when a root fix lands with `python scripts/orchestrator/enqueue.py land <the fix's branch> "
                      f"--fix-first \"<kb/Work note> run {facts.run}\"`, or by `land_lane.py release-hold <case> --reason "
                      f"...`. Then land {branch} again (`land_lane.py rearm {branch} --why ...`).", att["log"])
    ok, detail = True, ""
    if step.act == REQUEUE_BOUNCE:
        ok, detail = effects.requeue(pr.repo, pr.number, pr.head)
        if not ok:
            step = WatchStep(LEFT, f"pull request #{pr.number} is BOUNCED ({pr.bounced}) — {cls}: {why}; the lane's "
                                   f"re-queue FAILED ({detail}): the enqueue unit's finisher", red=cls)
    at = now.isoformat(timespec="seconds")
    w.update(state=step.state, why=step.why, pr=pr.number, at=at,
             reds=[*(w.get("reds") or []), {"run": facts.run, "class": cls, "why": why[:240],
                                             "act": {"requeue": REQUEUE_BOUNCE}.get(act, act), "ok": ok,
                                             "removed": pr.removed, "at": at}])
    effects.measure_red(pr.number, branch, facts.run, cls, why, act, ok, tuple(sorted(facts.failed)), note)
    say(f"land lane: {branch}: queue watch: {step.why}" + (f" — requeue done: {detail}" if step.act else ""))
    update(att, {"watch": w})


# ── the tick ───────────────────────────────────────────────────────────────────────────────────────────────────────
def stop_reason(cdir: pathlib.Path) -> str | None:
    """Why no attempt may start now: the loop's STOP, its fleet stop `scratch\\STOP-loop` (the hard context cap and
    STOP), the owner's global stop, or HALT. Never `STOP-UNIT` alone (kb/Work PB3402): that is the soft context cap's
    signal to the running UNIT, and a landing is its own process, which adds nothing to the unit's context, so the
    unit's size must not hold finished work."""
    halt = lane_dir(cdir) / HALT
    return loop_stop(cdir) or (f"HALT (a run ended QUEUE NOT ACTIVE; `land_lane.py clear-halt`) ({halt})"
                               if halt.exists() else None)


def loop_stop(cdir: pathlib.Path) -> str | None:
    """Why no agent of the loop may start: the loop's STOP, its fleet stop `scratch/STOP-loop`, or the owner's global
    stop (`stop_reason` without HALT, which is the landing's alone). THE one reader of the three: the review lane
    (review_lane.py, kb/Work PB3608) starts no reviewer or finisher under any of them either."""
    for path, what in ((cdir / "STOP", "the loop's STOP"),
                       (coord.fleet_stop(cdir / "scratch", "loop"), "the loop's fleet stop"),
                       (coord.global_stop(str(cdir)), "the owner's global STOP")):
        if path.exists():
            return f"{what} ({path})"
    return None


def reap(cdir: pathlib.Path, atts: list[dict[str, Any]], alive: Callable[[dict[str, Any]], bool], say: Say) -> None:
    for a in atts:
        if finished(a):
            continue
        if alive(a):
            continue
        # Its tip now is its end head: a runner that died after `enqueue.py land` committed (main merged in, the stamp)
        # leaves the branch IN FLIGHT in the survey, and only this record lets the lane retry it. A runner that wrote
        # its rc and then exited is not DIED: the fresh record decides, under the record's lock.
        update(a, lambda fresh: {} if "rc" in fresh else {
            "outcome": "DIED", "ended": now_utc().isoformat(timespec="seconds"),
            "verdict": last_verdict(pathlib.Path(a["log"])), "end_head": tip_of(a.get("worktree"))})
        if a.get("outcome") == "DIED":
            say(f"land lane: {a['branch']}: the runner of attempt {a['id']} is gone with no exit code (DIED; {a['log']})")
    for a in atts:
        if finished(a) and a.get("rc") == EXIT_QUEUE_NOT_ACTIVE and not a.get("halted"):
            halt = lane_dir(cdir) / HALT
            if not halt.exists():
                halt.write_text(f"{now_utc().isoformat(timespec='seconds')} {a['branch']} attempt {a['id']}: "
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
        if r["state"] in ("STOPPED", "BOUNCED", *QUEUE_OWNED) or r["tip"] != last.get("end_head") \
                or not r["worktree"]:
            continue
        out.append(r)
        seen.add(branch)
    for branch, rec in (rearmed or {}).items():
        if branch not in seen and not rearm_hold(rec, named.get(branch)):
            out.append(named[branch])
    return out


def launch(cdir: pathlib.Path, repo: pathlib.Path, row: dict[str, Any], why: str,
           start: Callable[[list[str], pathlib.Path], int], argv: list[str] | None = None) -> dict[str, Any]:
    """Start one attempt. Only `tick` calls it, under the tick lock, so the attempt's NUMBER (one past every attempt
    recorded; `attempt_order`) is taken by one process at a time and names one attempt."""
    now = now_utc()
    d = lane_dir(cdir) / ATTEMPTS
    d.mkdir(parents=True, exist_ok=True)
    n = max((attempt_order(p)[0] for p in d.glob("*.json")), default=0) + 1
    aid = f"{n:06d}-{now:%Y%m%dT%H%M%SZ}-{slug(row['name'])}"
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
         alive: Callable[[dict[str, Any]], bool] = runner_alive,
         wait_free: Callable[[int | None, str], tuple[bool, str]] = wait_free,
         argv_for: Callable[[dict[str, Any]], list[str] | None] = lambda row: None,
         effects: WatchEffects | None = None, now: dt.datetime | None = None, say: Say = print) -> int:
    d = lane_dir(cdir)
    d.mkdir(parents=True, exist_ok=True)
    lock = ExclusiveLock.try_take(d / TICK_LOCK, f"land_lane.py tick (pid {os.getpid()}, {now_utc():%Y-%m-%d %H:%M:%S}Z)")
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
        coord.write_json(d / SURVEY, {"at": now_utc().isoformat(timespec="seconds"), "rows": rows, "landing_order": order})
        branches = by_branch(atts)
        why = stop_reason(cdir)
        # THE QUEUE WATCH (kb/Work PB3527), on the facts this survey read: under a stop it reads, and acts on nothing.
        effects = effects or real_effects(cdir, say, repo)
        watch_queue(cdir, branches, rows, prs, effects, why, say, now)
        watch_holds(cdir, branches, effects, say, now)   # the hold's age alarm and a refused jump's alert (kb/Work PB5483)
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
                    say(f"land lane: {row['name']}: {dec.why}")   # why a retry-when-free exit still waits, every tick
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
                     {"rc": rc, "ended": now_utc().isoformat(timespec="seconds"), "end_head": tip_of(rec.get("worktree")),
                      "verdict": last_verdict(pathlib.Path(rec["log"])), "pr": queued_pr(pathlib.Path(rec["log"]))})
    print(f"land lane: attempt {rec['id']} ended exit {rc}: {current['verdict'] or 'no verdict line'}", flush=True)
    if rc == EXIT_CONFLICT:
        # The finisher's brief (kb/Work PB3640), written after the outcome is recorded, so a brief that fails leaves the
        # stop as it is: the failure is recorded on the attempt (`brief_error`), and `conflict-brief` computes one.
        try:
            named = write_conflict_brief(path.resolve().parents[2], current)   # <coord>/land-lane/attempts/<id>.json
            update(current, named)
            print(f"land lane: conflict brief {named['brief']} (against origin/main {named['brief_main'][:12]})",
                  flush=True)
        except (OSError, ValueError, KeyError, subprocess.SubprocessError) as e:
            update(current, {"brief_error": f"{type(e).__name__}: {e}"[:300]})
            print(f"land lane: the conflict brief could not be written ({type(e).__name__}: {e})", flush=True)
    return rc


# ── what the enqueue unit and next_unit.py read ────────────────────────────────────────────────────────────────────
ACTION = {STOPPED: "the enqueue unit's: step 4 finisher (a CONFLICT one is the review lane's: started with its brief and "
                   "collected; UNREVIEWED: step 2 reviewer; a repeated exit 2 or 9: handoff)",
          DONE: "the lane's queue watch follows its pull request into the merge queue; the enqueue unit's: file its "
                "leads (step 5), complete its landing record (step 6b), then collect it",
          FREE: "the lane retries it at this head once what it waits on is free (exit 12: the worktree's gate lock is free and no Linux gate of the tree runs; exit 13: the worktree's land lock is free; exit 15: no open pull request owns a DEVLOG entry the branch carries)",
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
        return None if self.survey_at is None else ((now or now_utc()) - self.survey_at).total_seconds() / 60


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
    """What only the enqueue unit's agents (or its clerk and its records) can do: rows BOUNCED in a survey younger than
    SURVEY_MAX_AGE_MIN (only while the lane is neither landing it nor re-armed to: its finisher already resolved it,
    and not one GitHub bounced as DIRTY, which the REVIEW LANE's conflict finisher takes, kb/Work PB3608), STOPPED
    attempts not yet collected (an exit 4 the review lane started a finisher for is collected by it), and ENQUEUED
    attempts not yet collected (their leads and landing records). A row WAITING FOR REVIEW is the review lane's
    (review_lane.agent_work reports what it could not do). A row the survey reads STOPPED is counted through its
    attempt, never by its state: uncollected, it is the unit's work; collected, the unit has already dispatched its
    owner (kb/Work PB3125)."""
    lane = view(cdir)
    age = lane.survey_age_min(now)
    reasons = []
    # A BOUNCED row whose red the lane RE-QUEUED is still the lane's while its watch runs: the survey this tick wrote read
    # the bounce before the watch re-queued it (kb/Work PB4271, PB4331). A red the lane leaves to a finisher ends the watch.
    requeued = {a["branch"] for a, c in lane.latest if c == DONE and (a.get("watch") or {}).get("reds")
                and (a.get("watch") or {}).get("state") not in WATCH_DONE}
    landing = {a["branch"] for a, c in lane.latest if c == "running"} | set(lane.rearms) | requeued
    if age is not None and age <= SURVEY_MAX_AGE_MIN:
        names = [r["name"] for r in lane.rows if r.get("state") == "BOUNCED" and r["name"] not in landing
                 and not r.get("conflicts")]
        if names:
            reasons.append(f"bounced: {', '.join(names)}")
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


def rearm_hold(rec: dict[str, Any], row: dict[str, Any] | None) -> str:
    """Why the next tick does NOT land a pending re-arm, read from the survey's row for the branch, or '' when it does:
    THE re-arm predicate, which `candidates` and the status line (`rearm_effect`) both ask, so the status line cannot
    promise a landing the lane never starts (kb/Work PB3628 and its review lead R1-1: a hand-kept copy dropped the
    worktree arm)."""
    if row is None:
        return "the branch is not in the last survey; the next tick drops the re-arm"
    if row.get("tip") != rec.get("head"):
        return (f"the branch moved to {str(row.get('tip'))[:12]}; the next tick drops the re-arm (a new head goes "
                "through review)")
    if not row.get("worktree"):
        return "the survey names no worktree for it, so the next tick does NOT land it (the re-arm waits for one)"
    if row.get("state") == "LANDED":
        return "its work is on main (the survey reads it LANDED): the next tick drops the re-arm, there is nothing to land"
    if row.get("state") == "ENQUEUED":
        return ("the survey reads it ENQUEUED: the merge queue holds its pull request, so the next tick does NOT land "
                "it (the queue watch keeps reading it until it merges, and acts if it leaves the queue unmerged)")
    if row.get("state") == "NOT QUEUED":
        return ("the survey reads it NOT QUEUED: its pull request was handed to the queue, so the next tick does NOT "
                "land it. The queue watch acts on it (a re-run, a requeue, then an alert to the operator); once it "
                "has ALERTED, only the operator does (the branch's status line says the watch's state)")
    return ""


def rearm_effect(rec: dict[str, Any], row: dict[str, Any] | None) -> str:
    """What the next tick does with a pending re-arm (rearm_hold, the one predicate)."""
    return rearm_hold(rec, row) or "the next tick lands it again at this head"


def brief_text(a: dict[str, Any]) -> str:
    """The status line's pointer to a CONFLICT attempt's brief (kb/Work PB3640): the command that prints it, and why
    the runner recorded none when it could not write one."""
    cmd = f"conflict brief: `land_lane.py conflict-brief {a['branch']}`"
    return cmd + (f" (the runner wrote none: {a['brief_error']}; the command computes one)" if a.get("brief_error")
                  else "" if a.get("brief") else " (none recorded; the command computes one)")


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
    rows = {r.get("name"): r for r in lane.rows}
    out += [f"{'REARMED':16} {b} @{str(r.get('head'))[:12]}: {r.get('why') or '-'} — {rearm_effect(r, rows.get(b))} "
            f"(re-armed {r.get('at')})" for b, r in sorted(lane.rearms.items())]
    order = {"running": 0, STOPPED: 1, HALTED: 2, FREE: 3, ONCE: 3, DONE: 4}
    for a, cls in sorted(lane.latest, key=lambda t: (order.get(t[1], 9), t[0]["branch"])):
        mark = " [collected]" if a.get("collected") else ""
        watch = f" [{watch_text(a)}]" if cls == DONE else ""
        brief = f" — {brief_text(a)}" if a.get("rc") == EXIT_CONFLICT else ""
        out.append(f"{cls.upper():16} {a['branch']} @{str(a.get('head'))[:12]} {outcome(a)}: {a.get('verdict') or '-'} — "
                   f"{ACTION.get(cls, '')}{mark}{watch}{brief} (attempt {a['id']}; {a['log']})")
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

    def wait_runners(cdir: pathlib.Path) -> None:
        """Wait until every runner an attempt names has EXITED: bounded by the runner's own exit (its outcome is
        written before it exits), never by a fixed wall-clock ceiling (kb/Work PB3022; PB3640 review R1-5)."""
        while any(runner_alive(a) for a in attempts(cdir)):
            time.sleep(0.2)

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
                self.wait = self.overlap                   # what a retry-when-free exit waits on: planted, or the real one
                self.prs: dict[str, Any] | None = None     # GitHub's facts: unobserved unless a case plants them
                self.now: dt.datetime | None = None
                self.effects: list[tuple] = []
                self.reds: dict[str, RedFacts] = {}        # each bounced branch's planted facts (red_facts)
                self.held: dict[str, dict] = {}            # the open causes (open_causes)

            def survey(self, repo: pathlib.Path, cdir: pathlib.Path) -> tuple[list, list[str], dict | None]:
                return self.rows, [r["name"] for r in self.rows if r["state"] == "WAITING TO LAND"], self.prs

            def watch_effects(self) -> WatchEffects:
                rec = self.effects.append
                return WatchEffects(rerun=lambda repo, run: rec(("rerun", repo, run)) or (True, "re-run requested"),
                                    requeue=lambda repo, pr, head: rec(("requeue", repo, pr)) or (True, "requeued"),
                                    alert=lambda subject, body, ref: rec(("alert", subject, body)),
                                    measure=lambda pr, b, read, cause, act, ok: rec(("measure", pr, b, read, cause, act)),
                                    red_facts=lambda att, pr: self.reds.get(att["branch"], RedFacts(unobserved="planted")),
                                    measure_red=lambda pr, b, run, cls, why, act, ok, cases, cause: rec(("red", pr, b, run, cls, act, ok)),
                                    open_causes=lambda: self.held, cause_note=lambda cases: "PB5478")

            def start(self, argv: list[str], log: pathlib.Path) -> int:
                self.started.append(json.loads(pathlib.Path(argv[-1]).read_text(encoding="utf-8"))["branch"])
                pid = 40000 + len(self.started)
                self.live.add(pid)
                return pid

            def alive(self, a: dict[str, Any]) -> bool:
                return a.get("pid") in self.live

            def overlap(self, rc: int | None, wt: str) -> tuple[bool, str]:
                self.asked.append(wt)
                return self.free, "shares scripts/x.py with w1069g (dispatched earlier)"

            def tick(self, cdir: pathlib.Path) -> list[str]:
                before = len(self.started)
                out: list[str] = []
                tick(cdir, root, survey=self.survey, start=self.start, alive=self.alive, wait_free=self.wait,
                     effects=self.watch_effects(), now=self.now, say=out.append)
                self.said = out
                return self.started[before:]

        def finish(cdir: pathlib.Path, branch: str, rc: int, verdict: str, end_head: str | None = None) -> None:
            """The runner's own completion of the branch's running attempt (the record `run_attempt` writes)."""
            for a in attempts(cdir):
                if a["branch"] == branch and not finished(a):
                    update(a, {"rc": rc, "verdict": verdict, "ended": now_utc().isoformat(),
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
        expect("agent work: the row WAITING FOR REVIEW is the review lane's, not the enqueue unit's (kb/Work PB3608), "
               "and the runs in flight are nobody's", w.reasons == [], str(w.reasons))

        # 1b. the real mapping from the survey's rows (the Fake above stands in for it): a row's report reaches the
        # review lane as its file's PATH, which it stats and reads (kb/Work PB4055: the mapping read a field the
        # survey's report lacked, and every tick on main crashed)
        import prune_worktrees  # noqa: PLC0415  (scripts/: it imports this module back, lazily)
        from reports import IndexedReport  # noqa: PLC0415
        rep_path = root / "reports" / "w1a-PB1-report.md"
        planted = [prune_worktrees.Row("w1a", "CHECK", [], [], 1, str(root / "wt-w1a"), None, 0, 5.0, "",
                                       IndexedReport(rep_path, "DONE", "PB1", False, 5.0), "WAITING FOR REVIEW",
                                       tip="a1"),
                   prune_worktrees.Row("w1b", "UNLANDED", [], [], 1, None, None, 0, 5.0, "", None, "DECISION")]
        mapped = row_dicts(planted, None)
        expect("the survey's rows map to the tick's dicts: a reported row carries its report's path and lead, an "
               "unreported one None for both",
               [(d["name"], d["report"], d["lead"], d["conflicts"]) for d in mapped]
               == [("w1a", str(rep_path), "PB1", False), ("w1b", None, None, False)], str(mapped))
        import queue_facts  # noqa: PLC0415
        dirty = {"w1a": queue_facts.PullRequest(1, "u1", "a1", False, merge_state="DIRTY"),
                 "w1b": queue_facts.PullRequest(2, "u2", "b1", True, merge_state="DIRTY")}
        expect("... with GitHub observed, a pull request GitHub cannot merge (DIRTY, not queued) is a conflict, a queued "
               "one is not (PullRequest.conflicts read through the real mapping)",
               [d["conflicts"] for d in row_dicts(planted, dirty)] == [True, False], str(row_dicts(planted, dirty)))
        foreseen_row = prune_worktrees.Row("w1c", "CHECK", [], [], 1, None, None, 0, 5.0, "", None, "BOUNCED", tip="c1",
                                           foreseen=(("w1a", "a1"),))
        expect("... and a queued head the queue's simulation finds conflicting with an entry ahead of it (kb/Work PB3723) "
               "is a conflict too, naming that entry's branch and head for the brief",
               [(d["conflicts"], d["foreseen"]) for d in row_dicts([foreseen_row], {})]
               == [(True, [{"branch": "w1a", "head": "a1"}])], str(row_dicts([foreseen_row], {})))

        # 2. exit 5 (MERGE NOT COMPLETED, BRANCH-CARRIED) is never relaunched at the same head; a new head (a finisher's commit) is eligible.
        f.rows[0] = row("w1a", "DECISION", "a2")                  # enqueue merged main in: the survey no longer lists it
        finish(c, "w1a", 5, "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on a2", end_head="a2")
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

        # 3. exit 3 (the landing check's STOP: a never-carried path or a duplicate public-API symbol; an edit meeting
        #    another branch's no longer stops, kb/Work PB4452 first ready lands): only a new commit clears it, so it is a
        #    STOP, the enqueue unit's (a finisher), never retried at the same head.
        c3 = fresh("check-stop")
        f3 = Fake([row("w2a", "WAITING TO LAND", "h1")])
        f3.tick(c3)
        finish(c3, "w2a", 3, "NOT QUEUED (exit 3, above)", end_head="h2")
        f3.live.clear()
        f3.rows[0] = row("w2a", "IN FLIGHT", "h2")
        f3.free = True
        expect("exit 3 is a STOP: never retried at the same head, and it is the enqueue unit's",
               f3.tick(c3) == [] and f3.asked == [] and any("w2a" in r for r in agent_work(c3).reasons),
               f"{f3.started} {f3.asked} {agent_work(c3).reasons}")

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
        import gate  # noqa: PLC0415

        held = ExclusiveLock.try_take(gate.Host(lock_wt).lock_path(), "gate (pid 356316)")
        try:
            while_held = wait_free(EXIT_GATE_BUSY, str(lock_wt))
        finally:
            held.release()
        after = wait_free(EXIT_GATE_BUSY, str(lock_wt))
        expect("wait_free for exit 12 reads the gate driver's own lock of the worktree: held names its holder, then free",
               while_held[0] is False and "pid 356316" in while_held[1] and after == (True, ""), f"{while_held} {after}")
        # exit 12 for the branch's OWN gate not having written its verdict yet (kb/Work PB3662): the lane launched the
        # landing beside the implementer's running gate (a resume merged main in, the gate started, the survey read the
        # new tip WAITING TO LAND). The same head must relaunch once the run writes its verdict, with no `rearm`.
        c3c = fresh("owed")
        f3c = Fake([row("w2c", "WAITING TO LAND", "o1")])
        f3c.wait = wait_free
        gate_wt = ScratchRepo.init(root / "wt-w2c").path
        owed_run = gate_wt / gate.RUN_ROOT / "000005-20261024T225004Z"
        owed_run.mkdir(parents=True)
        # The implementer's gate holds the worktree's gate lock until it has written its verdict. Its gate.pid names THIS
        # process, which stays alive afterwards: a process id says nothing about the gate (review R1-1: Windows reuses a
        # dead gate's), so the lane relaunches the moment the lock is free, whatever that pid file still names.
        gate.write_pid(owed_run, os.getpid())
        gate_held = ExclusiveLock.try_take(gate.Host(gate_wt).lock_path(),
                                           f"gate {gate_wt} (pid 356318, since T{gate.RUN_LABEL}{owed_run})")
        try:
            f3c.tick(c3c)
            finish(c3c, "w2c", EXIT_GATE_BUSY, "GATE BUSY — the gate run 000005-20261024T225004Z of this worktree is "
                   "still running", end_head="o1")
            f3c.live.clear()
            waiting = f3c.tick(c3c)
            waiting_said = " | ".join(f3c.said)
            owed_now = wait_free(EXIT_GATE_BUSY, str(gate_wt))
        finally:
            gate_held.release()                                    # the gate ended (or died): no verdict file, pid alive
        relaunched = f3c.tick(c3c)
        expect("exit 12 for a gate that is still running: the lane waits at that head while the worktree's gate lock is "
               "held (wait_free names the run it writes), and relaunches the SAME head once the lock is free, with no "
               "rearm and no enqueue-unit work, however alive its gate.pid process still reads (kb/Work PB3662, "
               "review R1-1)",
               waiting == [] and "000005-20261024T225004Z" in owed_now[1] and owed_now[0] is False
               and "still waits on" in waiting_said and relaunched == ["w2c"] and agent_work(c3c).reasons == []
               and not (lane_dir(c3c) / REARM).exists(), f"{waiting} {waiting_said} {owed_now} {relaunched}")
        # exit 13 (kb/Work PB3310): another `land` of the branch runs; the lane waits on the worktree's land lock, which
        # is enqueue.py's own (land_lock_path), exactly as it waits on the gate driver's for exit 12.
        land_held = ExclusiveLock.try_take(land_lock_path(lock_wt), "enqueue.py land x (pid 356317)")
        try:
            land_while_held = wait_free(EXIT_LAND_BUSY, str(lock_wt))
        finally:
            land_held.release()
        land_after = wait_free(EXIT_LAND_BUSY, str(lock_wt))
        expect("wait_free for exit 13 reads enqueue.py's own land lock of the worktree: held names its holder, then free; "
               "the exit is a retry-when-free class, never the enqueue unit's",
               land_while_held[0] is False and "pid 356317" in land_while_held[1] and land_after == (True, "")
               and classify({"rc": EXIT_LAND_BUSY}) == FREE, f"{land_while_held} {land_after}")
        # exit 15 (kb/Work PB3145): the branch merged a predecessor whose pull request (#701) is still open, so it adds the
        # predecessor's stamped entry beside its own. The lane waits on enqueue.py's own reader (open_predecessors) and
        # relaunches the SAME head once no open pull request owns an entry the branch carries, with no rearm.
        pw = ScratchRepo.init(root / "wt-w2d")
        (pw.path / "DEVLOG").mkdir()
        (pw.path / "DEVLOG" / "0001-pb1-base.md").write_text(
            "## Entry 1 — 2026-10-01 09:00 PDT — PB1: base\n\nWhy.\n", encoding="utf-8", newline="\n")
        pw.git("add", "DEVLOG")
        pw.git("commit", "--quiet", "-m", "base")
        pw.origin(root / "origin-w2d.git")
        pw.git("push", "--quiet", "origin", "main")
        pw.git("switch", "--quiet", "-c", "w2d")
        (pw.path / "DEVLOG" / "0050-pb9-the-predecessor.md").write_text(
            "## Entry 50 — 2026-10-24 10:00 PDT — PB9: the predecessor\n\nWhy.\n", encoding="utf-8", newline="\n")
        pw.git("add", "DEVLOG")
        pw.git("commit", "--quiet", "-m", "the merged predecessor's stamped entry")
        predecessor_oid = pw.git("rev-parse", "HEAD")
        (pw.path / "own.txt").write_text("the branch's own work", encoding="utf-8", newline="\n")
        pw.git("add", "own.txt")
        pw.git("commit", "--quiet", "-m", "the branch's own work")
        pred_listing = {"number": 701, "headRefName": "pred", "headRefOid": predecessor_oid,
                        "files": [{"path": "DEVLOG/0050-pb9-the-predecessor.md"}]}
        github: dict[str, Any] = {"open": [pred_listing], "fail": False}

        def planted_gh(args: list[str]) -> tuple[int, str, str]:
            """GitHub as enqueue.py reads it: the listing of the open pull requests and queue_facts.read's two calls, each
            open pull request with auto-merge on."""
            import queue_facts  # noqa: PLC0415
            if args[:2] in (["repo", "view"], ["api", "graphql"]):
                return (1, "", "rate limited") if github["fail"] else queue_facts.fake([
                    queue_facts.node(pr["number"], pr["headRefName"], pr["headRefOid"],
                                     auto="t")
                    for pr in github["open"]])(args)
            assert args[:2] == ["pr", "list"] and PR_LIST_FIELDS in args, args
            return (1, "", "rate limited") if github["fail"] else (0, json.dumps(github["open"]), "")
        c3d = fresh("predecessor")
        f3d = Fake([row("w2d", "WAITING TO LAND", "p1")])
        f3d.wait = lambda rc, wt_: wait_free(rc, wt_, gh=planted_gh)
        f3d.rows[0]["worktree"] = str(pw.path)
        f3d.tick(c3d)
        finish(c3d, "w2d", EXIT_PREDECESSOR_QUEUED, "PREDECESSOR QUEUED — the branch carries the stamped DEVLOG entries of "
               "the open pull request(s) #701 beside its own", end_head="p1")
        f3d.live.clear()
        pred_waiting = f3d.tick(c3d)
        pred_said = " | ".join(f3d.said)
        pred_open = wait_free(EXIT_PREDECESSOR_QUEUED, str(pw.path), gh=planted_gh)
        github["fail"] = True
        pred_unread = wait_free(EXIT_PREDECESSOR_QUEUED, str(pw.path), gh=planted_gh)
        github["fail"] = False
        github["open"] = [{"number": 702, "headRefName": "w2d", "headRefOid": predecessor_oid}]   # the branch's own pull request
        pred_own_only = wait_free(EXIT_PREDECESSOR_QUEUED, str(pw.path), gh=planted_gh)
        github["open"] = []                                       # #701 merged into main (or closed): nothing is open
        pred_relaunched = f3d.tick(c3d)
        expect("exit 15 (a merged predecessor's pull request is still open): a retry-when-free class, never the enqueue "
               "unit's; the lane waits at that head while enqueue.py's reader finds the pull request open (naming it), "
               "an unreadable GitHub is not free, the branch's own pull request is none; it relaunches the SAME head "
               "once none is open, with no rearm (kb/Work PB3145)",
               classify({"rc": EXIT_PREDECESSOR_QUEUED}) == FREE and pred_waiting == []
               and "still waits on" in pred_said and pred_open == (False, "open: #701")
               and pred_unread[0] is False and "rate limited" in pred_unread[1] and pred_own_only == (True, "")
               and pred_relaunched == ["w2d"] and agent_work(c3d).reasons == []
               and not (lane_dir(c3d) / REARM).exists(), f"{pred_waiting} {pred_said} {pred_open} {pred_unread} "
                                                           f"{pred_own_only} {pred_relaunched}")

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
        real_now = now_utc
        globals()["now_utc"] = lambda: stopped_at
        try:
            f4b = Fake([row("w3b", "WAITING TO LAND", "m1")])
            first4b = f4b.tick(c4b)
            finish(c4b, "w3b", 9, "NOT QUEUED (exit 9, above)")
            f4b.live.clear()
            again4b = f4b.tick(c4b)
        finally:
            globals()["now_utc"] = real_now
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
        old["at"] = (now_utc() - dt.timedelta(minutes=SURVEY_MAX_AGE_MIN + 5)).isoformat()
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
        fake = [sys.executable, "-c",
                f"print({verdict_line.render('MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on x', 'w9a')!r}); raise SystemExit(5)"]
        r = {"name": "w9a", "state": "WAITING TO LAND", "tip": head, "worktree": str(wt), "why": "", "lead": None}
        out: list[str] = []
        tick(c10, root, survey=lambda repo, cdir: ([r], ["w9a"], None), argv_for=lambda row: fake, say=out.append)
        wait_runners(c10)
        a = (attempts(c10) or [{}])[0]
        expect("a detached runner runs the attempt and records rc 5, the verdict and the end head",
               a.get("rc") == 5 and a.get("verdict") == "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on x" and a.get("end_head") == head,
               f"{a} {out}")

        # 10b. PB3113: a pid the OS reused. A live Python process holds the recorded pid but is not the runner (its command
        # line does not name the attempt's record), so the attempt is not RUNNING; the process whose command line does
        # name the record is the runner. Both are real processes, started here and ended in the `finally`.
        c10b = fresh("reused-pid")
        rec10b = {"id": "000001-x", "branch": "w10b", "_path": str(c10b / "attempts" / "000001-x.json")}
        # Each sleeper says `ready` once Python runs, and the check waits for it: Popen returns when execve closes the
        # child's close-on-exec pipe, before Linux has set the new image's argument area, so /proc/<pid>/cmdline can still
        # read EMPTY (kb/Work PB3973), and an unreadable command line reads as alive: the stranger would look like a runner.
        sleeper = "import time; print('ready', flush=True); time.sleep(600)"
        stranger = subprocess.Popen([sys.executable, "-c", sleeper], stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                    text=True)
        runner = subprocess.Popen([sys.executable, "-c", sleeper, "run", rec10b["_path"]], stdin=subprocess.DEVNULL,
                                  stdout=subprocess.PIPE, text=True)
        try:
            for proc in (stranger, runner):
                proc.stdout.readline()
            expect("a live Python process that is not the runner holds the recorded pid (a reused id): not alive; the "
                   "process whose command line names the record is",
                   not runner_alive({**rec10b, "pid": stranger.pid}) and runner_alive({**rec10b, "pid": runner.pid}),
                   f"stranger {runner_alive({**rec10b, 'pid': stranger.pid})} runner {runner_alive({**rec10b, 'pid': runner.pid})}")
        finally:
            for proc in (stranger, runner):
                proc.kill()
                proc.wait()
                proc.stdout.close()
        real_command = coord.process_command
        coord.process_command = lambda pid: ""          # a Python whose command line cannot be read: unknown, so alive
        try:
            unreadable = runner_alive({**rec10b, "pid": os.getpid()})
        finally:
            coord.process_command = real_command
        expect("a live Python process whose command line cannot be read is not called dead (it could be the runner)",
               unreadable)
        expect("a dead pid, no pid and no record path are not alive",
               not runner_alive({**rec10b, "pid": stranger.pid}) and not runner_alive({**rec10b, "pid": None})
               and not runner_alive({"pid": runner.pid}))

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
        finish(c12, "w10a", 5, "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on v1: a red already on main")
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
        finish(c12, "w10a", 5, "MERGE NOT COMPLETED, BRANCH-CARRIED (before the queue) on v1: main still red")
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
        # 12c. kb/Work PB3628: the stranded branch's shape. PR #467 bounced, a finisher fixed the branch locally (a new
        #      tip, reviewed CLEAN) and the enqueue unit re-armed it, but the survey read it NOT QUEUED (auto-merge was on
        #      at the pull request's OLD head), a state no reader acts on, and nothing launched for hours. The survey now
        #      reads such a branch WAITING TO LAND (prune_worktrees' self-test pins that rule on the real lifecycle), and
        #      the lane lands it; a branch the survey does read NOT QUEUED (its pull request AT the tip) stays the queue
        #      watch's even when re-armed, and the status line says so instead of promising a landing.
        c12c = fresh("rearm-not-queued")
        f12c = Fake([row("w12a", "WAITING TO LAND", "p1"), row("w12b", "WAITING TO LAND", "q1")])
        f12c.tick(c12c)
        finish(c12c, "w12a", 0, "ENQUEUED", end_head="p1")      # PR #467: queued at p1, then bounced (ci-gate failed)
        finish(c12c, "w12b", 0, "ENQUEUED", end_head="q1")
        f12c.live.clear()
        collect(c12c, ["w12a", "w12b"], say=lambda s: None)
        f12c.rows = [row("w12a", "WAITING TO LAND", "p2"), row("w12b", "NOT QUEUED", "q1")]   # w12a: the finisher's p2
        rearm(c12c, "w12a", "finisher 2 fixed the bounce, reviewed CLEAN at p2", say=said.append, tip_for=lambda wt: "p2")
        rearm(c12c, "w12b", "re-armed by hand", say=said.append, tip_for=lambda wt: "q1")
        expect("a re-armed branch the survey reads WAITING TO LAND at the finisher's new tip is launched, and one the "
               "survey reads NOT QUEUED (its pull request at the tip, the queue watch's) is not",
               f12c.tick(c12c) == ["w12a"], f"{f12c.started} {f12c.said}")
        status = status_lines(c12c)    # after the tick: the survey it wrote is the one the status line reads
        expect("the status line of a re-armed NOT QUEUED branch does not promise a landing, and names why",
               any(ln.startswith("REARMED") and "w12b" in ln and "does NOT land it" in ln and "NOT QUEUED" in ln and "only the operator does" in ln
                   for ln in status), "\n".join(status))
        # the one re-arm predicate (rearm_hold, review lead R1-1): every arm that keeps candidates() from launching a
        # re-arm is the arm the status line names, the worktree arm included, and only a launchable one is promised
        rec12 = {"head": "r1"}
        arms = {"launch": row("w12c", "WAITING TO LAND", "r1"), "no worktree": {**row("w12c", "WAITING TO LAND", "r1"),
                                                                              "worktree": ""},
                "queue": row("w12c", "ENQUEUED", "r1"), "moved": row("w12c", "WAITING TO LAND", "r2"), "gone": None,
                "landed": row("w12c", "LANDED", "r1")}
        agree = {k: (bool(candidates([r] if r else [], [], {}, {"w12c": rec12})),
                     rearm_effect(rec12, r) == "the next tick lands it again at this head") for k, r in arms.items()}
        expect("candidates() launches a re-arm exactly when the status line promises it (no worktree: neither)",
               agree == {"launch": (True, True), **{k: (False, False) for k in arms if k != "launch"}}
               and "no worktree" in rearm_effect(rec12, arms["no worktree"]), str(agree))
        # 12d. kb/Work PB3609: the lane's view of a MERGED branch. On 2026-10-24 the lane started `enqueue.py land` on a
        #      branch whose pull request had merged 14 minutes before, 0 commits ahead of main: the enqueue unit had
        #      re-armed it before a reboot, and the survey read the merged branch IN FLIGHT (touched inside --min-age), so
        #      no hold applied. The survey now reads it LANDED: the re-arm is DROPPED (not held as "the queue owns it",
        #      which is wrong for a merged branch), and the status line before the drop says why.
        landed_effect = rearm_effect(rec12, arms["landed"])
        expect("the status line of a re-armed LANDED branch names its work on main, never the queue's ownership",
               "on main" in landed_effect and "drops the re-arm" in landed_effect and "queue" not in landed_effect,
               landed_effect)
        c12d = fresh("rearm-landed")
        f12d = Fake([row("w12d", "WAITING TO LAND", "m1"), row("w12e", "WAITING TO LAND", "n1")])
        f12d.tick(c12d)
        for b, h in (("w12d", "m1"), ("w12e", "n1")):
            finish(c12d, b, 0, "ENQUEUED", end_head=h)
        f12d.live.clear()
        collect(c12d, ["w12d", "w12e"], say=lambda s: None)
        f12d.rows = [row("w12d", "LANDED", "m1"), row("w12e", "WAITING TO LAND", "n1")]   # PR of w12d merged
        rearm(c12d, "w12d", "re-armed before the reboot", say=said.append, tip_for=lambda wt: "m1")
        rearm(c12d, "w12e", "re-armed by hand", say=said.append, tip_for=lambda wt: "n1")
        expect("a re-armed branch whose work is on main is not launched and its re-arm is dropped, said; a re-arm of "
               "a branch still to land is untouched",
               f12d.tick(c12d) == ["w12e"] and not rearm_path(c12d, "w12d").exists()
               and any("w12d" in ln and "its work is on main" in ln and "dropped" in ln for ln in f12d.said),
               f"{f12d.started} {f12d.said}")

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

        def pr(number: int, *checks, in_queue: bool = False, head: str = "z1", others: tuple = ()) -> Any:
            return queue_facts.PullRequest(number, f"https://github.com/o/r/pull/{number}", head, in_queue,
                                           "2026-10-22T18:44:50Z", "BLOCKED", "", tuple(checks), "o/r", others=others)

        c14 = fresh("watch")
        names = ["q", "m", "rerun", "reopen", "alert", "red", "closed", "held", "drop", "qm"]
        f14 = Fake([row(f"w14{n}", "WAITING TO LAND", "z0") for n in names])
        f14.tick(c14)
        for n in names:
            finish(c14, f"w14{n}", 0, "ENQUEUED", end_head="z1")
        f14.live.clear()
        f14.rows = [row(f"w14{n}", "IN FLIGHT", "z1") for n in names]
        f14.rows[1]["content"] = "MERGED"                      # merged by the queue, the survey still calls it fresh
        n_of = {f"w14{n}": 380 + i for i, n in enumerate(names)}

        def facts(**over) -> dict[str, Any]:
            base = {b: pr(n_of[b], stuck_gate) for b in n_of
                    if b not in ("w14q", "w14m", "w14red", "w14closed", "w14drop", "w14qm")}
            base.update(w14q=pr(n_of["w14q"], green_gate, in_queue=True), w14red=pr(n_of["w14red"], red_gate),
                        w14drop=pr(n_of["w14drop"], green_gate, in_queue=True),
                        w14qm=pr(n_of["w14qm"], green_gate, in_queue=True))
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
        expect("queue watch: a pull request in the queue reads QUEUED and the watch does NOT end (kb/Work PB3772: it "
               "can leave the queue unmerged); one whose branch's work is on main ends it MERGED; a required check that "
               "failed ends it LEFT (BOUNCED: the finisher's); none acted on",
               watch("w14q").get("state") == IN_QUEUE and IN_QUEUE not in WATCH_DONE
               and watch("w14q").get("queued_at") == "2026-10-22T18:50:00+00:00"
               and watch("w14m").get("state") == MERGED
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
        expect("queue watch: stuck, then cleared by the re-run: the pull request enters the queue (QUEUED, still "
               "watched); a stuck one only 8 min after its re-run is still inside the grace",
               watch("w14rerun").get("state") == IN_QUEUE and len(watch("w14reopen")["reads"]) == 1,
               f"{watch('w14rerun')} {watch('w14reopen')}")
        rerun_q = pr(n_of["w14rerun"], green_gate, in_queue=True)
        reopen_q = pr(n_of["w14reopen"], green_gate, in_queue=True)
        f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q), t("19:08")
        f14.tick(c14)
        expect("queue watch: read 2 (still stuck one grace after the re-run) closes, reopens and re-queues the pull "
               "request, recorded as read 2",
               ("requeue", "o/r", n_of["w14reopen"]) in f14.effects and acts("w14reopen") == [RERUN, REQUEUE]
               and ("measure", n_of["w14reopen"], "w14reopen", 2, STUCK, REQUEUE) in f14.effects, str(f14.effects))
        f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q), t("19:12")
        f14.tick(c14)
        expect("queue watch: stuck, then cleared by the reopen: in the queue, no third read",
               watch("w14reopen").get("state") == IN_QUEUE and len(watch("w14reopen")["reads"]) == 2,
               str(watch("w14reopen")))
        f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q), t("19:19")
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
        expect("queue watch: status and the survey's NOT QUEUED row say the watch's state, when it read, that an "
               "ended watch reads nothing again, and each read",
               any("w14alert" in ln and "queue watch ALERTED" in ln and "read 1 stuck-check-run: rerun done" in ln
                   and "the watch has ENDED" in ln and "read at 2026-10-22T19:19:00+00:00" in ln
                   for ln in status_lines(c14))
               and "has ENDED" not in watch_text(next(a for a in attempts(c14) if a["branch"] == "w14q"))
               and "read 2 stuck-check-run: requeue done" in watch_text(watch_holder := next(
                   a for a in attempts(c14) if a["branch"] == "w14alert")), str(watch_holder))
        # kb/Work PB3772: PR #593's shape. QUEUED at 19:45 (the last reading in the queue), then out of it with auto-merge
        # OFF, its ci-gate green and no failed run: the queue dropped it (a ruleset toggle, a dequeue). The watch that
        # ended at QUEUED never saw it; this one waits the grace from the last reading in the queue, never re-runs a
        # check whose work passed, requeues, and reads it in the queue again. Its sibling qm is merged by the queue.
        dropped = queue_facts.PullRequest(n_of["w14drop"], "https://github.com/o/r/pull/389", "z1", False, "", "CLEAN",
                                          "", (green_gate,), "o/r")
        f14.rows[9]["content"] = "MERGED"
        f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q, w14drop=dropped, w14qm=None), t("19:50")
        f14.tick(c14)
        inside = (watch("w14drop").get("state"), acts("w14drop"), watch("w14qm").get("state"))
        f14.now = t("19:56")
        f14.tick(c14)
        expect("queue watch: a pull request QUEUED and then out of the queue with auto-merge off and no failed run is "
               "WATCHING inside the grace counted from its last reading in the queue, and past it read 1 is the "
               "REQUEUE (no re-run of a check that passed), cause not-queued, saying it left the queue; one the queue "
               "MERGED ends the watch MERGED (kb/Work PB3772)",
               inside == (WATCHING, [], MERGED) and acts("w14drop") == [REQUEUE]
               and ("requeue", "o/r", n_of["w14drop"]) in f14.effects
               and ("measure", n_of["w14drop"], "w14drop", 1, NOT_QUEUED, REQUEUE) in f14.effects
               and "left the merge queue" in watch("w14drop")["why"], f"{inside} {acts('w14drop')} {watch('w14drop')}")
        f14.prs, f14.now = facts(w14closed=None, w14rerun=rerun_q, w14reopen=reopen_q,
                                 w14drop=pr(n_of["w14drop"], green_gate, in_queue=True), w14qm=None), t("19:57")
        f14.tick(c14)
        expect("queue watch: the requeue put it back in the queue: QUEUED again, and watched on",
               watch("w14drop").get("state") == IN_QUEUE and watch("w14drop").get("queued_at") == "2026-10-22T19:57:00+00:00"
               and len(watch("w14drop")["reads"]) == 1, str(watch("w14drop")))
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
        audits = lambda status, done="", job="": queue_facts.CheckRun(   # noqa: E731
            "audits", status, "SUCCESS" if done else "", 120100000001, 40969292151, done, job_status=job)
        long_audits = watch_step(enq, "IN FLIGHT", pr(777, others=(audits("IN_PROGRESS", job="in_progress"),)), t("19:10"))
        audits_ended = [watch_step(enq, "IN FLIGHT", pr(777, others=(audits("COMPLETED", "2026-10-22T19:05:00Z"),)), t(hm))
                        for hm in ("19:10", "19:16")]
        expect("queue watch: a required check with no check-run yet because a job it needs still runs (PR #777's audits, "
               "25 min after the enqueue) is WATCHING with no remedy, never the requeue that cancels the run (kb/Work "
               "PB4052); once that work ends the grace counts from ITS end, and read 1 re-runs its workflow run",
               long_audits.state == WATCHING and not long_audits.act and "audits still running" in long_audits.why
               and audits_ended[0].state == WATCHING and not audits_ended[0].act
               and audits_ended[1].act == RERUN and audits_ended[1].run == 40969292151, f"{long_audits} {audits_ended}")
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
        update(late, {"rc": 0, "verdict": "ENQUEUED", "ended": now_utc().isoformat(), "end_head": "x1"})
        reap(c15, [stale], lambda a: False, say=lambda s: None)
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
            requeue=lambda repo, number, head: enqueue.requeue(repo, number, head, gh=silent,
                                                               state=lambda: (ruleset.ACTIVE, "x")),
            alert=lambda subject, body, ref: recorded.append(("alert", subject)),
            measure=lambda number, b, read, cause, act, ok: recorded.append(("measure", number, read, act, ok)),
            red_facts=lambda att, pr_: RedFacts(unobserved="not asked: no bounce here"),
            measure_red=lambda number, b, run, cls, why, act, ok, cases, cause: recorded.append(("red", number, cls)),
            open_causes=lambda: {}, cause_note=lambda cases: "")
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

        # 18. THE CLASS OF A MERGE-QUEUE RED (kb/Work PB4271, PB4331; the rule of CLAUDE.md Testing). First the one decision, pure: each
        #     class from planted facts. Then the facts read from a worktree (a planted gate record and a planted bounce's
        #     results, the fetch planted: no GitHub). Then the queue watch: an INFRA or RUNNER red re-queued once with no
        #     agent, a second red (or the same one again) left to the finisher, a MERGE red left at once, a failed
        #     re-queue never silent, and a re-queued branch no enqueue-unit work while its watch runs.
        import landing_measure as lm  # noqa: PLC0415
        import test_population as tp  # noqa: PLC0415
        rec_ = RedFacts(run=9001, conclusion="failure", record="000001-r", listed=frozenset({"A.Pass", "A.Skip"}),
                        passed=frozenset({"A.Pass"}))
        cls_of = {name: classify_red(f)[0] for name, f in {
            "unobserved": RedFacts(unobserved="GitHub unreachable"),
            "timed out": dataclasses.replace(rec_, conclusion="timed_out", failed=frozenset({"A.Pass"})),
            "no test failed": rec_,
            "no record": RedFacts(run=9001, conclusion="failure", failed=frozenset({"A.Pass"}), no_record="none covers"),
            "inert": RedFacts(run=9001, conclusion="failure", failed=frozenset({"B.New"}), record="000001-r", test_free="inert"),
            "tooling": RedFacts(run=9001, conclusion="failure", failed=frozenset({"B.New"}), record="000001-r",
                                test_free="tooling"),
            "skipped": dataclasses.replace(rec_, failed=frozenset({"A.Pass", "A.Skip"})),
            "absent": dataclasses.replace(rec_, failed=frozenset({"A.Pass", "B.New"})),
            "ran green": dataclasses.replace(rec_, failed=frozenset({"A.Pass"}))}.items()}
        expect("classify_red: results unread or no record is UNCLASSIFIED; a timed-out run or one where no test failed is "
               "INFRA; red cases that all ran GREEN in the record, or an inert or a tooling-lane record, are RUNNER; a red case the record "
               "listed but did not run is GENUINE; one the record lacks is MERGE (kb/Work PB4271, PB4331)",
               cls_of == {"unobserved": lm.UNCLASSIFIED, "timed out": lm.INFRA, "no test failed": lm.INFRA,
                          "no record": lm.UNCLASSIFIED, "inert": lm.RUNNER, "tooling": lm.RUNNER, "skipped": lm.GENUINE, "absent": lm.MERGE,
                          "ran green": lm.RUNNER}, str(cls_of))
        import gate  # noqa: PLC0415
        wt18 = root / "wt-w18"
        rec_dir = wt18 / gate.RUN_ROOT / "000001-20261031T000000Z"
        rec_dir.mkdir(parents=True)
        (rec_dir / "verdict.json").write_text(json.dumps({
            "schema": gate.RECORD_SCHEMA, "verdict": "GREEN", "line": "=== planted ===", "exit_code": 0, "head": "h18",
            "tree": "T", "head_tree": "T", "lane": "full"}), encoding="utf-8", newline="\n")
        tp.synthetic_trx(rec_dir / "Unit.trx", [("A.Pass", ["Passed"]), ("A.Skip", ["NotExecuted"])])
        bounce_dir = wt18 / gate.CI_BOUNCE_RESULTS

        def plant_bounce(number: int, cases: list[tuple[str, list[str]]], conclusion: str = "failure") -> None:
            remove_tree(bounce_dir)
            (bounce_dir / "test-results-unit").mkdir(parents=True)
            (bounce_dir / queue_facts.BOUNCE_SOURCE).write_text(
                f"GitHub Actions run 9001 (merge group of pull request #{number}, {conclusion}, x); planted\n",
                encoding="utf-8", newline="\n")
            tp.synthetic_trx(bounce_dir / "test-results-unit" / "u.trx", cases)
        att18 = {"branch": "w18", "worktree": str(wt18), "end_head": "h18"}
        pr18 = queue_facts.PullRequest(893, "u", "h18", False, "", "BLOCKED", "FAILED_CHECKS at 2026-10-31T10:00:00Z",
                                       (green_gate,), "o/r")
        fetched: list[str] = []

        answered = [9001]   # the run the planted fetch says the directory holds now (0: its download failed)

        def fetch(b: str, d: pathlib.Path) -> queue_facts.BounceFetch:
            fetched.append(b)
            return queue_facts.BounceFetch("ci bounce: planted", answered[0])
        plant_bounce(893, [("A.Pass", ["Failed"]), ("C.Other", ["Passed"])])
        ran_green = red_facts(att18, pr18, fetch)
        plant_bounce(1, [("A.Pass", ["Failed"])])
        other_pr = red_facts(att18, pr18, fetch)
        plant_bounce(893, [("A.Skip", ["Failed"])])
        reused_dir = rec_dir.parent / "000002-20261031T010000Z"
        reused_dir.mkdir()
        (reused_dir / "verdict.json").write_text(json.dumps({
            **json.loads((rec_dir / "verdict.json").read_text(encoding="utf-8")), "reused_from": rec_dir.name}),
            encoding="utf-8", newline="\n")
        reused = red_facts(att18, pr18, fetch)
        (rec_dir / "Unit.trx").unlink()
        pruned = red_facts(att18, pr18, fetch)
        gone = red_facts({**att18, "worktree": str(root / "no-such-worktree")}, pr18, fetch)
        # An EARLIER bounce's results of the same pull request (run 9001, a reused pull request keeps its number) still
        # sit in the directory when the NEW red's results cannot be downloaded (the fetch answers run 0), or when the
        # fetch answers another run than the one the directory's marker names (kb/Work PB4271, review w20261808a r1).
        plant_bounce(893, [("A.Pass", ["Failed"])])
        answered[0] = 0
        stale = red_facts(att18, pr18, fetch)
        answered[0] = 9003
        other_run = red_facts(att18, pr18, fetch)
        answered[0] = 9001
        expect("red_facts: results of an OLDER bounce are never the new red's: a fetch that could not download the new run's "
               "results (it answers run 0) or answers a run the directory's marker does not name leaves the red "
               "UNCLASSIFIED, the finisher's, with the fetch's own line, never classified RUNNER and re-queued on another "
               "run's cases (kb/Work PB4271)",
               stale.unobserved == other_run.unobserved == "ci bounce: planted" and not stale.failed and not other_run.failed
               and classify_red(stale)[0] == classify_red(other_run)[0] == lm.UNCLASSIFIED,
               f"{stale} {other_run}")
        expect("red_facts: the bounce's red cases (by name) beside the gate record that covers the queued head, the run a "
               "REUSED record names read for its cases; results of another pull request, a record whose results are gone "
               "and a worktree that is gone are said, never guessed (kb/Work PB4271)",
               ran_green.run == 9001 and ran_green.failed == {"A.Pass"} and ran_green.record == rec_dir.name
               and classify_red(ran_green)[0] == lm.RUNNER
               and other_pr.unobserved == "ci bounce: planted" and classify_red(other_pr)[0] == lm.UNCLASSIFIED
               and reused.record == rec_dir.name and classify_red(reused)[0] == lm.GENUINE
               and not pruned.record and "keeps no test results" in pruned.no_record
               and classify_red(pruned)[0] == lm.UNCLASSIFIED and "gone" in gone.unobserved and fetched == ["w18"] * 6,
               f"{ran_green} {other_pr} {reused} {pruned} {gone} {fetched}")

        # A COMPOSED verdict (kb/Work PB5099): its evidence is the run that retried the red cases AND the earlier run of the
        # same code state whose passes it counted, so a bounce's red case that only the earlier run listed is read there.
        wt18c = root / "wt-w18c"
        first_run = wt18c / gate.RUN_ROOT / "000001-20261031T000000Z"
        retry_run = first_run.parent / "000002-20261031T010000Z"
        first_run.mkdir(parents=True)
        retry_run.mkdir()
        (first_run / "verdict.json").write_text(json.dumps({
            "schema": gate.RECORD_SCHEMA, "verdict": "RED", "line": "=== planted ===", "exit_code": 1, "head": "h18",
            "tree": "T", "head_tree": "T", "lane": "full"}), encoding="utf-8", newline="\n")
        (retry_run / "verdict.json").write_text(json.dumps({
            "schema": gate.RECORD_SCHEMA, "verdict": "GREEN", "line": "=== planted ===", "exit_code": 0, "head": "h18",
            "tree": "T", "head_tree": "T", "tree_end": "T", "lane": "full", "composed_from": [first_run.name]}),
            encoding="utf-8", newline="\n")
        tp.synthetic_trx(first_run / "Unit.trx", [("A.Pass", ["Passed"]), ("A.Retried", ["Failed"])])
        tp.synthetic_trx(retry_run / "Unit.trx", [("A.Retried", ["Passed"])])
        att18c = {"branch": "w18c", "worktree": str(wt18c), "end_head": "h18"}
        bounce_dir = wt18c / gate.CI_BOUNCE_RESULTS
        plant_bounce(893, [("A.Pass", ["Failed"])])
        composed = red_facts(att18c, pr18, fetch)
        expect("red_facts: a composed verdict is read through every run that holds its evidence (gate.measuring_runs): a red "
               "case that ran GREEN only in the earlier run it composed with is RUNNER, not absent from the branch's "
               "population (kb/Work PB5099)",
               composed.record == retry_run.name and composed.listed == {"A.Pass", "A.Retried"}
               and classify_red(composed)[0] == lm.RUNNER, str(composed))

        c18 = fresh("reds")
        names18 = ["infra", "merge", "same", "fails"]
        f18 = Fake([row(f"w18{n}", "WAITING TO LAND", "k0") for n in names18])
        f18.tick(c18)
        for n in names18:
            finish(c18, f"w18{n}", 0, "ENQUEUED", end_head="k1")
        f18.live.clear()
        collect(c18, [f"w18{n}" for n in names18], say=lambda s: None)
        f18.rows = [row(f"w18{n}", "ENQUEUED" if n == "fails" else "BOUNCED", "k1") for n in names18]
        n18 = {f"w18{n}": 890 + i for i, n in enumerate(names18)}

        def bounced(b: str, removed: str) -> Any:
            return queue_facts.PullRequest(n18[b], "u", "k1", False, "", "BLOCKED", removed, (green_gate,), "o/r")
        infra = RedFacts(run=7001, conclusion="failure")                    # no test failed
        merge = dataclasses.replace(rec_, run=7002, failed=frozenset({"B.New"}))
        f18.reds = {"w18infra": infra, "w18merge": merge, "w18same": infra, "w18fails": infra}
        r1 = "FAILED_CHECKS at 2026-10-31T10:00:00Z"
        in_queue = queue_facts.PullRequest(n18["w18fails"], "u", "k1", True, "", "BLOCKED", "", (green_gate,), "o/r")
        f18.prs = {**{b: bounced(b, r1) for b in n18 if b != "w18fails"}, "w18fails": in_queue}
        f18.now = t("19:00")
        f18.tick(c18)
        first = {b: dict(next(a for a in attempts(c18) if a["branch"] == b).get("watch") or {}) for b in n18}
        work_after_requeue = agent_work(c18).reasons
        requeues = [e for e in f18.effects if e[0] == "requeue"]
        f18.prs = {"w18infra": bounced("w18infra", "FAILED_CHECKS at 2026-10-31T10:30:00Z"),   # a second red
                   "w18same": bounced("w18same", r1), "w18fails": in_queue}                    # the same one again
        f18.reds["w18infra"] = dataclasses.replace(infra, run=7003)
        f18.now = t("19:30")
        f18.tick(c18)
        later = {b: next(a for a in attempts(c18) if a["branch"] == b).get("watch") or {} for b in ("w18infra", "w18same")}
        f18.watch_effects = lambda: dataclasses.replace(
            Fake.watch_effects(f18), requeue=lambda repo, number, head: (False, "a run on the head is in progress"))
        f18.prs = {"w18fails": bounced("w18fails", r1)}
        f18.rows[-1] = row("w18fails", "BOUNCED", "k1")
        f18.tick(c18)
        failed_watch = next(a for a in attempts(c18) if a["branch"] == "w18fails").get("watch") or {}
        reds18 = [e for e in f18.effects if e[0] == "red"]
        expect("queue watch: an INFRA red is re-queued ONCE by the lane (no agent: the branch is no enqueue-unit work while "
               "its watch runs), recorded with its class; a MERGE red ends the watch BOUNCED with its class, the "
               "finisher's; a second red of the re-queued landing, or the same bounce read again, is never re-queued; "
               "a re-queue GitHub refuses ends the watch BOUNCED, said (kb/Work PB4271, PB4331)",
               first["w18infra"].get("state") == WATCHING and "INFRA" in first["w18infra"].get("why", "")
               and first["w18merge"].get("state") == LEFT and "MERGE" in first["w18merge"].get("why", "")
               and len(requeues) == 2 and {e[2] for e in requeues} == {n18["w18infra"], n18["w18same"]}
               and work_after_requeue == ["bounced: w18merge"]
               and later["w18infra"].get("state") == LEFT and "second red" in later["w18infra"].get("why", "")
               and later["w18same"].get("state") == LEFT and "did not take" in later["w18same"].get("why", "")
               and failed_watch.get("state") == LEFT and "re-queue FAILED" in failed_watch.get("why", "")
               and len([e for e in f18.effects if e[0] == "requeue"]) == 2
               and ("red", n18["w18infra"], "w18infra", 7001, lm.INFRA, "requeue", True) in reds18
               and ("red", n18["w18infra"], "w18infra", 7003, lm.INFRA, "finisher", True) in reds18
               and ("red", n18["w18merge"], "w18merge", 7002, lm.MERGE, "finisher", True) in reds18
               and ("red", n18["w18fails"], "w18fails", 7001, lm.INFRA, "requeue", False) in reds18,
               f"{first} {later} {failed_watch} {work_after_requeue} {f18.effects}")

        # ONE RE-RUN PER CAUSE (kb/Work PB5483): a RUNNER red whose red case another landing already re-queued, with no
        # fix landed, is HELD (never re-queued), recorded as hold-for-fix and the operator alerted; the same red with no
        # such history is re-queued; an INFRA red (no red case, no key) is re-queued once per landing as before.
        flaky = dataclasses.replace(rec_, run=7101, failed=frozenset({"A.Pass"}))
        pr19 = queue_facts.PullRequest(990, "u", "k1", False, "", "BLOCKED", "FAILED_CHECKS at 2026-11-13T02:00:00Z",
                                       (green_gate,), "o/r")
        pure_hold = bounce_step({}, pr19, lm.RUNNER, "w", flaky.failed, frozenset({"A.Pass"}))
        pure_fresh = bounce_step({}, pr19, lm.RUNNER, "w", flaky.failed, frozenset())
        pure_infra = bounce_step({}, pr19, lm.INFRA, "w", frozenset(), frozenset({"A.Pass"}))
        c19 = fresh("held")
        f19 = Fake([row("w19", "WAITING TO LAND", "k0")])
        f19.tick(c19)
        finish(c19, "w19", 0, "ENQUEUED", end_head="k1")
        f19.live.clear()
        collect(c19, ["w19"], say=lambda s: None)
        f19.rows = [row("w19", "BOUNCED", "k1")]
        f19.reds = {"w19": flaky}
        f19.held = {"A.Pass": {"held_at": None, "notes": [], "requeued": 1}}
        f19.prs = {"w19": queue_facts.PullRequest(990, "u", "k1", False, "", "BLOCKED",
                                                  "FAILED_CHECKS at 2026-11-13T02:00:00Z", (green_gate,), "o/r")}
        f19.now = t("19:00")
        f19.tick(c19)
        w19 = next(a for a in attempts(c19) if a["branch"] == "w19").get("watch") or {}
        rcls = rec_ if lm.RUNNER == classify_red(flaky)[0] else None
        expect("queue watch: a RUNNER red whose red case was already re-queued once (on any landing) with no fix landed "
               "is HELD: never re-queued, recorded hold-for-fix, the watch BOUNCED with HELD on its line and the operator "
               "alerted to land the root fix with --fix-first; with no such history it is re-queued, and an INFRA red "
               "(no red case) is re-queued once per landing as before (kb/Work PB5483)",
               pure_hold.state == LEFT and pure_hold.act == HOLD_BOUNCE and "HELD" in pure_hold.why
               and pure_fresh.act == REQUEUE_BOUNCE and pure_infra.act == REQUEUE_BOUNCE and rcls is not None
               and w19.get("state") == LEFT and "HELD" in w19.get("why", "")
               and not [e for e in f19.effects if e[0] == "requeue"]
               and ("red", 990, "w19", 7101, lm.RUNNER, lm.HOLD_FOR_FIX, True) in f19.effects
               and any(e[0] == "alert" and "--fix-first" in e[2] for e in f19.effects),
               f"{pure_hold} {pure_fresh} {pure_infra} {w19} {f19.effects}")
        hold_alert = next((e for e in f19.effects if e[0] == "alert" and "held" in e[1].lower()), ("", "", ""))
        # THE RELEASES (review R1-1): the PB5478 case. Its fix lands the ordinary way (no fix-first); the note the hold
        # names reads landed on origin/main, so the next read of the open causes releases it, recorded; the operator's
        # release refuses a cause that is not open; a hold older than HOLD_ALARM_H is alerted again, once per window.
        c20 = fresh("releases")
        lm.record_red(c20 / lm.FILE, 1607, "b1607", 43550577247, lm.RUNNER, "flake", "requeue", True,
                      "2026-11-13T01:00:00Z", cases=("A.Pass",))
        lm.record_red(c20 / lm.FILE, 1607, "b1607", 43553292493, lm.RUNNER, "flake", lm.HOLD_FOR_FIX, True,
                      "2026-11-13T02:00:00Z", cases=("A.Pass",), cause="PB5478")
        lm.record_red(c20 / lm.FILE, 1610, "b1610", 43560000001, lm.RUNNER, "other", "requeue", True,
                      "2026-11-13T02:30:00Z", cases=("B.Other",))
        lm.record(c20 / lm.FILE, {"pr": 1611, "branch": "fix-pb5478", "merged": "2026-11-13T03:00:00Z"})   # an ordinary fix
        quiet: list[str] = []
        still = open_causes_now(c20, lambda note: False, quiet.append)
        released = open_causes_now(c20, lambda note: note == "PB5478", quiet.append)
        rels = lm.read_all(c20 / lm.RELEASES)
        typo = release_hold(c20, "A.Pas", "typo", quiet.append)
        by_hand = release_hold(c20, "B.Other", "fixed by #1612", quiet.append)
        after_hand = lm.open_causes(lm.read_all(c20 / lm.FILE), lm.read_all(c20 / lm.RELEASES))
        noon = dt.datetime(2026, 11, 13, 14, 0, tzinfo=dt.timezone.utc)
        aged = {"A.Pass": {"held_at": "2026-11-13T01:00:00Z"}, "C.Young": {"held_at": "2026-11-13T13:00:00Z"},
                "D.Requeued": {"held_at": None}}
        alarms = (hold_alarms(aged, {}, noon, lm.HOLD_ALARM_H), hold_alarms(aged, {"A.Pass": "2026-11-13T13:00:00Z"},
                                                                             noon, lm.HOLD_ALARM_H))
        expect("hold releases (kb/Work PB5483 review R1-1, R1-4): a hold names its note (PB5478) and its alert says so; a "
               "fix landed the ordinary way releases it once that note reads landed on origin/main, the release recorded; "
               "the operator's release-hold releases an open cause and refuses one that is not; a hold older than "
               "HOLD_ALARM_H is alerted again, once per window; exit 17 (a refused jump, the pull request queued at the "
               "back) is a DONE landing, watched, never STOPPED",
               "PB5478" in hold_alert[2] and set(still) == {"A.Pass", "B.Other"} and set(released) == {"B.Other"}
               and [(r["case"], r["by"]) for r in rels] == [("A.Pass", lm.NOTE_LANDED)]
               and typo == 2 and by_hand == 0 and after_hand == {}
               and alarms == (["A.Pass"], []) and classify({"rc": EXIT_NOT_JUMPED}) == DONE,
               f"{hold_alert} {still} {released} {rels} {typo} {by_hand} {after_hand} {alarms}")
        # Review round 2. R2-1: a RUNNER red of more than FLAKE_CASES cases is no flake (never re-run, never held, the
        # finisher's), even when one of its cases is an open cause. R2-2: the hold's note is the ONE open note whose title or
        # Sites line names the test; a mere mention is none, two claimants are none. R2-3: a hold with no note gains one
        # filed later, on the next read, and that note's landing releases it; `name-hold` attaches one by hand.
        wide = frozenset(f"W.Case{i}" for i in range(lm.FLAKE_CASES + 1)) | {"A.Pass"}
        too_many = bounce_step({}, pr19, lm.RUNNER, "w", wide, frozenset({"A.Pass"}))
        small = bounce_step({}, pr19, lm.RUNNER, "w", frozenset({"A.Pass", "W.Case0"}), frozenset({"A.Pass"}))
        nrepo = ScratchRepo.init(root / "wt-notes")

        def nfile(nid: str, title: str, body: str, status: str = "open") -> str:
            return f"---\nid: {nid}\nstatus: {status}\ntitle: \"{nid} - {title}\"\n---\n\n{body}\n"
        nrepo.commit("notes", {
            "kb/Work/PB61.md": nfile("PB61", "the flake of CompilesRedefines", "**Sites:** tests/Unit/Foo.cs"),
            "kb/Work/PB62.md": nfile("PB62", "a doc note", "An example: CompilesRedefines shows the rule."),
            "kb/Work/PB63.md": nfile("PB63", "landed long ago", "**Sites:** Foo.ReadsSequentialFile", "landed")})
        nrepo.git("update-ref", MAIN, "HEAD")
        named_one = cause_note(nrepo.path, ("Unit.Foo.CompilesRedefines",))
        mention_only = cause_note(nrepo.path, ("Unit.Foo.ReadsSequentialFile",))
        nrepo.commit("a second claimant", {"kb/Work/PB64.md": nfile("PB64", "CompilesRedefines again", "x")})
        nrepo.git("update-ref", MAIN, "HEAD")
        ambiguous = cause_note(nrepo.path, ("Unit.Foo.CompilesRedefines",))
        c21 = fresh("late-note")
        lm.record_red(c21 / lm.FILE, 1, "b1", 21, lm.RUNNER, "flake", "requeue", True, "2026-11-13T01:00:00Z", cases=("Q.Case",))
        lm.record_red(c21 / lm.FILE, 2, "b2", 22, lm.RUNNER, "flake", lm.HOLD_FOR_FIX, True, "2026-11-13T02:00:00Z",
                      cases=("Q.Case",))                                           # held with no note
        filed: list[str] = []
        first_read = open_causes_now(c21, lambda n: False, quiet.append, lambda cases: "".join(filed))
        filed.append("PB5500")                                                     # the operator files the note
        second_read = open_causes_now(c21, lambda n: False, quiet.append, lambda cases: "".join(filed))
        landed_read = open_causes_now(c21, lambda n: n == "PB5500", quiet.append, lambda cases: "")
        c22 = fresh("name-hold")
        lm.record_red(c22 / lm.FILE, 1, "b1", 31, lm.RUNNER, "flake", "requeue", True, "2026-11-13T01:00:00Z", cases=("R.Case",))
        named_by_hand = (name_hold(c22, "R.Case", "PB77", quiet.append), name_hold(c22, "R.Cas", "PB77", quiet.append),
                         name_hold(c22, "R.Case", "nonsense", quiet.append))
        by_hand_released = open_causes_now(c22, lambda n: n == "PB77", quiet.append)
        expect("review round 2 (kb/Work PB5483): a RUNNER red of more than FLAKE_CASES cases is never re-run or held "
               "(the finisher's) while a small one meeting an open cause is held; the hold's note is the one open note "
               "whose title or Sites line names the test (a mention, a landed note or two claimants are none); a hold "
               "with no note gains one filed later and is released when it lands; name-hold attaches one by hand",
               too_many.state == LEFT and too_many.act == "" and "no flake" in too_many.why and small.act == HOLD_BOUNCE
               and named_one == "PB61" and mention_only == "" and ambiguous == ""
               and first_read["Q.Case"]["notes"] == [] and second_read["Q.Case"]["notes"] == ["PB5500"]
               and "Q.Case" not in landed_read
               and [(r["case"], r["by"]) for r in lm.read_all(c21 / lm.RELEASES)] == [("Q.Case", lm.NOTE_LANDED)]
               and named_by_hand == (0, 2, 2) and "R.Case" not in by_hand_released,
               f"{too_many} {small} {named_one!r} {mention_only!r} {ambiguous!r} {first_read} {second_read} "
               f"{landed_read} {named_by_hand} {by_hand_released}")

        # 17. THE CONFLICT BRIEF (kb/Work PB3640): main and the branch both edit scripts/x.py since their merge base;
        #     main also adds a file the branch never touches. The brief names the conflicted file, main's commit on it
        #     (and not the other file), the notes on both sides with their status and newest report.
        import git_merge  # noqa: PLC0415
        from reports import Report  # noqa: PLC0415
        cbr = ScratchRepo.init(root / "wt-brief")
        fixture_git = lambda *args, cwd=None: cbr.run(*args, cwd=cwd)   # noqa: E731  (the self-test's git: the fixture)

        def note(nid: str, status: str, title: str) -> str:
            return f"---\nid: {nid}\nstatus: {status}\ntitle: \"{nid} — {title}\"\n---\n\n# {nid}\n"
        cbr.commit("base", {"scripts/x.py": "a = 1\n", "kb/Work/PB9002.md": note("PB9002", "open", "the branch's fix")})
        merge_base = cbr.git("rev-parse", "HEAD")
        cbr.commit("WIP checkpoint: PB9001 main's side of x", {"scripts/x.py": "a = 2\n", "scripts/y.py": "y = 1\n",
                                                               "kb/Work/PB9001.md": note("PB9001", "landed", "main's fix")})
        cbr.git("update-ref", MAIN, "HEAD")
        cbr.git("reset", "-q", "--hard", merge_base)
        cbr.commit("WIP checkpoint: PB9002 the branch's side of x", {"scripts/x.py": "a = 3\n"})
        brief_reports = [Report(pathlib.Path("w9a-PB9001-report.md"), 9, "w9a", "PB9001", "DONE", ["PB9001"],
                                  False, "fix-main", None, "aaaa"),
                         Report(pathlib.Path("w9b-PB9002-report.md"), 9, "w9b", "PB9002", "DONE", ["PB9002"],
                                  False, "wbrief", str(cbr.path), "bbbb")]
        expect("conflict brief: git's dry run names the file both sides edited, not the one only main added",
               git_merge.merge_conflicts(cbr.path, MAIN) == ["scripts/x.py"],
               str(git_merge.merge_conflicts(cbr.path, MAIN)))
        brief_calls: list[tuple[str, ...]] = []

        def counting_git(*args: str, cwd: str | None = None) -> Any:
            brief_calls.append(args)
            return fixture_git(*args, cwd=cwd)
        many = ["scripts/x.py", *(f"tests/many/{n:04}.json" for n in range(2000))]
        conflict_brief("wbrief", str(cbr.path), many, git=counting_git, reports=brief_reports)
        expect("conflict brief: no git call's arguments grow with the conflicted files (763 files overflowed Windows' "
               "command line, kb/Work PB5002)",
               brief_calls and max(len(a) for a in brief_calls) < 20, str(max((len(a) for a in brief_calls), default=0)))
        ren = ScratchRepo.init(root / "wt-brief-rename")
        ren.commit("base", {"a.txt": "one\ntwo\nthree\nfour\n"})
        ren_base = ren.git("rev-parse", "HEAD")
        ren.commit("PB9003 main renames a to b", {"a.txt": None, "b.txt": "one\ntwo\nthree\nfour\n"})
        ren.commit("PB9004 main rewrites b", {"b.txt": "five\n"})
        ren.git("update-ref", MAIN, "HEAD")
        ren.git("reset", "-q", "--hard", ren_base)
        ren.commit("WIP checkpoint: PB9005 the branch edits a", {"a.txt": "one\ntwo\nthree\nFOUR\n"})
        renamed = conflict_brief("wren", str(ren.path), ["a.txt"], git=lambda *a, cwd=None: ren.run(*a, cwd=cwd),
                                 reports=[])
        ren_gained = next((s for s in renamed.split(chr(10) + "## ") if s.startswith("What main gained")), "")
        expect("conflict brief: a conflicted file main renamed away keeps main's rename commit (review w20261932b R1-1: "
               "without a pathspec git pairs the rename and names only the new path)",
               "PB9003 main renames a to b" in ren_gained.split("### a.txt", 1)[-1], renamed)
        text = conflict_brief("wbrief", str(cbr.path), ["scripts/x.py"], git=fixture_git, reports=brief_reports)
        sections = text.split(chr(10) + "## ")
        gained = next((s for s in sections if s.startswith("What main gained")), "")
        expect("conflict brief: main's commit on the conflicted file under its heading, and no other file's",
               "### scripts/x.py" in gained and "PB9001 main's side of x" in gained and "scripts/y.py" not in gained,
               text)
        expect("conflict brief: the branch's report, and the notes on both sides with their status and newest report",
               "report: w9b-PB9002-report.md (DONE" in text
               and "- PB9002 (open): PB9002 — the branch's fix — newest report: w9b-PB9002-report.md (DONE)" in text
               and "- PB9001 (landed): PB9001 — main's fix — newest report: w9a-PB9001-report.md (DONE)" in text,
               text)
        cbr.commit("WIP checkpoint: PB9002 more of the branch's side", {"scripts/z.py": "z = 1\n"})
        bare = conflict_brief("wbrief", str(cbr.path), ["scripts/x.py"], git=fixture_git, reports=[])
        own_section = next((s for s in bare.split(chr(10) + "## ") if s.startswith("The branch's notes")), "")
        expect("conflict brief: a note two branch subjects name is listed once, not once per subject (PB3640 review "
               "R1-2: the repeats ate the BRIEF_NOTES budget main's notes are read from)",
               own_section.count("- PB9002 (") == 1 and "- PB9001 (landed)" in bare, bare)
        # ... recorded by the runner on exit 4, pointed to by the status line, printed by `conflict-brief`
        c17 = fresh("conflict")
        fake4 = [sys.executable, "-c",
                 f"import sys; sys.path.append({str(HERE.parent)!r}); "  # as enqueue.py starts: its streams in UTF-8
                 "from standard_streams import ensure_utf8; ensure_utf8(); "
                 f"print({verdict_line.render(verdict_line.conflict_outcome(['scripts/x.py']), 'wbrief')!r}); "
                 "raise SystemExit(4)"]
        r17 = {"name": "wbrief", "state": "WAITING TO LAND", "tip": cbr.git("rev-parse", "HEAD"),
               "worktree": str(cbr.path), "why": "", "lead": None}
        tick(c17, root, survey=lambda repo, cdir: ([r17], ["wbrief"], None), argv_for=lambda row: fake4, say=out.append)
        wait_runners(c17)
        a17 = (attempts(c17) or [{}])[0]
        brief_file = pathlib.Path(a17.get("brief") or root / "absent")
        expect("conflict brief: the runner of an exit-4 attempt writes it beside the records and names it on the attempt",
               a17.get("rc") == 4 and brief_file.is_file() and "### scripts/x.py" in brief_file.read_text(encoding="utf-8"),
               str(a17))
        expect("conflict brief: the STOPPED status line points to `land_lane.py conflict-brief <branch>`",
               any(ln.startswith("STOPPED") and "conflict-brief wbrief" in ln for ln in status_lines(c17)),
               str(status_lines(c17)))
        shown: list[str] = []
        expect("conflict brief: `conflict-brief` prints the recorded brief while the branch is at the attempt's end head",
               show_conflict_brief(c17, "wbrief", fixture_git, shown.append) == 0
               and shown == [brief_file.read_text(encoding="utf-8").rstrip()], str(shown))
        moved = cbr.git("commit-tree", f"{MAIN}^{{tree}}", "-p", MAIN, "-m", "WIP checkpoint: PB9003 main moves on")
        cbr.git("update-ref", MAIN, moved)
        reshown: list[str] = []
        expect("conflict brief: once origin/main moves, `conflict-brief` computes a fresh brief against the new main, "
               "never the recorded one (PB3640 review R1-4)",
               a17.get("brief_main") and a17.get("brief_main") != moved
               and show_conflict_brief(c17, "wbrief", fixture_git, reshown.append) == 0
               and reshown != shown and f"Bringing origin/main ({moved[:12]})" in "".join(reshown),
               f"{a17.get('brief_main')} {reshown}")
        # ... and for a pull request the queue BOUNCED as DIRTY (no attempt stopped): computed now from the survey's row
        c17b = fresh("dirty")
        f17b = Fake([{**r17, "name": "wdirty", "state": "BOUNCED"}])
        f17b.tick(c17b)
        dirty_out: list[str] = []
        expect("conflict brief: a BOUNCED branch no attempt stopped gets one computed against origin/main",
               f17b.started == [] and show_conflict_brief(c17b, "wdirty", fixture_git, dirty_out.append) == 0
               and "leaves 1 file(s) in conflict: scripts/x.py" in "".join(dirty_out), str(dirty_out))
        cbr.git("update-ref", MAIN, "HEAD")
        clean_out: list[str] = []
        expect("conflict brief: nothing to brief once origin/main merges clean (exit 1, said)",
               show_conflict_brief(c17b, "wdirty", fixture_git, clean_out.append) == 1
               and "nothing to brief" in "".join(clean_out), str(clean_out))
        # ... and a QUEUED pull request whose head conflicts with an entry queued ahead of it (the survey's `foreseen`,
        #     kb/Work PB3723): origin/main merges clean, the entry ahead does not, so the brief is against that entry's
        #     head, saying it is queued ahead and how its head is brought in
        qbr = ScratchRepo.init(root / "wt-queue")
        qbr.commit("base", {"scripts/q.py": "q = 0\n"})
        qbr.git("update-ref", MAIN, "HEAD")
        qbr.commit("the entry queued ahead", {"scripts/q.py": "q = 1\n"})
        ahead_head = qbr.git("rev-parse", "HEAD")
        qbr.git("reset", "-q", "--hard", MAIN)
        qbr.commit("the queued branch", {"scripts/q.py": "q = 2\n"})
        c17c = fresh("foreseen")
        f17c = Fake([{"name": "wq", "state": "BOUNCED", "tip": qbr.git("rev-parse", "HEAD"), "worktree": str(qbr.path),
                      "why": "", "lead": None, "conflicts": True, "foreseen": [{"branch": "wahead", "head": ahead_head}]}])
        f17c.tick(c17c)
        foreseen_out: list[str] = []
        expect("conflict brief: a queued branch whose head conflicts with an entry ahead of it is briefed against THAT "
               "head, not origin/main (which merges clean): the file, the entry's name, and the merge command for its head",
               show_conflict_brief(c17c, "wq", lambda *a, cwd=None: qbr.run(*a, cwd=cwd), foreseen_out.append) == 0
               and "Bringing wahead" in "".join(foreseen_out) and "leaves 1 file(s) in conflict: scripts/q.py" in "".join(foreseen_out)
               and f"`git merge {ahead_head}`" in "".join(foreseen_out) and "--keep" not in "".join(foreseen_out)
               and "QUEUED ahead of this one" in "".join(foreseen_out) and "the entry queued ahead" in "".join(foreseen_out),
               "".join(foreseen_out))

        # 13. the runner's launch (no window, outliving the tick) is scripts/detached.py's, with its own self-test
        #     arms (kb/Work PB3117, PB3536): the tick starts every runner through it.
        expect("the tick starts every runner through the one detached launch (scripts/detached.py)",
               tick.__kwdefaults__["start"] is spawn_detached)
    finally:
        remove_tree(root)

    # 14. THE SHAPE OF THIS FILE'S OWN CODE (kb/Work PB3310, PB3640 review R1-1 and R1-3, PB3714, PB5109): exit 13 is a
    #     wait, no shared file is one, and the conflict brief foresees the merge with git's dry run and reads the outcome through
    #     the verdict line's one home. Read from this file's code outside this self-test, which spells what it forbids.
    source = pathlib.Path(__file__).read_text(encoding="utf-8").replace("\r\n", "\n")
    at = source.index("\ndef self_test(")
    code = source[:at] + source[source.index("\ndef ", at + 1):]
    expect("shape: a second land (exit 13) is a WAIT answered by its own reader, and no wait is a shared file (owner, "
           "kb/Work PB5109)",
           EXIT_LAND_BUSY in WAITS and "if rc == EXIT_LAND_BUSY:" in code and "driver_file" not in code and 16 not in WAITS)
    expect("shape: the conflict brief foresees the merge with git_merge.merge_conflicts and reads the outcome with "
           "verdict_line.outcome_files, never a merge-tree or a CONFLICT parse of its own",
           "git_merge.merge_conflicts(" in code and "verdict_line.outcome_files(" in code
           and '"merge' + '-tree"' not in code and "CONFLICT" + " in " not in code)
    print(f"=== land_lane.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
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
    rh = sub.add_parser("release-hold", help="the operator: release the hold on a cause (a red test case) whose fix is on "
                                             "main (kb/Work PB5483), recorded with the reason")
    rh.add_argument("case")
    rh.add_argument("--reason", required=True, help="what fixed it (the pull request, the note)")
    nh = sub.add_parser("name-hold", help="the operator: attach the kb/Work note of a held cause, so that note's landing "
                                          "releases the hold (kb/Work PB5483)")
    nh.add_argument("case")
    nh.add_argument("note")
    cb = sub.add_parser("conflict-brief", help="the brief a CONFLICT finisher starts from (kb/Work PB3640): the "
                                               "conflicted files, main's commits on them, their notes and reports")
    cb.add_argument("branch")
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
    if a.verb == "release-hold":
        return release_hold(cdir, a.case, a.reason)
    if a.verb == "name-hold":
        return name_hold(cdir, a.case, a.note)
    if a.verb == "conflict-brief":
        return show_conflict_brief(cdir, a.branch)
    if a.verb == "run":
        return run_attempt(a.attempt)
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
