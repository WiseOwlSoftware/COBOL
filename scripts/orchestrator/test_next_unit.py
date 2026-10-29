#!/usr/bin/env python3
"""Self-test for next_unit.py's rule 3 (kb/Work PB2665): a meter unit that cannot read its page is an ENVIRONMENT failure
the unit choice counts per unit type, whatever ran between the attempts.

Proves, against a coordination directory built from units.jsonl and readings.json lines:
  - a stale reading chooses `meter`, also after one and two failed attempts (a transient failure retries at once);
  - METER_ATTEMPTS failed attempts in a row close the meter: the choice is the next rule's unit and carries
    `meter_unreadable` ({failures, since, retry_at}), so the supervisor tells the owner once and the loop goes on;
  - a unit of another type between the failed meters neither extends nor breaks the run (the supervisor's own
    consecutive-failure count reset to zero at every interleaved `resume`, so it never passed 1 of 3);
  - a meter that read (no environment_failure) ends the run; another account's attempts never count;
  - the gate opens again once the newest attempt is a maximum reading age old, and a failed probe closes it again under
    the SAME `since` (one owner question per streak);
  - an explicit name (a handoff, the operator's steer) outranks the gate: the gate stops the automatic retry only.
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
MAX_AGE_H = 3.0
ME, OTHER = "account2", "account1"
fails = []


def check(name, got, want):
    if got != want:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def at(minutes_ago: float) -> str:
    return (NOW - dt.timedelta(minutes=minutes_ago)).isoformat()


def unit(name, minutes_ago, *, env=False, account=ME):
    """One units.jsonl line, as orchestrate.ps1 writes it (the fields next_unit.py reads)."""
    return {"unit": name, "account": account, "started_at": at(minutes_ago + 2), "ended_at": at(minutes_ago),
            "failed": False, "environment_failure": env}


def coord(case: str, lines: list[dict], reading_hours_ago: float | None = None) -> pathlib.Path:
    cdir = TMP / case
    cdir.mkdir()
    (cdir / "units.jsonl").write_text("".join(json.dumps(r) + "\n" for r in lines), encoding="utf-8", newline="\n")
    readings = []
    if reading_hours_ago is not None:
        noted = (NOW - dt.timedelta(hours=reading_hours_ago)).isoformat()
        readings.append({"account": ME, "noted_at": noted, "weekly_pct": 10, "session_pct": 5})
    (cdir / "readings.json").write_text(json.dumps(readings), encoding="utf-8", newline="\n")
    return cdir


def choose(cdir, handoff=None, operator=None):
    return next_unit.choose(handoff, cdir, REPO, NOW, False, None, MAX_AGE_H, ME, operator=operator)


def fail(minutes_ago, **kw):
    return unit("meter", minutes_ago, env=True, **kw)


# a stale reading, no attempts: meter; after one or two failures: meter again (a transient failure retries at once)
c = choose(coord("none", [], reading_hours_ago=4))
check("stale reading, no attempts: meter", (c["unit"], "meter_unreadable" in c), ("meter", False))
c = choose(coord("one", [fail(10)]))
check("one failed attempt: meter again", (c["unit"], "meter_unreadable" in c), ("meter", False))
c = choose(coord("two", [fail(20), unit("resume", 15), fail(10)]))
check("two failed attempts: meter again", c["unit"], "meter")

# the third closes the meter: the next rule's unit, the gate reported
lines = [fail(30), fail(20), fail(10)]
c = choose(coord("three", lines))
gate = c.get("meter_unreadable") or {}
check("three failed attempts: not a meter", c["unit"], "wave")
check("the gate counts the run", gate.get("failures"), 3)
check("the gate names the streak by its first attempt", gate.get("since"), at(30))
check("the gate reopens a maximum age after the newest attempt", gate.get("retry_at"),
      (NOW - dt.timedelta(minutes=10) + dt.timedelta(hours=MAX_AGE_H)).isoformat())

# a unit of another type between the failures neither extends nor breaks the run (PB2665 defect 3)
mixed = [fail(80), unit("resume", 70), fail(60), unit("wave", 50), unit("resume", 40), fail(30), unit("wave", 20)]
c = choose(coord("mixed", mixed))
check("failed meters with other units between them still close the meter", (c["unit"], c["meter_unreadable"]["failures"]),
      ("wave", 3))

# a meter that read ends the run; the run after it starts counting from zero
read_between = [fail(90), fail(80), unit("meter", 70), fail(60), fail(50)]
c = choose(coord("read", read_between))
check("a meter that read resets the run", (c["unit"], "meter_unreadable" in c), ("meter", False))

# another account's attempts never count (two accounts share units.jsonl's directory)
theirs = [fail(30, account=OTHER), fail(20, account=OTHER), fail(10, account=OTHER)]
c = choose(coord("theirs", theirs))
check("another account's failed attempts do not close this account's meter", c["unit"], "meter")

# once the newest attempt is a maximum age old, one probe is due; its failure closes the gate under the SAME since
old = [fail(60 * 5), fail(60 * 4.5), fail(60 * 4)]
c = choose(coord("probe", old))
check("a maximum age after the newest attempt the meter is tried again", (c["unit"], "meter_unreadable" in c),
      ("meter", False))
probe_failed = old + [fail(5)]
c = choose(coord("probe-failed", probe_failed))
check("a failed probe closes the meter again under the streak's first attempt",
      (c["unit"], c["meter_unreadable"]["failures"], c["meter_unreadable"]["since"]), ("wave", 4, at(60 * 5)))

# a fresh reading (the owner recorded one) needs no meter and reports no gate even with a failed run behind it
c = choose(coord("fresh", lines, reading_hours_ago=1))
check("a fresh reading: no meter, no gate", (c["unit"], "meter_unreadable" in c), ("wave", False))

# an explicit name outranks the gate: only the automatic retry is stopped
named = {"schema_version": 1, "unit": "resume", "outcome": "done", "summary": "x", "next_unit": "meter",
         "next_unit_reason": "the wave asked for a reading"}
c = choose(coord("named", lines), handoff=named)
check("a handoff that names the meter runs it", (c["unit"], "meter_unreadable" in c), ("meter", True))
steer = {"schema_version": 1, "unit": "operator", "outcome": "done", "summary": "x", "next_unit": "meter",
         "next_unit_reason": "operator: try the meter now"}
c = choose(coord("steered", lines), operator=steer)
check("the operator's steer for the meter runs it", (c["unit"], c.get("operator")), ("meter", True))

# the gate does not hide the rest of the chain: a handoff naming enqueue still wins, and the gate rides along
enq = {"schema_version": 1, "unit": "resume", "outcome": "done", "summary": "x", "next_unit": "enqueue",
       "next_unit_reason": "branch ready"}
c = choose(coord("enqueue", lines), handoff=enq)
check("a named enqueue is chosen while the meter is unreadable", (c["unit"], c["meter_unreadable"]["failures"]),
      ("enqueue", 3))

# rule 6 and the REVIEW LANE (kb/Work PB3608): a branch WAITING FOR REVIEW is the review lane's while it ticks; the enqueue
# unit starts for its holds, or for the branch while the lane is not ticking (the unit then ticks it once).
def lane_case(case: str, ticked_min_ago: float | None, holds: list[dict]) -> dict:
    cdir = coord(case, [], reading_hours_ago=0.1)
    real = dt.datetime.now(dt.timezone.utc)
    (cdir / "land-lane").mkdir()
    (cdir / "land-lane" / "survey.json").write_text(json.dumps({"at": real.isoformat(), "landing_order": [], "rows": [
        {"name": "w1a", "state": "WAITING FOR REVIEW", "tip": "a1", "worktree": "wt", "conflicts": False}]}),
        encoding="utf-8", newline="\n")
    (cdir / "review-lane" / "holds").mkdir(parents=True)
    if ticked_min_ago is not None:
        (cdir / "review-lane" / "last-tick.json").write_text(json.dumps({"at": (real - dt.timedelta(minutes=ticked_min_ago)).isoformat()}),
                                                              encoding="utf-8", newline="\n")
    for h in holds:
        (cdir / "review-lane" / "holds" / f"{h['branch']}.json").write_text(json.dumps(h), encoding="utf-8", newline="\n")
    return choose(cdir)


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

if fails:
    print("\n".join("FAIL: " + f for f in fails))
    sys.exit(1)
print("next_unit self-test: all checks OK")
