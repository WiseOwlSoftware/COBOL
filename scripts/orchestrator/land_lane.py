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
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402
from enqueue import (EXIT_CONFLICT, EXIT_GATE_BUSY, EXIT_GITHUB, EXIT_LAND_BUSY,
                     EXIT_NOT_JUMPED, EXIT_PREDECESSOR_QUEUED, EXIT_QUEUE_NOT_ACTIVE, EXIT_STORE_UNPUBLISHED,
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
    gate_host.Host.lock_path) is free, taken and released at once (an OS lock, freed when its holder dies, so a
    lock that reads held is a live gate). That lock is also `gate_runs.live_gate`'s reader, the one the landing
    answers exit 12 by (kb/Work PB3662: a gate holds it until its verdict file is written); a gate process id is
    never read (Windows reuses them). A hand run of linux-gate.sh holds no lock and gates nothing a landing reads
    (kb/Work PB4327), so it is not waited on."""
    import gate_host  # noqa: PLC0415  (scripts/: the gate driver owns its lock's path)
    return lock_free(lambda: gate_host.Host(pathlib.Path(worktree)).lock_path(), f"the gate lock of {worktree}")


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
#                 inert or the tooling lane, lane_classification.TEST_FREE_LANES: no compiler test reads the change):
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
    import gate_host  # noqa: PLC0415  (scripts/: the record, the run root, where a bounce's results land)
    import gate_runs  # noqa: PLC0415
    import lane_classification  # noqa: PLC0415  (scripts/: the lanes whose record keeps no test result)
    import queue_facts  # noqa: PLC0415
    import test_population as tp  # noqa: PLC0415  (scripts/: THE trx reader)
    wt = pathlib.Path(str(att.get("worktree") or ""))
    if not att.get("worktree") or not wt.is_dir():
        return RedFacts(unobserved=f"the branch's worktree {att.get('worktree') or '(none recorded)'} is gone")
    dest = wt / gate_host.CI_BOUNCE_RESULTS
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
    rec = gate_runs.read_record(proof.run) or {}
    # THE RUNS THAT HOLD THE EVIDENCE (kb/Work PB5099): the run that measured the head and, for a composed verdict, the
    # earlier runs of the same code state whose passes it counts (`gate_runs.measuring_runs`, the one resolution).
    measuring = gate_runs.measuring_runs(proof.run)
    measured = measuring[0] if measuring else proof.run
    if rec.get("lane") in lane_classification.TEST_FREE_LANES:
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


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
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
