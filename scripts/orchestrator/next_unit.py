#!/usr/bin/env python3
"""Choose the orchestrator's next unit deterministically when the last handoff does not name one.

    python scripts/orchestrator/next_unit.py [--operator-handoff FILE] [--handoff FILE] [--last-failed]
                                             [--last-started ISO] [--repo DIR] [--cluster LEAD ... [--work DIR]]

Prints JSON {unit, reason}. The order (docs/rearchitecture/DESIGN-orchestrator-loop.md section 3.2), first match wins:
  0. the OPERATOR's steer (steer.ps1, kb/Work PB2596) -> its next_unit; the JSON also carries `"operator": true`, which
     tells the supervisor to consume the steer when it starts the unit. It wins over every unit's handoff (a synthesized
     one included) and every check below, because the operator is the one actor that sees the whole machine. The
     campaign rule does not re-decide it either; a `campaign` steer runs the first --cluster lead.
  1. the handoff carries owner_question            -> owner-question
  2. the handoff names next_unit                    -> that unit; a wave-type unit (`wave` or `campaign`) names only a WAVE, and with --cluster the campaign rule below decides
     its lane (kb/Work PB2522). A handoff's `enqueue` is honored only while rule 6 finds agent work: the landing and
     review lanes own the rest (waiting for a merge is no unit's work), so otherwise it falls through to rules 3 to 7
     and the reason says so (kb/Work PB4180)
  3. no meter reading of THIS account in 3 hours   -> meter (two accounts share readings.json; kb/Work PB2478), unless
     the meter is UNREADABLE: METER_ATTEMPTS meter units of this account in a row ended without a reading (any other
     unit may have run between them) and the newest attempt is younger than the maximum age. The choice then carries
     `meter_unreadable` ({failures, since, retry_at}), the supervisor tells the owner once, and the next attempt
     waits for the maximum age (kb/Work PB2665: a meter that cannot reach its page is an ENVIRONMENT cause, not a
     failure of the work, so it neither runs back to back nor trips the breaker)
  4. the repository is dirty or has unpushed commits -> resume
  5. the last unit failed or ended split, or a branch classified UNLANDED got a commit after the last unit
     started (an agent of a unit that died)          -> resume (a meter unit is invisible here: it reads one page and
     touches no branch, so the supervisor never lets it replace the handoff or count as the last unit, kb/Work PB2665)
  6. AGENT WORK for the enqueue unit                 -> enqueue: the landing lane's survey lists a branch WAITING FOR
     REVIEW, or BOUNCED while the lane is neither landing it nor re-armed to, or a lane attempt STOPPED or ENQUEUED is
     not yet collected (its finisher or reviewer, its leads, its landing record; a survey row STOPPED counts through
     that attempt, kb/Work PB3125). The landing lane lands every branch WAITING TO LAND itself, beside whatever unit runs
     (land_lane.py, kb/Work PB3103), so a handoff's DONE branches alone start no enqueue unit, unless the lane has no
     current survey (it is not ticking: the enqueue unit's step 3 ticks it).
  7. otherwise                                       -> wave
THE CAMPAIGN RULE (--cluster LEAD, repeatable: the CAMPAIGN lane, kb/Work PB2120) decides every wave-type choice, from
rule 2 or rule 7: `campaign` when a lead's cluster has a ready note and the last wave-type unit was not a campaign, so
campaign waves alternate with fix-lane waves; the ready leads take turns, least recently run first, and the JSON names
the one chosen in `cluster`. A wave-type choice then carries `campaign` (run | between | waiting | landed | unknown, the
lane as a whole). With --cluster every choice but an owner question
carries `campaigns` ({lead: ready | waiting | landed | unknown}: the supervisor ends a lead's lane at `landed` and
records the map in units.jsonl) and `starved` (the leads whose ready note has waited through more than one wave-type
unit since their own last campaign wave, measured from that record). Without --cluster the output is the fix lane's,
byte for byte.
(A stale ledger page is NOT a reason for a unit: a headless unit cannot publish, so the supervisor announces an owed
publish through ledger_state.py and the attended session publishes.)
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE.parent / "spec"))
import account  # noqa: E402
import coord  # noqa: E402
import work  # noqa: E402  (cluster_order: the one reader of cluster: and blocked_by:)
from standard_streams import ensure_utf8  # noqa: E402

WAVE_TYPES = ("wave", "campaign")
# Consecutive meter units of one account that ended without a reading before the meter counts as UNREADABLE (kb/Work
# PB2665): three is the breaker's own count, and the owner is told once, at the third.
METER_ATTEMPTS = 3


def git(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                          errors="replace")


def meter_age_hours(cdir: pathlib.Path, now: dt.datetime, acct_name: str) -> float | None:
    """Hours since the account's newest reading (None: it has none). Another account's reading is not this meter."""
    readings = [r for r in coord.read_json(cdir / "readings.json", []) if r.get("account") == acct_name]
    if not readings:
        return None
    newest = max(dt.datetime.fromisoformat(r["noted_at"].replace("Z", "+00:00")) for r in readings)
    return (now - newest).total_seconds() / 3600


def repo_state(repo: pathlib.Path) -> str:
    dirty = coord.porcelain_work(git(repo, "status", "--porcelain").stdout.splitlines())
    if dirty:
        return f"{len(dirty)} uncommitted path(s) in {repo}"
    ahead = git(repo, "rev-list", "--count", "@{u}..HEAD").stdout.strip()
    if ahead and ahead != "0":
        return f"{ahead} unpushed commit(s) in {repo}"
    return ""


def fresh_unlanded(repo: pathlib.Path, since: dt.datetime) -> list[str]:
    import prune_worktrees  # noqa: PLC0415  (classifies against origin/main by content; no fetch here)
    fresh = []
    for b in git(repo, "branch", "--format=%(refname:short) %(committerdate:iso-strict)").stdout.splitlines():
        name, _, when = b.partition(" ")
        if name != "main" and when and dt.datetime.fromisoformat(when) > since:
            fresh.append(name)
    if not fresh:
        return []
    # the repository it listed, never prune_worktrees' default (kb/Work PB2590: a supervisor under test reads a scratch
    # one); ONE graph for every fresh branch, never a merge-base per branch (kb/Work PB3582)
    names, graph = prune_worktrees.main_names(repo), prune_worktrees.branch_graph(fresh, repo)
    return [b for b in fresh if prune_worktrees.classify(b, names, graph, repo)[0] == "UNLANDED"]


def unit_history(cdir: pathlib.Path) -> list[dict]:
    """units.jsonl, the supervisor's own record of every unit it ran, oldest first (an unreadable line is skipped)."""
    path = cdir / "units.jsonl"
    if not path.is_file():
        return []
    out = []
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        try:
            rec = json.loads(line)
        except ValueError:
            continue
        if isinstance(rec, dict):
            out.append(rec)
    return out


