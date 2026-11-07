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
                state: RUNNING (no report answers it, reports.answers), DONE and reviewed CLEAN, DONE with review
                FINDINGS, DONE and not yet reviewed, or another status (SPLIT, DISCHARGED, BLOCKED, GATE-PENDING)
  landings      the landing records (landing_measure.read_all) enqueued and merged since the unit started (whether a
                pull request is in the merge queue NOW is the hosting service's to say: a record enqueued and never
                merged may be a closed pull request, so the line does not guess)
  oldest        the item that has waited longest, and on what
STALLS: a group RUNNING longer than `RUNNING_STALL_MIN` (the owner's unit hand-off, 60 minutes), and a DONE report not
reviewed `REVIEW_STALL_MIN` after its implementer's last act (half the owner's 30-minute dispatch-to-queue bar). Each is
named with its slug, its notes and its minutes.
A DONE REPORT IS NOT YET A RETURN (kb/Work PB4918). The rolling wave starts a branch's review only when its implementer
RETURNS, and an implementer writes its report, then gates the tip it reports and fills the gate line in at the verdict, so
the report's mtime can be a whole gate (25 to 60 minutes) before the return. `implementer_activity` reads the report's own
worktree: a gate holding its lock there means the implementer is still working (the group counts as running, under
`RUNNING_STALL_MIN`, from its dispatch), and otherwise the review clock starts at the newest of the report's mtime and the
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
import sys
import time

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
import landing_measure  # noqa: E402
import reports  # noqa: E402  (THE fleet-report reader: a report, a ledger group's slug, "answered")
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

RUNNING_STALL_MIN = 60   # the owner, 2026-10-23: "no wave may take hours; one chain link per wave, hand off at ~60 min"
REVIEW_STALL_MIN = 15    # half the owner's 30-minute dispatch-to-queue bar (kb/Work PB3638, landing_measure.BUDGETS)


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


def progress(cdir: pathlib.Path, since: float, now: float | None = None) -> dict:
    """The progress figures and the stalls, as a dict (render prints the line)."""
    now = time.time() if now is None else now
    reps = reports.load_reports(coord.reports_dir(str(cdir)))
    groups: dict[str, int] = {"running": 0, "clean": 0, "findings": 0, "unreviewed": 0, "other": 0}
    waiting: list[dict] = []   # {slug, notes, minutes, on, limit, lane}
    for g in coord.ledger_groups(cdir):
        slug, notes = reports.ledger_slug(g), list(g.get("notes", []))
        mine = [r for r in reps if reports.answers(r, g)]
        if not mine:
            groups["running"] += 1
            if isinstance(g.get("at"), (int, float)):
                waiting.append({"slug": slug, "notes": notes, "minutes": (now - g["at"]) / 60, "on": "running",
                                "limit": RUNNING_STALL_MIN, "lane": stall_lane(g)})
            continue
        r = mine[-1]   # load_reports' order: the newest last
        if r.status != "DONE":
            groups["other"] += 1
        elif r.review and r.review.verdict == "CLEAN":
            groups["clean"] += 1
        elif r.review:
            groups["findings"] += 1
        else:
            gating, last_act = implementer_activity(r.worktree)
            if gating and isinstance(g.get("at"), (int, float)):
                # its report is written but its implementer is still gating the tip: no review can have started
                groups["running"] += 1
                waiting.append({"slug": slug, "notes": notes, "minutes": (now - g["at"]) / 60, "on": "running",
                                "limit": RUNNING_STALL_MIN, "lane": stall_lane(g)})
                continue
            groups["unreviewed"] += 1
            try:
                waiting.append({"slug": slug, "notes": notes,
                                "minutes": (now - max(r.path.stat().st_mtime, last_act)) / 60,
                                "on": "its branch review", "limit": REVIEW_STALL_MIN,
                                "lane": stall_lane(g)})
            except OSError:
                pass
    recs = landing_measure.read_all(cdir / landing_measure.FILE)

    def after(field: str) -> list[dict]:
        return [x for x in recs if x.get(field) and landing_measure.iso_time(x[field]).timestamp() >= since]

    landings = {"enqueued": len(after("enqueued")), "merged": len(after("merged"))}
    waiting.sort(key=lambda w: -w["minutes"])
    return {"groups": groups, "landings": landings, "oldest": waiting[0] if waiting else None,
            "stalls": [w for w in waiting if w["minutes"] > w["limit"]],
            "unit_minutes": (now - since) / 60}


