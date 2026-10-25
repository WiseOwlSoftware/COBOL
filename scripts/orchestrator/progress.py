#!/usr/bin/env python3
"""progress.py — the loop's progress line, and the stalls it names (kb/Work PB3638 defect 3).

WHY. The owner, 2026-10-24 ~12:15 PDT: "It's stupid to have work running with no updates or progress for multiple
hours. Other items simply block and also make no progress." The 10:04 campaign unit of that day wrote two milestones
in two hours, and the operator learned where the tier-1 PB3612 stood only by reading the wave's files. The supervisor
(`orchestrate.ps1`) now runs this script every `-ProgressSeconds` while a unit runs, in the background like its other
ticks, says the line in its log, and posts each NEW stall to the operator's mailbox, once per stalled item (a message
per tick would wake the attended session every ten minutes for news it already has).

THE LINE, read from the coordination directory only (no git, no GitHub: it runs every few minutes):
  groups        every dispatch-ledger group (coord.ledger_groups: the dispatched work still in flight) by its
                state: RUNNING (no report answers it, plan_wave.answers), DONE and reviewed CLEAN, DONE with review
                FINDINGS, DONE and not yet reviewed, or another status (SPLIT, DISCHARGED, BLOCKED, GATE-PENDING)
  landings      the landing records (landing_measure.read_all) enqueued and merged since the unit started (whether a
                pull request is in the merge queue NOW is the hosting service's to say: a record enqueued and never
                merged may be a closed pull request, so the line does not guess)
  oldest        the item that has waited longest, and on what
STALLS: a group RUNNING longer than `RUNNING_STALL_MIN` (the owner's unit hand-off, 60 minutes), and a DONE report not
reviewed `REVIEW_STALL_MIN` after it was written (half the owner's 30-minute dispatch-to-queue bar). Each is named with
its slug, its notes and its minutes.

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
import plan_wave  # noqa: E402

RUNNING_STALL_MIN = 60   # the owner, 2026-10-23: "no wave may take hours; one chain link per wave, hand off at ~60 min"
REVIEW_STALL_MIN = 15    # half the owner's 30-minute dispatch-to-queue bar (kb/Work PB3638, landing_measure.BUDGETS)


def progress(cdir: pathlib.Path, since: float, now: float | None = None) -> dict:
    """The progress figures and the stalls, as a dict (render prints the line)."""
    now = time.time() if now is None else now
    reps = plan_wave.load_reports(coord.reports_dir(str(cdir)))
    groups: dict[str, int] = {"running": 0, "clean": 0, "findings": 0, "unreviewed": 0, "other": 0}
    waiting: list[dict] = []   # {slug, notes, minutes, on}
    for g in coord.ledger_groups(cdir):
        slug, notes = plan_wave.ledger_slug(g), list(g.get("notes", []))
        mine = [r for r in reps if plan_wave.answers(r, g)]
        if not mine:
            groups["running"] += 1
            if isinstance(g.get("at"), (int, float)):
                waiting.append({"slug": slug, "notes": notes, "minutes": (now - g["at"]) / 60, "on": "running",
                                "limit": RUNNING_STALL_MIN})
            continue
        r = mine[-1]   # load_reports' order: the newest last
        if r.status != "DONE":
            groups["other"] += 1
        elif r.review and r.review.verdict == "CLEAN":
            groups["clean"] += 1
        elif r.review:
            groups["findings"] += 1
        else:
            groups["unreviewed"] += 1
            try:
                waiting.append({"slug": slug, "notes": notes, "minutes": (now - r.path.stat().st_mtime) / 60,
                                "on": "its branch review", "limit": REVIEW_STALL_MIN})
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


def stall_lines(p: dict) -> list[str]:
    """One line per stall, its subject stable for one stalled item (the mailbox's --unless-pending key)."""
    return [f"STALL {w['slug']} ({', '.join(w['notes'])}): {w['minutes']:.0f} min on {w['on']}, past its "
            f"{w['limit']}-minute limit" for w in p["stalls"]]


STALLS_POSTED = "progress-stalls.json"   # {subject: when it was last posted}, in the coordination directory
REPOST_MIN = 60                          # a stall still open an hour after its message is posted again


def post_stalls(cdir: pathlib.Path, p: dict, unit_line: str, ref: str, now: float | None = None) -> list[str]:
    """Post each stall to the operator's mailbox (mailbox.send, kind `info`, from the loop) and return the subjects
    posted. One message per stalled ITEM: the subject is the stall line's stable head (`STALL <slug> (<notes>)`), sent
    with --unless-pending while one is open, and again only `REPOST_MIN` after the last post (the attended session is
    woken by every message, kb/Work PB2482, so a message per tick would cost it a turn every ten minutes for news it
    has). `ref` is an existing path the message points at (the unit's log)."""
    import mailbox  # noqa: PLC0415  (THE writer of a message)
    now = time.time() if now is None else now
    posted = coord.read_json(cdir / STALLS_POSTED, {})
    sent = []
    for line in stall_lines(p):
        subject = line.split(":", 1)[0]
        if now - float(posted.get(subject, 0)) < REPOST_MIN * 60:
            continue
        if mailbox.send(cdir, to="operator", kind="info", subject=subject, body=f"{line}. {unit_line}"[:mailbox.BODY_MAX],
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
        {"wave": "7", "letter": "A", "notes": ["PB701"], "files": [], "at": now - 90 * 60},    # running 90 min: a stall
        {"wave": "7", "letter": "B", "notes": ["PB702"], "files": [], "at": now - 20 * 60},    # running 20 min
        {"wave": "7", "letter": "C", "notes": ["PB703"], "files": [], "at": now - 80 * 60},    # DONE, CLEAN
        {"wave": "7", "letter": "D", "notes": ["PB704"], "files": [], "at": now - 80 * 60},    # DONE, unreviewed 40 min
        {"wave": "7", "letter": "E", "notes": ["PB705"], "files": [], "at": now - 80 * 60},    # SPLIT
        {"wave": "7", "letter": "F", "notes": ["PB706"], "files": [], "at": now - 80 * 60}]})  # DONE, FINDINGS

    def report(name: str, status: str, review: str, age_min: float) -> None:
        p = rd / name
        p.write_text(f"{status}\n**Status:** {status} · **branch:** `b{name[:3]}` · **HEAD:** `abcdef12`\n{review}",
                     encoding="utf-8", newline="\n")
        os.utime(p, (now - age_min * 60, now - age_min * 60))

    report("w7c-PB703-report.md", "DONE", "**Review:** CLEAN w7c-review-r1 abcdef12\n", 50)
    report("w7d-PB704-report.md", "DONE", "", 40)
    report("w7e-PB705-report.md", "SPLIT", "", 30)
    report("w7f-PB706-report.md", "DONE", "**Review:** FINDINGS 2 w7f-review-r1 abcdef12 E:/s/review-w7-f.jsonl\n", 30)
    since = now - 100 * 60
    iso = dt.datetime.fromtimestamp
    (cdir / landing_measure.FILE).write_text("".join(json.dumps(x) + chr(10) for x in (
        {"pr": 1, "branch": "b1", "enqueued": iso(now - 50 * 60).astimezone().isoformat(),
         "merged": iso(now - 30 * 60).astimezone().isoformat()},
        {"pr": 2, "branch": "b2", "enqueued": iso(now - 10 * 60).astimezone().isoformat()},
        {"pr": 3, "branch": "b3", "enqueued": iso(now - 300 * 60).astimezone().isoformat(),
         "merged": iso(now - 200 * 60).astimezone().isoformat()})), encoding="utf-8", newline="\n")
    p = progress(cdir, since, now)
    check(p["groups"] == {"running": 2, "clean": 1, "findings": 1, "unreviewed": 1, "other": 1},
          f"each ledger group by its state: {p['groups']}")
    check(p["landings"] == {"enqueued": 2, "merged": 1},
          f"the landings enqueued and merged since the unit started, never an earlier one: {p['landings']}")
    check(p["oldest"] and p["oldest"]["slug"] == "w7a" and round(p["oldest"]["minutes"]) == 90,
          f"the oldest waiting item is the longest-running group: {p['oldest']}")
    check([(w["slug"], w["on"]) for w in p["stalls"]] == [("w7a", "running"), ("w7d", "its branch review")],
          f"a group past its running limit and a DONE report past its review limit are stalls: {p['stalls']}")
    line = render("wave", p)
    check(line.startswith("PROGRESS wave at 100 min: groups running 2") and "oldest waiting: w7a (PB701) 90 min" in line,
          f"the line: {line}")
    check(stall_lines(p)[0] == "STALL w7a (PB701): 90 min on running, past its 60-minute limit",
          f"a stall line is stable for its item: {stall_lines(p)}")
    ref = rd / "w7c-PB703-report.md"
    first = post_stalls(cdir, p, line, str(ref), now)
    again = post_stalls(cdir, p, line, str(ref), now + 10 * 60)
    import mailbox  # noqa: PLC0415
    inbox = [m["subject"] for m in mailbox.listing(cdir, "operator")]
    check(first == ["STALL w7a (PB701)", "STALL w7d (PB704)"] and again == [] and sorted(inbox) == sorted(first),
          f"each stalled item is posted once, never once per tick: {first} / {again} / {inbox}")
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
    ap.add_argument("--post", metavar="REF", help="post each new stall to the operator's mailbox, pointing at REF (an "
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
            print(f"posted to the operator's mailbox: {subject}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