def ran_campaign(rec: dict, lead: str) -> bool:
    """A campaign unit of `lead`. A line written before the supervisor named the cluster ran the one lead it had."""
    return rec.get("unit") == "campaign" and rec.get("cluster", lead) == lead


def attempted_at(rec: dict) -> dt.datetime | None:
    """When a units.jsonl line's unit ended (else started), offset-aware, or None when the line has no usable time."""
    for key in ("ended_at", "started_at"):
        try:
            when = dt.datetime.fromisoformat(str(rec[key]).replace("Z", "+00:00"))
        except (KeyError, ValueError):
            continue
        if when.tzinfo is not None:
            return when
    return None


def meter_failure_run(history: list[dict], acct_name: str) -> list[dict]:
    """The newest unbroken run of this account's meter units that ended without a reading, oldest first. Only meter
    units count, so a unit of another type between them neither extends nor breaks the run (kb/Work PB2665: a resume
    between two failed meters reset the supervisor's consecutive-failure count to zero, so a meter that failed on
    every cycle printed `FAILURE 1 of 3` for ever). The supervisor records `environment_failure` on the line of a
    unit that reads the environment (`$ReadOnlyUnit`) and ended without a `done` handoff, valid or not."""
    run: list[dict] = []
    for rec in reversed(history):
        if rec.get("unit") != "meter" or rec.get("account") != acct_name:
            continue
        if not rec.get("environment_failure"):
            break
        run.append(rec)
    return run[::-1]


