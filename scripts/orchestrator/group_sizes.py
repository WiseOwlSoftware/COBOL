#!/usr/bin/env python3
"""group_sizes.py — THE history of how long the loop's groups ran, and the group size a campaign's history allows
(kb/Work PB4944, item 2 of its proposal: "the planner's sizing reads it (a campaign whose groups always run over is
split smaller)").

WHY. The planner sizes a group by TURNS (clusters.notes_cap: the notes whose predicted turns fit three quarters of the
implementer's cap; kb/Work PB4396), and the owner sizes a unit by TIME ("no wave may take hours; one chain link per
wave, hand off at ~60 min", 2026-10-23; a campaign's own `running_limit_min:`, 2026-11-07). The two disagree on work
whose turns are cheap and whose minutes are not: the 1985 audit's implementers ran 60 to 150 minutes on groups the turn
fit called small. The PROGRESS line counts such a group OVER-SIZE (progress.py), but a count on one tick is forgotten by
the next unit, so nothing told the planner. This file is the memory: what each group was seen to take, and the cap the
planner derives from it.

THE RECORD (`record`, called by progress.py `--record` on every progress tick the supervisor runs): one entry per
dispatch-ledger group seen RUNNING, keyed by its slug (`w<wave><letter>`), `{wave, notes, campaigns, limit, minutes,
over, running, seen}`. `minutes` is the longest run seen (dispatch to the tick), `over` is sticky once a tick counted the
group OVER-SIZE (still moving and past its limit; a STALL is not over-size, it stopped), `campaigns` is work.note_campaigns
(the clusters its notes belong to), `limit` the one it was held to. `running` says the latest tick saw the group still
running: a tick reads the WHOLE shared dispatch ledger (progress.progress walks coord.ledger_groups), so a group missing
from its running list has stopped running (its report is in, it waits on review, or it landed) and its entry is FINISHED,
`running: false`; a finisher's later run of the same group makes it running again. A tick rewrites the entries it saw,
marks the rest finished and keeps them; an entry not seen for `KEEP_DAYS` is dropped. It lives in the coordination directory beside the dispatch ledger,
whose groups leave it when they land, which is why the history cannot be read back from the ledger.

THE CAP (`campaign_cap`, read by plan_wave.py for a `--cluster` wave): a campaign whose latest `WINDOW` FINISHED groups
were ALL over-size, with at least `MIN_SAMPLES` of them, is held to the notes its measured pace fits in its limit:
`limit // median(minutes per note)`, never fewer than one note (a note is never split) and never more than the turn cap.
A campaign one of whose recent groups fitted keeps the turn cap: the rule acts on a campaign whose groups ALWAYS run
over, the owner's wording, and one fast group says the size is not the cause. A group still RUNNING is read by neither
test: it has not fitted (its run is not over, and the planner plans while dispatches run), and its minutes so far are
not its pace (kb/Work PB4944 review R1-1: a group five minutes old suppressed the cap of a campaign whose every finished
group ran over). The fix lane keeps the turn cap (its
limit is always the owner's 60 minutes, and its groups are many campaigns' notes, not one pace).

Usage:
    python scripts/orchestrator/group_sizes.py [--coord <dir>] [--cluster <lead>]   # the history, or one campaign's cap
    python scripts/orchestrator/group_sizes.py --self-test
"""
from __future__ import annotations

