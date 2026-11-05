#!/usr/bin/env python3
"""Self-test for next_unit.py's choice of the next unit.

Proves, against a coordination directory built from units.jsonl lines:
  - no meter unit is ever chosen (kb/Work PB4748): budget.py anchors on the status line's cached rate_limits, so a
    coordination directory with no reading at all, or a days-old one, still chooses the work's own unit;
  - rule 5 and the REVIEW LANE (kb/Work PB3608): a branch WAITING FOR REVIEW is the review lane's while it ticks, and
    the enqueue unit starts only for its holds or while it is not ticking;
  - a handoff's `enqueue` is a request (kb/Work PB4180): it runs the unit only while rule 5 finds agent work, so a
    handoff that names it to wait for a merge, with only WAITING TO LAND / ENQUEUED / NOT QUEUED rows in the survey,
    runs the wave and yields to rule 4; every other named unit is still an order;
  - `enqueue_chains` finds the runs of consecutive enqueue units (the drift measure; a retired meter line in the
    history is invisible).
Run: python scripts/orchestrator/test_next_unit.py"""
import datetime as dt
import json
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE))
from scratch_repo import scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
from standard_streams import ensure_utf8  # noqa: E402
import next_unit  # noqa: E402

ensure_utf8()

TMP = scratch_dir("next-unit-test-")
REPO = TMP / "repo"          # not a git tree: clean, nothing unpushed, no worktrees
REPO.mkdir()
NOW = dt.datetime(2026, 10, 25, 12, 0, tzinfo=dt.timezone.utc)
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def at(minutes_ago: float) -> str:
    return (NOW - dt.timedelta(minutes=minutes_ago)).isoformat()


def unit(name, minutes_ago):
    """One units.jsonl line, as orchestrate.ps1 writes it (the fields next_unit.py reads)."""
    return {"unit": name, "account": "account2", "started_at": at(minutes_ago + 2), "ended_at": at(minutes_ago),
            "failed": False}


def coord(case: str, lines: list[dict], readings: list[dict] | None = None) -> pathlib.Path:
    cdir = TMP / case
    cdir.mkdir()
    (cdir / "units.jsonl").write_text("".join(json.dumps(r) + "\n" for r in lines), encoding="utf-8", newline="\n")
    if readings is not None:
        (cdir / "readings.json").write_text(json.dumps(readings), encoding="utf-8", newline="\n")
    return cdir


def choose(cdir, handoff=None, operator=None):
    return next_unit.choose(handoff, cdir, REPO, NOW, False, None, operator=operator)


# no meter unit, whatever the readings say (kb/Work PB4748)
c = choose(coord("no-readings", []))
check("no reading at all: the work's own unit, never a meter", c["unit"], "wave")
old = [{"account": "account2", "noted_at": (NOW - dt.timedelta(hours=54)).isoformat(), "weekly_pct": 10, "session_pct": 0}]
c = choose(coord("old-reading", [], old))
check("a 54-hour-old reading: still the work's own unit", c["unit"], "wave")
c = choose(coord("old-meter-lines", [unit("meter", 30), unit("meter", 20), unit("meter", 10)]))
check("the history's retired meter lines change nothing", (c["unit"], "meter_unreadable" in c), ("wave", False))

# rule 5 and the REVIEW LANE (kb/Work PB3608): a branch WAITING FOR REVIEW is the review lane's while it ticks; the enqueue
# unit starts for its holds, or for the branch while the lane is not ticking (the unit then ticks it once).
def lane_case(case: str, ticked_min_ago: float | None, holds: list[dict], handoff: dict | None = None,
              rows: list[dict] | None = None) -> dict:
    cdir = coord(case, [])
    real = dt.datetime.now(dt.timezone.utc)
    (cdir / "land-lane").mkdir()
    rows = rows if rows is not None else [
        {"name": "w1a", "state": "WAITING FOR REVIEW", "tip": "a1", "worktree": "wt", "conflicts": False}]
    (cdir / "land-lane" / "survey.json").write_text(json.dumps({"at": real.isoformat(), "landing_order": [], "rows": rows}),
                                                    encoding="utf-8", newline="\n")
    (cdir / "review-lane" / "holds").mkdir(parents=True)
    if ticked_min_ago is not None:
        (cdir / "review-lane" / "last-tick.json").write_text(json.dumps({"at": (real - dt.timedelta(minutes=ticked_min_ago)).isoformat()}),
                                                              encoding="utf-8", newline="\n")
    for h in holds:
        (cdir / "review-lane" / "holds" / f"{h['branch']}.json").write_text(json.dumps(h), encoding="utf-8", newline="\n")
    return choose(cdir, handoff=handoff)