def meter_unreadable(run: list[dict], now: dt.datetime, max_age_h: float) -> dict | None:
    """The gate on rule 3: None while the meter may be tried; else {failures, since, retry_at} once METER_ATTEMPTS
    attempts in a row failed and the newest is younger than the maximum age (the cadence of a healthy meter), so a
    page that cannot be read is retried once per maximum age and not once per loop iteration. `since` names the
    streak (its first attempt): the key under which the supervisor tells the owner exactly once."""
    if len(run) < METER_ATTEMPTS:
        return None
    first, last = attempted_at(run[0]), attempted_at(run[-1])
    if first is None or last is None:
        return None
    retry_at = last + dt.timedelta(hours=max_age_h)
    if now >= retry_at:
        return None
    return {"failures": len(run), "since": first.isoformat(), "retry_at": retry_at.isoformat()}


def lane_states(leads: list[str], work_dir: pathlib.Path) -> dict[str, dict]:
    """Each lead's lane, from work.py's cluster_order (the one reader of cluster: and blocked_by:)."""
    items = work.load(work_dir)
    lanes = {}
    for lead in leads:
        view = work.cluster_order(items, lead)
        ready = [n["id"] for n in view["notes"] if n["ready"]]
        state = ("unknown" if not view["named"] else "landed" if not view["notes"]
                 else "ready" if ready else "waiting")
        lanes[lead] = {"state": state, "ready": ready, "notes": view["notes"]}
    return lanes


def started_at(rec: dict) -> dt.datetime | None:
    """A units.jsonl line's start, offset-aware, or None when it has none the walk can trust."""
    try:
        when = dt.datetime.fromisoformat(str(rec["started_at"]).replace("Z", "+00:00"))
    except (KeyError, ValueError):
        return None
    return when if when.tzinfo is not None else None


def starved(lanes: dict[str, dict], history: list[dict], now: dt.datetime) -> list[dict]:
    """The leads whose ready note has waited through MORE THAN ONE wave-type unit since their own last campaign wave
    (kb/Work PB2522: PB2151 waited almost nine hours through eight units, three of them waves, and nothing said so).
    Measured from the per-choice lane state the supervisor records in units.jsonl (`campaigns`): the walk back from
    the newest unit stops at the lead's
    own campaign unit or at a line that did not record the lead ready, so a gap in the record under-reports, never
    over-reports. `hours` runs from the oldest unit of that unbroken run to now."""
    out = []
    for lead, lane in lanes.items():
        if lane["state"] != "ready":
            continue
        since, waves = None, 0
        for rec in reversed(history):
            if ran_campaign(rec, lead) or (rec.get("campaigns") or {}).get(lead) != "ready":
                break
            when = started_at(rec)
            if when is None:
                break
            since = when
            waves += rec.get("unit") in WAVE_TYPES
        if waves > 1 and since is not None:
            last = next((r.get("started_at") for r in reversed(history) if ran_campaign(r, lead)), None)
            out.append({"cluster": lead, "hours": round((now - since).total_seconds() / 3600, 1), "waves": waves,
                        "last_campaign_at": last})
    return out


