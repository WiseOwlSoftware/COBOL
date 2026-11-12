#!/usr/bin/env python3
"""progress.py — the loop's progress line, and the stalls it names (kb/Work PB3638 defect 3).

WHY. The owner, 2026-10-24 ~12:15 PDT: "It's stupid to have work running with no updates or progress for multiple
hours. Other items simply block and also make no progress." The 10:04 campaign unit of that day wrote two milestones
in two hours, and the operator learned where the tier-1 PB3612 stood only by reading the wave's files. The supervisor
(`orchestrate.ps1`) now runs this script every `-ProgressSeconds` while a unit runs, in the background like its other
ticks, says the line in its log, and posts each NEW stall to the mailbox of the lane that dispatched the group (PB4190), once per stalled item (a message
per tick would wake the attended session every ten minutes for news it already has).

THE LINE, read from the coordination directory only (no git, no GitHub: it runs every few minutes):
  groups        every dispatch-ledger group (coord.ledger_groups: the dispatched work still in flight) by its
                state: RUNNING (no report answers it, reports.answers, and it has left an act), QUEUED (no report
                and no act: the rolling wave has not started it, kb/Work PB5097), DONE and reviewed CLEAN, DONE with
                review FINDINGS, DONE and not yet reviewed, or another status (SPLIT, DISCHARGED, BLOCKED,
                GATE-PENDING)
  landings      the landing records (landing_measure.read_all) enqueued and merged since the unit started (whether a
                pull request is in the merge queue NOW is the hosting service's to say: a record enqueued and never
                merged may be a closed pull request, so the line does not guess)
  oldest        the item that has waited longest, and on what
  reviewed      the groups whose branch review has a verdict, and how many of them drew FINDINGS at round 1 (a CLEAN
                stamp at round 2 or later means round 1 found something): the round-1 FINDINGS rate (kb/Work PB4888 lever 5)
  queue empty   the minutes of the unit's window in which no landing record was enqueued and unmerged (the merge queue
                sat empty; PB4888 lever 5). A record with no merged stamp counts as in the queue for at most
                `QUEUE_OPEN_MAX_MIN` after its enqueue, so a closed, never-merged record (kb/Work PB3725) cannot fill the
                window forever; a pull request queued longer than that is under-counted
A QUEUED GROUP IS NOT A STALLED GROUP (kb/Work PB5097). The ledger stamps `at` once for the whole wave, when it is planned,
and the rolling wave starts a group only when a worker is free (a wave of eight groups at concurrency six starts two of them
an hour later), so a group's `at` is no clock of its own. A group with no report and no act in the coordination directory
(`group_activity`: the first thing an implementer does is make its scratch directory, MANDATORY-PRACTICES P16) has not begun:
it is `queued`, counted apart from `running`, and neither mailed as a STALL nor held to a limit. A queued group is
ORPHANED when its wave has begun (a group of it left an act or a report) and has left none for `ORPHAN_IDLE_MIN` minutes with
no group running, so its runner is gone and will never start it: `answer_orphans` writes the `NOT STARTED` report that answers
its dispatch, so the notes it holds return to the planner (an unanswered dispatch holds its notes, `held_by_dispatch`).
A GATE IS NEVER SILENT (kb/Work PB5097). A gate in its test phase and a gate waiting in the slot queue write a line to their
console log at least every `gate_slot.HEARTBEAT_S`; the gate's output is redirected into the group's scratch directory,
so the line is an act of the group (`group_activity`). The two heartbeats fit twice in `STALL_IDLE_MIN`.
TWO SIGNALS, TWO WORDS (kb/Work PB4944). A STALL means a group stopped MOVING: a group RUNNING, or a DONE report awaiting
its review, with no act in the coordination directory for `STALL_IDLE_MIN` minutes (`group_activity`: the group's scratch
directory, its scratch files and its reports, and for a DONE report the worktree's last act). A gate waiting for a slot or
running its tests writes a heartbeat line to its log (kb/Work PB5097), so neither is a stall. OVER-SIZE means a group still MOVING that
is past its limit: `RUNNING_LIMIT_MIN` (the owner's unit hand-off, 60 minutes) or its campaign's own `running_limit_min:`
(work.campaign_running_limit; the owner's decision of 2026-11-07), or a DONE report awaiting its review past
`REVIEW_LIMIT_MIN` (half the owner's 30-minute dispatch-to-queue bar). Over-size is a finding about the SIZE of the work:
it is counted in the PROGRESS line, per wave, and never mailed. Only a stall is mailed.
A DONE REPORT IS NOT YET A RETURN (kb/Work PB4918). The rolling wave starts a branch's review only when its implementer
RETURNS, and an implementer writes its report, then gates the tip it reports and fills the gate line in at the verdict, so
the report's mtime can be a whole gate (25 to 60 minutes) before the return. `implementer_activity` reads the report's own
worktree: a gate holding its lock there means the implementer is still working (the group counts as running, with the
idle clock of its activity, from its dispatch), and otherwise the review clock starts at the newest of the report's mtime and the
worktree's last act (its HEAD log, its STATUS.md), never at the report alone.

Usage:
    python scripts/orchestrator/progress.py [--coord <dir>] [--since ISO] [--json]
    python scripts/orchestrator/progress.py --self-test
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import pathlib
import re
import sys
import time

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "spec"))   # work.py, THE register reader
import coord  # noqa: E402
import work  # noqa: E402  (a campaign's running limit: work.campaign_running_limit)
import group_sizes  # noqa: E402  (THE history of the groups' run times, kb/Work PB4944)
import landing_measure  # noqa: E402
import reports  # noqa: E402  (THE fleet-report reader: a report, a ledger group's slug, "answered")
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

RUNNING_LIMIT_MIN = 60   # the owner, 2026-10-23: "no wave may take hours; one chain link per wave, hand off at ~60 min"
REVIEW_LIMIT_MIN = 15    # half the owner's 30-minute dispatch-to-queue bar (kb/Work PB3638, landing_measure.BUDGETS)
STALL_IDLE_MIN = 20      # no act this long is a stall (kb/Work PB4944): at least two gate heartbeats (gate_slot.HEARTBEAT_S, PB5097)
ORPHAN_IDLE_MIN = 120    # a begun wave with no running group and no act this long has lost its runner (above the reviewer's 90-minute ceiling)
QUEUE_OPEN_MAX_MIN = 60  # an unmerged landing record counts as in the merge queue this long (queue_empty_minutes)
ACTIVITY_SCAN_MAX = 5000  # files of one group's scratch directory read for the newest mtime
REVIEW_ROUND = re.compile(r"-r(?P<n>[0-9]+)$")   # a reviewer id's round: `w7c-review-r2` is round 2


def stall_lane(g: dict) -> str:
    """The inbox a stalled group's message goes to (kb/Work PB4190): the lane that dispatched it
    (dispatch_ledger.record_dispatch's `lane`), the one that can act on it; a group recorded with no lane goes to the
    operator, the session that runs this loop."""
    return g.get("lane") or "operator"


def implementer_activity(worktree: str | None) -> tuple[bool, float]:
    """(a gate holds the lock of the report's worktree, the mtime of the worktree's last act): what says a DONE report's
    implementer has NOT returned yet (kb/Work PB4918). The lock is the gate's own (`gate.live_gate`, the one reader of "a
    gate is running here"; the worktree's git directory is read with no git process, as the branch survey does); the last
    act is the newest of its HEAD log and its STATUS.md (the two files an implementer's checkpoint rewrites, as
    `prune_worktrees.survey` reads them). A report naming no worktree, or one that is gone, says (False, 0.0): the clock then
    runs from the report alone."""
    if not worktree or not pathlib.Path(worktree).is_dir():
        return False, 0.0
    import gate  # noqa: PLC0415  (scripts/, on the path through coord; heavy, so only for a DONE report awaiting review)
    import prune_worktrees  # noqa: PLC0415
    gdir = prune_worktrees.worktree_git_dir(worktree)
    stamps = []
    for p in (gdir / "logs" / "HEAD", pathlib.Path(worktree) / "STATUS.md"):
        try:
            stamps.append(p.stat().st_mtime)
        except OSError:
            pass
    return gate.live_gate(gdir / gate.LOCK_NAME) is not None, max(stamps, default=0.0)


def group_activity(cdir: pathlib.Path, g: dict) -> float:
    """The mtime of a group's newest act in the coordination directory (kb/Work PB4944), or 0.0 when it left none. Three
    places, each at the top of `scratch/` and one directory below it (a campaign wave keeps its groups' scratch under its
    own, `scratch/<wave scratch>/<slug>/`, which is where every Mythos 1985-campaign implementer writes): the group's
    scratch directory and files named for its slug `w<wave><letter>` (the gate log it redirects, its probes and batches,
    MANDATORY-PRACTICES P16); its branch review's findings file and scratch, which the rolling wave names
    `review-w<wave>-<letter>` (hyphenated, so the slug never matches them); and its reports (`scratch/reports/<slug>-*`).
    The loop reads no git, so a commit shows only as the STATUS.md and gate log the checkpoint rewrites, which land in
    these places; a gate waiting for a slot or running its tests writes a heartbeat line to its log (gate_slot.HEARTBEAT_S,
    kb/Work PB5097), so neither is silence. 0.0 also says the group has not STARTED: an implementer's first act is its scratch
    directory (P16), and a group the rolling wave has not begun has no file anywhere."""
    slug = reports.ledger_slug(g)
    review = f"review-w{g['wave']}-{str(g['letter']).lower()}"
    scratch = cdir / "scratch"
    newest, seen = 0.0, 0
    for prefix in ("", "*/"):
        for name in (f"{prefix}*{slug}*", f"{prefix}{review}*"):
            for top in scratch.glob(name):
                walk = [(str(top.parent), [], [top.name])] if top.is_file() else os.walk(top)
                for root, _dirs, files in walk:
                    if top.is_dir():
                        try:   # a directory is itself an act: the implementer's first command makes its scratch (R1-1)
                            newest = max(newest, pathlib.Path(root).stat().st_mtime)
                        except OSError:
                            pass
                    for f in files:
                        seen += 1
                        try:
                            newest = max(newest, (pathlib.Path(root) / f).stat().st_mtime)
                        except OSError:
                            pass
                    if seen >= ACTIVITY_SCAN_MAX:
                        return newest
    for p in scratch.glob(f"reports/{slug}-*"):
        try:
            newest = max(newest, p.stat().st_mtime)
        except OSError:
            pass
    return newest


def review_round(r: "reports.Review") -> int:
    """The round a branch review stamp belongs to (`...-r2`): 1 when its reviewer id names none."""
    m = REVIEW_ROUND.search(r.reviewer)
    return int(m.group("n")) if m else 1


def queue_empty_minutes(recs: list[dict], since: float, now: float) -> float:
    """The minutes of [since, now] in which no landing record was in the merge queue (kb/Work PB4888 lever 5): the union of
    the records' [enqueued, merged] intervals, cut to the window, taken from the window's length. A record with no merged
    stamp is a pull request still queued, or one closed and never merged (the register holds such records from weeks ago):
    it counts as queued for at most `QUEUE_OPEN_MAX_MIN` after its enqueue, the longest a merge-group run takes, so an
    abandoned record never fills the window and the figure can still be nonzero."""
    spans = []
    for x in recs:
        if not x.get("enqueued"):
            continue
        a = landing_measure.iso_time(x["enqueued"]).timestamp()
        b = (landing_measure.iso_time(x["merged"]).timestamp() if x.get("merged")
             else min(now, a + QUEUE_OPEN_MAX_MIN * 60))
        a, b = max(a, since), min(b, now)
        if b > a:
            spans.append((a, b))
    busy, end = 0.0, since
    for a, b in sorted(spans):
        a = max(a, end)
        if b > a:
            busy += b - a
            end = b
    return max(0.0, now - since - busy) / 60


def progress(cdir: pathlib.Path, since: float, now: float | None = None,
             work_dir: pathlib.Path | None = None) -> dict:
    """The progress figures, the stalls and the over-size groups, as a dict (render prints the line). `work_dir` is the
    register a campaign's `running_limit_min:` is read from (default kb/Work)."""
    now = time.time() if now is None else now
    reps = reports.load_reports(coord.reports_dir(str(cdir)))
    groups: dict[str, int] = {"running": 0, "queued": 0, "clean": 0, "findings": 0, "unreviewed": 0, "other": 0}
    queued: list[dict] = []    # {slug, wave, notes, minutes, lane}: dispatched, not begun (PB5097)
    wave_act: dict[str, float] = {}      # a wave's newest act or report: the runner's last sign of life
    wave_running: set[str] = set()       # the waves with a group running or gating
    waiting: list[dict] = []   # {slug, wave, notes, minutes, idle, on, limit, lane}
    reviewed = {"total": 0, "round1_findings": 0}
    register: list[dict] | None = None   # the notes, read once and only when a running group asks for its campaigns

    def wait(g: dict, since_act: float, on: str, acts: float) -> dict:
        """One waiting item: `minutes` since `since_act`, `idle` since the newest act (its own, or the dispatch). A
        RUNNING item is held to its campaign's limit and names its campaigns (work.note_campaigns, what group_sizes
        records); a review wait is held to REVIEW_LIMIT_MIN."""
        nonlocal register
        notes = list(g.get("notes", []))
        item = {"slug": reports.ledger_slug(g), "wave": str(g["wave"]), "notes": notes,
                "minutes": (now - since_act) / 60, "idle": (now - max(acts, since_act)) / 60, "on": on,
                "limit": REVIEW_LIMIT_MIN, "lane": stall_lane(g)}
        if on == "running":
            if register is None:
                register = work.read_notes(work_dir or work.WORK)[0]
            item["campaigns"] = work.note_campaigns(register, notes)
            item["limit"] = work.campaign_running_limit(register, notes) or RUNNING_LIMIT_MIN
        return item

    for g in coord.ledger_groups(cdir):
        slug = reports.ledger_slug(g)
        mine = [r for r in reps if reports.answers(r, g)]
        acts = group_activity(cdir, g)
        wave = str(g["wave"])
        if acts or mine:
            wave_act[wave] = max(wave_act.get(wave, 0.0), acts, *(r.path.stat().st_mtime for r in mine if r.path.exists()))
        if not mine and not acts:
            groups["queued"] += 1
            queued.append({"slug": slug, "wave": wave, "notes": list(g.get("notes", [])), "lane": stall_lane(g),
                           "minutes": (now - g["at"]) / 60 if isinstance(g.get("at"), (int, float)) else 0.0})
            continue
        if not mine:
            groups["running"] += 1
            wave_running.add(wave)
            if isinstance(g.get("at"), (int, float)):
                waiting.append(wait(g, g["at"], "running", acts))
            continue
        r = mine[-1]   # load_reports' order: the newest last
        if r.status != "DONE":
            groups["other"] += 1
        elif r.review:
            groups["clean" if r.review.verdict == "CLEAN" else "findings"] += 1
            reviewed["total"] += 1
            # a FINDINGS stamp, or a CLEAN stamp past round 1, is a branch whose round 1 drew findings
            reviewed["round1_findings"] += r.review.verdict != "CLEAN" or review_round(r.review) > 1
        else:
            gating, last_act = implementer_activity(r.worktree)
            if gating and isinstance(g.get("at"), (int, float)):
                # its report is written but its implementer is still gating the tip: no review can have started
                groups["running"] += 1
                wave_running.add(wave)
                waiting.append(wait(g, g["at"], "running", max(acts, last_act)))
                continue
            groups["unreviewed"] += 1
            try:
                returned = max(r.path.stat().st_mtime, last_act)
                waiting.append(wait(g, returned, "its branch review", max(acts, returned)))
            except OSError:
                pass
    recs = landing_measure.read_all(cdir / landing_measure.FILE)

    def after(field: str) -> list[dict]:
        return [x for x in recs if x.get(field) and landing_measure.iso_time(x[field]).timestamp() >= since]

    landings = {"enqueued": len(after("enqueued")), "merged": len(after("merged"))}
    waiting.sort(key=lambda w: -w["minutes"])
    stalls = [w for w in waiting if w["idle"] > STALL_IDLE_MIN]
    oversize = [w for w in waiting if w["idle"] <= STALL_IDLE_MIN and w["minutes"] > w["limit"]]
    by_wave: dict[str, int] = {}
    for w in oversize:
        by_wave[w["wave"]] = by_wave.get(w["wave"], 0) + 1
    # the running groups as group_sizes.record keeps them (kb/Work PB4944): the planner's memory of a campaign's pace
    running = [{**w, "over": any(w is o for o in oversize)} for w in waiting if w["on"] == "running"]
    # a queued group of a begun wave that has stopped moving: its runner is gone (PB5097; `answer_orphans`)
    orphans = [q for q in queued if q["wave"] in wave_act and q["wave"] not in wave_running
               and (now - wave_act[q["wave"]]) / 60 > ORPHAN_IDLE_MIN]
    return {"groups": groups, "queued": queued, "orphans": orphans, "landings": landings,
            "oldest": waiting[0] if waiting else None, "stalls": stalls, "oversize": oversize, "oversize_by_wave": by_wave, "running": running, "reviewed": reviewed,
            "queue_empty_min": queue_empty_minutes(recs, since, now), "unit_minutes": (now - since) / 60}


def render(unit: str, p: dict) -> str:
    g, la, o, rv = p["groups"], p["landings"], p["oldest"], p["reviewed"]
    line = (f"PROGRESS {unit} at {p['unit_minutes']:.0f} min: groups running {g['running']}, queued {g['queued']}, DONE reviewed CLEAN "
            f"{g['clean']}, FINDINGS {g['findings']}, unreviewed {g['unreviewed']}, other {g['other']}; landings since "
            f"the unit started: enqueued {la['enqueued']}, merged {la['merged']}; round-1 FINDINGS "
            f"{rv['round1_findings']} of {rv['total']} reviewed; merge queue empty {p['queue_empty_min']:.0f} of "
            f"{p['unit_minutes']:.0f} min; over-size {len(p['oversize'])}")
    if p["oversize_by_wave"]:
        line += " (" + ", ".join(f"wave {w}: {n}" for w, n in sorted(p["oversize_by_wave"].items())) + ")"
    if o:
        line += f"; oldest waiting: {o['slug']} ({', '.join(o['notes'])}) {o['minutes']:.0f} min on {o['on']}"
    return line


def answer_orphans(cdir: pathlib.Path, p: dict, now: float | None = None) -> list[str]:
    """Answer each ORPHANED queued group's dispatch with a `NOT STARTED` report (kb/Work PB5097) and return the slugs
    answered. The report is the one `reports.answers` reads (`<reports>/<slug>-<lead note>-report.md`, written now, so at
    or after the group's `at`), and `plan_wave` plans a NOT STARTED report's notes again as finishers; without it the
    unanswered dispatch held its notes from the planner for 22 hours while its runner was dead (wave 20261973 groups G
    and H). A report that already exists is never overwritten (the runner may have started the group in the meantime)."""
    now = time.time() if now is None else now
    out = []
    for q in p.get("orphans", []):
        if not q["notes"]:
            continue
        path = coord.reports_dir(str(cdir)) / f"{q['slug']}-{q['notes'][0]}-report.md"
        if path.exists():
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        when = dt.datetime.fromtimestamp(now).astimezone().isoformat(timespec="minutes")
        nl = chr(10)
        path.write_text(
            f"# {q['slug']} - {', '.join(q['notes'])} - never started{nl}{nl}"
            f"**Status:** NOT STARTED · **worktree:** none · **branch:** none · **HEAD:** none{nl}{nl}"
            f"Written by progress.py at {when} (kb/Work PB5097): the dispatch was queued in wave {q['wave']}, the wave began and "
            f"then showed no act for more than {ORPHAN_IDLE_MIN} minutes with no group running, so its runner is gone and "
            f"will never start this group. No scratch directory, worktree, branch or report existed for it. Its notes are "
            f"unchanged and open: plan them fresh from current main.{nl}", encoding="utf-8", newline=nl)
        out.append(q["slug"])
    return out


def stall_line(w: dict) -> str:
    """One stall's line, its subject (the text before the first colon) stable for one stalled item (the mailbox's
    --unless-pending key)."""
    return (f"STALL {w['slug']} ({', '.join(w['notes'])}): no progress for {w['idle']:.0f} min on {w['on']} "
            f"({w['minutes']:.0f} min since it began)")


def stall_lines(p: dict) -> list[str]:
    """One line per stall."""
    return [stall_line(w) for w in p["stalls"]]


STALLS_POSTED = "progress-stalls.json"   # {subject: when it was last posted}, in the coordination directory
REPOST_MIN = 60                          # a stall still open an hour after its message is posted again


def post_stalls(cdir: pathlib.Path, p: dict, unit_line: str, ref: str, now: float | None = None) -> list[str]:
    """Post each stall to the inbox of the lane that dispatched its group (`stall_lane`; mailbox.send, kind `info`,
    from the loop; kb/Work PB4190: the operator can neither act on the Mythos lane's agents nor speed them, and six
    messages about one Mythos wave woke it to close each) and return the subjects posted. One message per stalled
    ITEM: the subject is the stall line's stable head (`STALL <slug> (<notes>)`), sent
    with --unless-pending while one is open, and again only `REPOST_MIN` after the last post (the attended session is
    woken by every message, kb/Work PB2482, so a message per tick would cost it a turn every ten minutes for news it
    has). `ref` is an existing path the message points at (the unit's log)."""
    import mailbox  # noqa: PLC0415  (THE writer of a message)
    now = time.time() if now is None else now
    posted = coord.read_json(cdir / STALLS_POSTED, {})
    sent = []
    for w in p["stalls"]:
        line = stall_line(w)
        subject = line.split(":", 1)[0]
        if now - float(posted.get(subject, 0)) < REPOST_MIN * 60:
            continue
        if mailbox.send(cdir, to=w["lane"], kind="info", subject=subject, body=f"{line}. {unit_line}"[:mailbox.BODY_MAX],
                        refs=[ref], sender="loop", session="orchestrate.ps1 progress", unless_pending=True):
            sent.append(subject)
        posted[subject] = now
    stalled = {line.split(":", 1)[0] for line in stall_lines(p)}
    coord.write_json(cdir / STALLS_POSTED, {s: t for s, t in posted.items() if s in stalled})
    return sent


def self_test() -> int:
    from scratch_repo import scratch_dir  # noqa: PLC0415  (scripts/, on the path through coord)
    fails: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            fails.append(what)

    cdir = scratch_dir("progress-selftest-")
    rd = cdir / "scratch" / "reports"
    rd.mkdir(parents=True)
    now = 1_900_000_000.0
    coord.write_json(cdir / coord.DISPATCH_LEDGER, {"groups": [
        {"wave": "7", "letter": "A", "notes": ["PB701"], "files": [], "at": now - 90 * 60,
         "lane": "operator"},                                                                  # running 90 min: a stall
        {"wave": "7", "letter": "B", "notes": ["PB702"], "files": [], "at": now - 20 * 60},    # running 20 min
        {"wave": "7", "letter": "C", "notes": ["PB703"], "files": [], "at": now - 80 * 60},    # DONE, CLEAN
        {"wave": "7", "letter": "D", "notes": ["PB704"], "files": [], "at": now - 80 * 60},    # DONE, unreviewed 40 min
        {"wave": "7", "letter": "E", "notes": ["PB705"], "files": [], "at": now - 80 * 60},    # SPLIT
        {"wave": "7", "letter": "F", "notes": ["PB706"], "files": [], "at": now - 80 * 60},    # DONE, FINDINGS
        {"wave": "7", "letter": "G", "notes": ["PB707"], "files": [], "at": now - 70 * 60,
         "lane": "mythos"},                                                                    # the Mythos lane's, running 70 min
        {"wave": "7", "letter": "H", "notes": ["PB708"], "files": [], "at": now - 45 * 60},    # DONE report, its gate still running
        {"wave": "7", "letter": "I", "notes": ["PB709"], "files": [], "at": now - 80 * 60},    # DONE report, worktree active 5 min ago
        {"wave": "7", "letter": "J", "notes": ["PB712"], "files": [], "at": now - 120 * 60},   # running 120 min, wrote a log 5 min ago
        {"wave": "7", "letter": "K", "notes": ["PB711"], "files": [], "at": now - 100 * 60},   # running 100 min, a 150-minute campaign
        {"wave": "7", "letter": "L", "notes": ["PB713"], "files": [], "at": now - 25 * 60},    # running 25 min, 25 min silent
        {"wave": "7", "letter": "N", "notes": ["PB714"], "files": [], "at": now - 50 * 60},    # a gate in its test phase: a fresh heartbeat
        {"wave": "7", "letter": "O", "notes": ["PB715"], "files": [], "at": now - 45 * 60},    # a gate in the slot queue: a fresh heartbeat
        {"wave": "7", "letter": "Q", "notes": ["PB716"], "files": [], "at": now - 200 * 60},   # queued 200 min: the wave has not started it
        {"wave": "9", "letter": "A", "notes": ["PB901"], "files": [], "at": now - 300 * 60},   # SPLIT 150 min ago, the wave's last act
        {"wave": "9", "letter": "B", "notes": ["PB902"], "files": [], "at": now - 300 * 60},   # queued: its wave's runner is gone (orphaned)
        {"wave": "10", "letter": "A", "notes": ["PB1001"], "files": [], "at": now - 1362 * 60}]})   # a planned wave never launched
    register = cdir / "register"
    register.mkdir()
    nl = chr(10)
    for name, body in (("PB710", "cluster: [PB710]" + nl + "running_limit_min: 150"), ("PB711", "cluster: [PB710]")):
        (register / f"{name}.md").write_text(f"---{nl}id: {name}{nl}title: t{nl}{body}{nl}---{nl}", encoding="utf-8", newline=nl)
    import gate_slot  # noqa: PLC0415  (scripts/, on the path through coord)
    beat = gate_slot.HEARTBEAT_S   # a gate's heartbeat is a line in the log it redirects into its group's scratch (PB5097)
    check(2 * beat <= STALL_IDLE_MIN * 60,
          f"two gate heartbeats ({beat:.0f} s each) fit in the stall window ({STALL_IDLE_MIN} min): a live gate is never idle that long")
    for slug, wave_scratch, last_line, written in (
            ("w7j", "", "gate-slot: waiting, 2 ahead", 5 * 60), ("w7k", "camp", "gate-slot: waiting, 2 ahead", 5 * 60),
            ("w7a", "", "gate-slot: waiting, 2 ahead", 90 * 60),   # a gate that wrote nothing for 90 minutes: a real stall
            ("w7g", "", "gate-slot: waiting, 2 ahead", 70 * 60), ("w7l", "", "compiling", 25 * 60),
            ("w7n", "", "build-local: tests running, 0 of 3 test host(s) finished, 1500 s since the hosts started", beat),
            ("w7o", "", "gate-slot: still waiting, 1 ahead (ticket 5112, cap 3, implementer; 1500 s in the queue)", beat)):
        # a campaign wave's groups write under the wave's own scratch
        log = cdir / "scratch" / wave_scratch / slug / "gate1.log"   # the gate log an implementer redirects
        log.parent.mkdir(parents=True)
        log.write_text(last_line + nl, encoding="utf-8", newline=nl)
        os.utime(log, (now - written, now - written))

    def worktree(name: str) -> pathlib.Path:
        wt = cdir / "worktrees" / name   # the path shape a report header's worktree is read from (reports.WORKTREE)
        (wt / ".git" / "logs").mkdir(parents=True)
        return wt

    def report(name: str, status: str, review: str, age_min: float, wt: pathlib.Path | None = None) -> None:
        p = rd / name
        where = f" · **worktree:** `{wt}`" if wt else ""
        p.write_text(f"{status}\n**Status:** {status} · **branch:** `b{name[:3]}`{where} · **HEAD:** `abcdef12`\n{review}",
                     encoding="utf-8", newline="\n")
        os.utime(p, (now - age_min * 60, now - age_min * 60))

    report("w7c-PB703-report.md", "DONE", "**Review:** CLEAN w7c-review-r1 abcdef12\n", 50)
    report("w7d-PB704-report.md", "DONE", "", 40)
    report("w7e-PB705-report.md", "SPLIT", "", 30)
    report("w7f-PB706-report.md", "DONE", "**Review:** FINDINGS 2 w7f-review-r1 abcdef12 E:/s/review-w7-f.jsonl\n", 30)
    report("w9a-PB901-report.md", "SPLIT", "", 150)
    wt_gating, wt_active = worktree("wt-h"), worktree("wt-i")
    report("w7h-PB708-report.md", "DONE", "", 40, wt_gating)
    report("w7i-PB709-report.md", "DONE", "", 40, wt_active)
    head_log = wt_gating / ".git" / "logs" / "HEAD"   # the gating implementer's last checkpoint commit, 3 minutes ago
    head_log.write_text("x" + nl, encoding="utf-8", newline=nl)
    os.utime(head_log, (now - 3 * 60, now - 3 * 60))
    status_md = wt_active / "STATUS.md"
    status_md.write_text("STATUS-AT: x\n", encoding="utf-8", newline="\n")
    os.utime(status_md, (now - 5 * 60, now - 5 * 60))   # the implementer's last act, 5 minutes ago
    since = now - 100 * 60
    iso = dt.datetime.fromtimestamp
    (cdir / landing_measure.FILE).write_text("".join(json.dumps(x) + chr(10) for x in (
        {"pr": 1, "branch": "b1", "enqueued": iso(now - 50 * 60).astimezone().isoformat(),
         "merged": iso(now - 30 * 60).astimezone().isoformat()},
        {"pr": 2, "branch": "b2", "enqueued": iso(now - 10 * 60).astimezone().isoformat()},
        {"pr": 3, "branch": "b3", "enqueued": iso(now - 300 * 60).astimezone().isoformat(),
         "merged": iso(now - 200 * 60).astimezone().isoformat()},
        {"pr": 4, "branch": "b4", "enqueued": iso(now - 200 * 60).astimezone().isoformat()})),   # closed, never merged
        encoding="utf-8", newline="\n")
    import gate  # noqa: PLC0415
    import gate_slot  # noqa: PLC0415
    held = gate_slot.ExclusiveLock.try_take(wt_gating / ".git" / gate.LOCK_NAME, "gate (self-test)")
    try:
        p = progress(cdir, since, now, register)
    finally:
        if held:
            held.release()
    check(held is not None, "the self-test took the gate lock of the gating worktree")
    check(p["groups"] == {"running": 8, "queued": 4, "clean": 1, "findings": 1, "unreviewed": 2, "other": 2},
          f"each ledger group by its state, a group with no report and no act QUEUED, never running (PB5097): {p['groups']}")
    check(sorted(q["slug"] for q in p["queued"]) == ["w10a", "w7b", "w7q", "w9b"]
          and not any(w["slug"] in ("w7b", "w7q", "w9b", "w10a") for w in p["stalls"] + p["oversize"] + p["running"])
          and p["oldest"]["slug"] == "w7j",
          f"a group the rolling wave has not started (no report, no act) is queued: never a stall, never over-size, never "
          f"in the running list, however long ago its wave was planned (PB5097): {p['queued']} {p['stalls']}")
    check(not any(w["slug"] in ("w7n", "w7o") for w in p["stalls"]) and {w["slug"] for w in p["running"]} >= {"w7n", "w7o"},
          f"a gate in its test phase and a gate in the slot queue, each past {STALL_IDLE_MIN} minutes since its group was "
          f"dispatched and with a heartbeat {beat:.0f} s old, are running and not stalled (PB5097): {p['stalls']}")
    check([q["slug"] for q in p["orphans"]] == ["w9b"],
          f"a queued group of a wave that began and has shown no act for {ORPHAN_IDLE_MIN} minutes with nothing running is "
          f"orphaned; one of a wave with a running group (w7b, w7q) or of a wave that never began (w10a) is not: {p['orphans']}")
    check(not any(w["slug"] in ("w7h", "w7i") for w in p["stalls"]),
          f"a DONE report whose implementer still gates is running, not a review stall (PB4918), and one whose worktree "
          f"was active 5 minutes ago has waited 5 minutes for review, not 40: {p['stalls']}")
    check(sorted(round(w["minutes"]) for w in p["stalls"]) == [25, 40, 70, 90], f"the stalls' minutes: {p['stalls']}")
    check([(w["slug"], w["limit"]) for w in p["oversize"]] == [("w7j", RUNNING_LIMIT_MIN)] and p["oversize_by_wave"] == {"7": 1},
          f"a group still writing past its limit is OVER-SIZE, never a stall, and a campaign's own limit (150) lifts "
          f"group K above its 100 minutes (PB4944): {p['oversize']}")
    run = {w["slug"]: w for w in p["running"]}
    check(sorted(run) == ["w7a", "w7g", "w7h", "w7j", "w7k", "w7l", "w7n", "w7o"] and run["w7j"]["over"] and not run["w7k"]["over"]
          and not run["w7a"]["over"] and run["w7k"]["campaigns"] == ["PB710"] and run["w7k"]["limit"] == 150
          and run["w7j"]["campaigns"] == [],
          f"the running groups carry their campaigns, their limit and whether a tick counted them over-size, a stalled one "
          f"not (what group_sizes.record keeps, PB4944): {p['running']}")
    group_sizes.record(cdir, p["running"], now)
    kept = group_sizes.read(cdir)
    check(sorted(kept) == sorted(run) and kept["w7j"]["over"] and round(kept["w7k"]["minutes"]) == 100,
          f"a tick's running groups are recorded in the group-size history: {kept}")
    check(p["reviewed"] == {"total": 2, "round1_findings": 1},
          f"two reviewed groups, one of which drew FINDINGS at round 1 (PB4888 lever 5): {p['reviewed']}")
    check(round(p["queue_empty_min"]) == 70,
          f"the window's 100 minutes less 20 (a merged record) and 10 (an open one) in the queue: {p['queue_empty_min']}")
    rev = cdir / "scratch" / "camp" / "review-w7-m.jsonl"   # a branch review's findings file, hyphenated, in a wave scratch
    rev.write_text("{}" + nl, encoding="utf-8", newline=nl)
    os.utime(rev, (now - 4 * 60, now - 4 * 60))
    m = group_activity(cdir, {"wave": "7", "letter": "M", "notes": []})
    check(round((now - m) / 60) == 4 and round((now - group_activity(cdir, {"wave": "7", "letter": "K", "notes": []})) / 60) == 5
          and group_activity(cdir, {"wave": "7", "letter": "B", "notes": []}) == 0.0,
          f"a group's acts are its scratch under a wave scratch (K) and its review's hyphenated findings file (M), and a "
          f"silent one (L) has none: {m}")
    empty = cdir / "scratch" / "w7e"   # R1-1: a started group's first act is an EMPTY scratch directory, not a file
    empty.mkdir()
    os.utime(empty, (now - 3 * 60, now - 3 * 60))
    e = group_activity(cdir, {"wave": "7", "letter": "E", "notes": []})
    check(round((now - e) / 60) == 3,
          f"an empty scratch directory is the group's start signal, so its mtime is its act (not 0.0, queued): {e}")
    r2 = reports.Review("CLEAN", 0, "w7c-review-r2", "abcdef12", None)
    check(review_round(r2) == 2 and review_round(reports.Review("CLEAN", 0, "w7c", "abcdef12", None)) == 1,
          "a reviewer id's -rN suffix is its round")
    check(p["landings"] == {"enqueued": 2, "merged": 1},
          f"the landings enqueued and merged since the unit started, never an earlier one: {p['landings']}")
    check(p["oldest"] and p["oldest"]["slug"] == "w7j" and round(p["oldest"]["minutes"]) == 120,
          f"the oldest waiting item is the longest-running group: {p['oldest']}")
    check([(w["slug"], w["on"]) for w in p["stalls"]] == [("w7a", "running"), ("w7g", "running"),
                                                                     ("w7d", "its branch review"), ("w7l", "running")],
          f"a started group or a DONE report with no act for {STALL_IDLE_MIN} minutes is a stall, a quiet one 25 minutes into "
          f"its run included, and a busy one is not (PB4944): {p['stalls']}")
    line = render("wave", p)
    check(line.startswith("PROGRESS wave at 100 min: groups running 8, queued 4") and "oldest waiting: w7j (PB712) 120 min" in line
          and "round-1 FINDINGS 1 of 2 reviewed; merge queue empty 70 of 100 min; over-size 1 (wave 7: 1)" in line,
          f"the line: {line}")
    check(stall_lines(p)[0] == "STALL w7a (PB701): no progress for 90 min on running (90 min since it began)",
          f"a stall line is stable for its item: {stall_lines(p)}")
    ref = rd / "w7c-PB703-report.md"
    first = post_stalls(cdir, p, line, str(ref), now)
    again = post_stalls(cdir, p, line, str(ref), now + 10 * 60)
    import mailbox  # noqa: PLC0415
    inbox = [m["subject"] for m in mailbox.listing(cdir, "operator")]
    mythos = [m["subject"] for m in mailbox.listing(cdir, "mythos")]
    check(first == ["STALL w7a (PB701)", "STALL w7g (PB707)", "STALL w7d (PB704)", "STALL w7l (PB713)"] and again == [],
          f"each stalled item is posted once, never once per tick: {first} / {again}")
    check(sorted(inbox) == ["STALL w7a (PB701)", "STALL w7d (PB704)", "STALL w7l (PB713)"] and mythos == ["STALL w7g (PB707)"],
          f"a stall goes to the lane that dispatched its group, a group with no recorded lane to the operator "
          f"(PB4190): operator {inbox}, mythos {mythos}")
    answered = answer_orphans(cdir, p, now)
    written = rd / "w9b-PB902-report.md"
    if written.exists():
        os.utime(written, (now, now))   # the self-test's clock is the future of the file system's: report written "now"
    again_p = progress(cdir, since, now, register)
    check(answered == ["w9b"] and answer_orphans(cdir, p, now) == [] and written.is_file()
          and reports.parse_report(written).status == "NOT STARTED"
          and again_p["groups"]["queued"] == 3 and again_p["groups"]["other"] == 3 and again_p["orphans"] == [],
          f"an orphaned dispatch is answered by a NOT STARTED report (once, never over a report that exists), so its "
          f"notes are no longer held and the group is no longer queued (PB5097): {answered} {again_p['groups']}")
    cleared = post_stalls(cdir, {**p, "stalls": []}, line, str(ref), now + 20 * 60)
    check(cleared == [] and coord.read_json(cdir / STALLS_POSTED, None) == {},
          "a stall that ended leaves no record behind, so its next episode is posted")
    empty = progress(cdir / "nowhere", since, now)
    check(empty["oldest"] is None and empty["stalls"] == [] and sum(empty["groups"].values()) == 0,
          f"an empty coordination directory reads nothing running: {empty}")
    for x in fails:
        print(f"FAIL: {x}")
    print(f"=== PROGRESS SELF-TEST: {'GREEN' if not fails else 'RED'} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--coord", help="the coordination directory (default: coord.coord_path())")
    ap.add_argument("--since", help="the unit's start, an ISO time with offset (default: one hour ago)")
    ap.add_argument("--unit", default="unit", help="the unit's name, for the line")
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--post", metavar="REF", help="post each new stall to its dispatching lane's mailbox, pointing at REF (an "
                                                  "existing path: the unit's log)")
    ap.add_argument("--record", action="store_true", help="record the running groups' sizes in the coordination "
                                                           "directory's group_sizes history, and answer each orphaned "
                                                           "queued dispatch with a NOT STARTED report (the supervisor's tick)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    since = landing_measure.iso_time(a.since).timestamp() if a.since else time.time() - 3600
    cdir = coord.coord_path(a.coord)
    p = progress(cdir, since)
    if a.record:
        group_sizes.record(cdir, p["running"])
        for slug in answer_orphans(cdir, p):
            print(f"answered the orphaned dispatch {slug} with a NOT STARTED report (its runner is gone, PB5097)")
    if a.json:
        print(json.dumps(p, indent=1))
        return 0
    line = render(a.unit, p)
    print(line)
    for s in stall_lines(p):
        print(s)
    if a.post:
        for subject in post_stalls(cdir, p, line, a.post):
            print(f"posted to the dispatching lane's mailbox: {subject}")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