import argparse
import pathlib
import statistics
import sys
import time
from typing import Any

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import coord  # noqa: E402
sys.path.append(str(HERE.parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

FILE = "group-sizes.json"   # {"groups": {slug: entry}}, in the coordination directory
KEEP_DAYS = 30              # an entry not seen this long is dropped
WINDOW = 5                  # the campaign's latest recorded groups the cap reads
MIN_SAMPLES = 3             # fewer recorded groups say nothing about a campaign's pace


def read(cdir: pathlib.Path) -> dict[str, dict[str, Any]]:
    """The recorded groups, {slug: entry}; {} when nothing is recorded."""
    return dict(coord.read_json(cdir / FILE, {}).get("groups", {}))


def record(cdir: pathlib.Path, running: list[dict[str, Any]], now: float | None = None) -> int:
    """Upsert one entry per RUNNING group of a progress tick (progress.progress's `running`: each `{slug, wave, notes,
    campaigns, limit, minutes, over}`: every running group of the shared ledger), mark every other kept entry finished,
    drop the entries unseen for KEEP_DAYS, and return how many were written."""
    now = time.time() if now is None else now
    groups = {s: e for s, e in read(cdir).items() if now - float(e.get("seen", 0)) <= KEEP_DAYS * 86400}
    for e in groups.values():
        e["running"] = False   # the tick read the whole ledger: a group it did not see running has finished
    for w in running:
        was = groups.get(w["slug"], {})
        groups[w["slug"]] = {"wave": str(w["wave"]), "notes": list(w["notes"]), "campaigns": list(w["campaigns"]),
                             "limit": int(w["limit"]), "minutes": round(max(float(was.get("minutes", 0)), w["minutes"]), 1),
                             "over": bool(was.get("over")) or bool(w["over"]), "running": True, "seen": now}
    coord.write_json(cdir / FILE, {"groups": groups})
    return len(running)


def campaign_cap(cdir: pathlib.Path, campaign: str, cap: int) -> tuple[int, str | None]:
    """The group size a `--cluster` wave of `campaign` plans at, from the turn cap `cap` (clusters.notes_cap), and why
    when the history lowered it (None when it did not; the module docstring's THE CAP). Only FINISHED groups are read."""
    mine = sorted((e for e in read(cdir).values() if campaign in e["campaigns"] and not e["running"]),
                  key=lambda e: -float(e.get("seen", 0)))[:WINDOW]
    if len(mine) < MIN_SAMPLES or not all(e.get("over") for e in mine):
        return cap, None
    pace = statistics.median(float(e["minutes"]) / max(1, len(e.get("notes", []))) for e in mine)
    limit = int(mine[0]["limit"])
    fit = max(1, int(limit // pace)) if pace > 0 else cap
    if fit >= cap:
        return cap, None
    return fit, (f"campaign {campaign}'s latest {len(mine)} finished groups all ran past their {limit}-minute limit, at a median "
                 f"{pace:.0f} minutes per note, so a group holds {fit} note{'s' if fit != 1 else ''} (the turn cap is "
                 f"{cap}; kb/Work PB4944, group_sizes.campaign_cap)")


def self_test() -> int:
    from scratch_repo import scratch_dir  # noqa: PLC0415  (scripts/, on the path through coord)
    fails: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            fails.append(what)

    cdir = scratch_dir("group-sizes-selftest-")
    now = 1_900_000_000.0

    def run(slug: str, notes: int, minutes: float, over: bool, campaigns: tuple[str, ...] = ("PB10",),
            limit: int = 60) -> dict[str, Any]:
        return {"slug": slug, "wave": slug[1:-1], "notes": [f"PB{100 + k}" for k in range(notes)],
                "campaigns": list(campaigns), "limit": limit, "minutes": minutes, "over": over}

    check(campaign_cap(cdir, "PB10", 4) == (4, None), "an empty history keeps the turn cap")
    record(cdir, [run("w1a", 2, 50, False)], now - 3600)
    record(cdir, [run("w1a", 2, 40, True)], now - 3000)   # an earlier tick saw it longer; a later one counted it over
    e = read(cdir)["w1a"]
    check(e["minutes"] == 50 and e["over"] is True and e["seen"] == now - 3000 and e["running"] is True,
          f"an entry keeps the longest run seen and stays over-size once counted so: {e}")
    record(cdir, [run("w2a", 2, 130, True), run("w3a", 3, 200, True, ("PB10", "PB20"))], now - 600)
    check(read(cdir)["w1a"]["running"] is False and campaign_cap(cdir, "PB10", 4) == (4, None),
          "a group a tick did not see running is finished, and a group still running is read by neither test "
          f"(one finished group says nothing yet): {read(cdir)}")
    # the next group of the campaign was dispatched five minutes ago (kb/Work PB4944 review R1-1): it has not fitted
    record(cdir, [run("w4y", 2, 5, False)], now - 500)
    cap, why = campaign_cap(cdir, "PB10", 4)
    # finished paces per note: w1a 25, w2a 65, w3a 66.7 -> median 65; 60 // 65 = 0 -> never fewer than one note
    check(cap == 1 and why and "latest 3 finished groups" in why and "65 minutes per note" in why,
          f"a campaign whose latest groups all ran over is held to the notes its pace fits, at least one, whatever a "
          f"group still running has taken so far: {cap} {why}")
    check(campaign_cap(cdir, "PB20", 4) == (4, None), "fewer than MIN_SAMPLES recorded groups say nothing")
    record(cdir, [], now - 300)   # w4y finished inside its limit
    check(campaign_cap(cdir, "PB10", 4) == (4, None), "one recent group that fitted keeps the turn cap")
    long = cdir / "long"
    long.mkdir()
    record(long, [run(f"w{k}a", 4, 160, True, limit=150) for k in range(1, 4)], now - 60)
    record(long, [], now)
    check(campaign_cap(long, "PB10", 6) == (3, campaign_cap(long, "PB10", 6)[1]) and campaign_cap(long, "PB10", 2) == (2, None),
          f"the cap is the campaign's own limit over its pace (150 // 40 = 3), never above the turn cap: "
          f"{campaign_cap(long, 'PB10', 6)}")
    record(cdir, [], now + (KEEP_DAYS + 1) * 86400)
    check(read(cdir) == {}, "an entry unseen for KEEP_DAYS is dropped")
    for x in fails:
        print(f"FAIL: {x}")
    print(f"=== GROUP SIZES SELF-TEST: {'GREEN' if not fails else 'RED'} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--coord", help="the coordination directory (default: coord.coord_path())")
    ap.add_argument("--cluster", help="print this campaign's group size against the turn cap (clusters.notes_cap)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    cdir = coord.coord_path(a.coord)
    if a.cluster:
        from clusters import notes_cap  # noqa: PLC0415
        cap, why = campaign_cap(cdir, a.cluster, notes_cap(coord.rules()))
        print(f"campaign {a.cluster}: {cap} notes per group" + (f" ({why})" if why else " (the turn cap)"))
        return 0
    for slug, e in sorted(read(cdir).items(), key=lambda kv: kv[1].get("seen", 0)):
        print(f"{slug}: {len(e['notes'])} notes, {e['minutes']:.0f} of {e['limit']} min"
              f"{' OVER-SIZE' if e['over'] else ''}; campaigns {', '.join(e['campaigns']) or '-'}")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