def render(unit: str, p: dict) -> str:
    g, la, o = p["groups"], p["landings"], p["oldest"]
    line = (f"PROGRESS {unit} at {p['unit_minutes']:.0f} min: groups running {g['running']}, DONE reviewed CLEAN "
            f"{g['clean']}, FINDINGS {g['findings']}, unreviewed {g['unreviewed']}, other {g['other']}; landings since "
            f"the unit started: enqueued {la['enqueued']}, merged {la['merged']}")
    if o:
        line += f"; oldest waiting: {o['slug']} ({', '.join(o['notes'])}) {o['minutes']:.0f} min on {o['on']}"
    return line


def stall_line(w: dict) -> str:
    """One stall's line, its subject (the text before the first colon) stable for one stalled item (the mailbox's
    --unless-pending key)."""
    return (f"STALL {w['slug']} ({', '.join(w['notes'])}): {w['minutes']:.0f} min on {w['on']}, past its "
            f"{w['limit']}-minute limit")


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
        {"wave": "7", "letter": "I", "notes": ["PB709"], "files": [], "at": now - 80 * 60}]})  # DONE report, worktree active 5 min ago

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
    wt_gating, wt_active = worktree("wt-h"), worktree("wt-i")
    report("w7h-PB708-report.md", "DONE", "", 40, wt_gating)
    report("w7i-PB709-report.md", "DONE", "", 40, wt_active)
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
         "merged": iso(now - 200 * 60).astimezone().isoformat()})), encoding="utf-8", newline="\n")
    import gate  # noqa: PLC0415
    import gate_slot  # noqa: PLC0415
    held = gate_slot.ExclusiveLock.try_take(wt_gating / ".git" / gate.LOCK_NAME, "gate (self-test)")
    try:
        p = progress(cdir, since, now)
    finally:
        if held:
            held.release()
    check(held is not None, "the self-test took the gate lock of the gating worktree")
    check(p["groups"] == {"running": 4, "clean": 1, "findings": 1, "unreviewed": 2, "other": 1},
          f"each ledger group by its state: {p['groups']}")
    check(not any(w["slug"] in ("w7h", "w7i") for w in p["stalls"]),
          f"a DONE report whose implementer still gates is running, not a review stall (PB4918), and one whose worktree "
          f"was active 5 minutes ago has waited 5 minutes for review, not 40: {p['stalls']}")
    check(sorted(round(w["minutes"]) for w in p["stalls"]) == [40, 70, 90], f"the stalls' minutes: {p['stalls']}")
    check(p["landings"] == {"enqueued": 2, "merged": 1},
          f"the landings enqueued and merged since the unit started, never an earlier one: {p['landings']}")
    check(p["oldest"] and p["oldest"]["slug"] == "w7a" and round(p["oldest"]["minutes"]) == 90,
          f"the oldest waiting item is the longest-running group: {p['oldest']}")
    check([(w["slug"], w["on"]) for w in p["stalls"]] == [("w7a", "running"), ("w7g", "running"),
                                                                     ("w7d", "its branch review")],
          f"a group past its running limit and a DONE report past its review limit are stalls: {p['stalls']}")
    line = render("wave", p)
    check(line.startswith("PROGRESS wave at 100 min: groups running 4") and "oldest waiting: w7a (PB701) 90 min" in line,
          f"the line: {line}")
    check(stall_lines(p)[0] == "STALL w7a (PB701): 90 min on running, past its 60-minute limit",
          f"a stall line is stable for its item: {stall_lines(p)}")
    ref = rd / "w7c-PB703-report.md"
    first = post_stalls(cdir, p, line, str(ref), now)
    again = post_stalls(cdir, p, line, str(ref), now + 10 * 60)
    import mailbox  # noqa: PLC0415
    inbox = [m["subject"] for m in mailbox.listing(cdir, "operator")]
    mythos = [m["subject"] for m in mailbox.listing(cdir, "mythos")]
    check(first == ["STALL w7a (PB701)", "STALL w7g (PB707)", "STALL w7d (PB704)"] and again == [],
          f"each stalled item is posted once, never once per tick: {first} / {again}")
    check(sorted(inbox) == ["STALL w7a (PB701)", "STALL w7d (PB704)"] and mythos == ["STALL w7g (PB707)"],
          f"a stall goes to the lane that dispatched its group, a group with no recorded lane to the operator "
          f"(PB4190): operator {inbox}, mythos {mythos}")
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
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    since = landing_measure.iso_time(a.since).timestamp() if a.since else time.time() - 3600
    cdir = coord.coord_path(a.coord)
    p = progress(cdir, since)
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