def enqueue_chains(history: list[dict]) -> list[dict]:
    """The drift measure of kb/Work PB4180: every run of two or more consecutive `enqueue` units in units.jsonl, oldest
    first, each {units, started_at, hours (the units' own durations summed), reasons}. A `meter` unit between two is
    invisible (read-only: it is no part of the work chain, section 4.5), any other unit ends the run. A chain is the
    footprint of a handoff that named `enqueue` for work the lanes own; the choice above no longer starts one, so a
    chain that starts after the fix is a regression."""
    chains: list[list[dict]] = []
    run: list[dict] = []
    for rec in [*history, {"unit": None}]:
        if rec.get("unit") == "meter":
            continue
        if rec.get("unit") == "enqueue":
            run.append(rec)
            continue
        if len(run) > 1:
            chains.append(run)
        run = []
    return [{"units": len(r), "started_at": r[0].get("started_at"),
             "hours": round(sum(float(u.get("duration_s") or 0) for u in r) / 3600, 2),
             "reasons": [u.get("reason", "") for u in r]} for r in chains]


def lane_why(lead: str, lane: dict) -> str:
    if lane["state"] == "unknown":
        return f"no kb/Work note names the cluster {lead}"
    if lane["state"] == "landed":
        return f"the cluster {lead} is landed: the campaign lane ends"
    return f"campaign {lead}: no ready note (" + "; ".join(
        f"{n['id']} waits on {', '.join(n['waiting_on'])}" for n in lane["notes"][:4]) + ")"


def campaign_choice(lanes: dict[str, dict], history: list[dict], named_reason: str | None) -> dict[str, str]:
    """The campaign rule: the lane a wave-type unit runs. `named_reason` is the handoff's reason when the last handoff
    named the wave, else None; the alternation is the supervisor's rule, not the model's (kb/Work PB2522), so the
    handoff's reason is kept in the log and decides nothing."""
    named = "" if named_reason is None else f"named by the last handoff: {named_reason}; "
    states = [lane["state"] for lane in lanes.values()]
    ready = [lead for lead, lane in lanes.items() if lane["state"] == "ready"]
    if not ready:
        overall = ("waiting" if "waiting" in states else "landed" if all(s == "landed" for s in states)
                   else "unknown")
        return {"unit": "wave", "reason": named + "; ".join(lane_why(k, v) for k, v in lanes.items()),
                "campaign": overall}
    wave_types = [r for r in history if r.get("unit") in WAVE_TYPES]
    if wave_types and wave_types[-1].get("unit") == "campaign":
        return {"unit": "wave", "reason": named + f"a fix-lane wave between campaign waves ({', '.join(ready)} ready)",
                "campaign": "between"}
    # Round-robin: the ready lead whose own last campaign unit is oldest (never run = oldest), ties in -Cluster order.
    last_run = {lead: max((i for i, r in enumerate(history) if ran_campaign(r, lead)), default=-1) for lead in ready}
    lead = min(ready, key=lambda k: (last_run[k], ready.index(k)))
    ids = lanes[lead]["ready"]
    handoff = "" if named_reason is None else f"; the handoff named wave: {named_reason}"
    return {"unit": "campaign", "cluster": lead,
            "reason": f"campaign {lead}: {len(ids)} ready note(s): {', '.join(ids[:6])}{handoff}", "campaign": "run"}


def meter_due(cdir: pathlib.Path, history: list[dict], now: dt.datetime, acct_name: str,
              max_age_h: float) -> tuple[str | None, dict | None]:
    """Rule 3's facts: (the reason a meter unit is due, else None; the unreadable gate when it is due but closed)."""
    age = meter_age_hours(cdir, now, acct_name)
    if age is not None and age <= max_age_h:
        return None, None
    reason = f"no {acct_name} meter reading" if age is None else f"{acct_name} meter reading {age:.1f} h old"
    gate = meter_unreadable(meter_failure_run(history, acct_name), now, max_age_h)
    return (None, gate) if gate else (reason, None)