c = lane_case("lane-ticking", 1, [])
check("a branch WAITING FOR REVIEW starts no enqueue unit while the review lane ticks", c["unit"], "wave")
c = lane_case("lane-hold", 1, [{"branch": "w1a", "head": "a1", "why": "2 reviewers ended with no verdict", "collected": False}])
check("a review lane hold starts the enqueue unit, naming the branch", (c["unit"], "review lane holds: w1a" in c["reason"]), ("enqueue", True))
c = lane_case("lane-collected", 1, [{"branch": "w1a", "head": "a1", "why": "x", "collected": True}])
check("a collected hold is nobody's work", c["unit"], "wave")
c = lane_case("lane-stale", 500, [])
check("a review lane that is not ticking hands its waiting branch to the unit, which ticks it once",
      (c["unit"], "not ticking" in c["reason"]), ("enqueue", True))
c = lane_case("lane-never", None, [])
check("a review lane that never ticked does the same", c["unit"], "enqueue")

# a handoff's `enqueue` is a request, not an order (kb/Work PB4180): the unit waits for no merge, because the landing
# lane lands every branch WAITING TO LAND and the review lane reviews the rest. Replay the 2026-10-30 chain: a handoff
# names enqueue ("f7f-29 lands once #811 merges") while the survey holds nothing but lane-owned rows.
waiting = [{"name": "f7f-29", "state": "WAITING TO LAND", "tip": "b1", "worktree": "wt", "conflicts": False},
           {"name": "17b-15", "state": "ENQUEUED", "tip": "b2", "worktree": "wt", "conflicts": False},
           {"name": "468-1", "state": "NOT QUEUED", "tip": "b3", "worktree": "wt", "conflicts": False}]
wait_handoff = {"schema_version": 1, "unit": "enqueue", "outcome": "done", "summary": "x", "next_unit": "enqueue",
                "next_unit_reason": "f7f-29 lands once #811 merges",
                "branches_pending": [{"branch": "f7f-29", "status": "DONE"}, {"branch": "17b-15", "status": "DONE"}]}
c = lane_case("wait-for-merge", 1, [], handoff=wait_handoff, rows=waiting)
check("a handoff naming enqueue to wait for a merge runs the wave", c["unit"], "wave")
check("the choice says the named enqueue was declined", "named enqueue for work the lanes own" in c["reason"], True)
c = lane_case("wait-hold", 1, [{"branch": "w1a", "head": "a1", "why": "2 reviewers ended with no verdict", "collected": False}],
              handoff=wait_handoff, rows=waiting)
check("a handoff naming enqueue while a hold stands runs the unit, naming the handoff and the hold",
      (c["unit"], "named by the last handoff" in c["reason"], "review lane holds: w1a" in c["reason"]), ("enqueue", True, True))
c = lane_case("wait-stale", 500, [], handoff=wait_handoff)
check("a handoff naming enqueue while the review lane is not ticking and a branch waits for review runs the unit",
      c["unit"], "enqueue")
# a declined enqueue falls through to the rules after it, not to the wave alone
bare = {**wait_handoff, "branches_pending": []}   # no DONE branch, so no unticking-lane work either
c = choose(coord("wait-failed", []), handoff={**bare, "outcome": "failed"})
check("a declined enqueue yields to rule 4: the last unit failed", c["unit"], "resume")
# every other named unit is still an order
for name in ("resume", "wave"):
    c = choose(coord("named-" + name, []), handoff={**wait_handoff, "next_unit": name})
    check(f"a handoff naming {name} still runs it", c["unit"], name)


# the drift measure: runs of consecutive enqueue units (a retired meter line between two is invisible, any other unit
# ends the run)
def timed(name, minutes_ago, minutes):
    return {**unit(name, minutes_ago), "duration_s": minutes * 60, "reason": name + " reason"}


chain_lines = [timed("wave", 600, 30), timed("enqueue", 500, 20), timed("meter", 470, 1), timed("enqueue", 440, 40),
               timed("enqueue", 380, 10), timed("wave", 300, 30), timed("enqueue", 200, 15), timed("resume", 150, 5),
               timed("enqueue", 100, 10), timed("enqueue", 50, 10)]
chains = next_unit.enqueue_chains(chain_lines)
check("enqueue_chains finds the runs of two or more", [(x["units"], x["hours"]) for x in chains], [(3, 1.17), (2, 0.33)])
check("enqueue_chains names each run by its first unit's start", chains[0]["started_at"], chain_lines[1]["started_at"])
check("enqueue_chains reports a lone enqueue as no chain", next_unit.enqueue_chains(chain_lines[:2]), [])
check("enqueue_chains ends a run at the history's end", len(next_unit.enqueue_chains(chain_lines[-2:])), 1)

if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("next_unit self-test: all checks OK")