def base_choice(h: dict, cdir: pathlib.Path, repo: pathlib.Path, now: dt.datetime, last_failed: bool,
                last_started: dt.datetime | None, meter_reason: str | None) -> dict[str, str]:
    """Rules 2 to 7: the unit, before the campaign rule decides the lane of a wave-type one. `meter_reason` is rule
    3's verdict from `meter_due`: the reason a meter unit is due, else None (fresh, or due but unreadable).

    A handoff's `enqueue` is a REQUEST, not an order (kb/Work PB4180): `enqueue` is the one unit whose work the lanes
    own (the landing lane lands and the review lane reviews, beside whatever unit runs), so rule 2 honors the name only
    while `enqueue_work` (rule 6's one reader) finds work no lane owns. Otherwise the choice falls through to rules 3
    to 7 and says so: a handoff that names `enqueue` to wait for a merge no longer chains one enqueue unit after
    another while the waves and campaigns wait (28 chains, 75 units, 49.6 hours, 2026-10-01 to 2026-10-30)."""
    named = h.get("next_unit")
    if named and named != "enqueue":
        return {"unit": named, "reason": "named by the last handoff: " + h.get("next_unit_reason", "")}
    landing = enqueue_work(h, cdir)
    if named and landing:
        return {"unit": "enqueue", "reason": "named by the last handoff: " + h.get("next_unit_reason", "") + "; "
                                             + landing["reason"]}
    choice = after_handoff(h, repo, last_failed, last_started, meter_reason, landing)
    if named:
        choice["reason"] = ("the last handoff named enqueue for work the lanes own (a merge to wait for, a branch to "
                            "land or review), so no enqueue unit runs; " + choice["reason"])
    return choice


def after_handoff(h: dict, repo: pathlib.Path, last_failed: bool, last_started: dt.datetime | None,
                  meter_reason: str | None, landing: dict[str, str] | None) -> dict[str, str]:
    """Rules 3 to 7; `landing` is rule 6's verdict from `enqueue_work`."""
    if meter_reason:
        return {"unit": "meter", "reason": meter_reason}
    state = repo_state(repo)
    if state:
        return {"unit": "resume", "reason": state}
    if last_failed or h.get("outcome") in ("split", "failed"):
        return {"unit": "resume", "reason": "the last unit failed or ended split"}
    if last_started:
        fresh = fresh_unlanded(repo, last_started)
        if fresh:
            return {"unit": "resume", "reason": "unlanded branches from the last unit: " + ", ".join(fresh)}
    return landing or {"unit": "wave", "reason": "nothing pending"}


def enqueue_work(h: dict, cdir: pathlib.Path) -> dict[str, str] | None:
    """Rule 6: an `enqueue` unit only for what its agents (or its clerk and records) must do — the landing lane lands
    the branches WAITING TO LAND itself (land_lane.agent_work, kb/Work PB3103), and the review lane reviews the branches
    WAITING FOR REVIEW and finishes the CONFLICT stops (review_lane.agent_work: only its holds, or its own work while it
    is not ticking, reach the unit, kb/Work PB3608). DONE branches in a handoff start one
    only while the lane has no survey younger than land_lane.SURVEY_MAX_AGE_MIN: the lane is not ticking, and the
    enqueue unit's step 3 ticks it."""
    import land_lane  # noqa: PLC0415  (it imports enqueue.py, gate_slot and ruleset: only rule 6 needs them)
    import review_lane  # noqa: PLC0415  (and the review lane's holds: what its agents could not do, kb/Work PB3608)
    work = land_lane.agent_work(cdir)
    reasons = [*work.reasons, *review_lane.agent_work(cdir)]
    if reasons:
        return {"unit": "enqueue", "reason": "agent work for the enqueue unit: " + "; ".join(reasons)}
    done = [b["branch"] for b in h.get("branches_pending", []) if b.get("status") == "DONE"]
    if done and (work.survey_age_min is None or work.survey_age_min > land_lane.SURVEY_MAX_AGE_MIN):
        return {"unit": "enqueue", "reason": f"finished branches waiting: {', '.join(done)}; the landing lane has no "
                                             f"survey in {land_lane.SURVEY_MAX_AGE_MIN} min (the enqueue unit ticks it)"}
    return None


def choose(handoff: dict | None, cdir: pathlib.Path, repo: pathlib.Path, now: dt.datetime, last_failed: bool,
           last_started: dt.datetime | None, max_meter_age_h: float, acct_name: str,
           clusters: list[str] | None = None, work_dir: pathlib.Path = work.WORK,
           operator: dict | None = None) -> dict:
    h = handoff or {}
    history = unit_history(cdir)
    meter_reason, unreadable = meter_due(cdir, history, now, acct_name, max_meter_age_h)
    if operator and operator.get("next_unit"):
        # Rule 0: the operator's steer names the unit itself, so neither rules 1-7 nor the campaign rule re-decide it.
        choice = {"unit": operator["next_unit"], "reason": "the operator: " + operator.get("next_unit_reason", ""),
                  "operator": True}
        if choice["unit"] == "campaign" and clusters:
            choice["cluster"] = clusters[0]  # a steer names no lead: it runs the first (`-Unit campaign` is a steer)
    elif h.get("owner_question"):
        return {"unit": "owner-question", "reason": "the last handoff asks the owner"}
    else:
        choice = base_choice(h, cdir, repo, now, last_failed, last_started, meter_reason)
    if unreadable:
        choice["meter_unreadable"] = unreadable
    if not clusters:
        return choice
    lanes = lane_states(clusters, work_dir)
    if choice["unit"] in WAVE_TYPES and not choice.get("operator"):
        # The handoff named THIS wave only when its next_unit is wave-type.
        named = h.get("next_unit_reason", "") if h.get("next_unit") in WAVE_TYPES else None
        choice = campaign_choice(lanes, history, named)
    choice["campaigns"] = {lead: lane["state"] for lead, lane in lanes.items()}
    choice["starved"] = starved(lanes, history, now)
    return choice


def read_handoff(path: str | None) -> dict | None:
    """A handoff file's JSON, or None when it is not named, absent or unreadable."""
    if not path:
        return None
    try:
        return json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--operator-handoff", help="the operator's steer, handoff.operator.json, already validated by the "
                    "supervisor against handoff.schema.json (absent or unreadable = none)")
    ap.add_argument("--handoff", help="the last unit's handoff.json (absent or unreadable = none)")
    ap.add_argument("--last-failed", action="store_true")
    ap.add_argument("--last-started", help="ISO time the last unit started")
    ap.add_argument("--repo", default=str(coord.REPO))
    ap.add_argument("--coord")
    ap.add_argument("--now")
    ap.add_argument("--cluster", action="append", default=[],
                    help="a campaign lane's lead (repeatable, in the supervisor's -Cluster order)")
    ap.add_argument("--work", default=str(work.WORK), help="the register directory (a test seam)")
    ap.add_argument("--enqueue-chains", action="store_true",
                    help="print the runs of consecutive enqueue units in units.jsonl as JSON (kb/Work PB4180) and exit")
    a = ap.parse_args(argv)
    if a.enqueue_chains:
        print(json.dumps(enqueue_chains(unit_history(coord.coord_dir(a.coord)))))
        return 0
    handoff, operator = read_handoff(a.handoff), read_handoff(a.operator_handoff)
    now = dt.datetime.fromisoformat(a.now) if a.now else dt.datetime.now(dt.timezone.utc)
    started = dt.datetime.fromisoformat(a.last_started) if a.last_started else None
    acct = account.current()
    print(json.dumps(choose(handoff, coord.coord_dir(a.coord), pathlib.Path(a.repo), now, a.last_failed, started,
                            acct.quota_at(now)["meter_max_age_hours"], acct.name, a.cluster, pathlib.Path(a.work), operator)))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
