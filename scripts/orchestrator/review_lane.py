#!/usr/bin/env python3
"""review_lane.py — THE LOOP'S REVIEW LANE: every branch the survey lists WAITING FOR REVIEW gets ONE read-only
`cobol-reviewer` in its own headless session, and every CONFLICT stop gets ONE `cobol-finisher`, the moment each is
ready and while whatever unit runs keeps running (kb/Work PB3608; DESIGN-orchestrator-loop.md section 4.9.1). The
sibling of land_lane.py (the landing lane) and built the same way: deterministic, state on disk, a detached runner per
agent. The lane DISPATCHES agents; it decides nothing a script cannot.

    python scripts/orchestrator/review_lane.py tick [--repo DIR] [--claude EXE] [--max-agents N] ...
    python scripts/orchestrator/review_lane.py status [--json]      one line per run and per hold
    python scripts/orchestrator/review_lane.py collect <branch> ... the enqueue unit has handled these holds
    python scripts/orchestrator/review_lane.py run <run record>     (the detached runner `tick` starts; not by hand)
    python scripts/orchestrator/review_lane.py --self-test

⛔ WHY (kb/Work PB3608, the owner's question 2026-10-23 ~16:00 PDT, "We need to get pending branches to main. How do we
do that efficiently?"). A branch DONE outside a rolling wave (an attended branch, a finisher's new tip, a lane's notes
branch) was reviewed only when the loop's next `enqueue` unit ran, and units run one at a time: that day's ran 3,308 s,
4,579 s and over two hours. `fix-self-test-wall-leads` was DONE at ~14:00 and still WAITING FOR REVIEW at 16:00, until
the operator dispatched its reviewer by hand (CLEAN in 187 s). The landing lane already starts every WAITING TO LAND
branch within its tick; this lane does the same for the two steps before it that need a model. The enqueue unit keeps
the finishers of every other stop, the leads and the records; it no longer starts reviews or CONFLICT finishers.

THE TICK is chained after the landing lane's (orchestrate.ps1 starts it when a land tick completes), because it reads
the survey that tick wrote (`<coord>/land-lane/survey.json`; a survey older than land_lane.SURVEY_MAX_AGE_MIN launches nothing)
and no second survey is taken. In order:
  1. REAP: a run whose runner is gone without an rc is DIED. Every finished run is JUDGED once: a CONFLICT finisher
     that ended with its branch's head unchanged is a HOLD (the enqueue unit decides: a stop resolved with no commit is
     re-armed, `land_lane.py rearm`); a review that ended with no verdict at its head just counts toward ATTEMPT_LIMIT.
  2. Stops and the budget: under the loop's STOP, its fleet stop or the owner's global stop (land_lane.loop_stop), or when
     budget.py's decision is not `go`, no agent starts (a running one is stopped by its own runner on a stop file, or
     finishes). The landing lane spends no tokens and ticks through a budget hold; this lane spends the loop's.
  3. LAUNCH, at most MAX_AGENTS runs alive at once (reviewers and finishers share the cap), the CONFLICT finishers first
     (a stopped landing, already past its gate and review, costs most to leave), then the reviews in survey order. Each
     candidate is decided by `plan_review` / `plan_conflict`, from the disk: the survey row, this lane's runs, and every
     findings file that names the branch (the lease below). The tip is read again just before the launch; a branch that
     moved since the survey is left for the next tick.

THE LEASE, per branch HEAD (review_findings.py is the one reader of the findings files, `rounds`):
    a run of this lane alive at the head                          wait
    any findings file with a VERDICT at the head                 reviewed: its reviewer stamps the report (nothing to
                                                                 start); a verdict older than REPORT_GRACE_MIN whose stamp
                                                                 never came is STAMPED BY THE LANE, whichever file holds it
    the report younger than REPORT_GRACE_MIN                     wait: a wave's reviewer may be about to write its `start`
    a foreign `start` at the head, no verdict, younger than the  wait: a rolling wave's reviewer (the survey cannot tell it
    reviewer ceiling (REVIEWER_CEILING_MIN)                      from a branch waiting for one, kb/Work PB3283)
    the lane's own unfinished round, no live runner              START THE ROUND AGAIN (a killed reviewer: its recorded
                                                                 findings stand, `review_findings.py start` allows it)
    an UNCOLLECTED hold at the head                              wait for the enqueue unit's `collect`: after a torn-file
                                                                 hold its hand-dispatched reviewer records in the lane's own
                                                                 file, where no lease is granted (PB4049)
    ATTEMPT_LIMIT runs ended with no verdict at the head         HOLD (the enqueue unit's; the operator is told once)
RESTART-SAFE: the lane's own findings file is `<coord>/scratch/review-<branch slug>.jsonl`. A reviewer killed mid-append
leaves it torn at its last line; the tick mends exactly that (`review_findings.repair_interrupted_append`) and starts the
round again, through `last_verdict`'s own reading rules: a torn file is never read for a verdict. Any other tear is a
HOLD for the operator. Nothing is repaired by hand.

ROUNDS (the review skill's branch form): round 1 reviews the whole branch (origin/main...head); a later round (a finisher
committed on a branch the lane reviewed) reviews the diff since the head the previous round reviewed. The blocking
severities are `critical,high,medium` for rounds 1..REVIEW_FIX_ROUNDS and `critical,high` after (review_findings.
BLOCKING_DEFAULT / BLOCKING_AT_CAP: a finding that cannot be answered any more becomes a lead).

THE CONFLICT FINISHER (kb/Work PB3640, PB3670 item 4): an exit-4 landing attempt, or a pull request the queue BOUNCED as
DIRTY, gets ONE `cobol-finisher` per head whose prompt carries `land_lane.py conflict-brief <branch>` verbatim. The lane
COLLECTS the exit-4 attempt when it starts the finisher (the unit need not dispatch it). The finisher's commits are
unreviewed: the branch reads WAITING FOR REVIEW and this lane reviews the new tip, then the landing lane lands it.

THE RUNNER (`run`, detached through scripts/detached.py, the one launch that outlives its caller) starts
`claude -p --agent <role>` in the main checkout with the prompt on stdin and its stream-json in the run's log, under the
loop's account (the supervisor exports it to this tick: CLAUDE_CONFIG_DIR is inherited), and ends it on the role's
CEILING (a STALLED review is read as no verdict) or on a loop stop (STOPPED). Then it records rc, the end head and, for
a review, the verdict the findings file holds.

STATE, under `<coord>/review-lane/`:
  runs/<id>.json    one per agent, <id> = `<n>-<UTC stamp>-<kind>-<slug>` (n: the NUMBER, one past every run, taken under
                    the tick lock; land_lane.attempt_order orders it): kind, branch, head, worktree, report, round,
                    prev_head, findings, argv, log, prompt, pid; at its end rc, ended, outcome (DIED, STALLED, STOPPED),
                    end_head, verdict. Written through `land_lane.update` (the one writer of a lane record).
  prompts/<id>.md   the prompt the agent was given (the forensic copy)
  holds/<slug>.json what only the enqueue unit or the operator can resolve: branch, head, why, at, collected
  last-tick.json    when the lane last ticked (`status` says a lane that is not ticking, the enqueue unit ticks it once)
  tick.lock         an OS lock: one tick at a time
`agent_work` is next_unit.py's reader: the enqueue unit starts for a hold, or for review-lane work while the lane is not
ticking.

Exit: 0 done (a tick already running included) · 1 a launch failed (printed; the supervisor never treats it as fatal) ·
2 usage.
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
import uuid
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402
import land_lane  # noqa: E402  (the lane record's one writer, its stops, its survey, its attempts)
import review_findings  # noqa: E402  (the one reader of the findings files)
from detached import kill_tree, spawn_detached  # noqa: E402  (THE one way a process outlives its caller, kb/Work PB3536)
from gate_slot import ExclusiveLock  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

LANE_DIR = "review-lane"
RUNS = "runs"
PROMPTS = "prompts"
HOLDS = "holds"
TICK_LOCK = "tick.lock"
LAST_TICK = "last-tick.json"
REVIEW, CONFLICT = "review", "conflict"
#: The agent each kind runs: `.claude/agents/<role>.md` (its model, effort, hooks and turn cap are the role's own).
ROLE = {REVIEW: "cobol-reviewer", CONFLICT: "cobol-finisher"}
#: Concurrent agents (reviewers and finishers together). The landing lane's gates are capped by the gate slot; the agents
#: are capped here, and each one's tokens are the loop's (budget.py decides before any starts).
MAX_AGENTS = 3
#: The wave's own stall rules (wf_rolling_wave.js: reviewer_ceiling_min 90, implementer_ceiling_min 240): a review that
#: has run this long is STALLED, and so a foreign `start` that old is a dead reviewer's, not a running one's.
REVIEWER_CEILING_MIN = 90
FINISHER_CEILING_MIN = 240
CEILING_MIN = {REVIEW: REVIEWER_CEILING_MIN, CONFLICT: FINISHER_CEILING_MIN}
#: A report this young may belong to a rolling wave whose reviewer has not started (module doc, THE LEASE): the lane waits.
REPORT_GRACE_MIN = 5
#: Reviews that may END WITH NO VERDICT at one head before the lane stops trying (a HOLD). A review the loop's own stop
#: ended (STOPPED) is not one: it was cut off from outside, and the next tick starts the round again.
ATTEMPT_LIMIT = 2
#: Review finisher rounds the rolling wave allows (review_fix_rounds): rounds above it block on BLOCKING_AT_CAP.
REVIEW_FIX_ROUNDS = 2
#: The lane has not ticked in this long: `agent_work` hands its work to the enqueue unit (which ticks it once).
LANE_STALE_MIN = land_lane.SURVEY_MAX_AGE_MIN
POLL_S = 2.0   # the runner's look at its agent

Say = Callable[[str], None]


# ── the state ──────────────────────────────────────────────────────────────────────────────────────────────────────
def lane_dir(cdir: pathlib.Path) -> pathlib.Path:
    return cdir / LANE_DIR


def findings_path(cdir: pathlib.Path, branch: str) -> pathlib.Path:
    """The lane's own findings file for a branch: `review-<branch slug>.jsonl` in the coordination scratch, the name
    `review_findings.NAMED_FINDINGS` reads back to its branch (units/enqueue.md's own convention)."""
    return cdir / "scratch" / f"review-{land_lane.slug(branch)}.jsonl"


def runs(cdir: pathlib.Path, unreadable: list[str] | None = None) -> list[dict[str, Any]]:
    """Every run, oldest first. A file that is not a run record is left out and its path appended to `unreadable`."""
    d = lane_dir(cdir) / RUNS
    out = []
    for p in sorted(d.glob("*.json"), key=land_lane.attempt_order) if d.is_dir() else []:
        try:
            rec = coord.read_json(p, None)
        except ValueError:
            rec = None
        if isinstance(rec, dict) and isinstance(rec.get("branch"), str) and rec.get("id") and rec.get("kind") in ROLE:
            rec["_path"] = str(p)
            out.append(rec)
        elif unreadable is not None:
            unreadable.append(str(p))
    return out


def holds(cdir: pathlib.Path) -> list[dict[str, Any]]:
    d = lane_dir(cdir) / HOLDS
    out = []
    for p in sorted(d.glob("*.json")) if d.is_dir() else []:
        try:
            rec = coord.read_json(p, None)
        except ValueError:
            rec = None
        if isinstance(rec, dict) and rec.get("branch"):
            rec["_path"] = str(p)
            out.append(rec)
    return out


def finished(r: dict[str, Any]) -> bool:
    return "rc" in r or r.get("outcome") == "DIED"


def mine(rs: list[dict[str, Any]], branch: str, kind: str) -> list[dict[str, Any]]:
    return [r for r in rs if r["branch"] == branch and r["kind"] == kind]


def same_commit(a: str | None, b: str | None) -> bool:
    """Two spellings of one commit: the findings files record the head a reviewer was given, full or abbreviated."""
    return bool(a) and bool(b) and (a.startswith(b) or b.startswith(a))


def minutes_since(iso: str, now: dt.datetime) -> float:
    return (now - dt.datetime.fromisoformat(iso)).total_seconds() / 60


# ── the decisions ──────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass(frozen=True)
class Plan:
    launch: bool
    why: str
    round: int = 0
    prev_head: str | None = None
    hold: bool = False   # not a wait: only the enqueue unit or the operator resolves it
    stamp: pathlib.Path | None = None   # a findings file holds the verdict at the head, the report lacks its stamp


def blocking_for(rnd: int) -> str:
    return ",".join(review_findings.BLOCKING_DEFAULT if rnd <= REVIEW_FIX_ROUNDS else review_findings.BLOCKING_AT_CAP)


def standing(row: dict[str, Any], cdir: pathlib.Path, now: dt.datetime, own: pathlib.Path) -> Plan | None:
    """Is a review of the row's tip already done or being done (THE LEASE, module doc)? Reads every findings file that
    names the branch, the lane's own and a rolling wave's alike. The lane's own unfinished round is NOT a lease (the
    caller found no live runner for it): it is the round to start again. A torn file is not read (the survey holds the
    branch it names)."""
    for path in review_findings.findings_files(cdir / "scratch"):
        rs = review_findings.rounds(path, row["name"])
        for x in rs or []:
            if not same_commit(x.head, row["tip"]):
                continue
            if x.verdict:
                # The survey reads only the report's stamp, so a verdict whose stamp never came (the reviewer was killed
                # between the two, or a wave stopped before its step 5) is stamped by the lane, from any file, once it is
                # older than the grace in which its own reviewer stamps it (R1-2). `stamp` writes the file's NEWEST verdict:
                # only the file's newest round may be stamped.
                if x.round != max(y.round for y in rs if y.verdict):
                    continue
                if minutes_since(x.ended, now) < REPORT_GRACE_MIN:
                    return Plan(False, f"reviewed: {path.name} round {x.round} has its {x.verdict} at this head, "
                                       f"{max(minutes_since(x.ended, now), 0):.0f} min ago; its reviewer stamps it")
                return Plan(False, f"reviewed: {path.name} round {x.round} has its {x.verdict} at this head, unstamped", stamp=path)
            if os.path.normcase(str(path)) != os.path.normcase(str(own)) \
                    and minutes_since(x.started, now) < REVIEWER_CEILING_MIN:
                return Plan(False, f"a reviewer ({x.reviewer}, {path.name} round {x.round}) started "
                                   f"{minutes_since(x.started, now):.0f} min ago and has no verdict yet")
    return None


def plan_review(row: dict[str, Any], cdir: pathlib.Path, rs: list[dict[str, Any]], now: dt.datetime,
                say: Say) -> Plan:
    """What the lane does about a branch WAITING FOR REVIEW at `row['tip']` (module doc: THE LEASE, ROUNDS)."""
    tip, branch = row["tip"], row["name"]
    ours = mine(rs, branch, REVIEW)
    if any(not finished(r) and same_commit(r["head"], tip) for r in ours):
        return Plan(False, "its reviewer is running")
    own = findings_path(cdir, branch)
    other = standing(row, cdir, now, own)
    if other:
        return other
    # An uncollected hold at the head is the enqueue unit's: it dispatches a reviewer by hand into the hold's own findings
    # file, which for a torn-file hold is the lane's OWN file (so `standing` leases nothing there, and once the operator
    # mends it the unit's open round would read as a killed round of the lane's: a second reviewer of the same round,
    # PB4049). The lane waits for `collect`, whatever the hold's reason.
    open_hold = next((h for h in holds(cdir) if h["branch"] == branch and same_commit(h.get("head"), tip)
                      and not h.get("collected")), None)
    if open_hold:
        return Plan(False, f"held at this head until the enqueue unit collects it ({open_hold['why']})", hold=True)
    # A rolling wave's implementer has just returned DONE and its reviewer has not written its `start` yet: nothing on disk
    # says so (kb/Work PB3283), but its report is minutes old. A finisher's, an attended or a lane's branch waits this long too.
    try:
        age = now.timestamp() / 60 - os.stat(row["report"]).st_mtime / 60
    except OSError:
        age = None
    if age is not None and age < REPORT_GRACE_MIN:
        return Plan(False, f"its report was written {max(age, 0):.0f} min ago: a rolling wave's reviewer may be about to start "
                           f"(the lane waits {REPORT_GRACE_MIN} min)")
    # A reviewer ended by a loop stop (STOPPED) failed at nothing: the stop restarts the round, however often it came (R1-3).
    failed = [r for r in ours if finished(r) and same_commit(r["head"], tip) and not r.get("verdict")
              and r.get("outcome") != "STOPPED"]
    if len(failed) >= ATTEMPT_LIMIT:
        return Plan(False, f"{len(failed)} reviewers ended with no verdict at {tip[:12]} ({failed[-1].get('outcome') or 'exit ' + str(failed[-1].get('rc'))})",
                    hold=True)
    rounds = review_findings.rounds(own, branch)
    if rounds is None:
        mended = review_findings.repair_interrupted_append(own)
        if mended is None:
            return Plan(False, f"{own} is torn other than by a cut-off append: only the operator mends it "
                               f"({review_findings.remedy(own, None)})", hold=True)
        say(f"review lane: {branch}: {own.name} was cut off mid-append by a killed reviewer; mended ({'; '.join(mended)})")
        rounds = review_findings.rounds(own, branch) or []
    done = [x for x in rounds if x.verdict]
    open_ = [x for x in rounds if not x.verdict]
    rnd = open_[-1].round if open_ else (done[-1].round + 1 if done else 1)
    prior = [x for x in done if x.round < rnd]
    prev = prior[-1].head if prior else None
    why = (f"round {rnd} again: its reviewer was killed before a verdict" if open_ else
           f"round {rnd}: the branch moved past the head round {done[-1].round} reviewed" if done else "round 1")
    return Plan(True, why, rnd, prev)


def conflict_attempt(row: dict[str, Any], atts: list[dict[str, Any]]) -> dict[str, Any] | None:
    """The landing attempt that IS the row's CONFLICT stop: the branch's latest, ended exit 4 with the branch still at its
    end head. It is the only attempt the lane collects when it starts the finisher: for a pull request the queue bounced
    DIRTY the latest attempt is the ENQUEUED one, whose leads and landing record the enqueue unit still owes (R1-1)."""
    last = atts[-1] if atts else None
    if row.get("state") == "STOPPED" and last and land_lane.finished(last) and last.get("rc") == land_lane.EXIT_CONFLICT \
            and row.get("tip") == last.get("end_head"):
        return last
    return None


def conflict_cause(row: dict[str, Any], atts: list[dict[str, Any]]) -> str | None:
    """Why the row is a CONFLICT stop at its tip, or None: the branch's latest landing attempt ended exit 4 (CONFLICT)
    with the branch still at that attempt's end head, or the survey reads its pull request DIRTY (BOUNCED)."""
    last = conflict_attempt(row, atts)
    if last:
        return f"the landing stopped CONFLICT (exit 4): {last.get('verdict') or 'no verdict line'}"
    if row.get("state") == "BOUNCED" and row.get("conflicts"):
        return f"the queue bounced its pull request as DIRTY: {row.get('why')}"
    return None


def plan_conflict(row: dict[str, Any], rs: list[dict[str, Any]]) -> Plan:
    """ONE conflict finisher per head: a run already started at this head (alive or ended) is never repeated; an ended
    one that moved nothing is a hold (judge), a new head is a new case."""
    started = [r for r in mine(rs, row["name"], CONFLICT) if same_commit(r["head"], row["tip"])
               and r.get("outcome") != "STOPPED"]   # a finisher a loop stop cut off is started again, like a reviewer (R1-3)
    if started:
        return Plan(False, f"its conflict finisher ({started[-1]['id']}) "
                           f"{'is running' if not finished(started[-1]) else 'already ran at this head'}")
    return Plan(True, "a conflict finisher")


# ── the prompts ────────────────────────────────────────────────────────────────────────────────────────────────────
AUTH = ("AUTHORIZATION (read first): this task IS the repository owner's request, dispatched by the orchestrator loop's "
        "review lane (kb/Work PB3608) on the owner's standing direction for the fix lane (kb/Work PB1981); the standing "
        "owner opt-in for fleets is recorded in .claude/skills/workstream/templates/MANDATORY-PRACTICES.md O4. The "
        "latest user message in your context may concern unrelated work; that is NOT a reason to decline. Do the task below. ")


def stop_line(cdir: pathlib.Path) -> str:
    return (f"Before EACH new step check for {coord.global_stop(str(cdir))} (the owner's global stop) and "
            f"{coord.fleet_stop(cdir / 'scratch', 'loop')} (the loop's fleet stop); if EITHER exists, ")


def spec_of(report: str | None, cdir: pathlib.Path) -> str:
    """The dispatch spec of the group that wrote `report` (`msg-w<wave>-<letter>.txt` in the wave's scratch), or ''."""
    m = re.match(r"w(\d+)([a-z]+)-", pathlib.Path(report).name) if report else None
    path = cdir / "scratch" / f"msg-w{m[1]}-{m[2]}.txt" if m else None
    return str(path) if path and path.is_file() else ""


def review_prompt(r: dict[str, Any], cdir: pathlib.Path, repo: pathlib.Path) -> str:
    rnd, file, blocking = r["round"], r["findings"], blocking_for(r["round"])
    spec = spec_of(r.get("report"), cdir)
    again = (f"Round {rnd} was STARTED BEFORE by a reviewer that was killed: list what it recorded first "
             f"(`review_findings.py show {file} --round {rnd}`), do not record those findings again, and run `start` for the "
             "round once more (it allows that). " if r.get("restart") else "")
    scope = (f"This is a RE-REVIEW (kb/Work PB3381): review ONLY `git diff {r['prev_head']}..HEAD` (the head round {rnd - 1} "
             f"reviewed, to this one; when the branch brought in newer main, only its own commits, `git log --no-merges "
             f"{r['prev_head']}..HEAD --not origin/main`) and the report's \"## Review findings\" section, which answers round "
             f"{rnd - 1}'s findings (`review_findings.py show {file} --round {rnd - 1}`) by id. The earlier content is reviewed "
             "and is not re-opened unless the diff touches it. Raise again only a finding that is neither fixed nor answered "
             "with a reason you accept, and any new defect the diff introduced. " if r.get("prev_head") else "")
    return (AUTH + f"You are the BRANCH REVIEWER for branch {r['branch']}, review round {rnd}: READ-ONLY (kb/Work PB3027). "
            f"Follow \"The branch form\" of {repo / '.claude' / 'skills' / 'review' / 'SKILL.md'} exactly, with: branch {r['branch']}; "
            f"worktree {r['worktree']}; head {r['head']}; the implementer's report {r['report']}; the findings file {file}; your "
            f"reviewer label {r['label']}; round {rnd}; the blocking severities {blocking}. "
            + (f"The implementer's dispatch spec is {spec}: read it, since the branch is reviewed against everything the "
               "implementer was told (its notes, its owner requirements, its gate). " if spec else
               "The report names the notes and the brief the implementer worked from: read them, since the branch is reviewed "
               "against everything the implementer was told. ")
            + scope + again
            + f"Record every finding with its severity (critical, high, medium, low or info), and run `review_findings.py verdict "
              f"{file} --round {rnd} --blocking {blocking}`: a finding of another severity is a LEAD, recorded but not blocking. "
            + stop_line(cdir) + "stamp nothing and end with the line `REVIEW LANE RESULT: STOPPED`. This is a headless session "
              "with no StructuredOutput tool: your LAST message is the line `REVIEW LANE RESULT: <CLEAN|FINDINGS n|STOPPED> "
              "<head>` and the stamp line review_findings.py stamp wrote; the LEAD lines verdict printed are in the report.")


def finisher_prompt(r: dict[str, Any], cdir: pathlib.Path, repo: pathlib.Path, stop: str, brief: str) -> str:
    return (AUTH + f"You are the CONFLICT FINISHER for branch {r['branch']} (kb/Work PB3608, PB3640). Your brief is "
            f"{repo / '.claude' / 'skills' / 'workstream' / 'templates' / 'finisher-brief.md'}: read it WHOLE and follow it, "
            f"the CONFLICT stop first. Branch {r['branch']}; worktree {r['worktree']}; its report {r['report']}; the stop: "
            f"{stop} {stop_line(cdir)}checkpoint-commit, write STATUS.md NEXT and your report, and stop with SPLIT. "
            "This is a headless session with no StructuredOutput tool: your LAST message is the line `FINISHER RESULT: "
            "<DONE|SPLIT|BLOCKED|GATE-PENDING> <head> <gate verdict line>`, the same details in your report. "
            "THE CONFLICT BRIEF (`land_lane.py conflict-brief`, verbatim):\n\n" + brief)


def claude_argv(claude: list[str], kind: str, mode: str) -> list[str]:
    """The headless session of one agent: the role's own definition (`--agent`: model, effort, hooks, turn cap), the
    loop's permission mode and no prompts, stream-json in the run's log, a fresh session id; the prompt arrives on stdin."""
    return [*claude, "-p", "--agent", ROLE[kind], "--permission-mode", mode, "--permission-prompts", "none",
            "--output-format", "stream-json", "--verbose", "--session-id", str(uuid.uuid4())]


# ── the tick ───────────────────────────────────────────────────────────────────────────────────────────────────────
def hold(cdir: pathlib.Path, row: dict[str, Any], why: str, say: Say) -> None:
    """Record that only the enqueue unit or the operator resolves `row`'s branch at its tip (idempotent per head: the
    first record, and the unit's `collected`, stand); the operator is told once through the mailbox."""
    path = lane_dir(cdir) / HOLDS / f"{land_lane.slug(row['name'])}.json"
    old = coord.read_json(path, None)
    if isinstance(old, dict) and same_commit(old.get("head"), row["tip"]):
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    coord.write_json(path, {"branch": row["name"], "head": row["tip"], "why": why, "collected": False,
                            "findings": str(findings_path(cdir, row["name"])),   # a hand-dispatched reviewer records here (R1-5)
                            "at": land_lane.now_utc().isoformat(timespec="seconds")})
    say(f"review lane: {row['name']} @{row['tip'][:12]}: HOLD — {why}")
    land_lane.notify_operator(cdir, f"Review lane holds {row['name']}", f"{row['name']} at {row['tip']}: {why}",
                              str(path), say)


def judge(cdir: pathlib.Path, rs: list[dict[str, Any]], rows: dict[str, dict[str, Any]], say: Say) -> None:
    """Judge each finished run once. A CONFLICT finisher that ended with the head where it found it resolved nothing the
    lane can see: a hold (the enqueue unit re-arms a stop resolved with no commit, or files what it found)."""
    for r in rs:
        if not finished(r) or r.get("judged"):
            continue
        if r["kind"] == CONFLICT and same_commit(r.get("end_head"), r["head"]) and r["branch"] in rows \
                and r.get("outcome") != "STOPPED":
            hold(cdir, {"name": r["branch"], "tip": r["head"]},
                 f"the conflict finisher {r['id']} ended ({r.get('outcome') or 'exit ' + str(r.get('rc'))}) with the head "
                 f"unchanged: read its report; a stop it resolved with no commit is re-armed (`land_lane.py rearm`)", say)
        land_lane.update(r, {"judged": True})


def reap(rs: list[dict[str, Any]], alive: Callable[[dict[str, Any]], bool], tip: Callable[[str | None], str | None],
         say: Say) -> None:
    for r in rs:
        if finished(r) or alive(r):
            continue
        land_lane.update(r, lambda fresh: {} if "rc" in fresh else {
            "outcome": "DIED", "ended": land_lane.now_utc().isoformat(timespec="seconds"), "end_head": tip(r.get("worktree")),
            "verdict": verdict_of(r)})
        if r.get("outcome") == "DIED":
            say(f"review lane: {r['branch']}: the runner of {r['id']} is gone with no exit code (DIED; {r['log']})")


def verdict_of(r: dict[str, Any]) -> str | None:
    """What a review's findings file holds for the run's own round: CLEAN or FINDINGS, or None (no verdict)."""
    if r["kind"] != REVIEW:
        return None
    rs = review_findings.rounds(pathlib.Path(r["findings"]), r["branch"]) or []
    return next((x.verdict for x in rs if x.round == r["round"] and x.verdict), None)


def read_survey(cdir: pathlib.Path, now: dt.datetime) -> tuple[list[dict[str, Any]], float | None]:
    """(the landing lane's last survey rows, its age in minutes or None when it never surveyed)."""
    lane = land_lane.view(cdir)
    return lane.rows, lane.survey_age_min(now)


def budget_decision(borrow_days: int, telemetry_dir: str | None) -> str:
    import budget  # noqa: PLC0415  (it reads the telemetry sink: only a tick that would spend asks)
    return str(budget.current(borrow_days=borrow_days, **({"telemetry_dir": telemetry_dir} if telemetry_dir else {}))["decision"])


def launch(cdir: pathlib.Path, repo: pathlib.Path, kind: str, row: dict[str, Any], plan: Plan, prompt: Callable[[dict], str],
           argv: list[str], start: Callable[[list[str], pathlib.Path], int]) -> dict[str, Any]:
    """Start one agent. Only `tick` calls it, under the tick lock, so the run's NUMBER names one run."""
    now = land_lane.now_utc()
    d = lane_dir(cdir) / RUNS
    d.mkdir(parents=True, exist_ok=True)
    n = max((land_lane.attempt_order(p)[0] for p in d.glob("*.json")), default=0) + 1
    rid = f"{n:06d}-{now:%Y%m%dT%H%M%SZ}-{kind}-{land_lane.slug(row['name'])}"
    path = d / f"{rid}.json"
    log = cdir / "logs" / f"review-lane-{rid}.log"
    rec = {"id": rid, "kind": kind, "branch": row["name"], "head": row["tip"], "worktree": row["worktree"],
           "report": row.get("report"), "round": plan.round, "prev_head": plan.prev_head, "restart": "again" in plan.why,
           "findings": str(findings_path(cdir, row["name"])), "label": f"lane-{kind}-{land_lane.slug(row['name'])}-r{plan.round}-{n}",
           "why": plan.why, "argv": argv, "cwd": str(repo), "log": str(log), "prompt": str(lane_dir(cdir) / PROMPTS / f"{rid}.md"),
           "ceiling_min": CEILING_MIN[kind], "started": now.isoformat(timespec="seconds"), "pid": None, "collected": False}
    pr = pathlib.Path(rec["prompt"])
    pr.parent.mkdir(parents=True, exist_ok=True)
    pr.write_text(prompt(rec), encoding="utf-8", newline="\n")
    coord.write_json(path, rec)   # a new record: no other writer knows its path yet
    rec["_path"] = str(path)
    pid = start([sys.executable, str(pathlib.Path(__file__).resolve()), "run", str(path)], log)
    return land_lane.update(rec, {"pid": pid})


def conflict_brief_text(cdir: pathlib.Path, branch: str) -> str:
    lines: list[str] = []
    rc = land_lane.show_conflict_brief(cdir, branch, say=lines.append)
    return "\n".join(lines) if rc == 0 else f"(no brief: {' '.join(lines)}; compute it with `land_lane.py conflict-brief {branch}`)"


def tick(cdir: pathlib.Path, repo: pathlib.Path, *, claude: list[str] = ("claude",), mode: str = "bypassPermissions",
         max_agents: int = MAX_AGENTS, budget: Callable[[], str] = lambda: "go",
         start: Callable[[list[str], pathlib.Path], int] = spawn_detached,
         alive: Callable[[dict[str, Any]], bool] = land_lane.runner_alive,
         tip: Callable[[str | None], str | None] = land_lane.tip_of,
         argv_for: Callable[[str], list[str]] | None = None,
         brief_for: Callable[[str], str] | None = None, now: dt.datetime | None = None, say: Say = print) -> int:
    now = now or land_lane.now_utc()
    d = lane_dir(cdir)
    d.mkdir(parents=True, exist_ok=True)
    lock = ExclusiveLock.try_take(d / TICK_LOCK, f"review_lane.py tick (pid {os.getpid()}, {land_lane.now_utc():%Y-%m-%d %H:%M:%S}Z)")
    if lock is None:
        say(f"review lane: a tick is already running ({ExclusiveLock.holder(d / TICK_LOCK)}); nothing done")
        return 0
    with lock:
        unreadable: list[str] = []
        rs = runs(cdir, unreadable)
        for p in unreadable:
            say(f"review lane: {p} is not a run record; left out (its branch may be reviewed again): inspect it")
        reap(rs, alive, tip, say)
        rows, age = read_survey(cdir, now)
        if age is None or age > land_lane.SURVEY_MAX_AGE_MIN:
            say("review lane: " + ("no survey yet: the landing lane has not ticked" if age is None else
                                    f"the landing lane's survey is {age:.0f} min old (over {land_lane.SURVEY_MAX_AGE_MIN}): nothing launched")
                + "; run `land_lane.py tick` (it takes the survey), then this tick again")
            return 0
        # Only a tick that read a fresh survey is a TICK: one that could act on nothing leaves `agent_work` saying the lane
        # is not ticking, so the unit that runs it takes the survey first (R1-4: -LandLaneSeconds 0 has no other tick).
        coord.write_json(d / LAST_TICK, {"at": land_lane.now_utc().isoformat(timespec="seconds")})
        named = {r["name"]: r for r in rows}
        judge(cdir, runs(cdir), named, say)
        rs = runs(cdir)
        for h in holds(cdir):   # a hold lasts while the branch is at its head
            row = named.get(h["branch"])
            if row is None or not same_commit(row["tip"], h["head"]):
                pathlib.Path(h["_path"]).unlink(missing_ok=True)
        why = land_lane.loop_stop(cdir)
        if why:
            say(f"review lane: {why}: no agent starts")
            return 0
        slots = max_agents - sum(1 for r in rs if not finished(r))
        if slots <= 0:
            return 0
        atts = land_lane.by_branch(land_lane.attempts(cdir))
        cands: list[tuple[str, dict[str, Any], Plan, str]] = []
        for row in rows:
            cause = conflict_cause(row, atts.get(row["name"], []))
            if cause and row.get("worktree"):
                plan = plan_conflict(row, rs)
                if plan.launch:
                    cands.append((CONFLICT, row, plan, cause))
        for row in rows:
            if row.get("state") != "WAITING FOR REVIEW" or not row.get("worktree") or not row.get("report"):
                continue
            plan = plan_review(row, cdir, rs, now, say)
            if plan.stamp:
                # A reviewer killed between its verdict and its stamp (or a wave that stopped before its step 5): the verdict
                # is computed and on disk, so stamping it is the lane's (a script's) job, not a second review.
                try:
                    say(f"review lane: {row['name']}: stamped from its own verdict: {review_findings.stamp(pathlib.Path(row['report']), plan.stamp)}")
                except (review_findings.RecordError, OSError) as exc:
                    hold(cdir, row, f"its review has a verdict at this head but the report cannot be stamped: {exc}", say)
            elif plan.hold:
                hold(cdir, row, plan.why, say)
            elif plan.launch:
                cands.append((REVIEW, row, plan, ""))
            else:
                say(f"review lane: {row['name']}: {plan.why}")
        if not cands:
            return 0
        try:
            decision = budget()
        except Exception as exc:  # noqa: BLE001  (an estimate that cannot be read launches nothing that spends the quota)
            say(f"review lane: the budget could not be read ({type(exc).__name__}: {exc}); no agent starts")
            return 1
        if decision != "go":
            say(f"review lane: budget decision {decision}: no agent starts ({len(cands)} waiting)")
            return 0
        failed = 0
        for kind, row, plan, cause in cands[:slots]:
            if tip(row["worktree"]) != row["tip"]:
                say(f"review lane: {row['name']}: moved since the survey; left for the next tick")
                continue
            brief = (brief_for or (lambda b: conflict_brief_text(cdir, b)))(row["name"]) if kind == CONFLICT else ""
            argv = (argv_for or (lambda k: claude_argv(list(claude), k, mode)))(kind)
            try:
                rec = launch(cdir, repo, kind, row, plan,
                             lambda r: review_prompt(r, cdir, repo) if kind == REVIEW else finisher_prompt(r, cdir, repo, cause, brief),
                             argv, start)
            except OSError as exc:
                failed += 1
                say(f"review lane: {row['name']}: the runner did not start ({exc})")
                continue
            if kind == CONFLICT:   # the unit need not dispatch what the lane did: the exit-4 attempt only, never an ENQUEUED one
                stop = conflict_attempt(row, atts.get(row["name"], []))
                if stop and not stop.get("collected"):
                    land_lane.update(stop, {"collected": True})
            rs.append(rec)
            say(f"review lane: {row['name']} @{row['tip'][:12]}: {ROLE[kind]} started ({plan.why}; pid {rec['pid']}, {rec['log']})")
        return 1 if failed else 0


# ── the runner ─────────────────────────────────────────────────────────────────────────────────────────────────────
def run_agent(path: pathlib.Path) -> int:
    """Run the agent of the record to its end: its prompt on stdin, its stream in the log; stop it on its CEILING
    (STALLED) or on a loop stop (STOPPED); then record rc, the end head and the verdict its findings file holds."""
    rec = coord.read_json(path, None)
    if not isinstance(rec, dict):
        print(f"review lane: {path} is not a run record")
        return 2
    cdir = path.resolve().parents[2]   # <coord>/review-lane/runs/<id>.json
    outcome = None
    env = {**os.environ, "COBOL_COORD_DIR": str(cdir)}
    with open(rec["prompt"], "rb") as prompt, open(rec["log"], "ab") as log:
        # its own session on POSIX: kill_tree's tree there is the process group, which the agent must lead (kb/Work PB4072)
        proc = subprocess.Popen(rec["argv"], cwd=rec.get("cwd") or None, stdin=prompt, stdout=log, stderr=subprocess.STDOUT,
                                env=env, start_new_session=os.name != "nt")
        began = time.monotonic()
        while proc.poll() is None:
            if time.monotonic() - began > float(rec.get("ceiling_min", CEILING_MIN[rec["kind"]])) * 60:
                outcome = "STALLED"
            elif land_lane.loop_stop(cdir):
                outcome = "STOPPED"
            if outcome:
                kill_tree(proc.pid)
                break
            time.sleep(float(rec.get("poll_s", POLL_S)))
        rc = proc.wait()
    done = {"rc": rc, "ended": land_lane.now_utc().isoformat(timespec="seconds"), "end_head": land_lane.tip_of(rec.get("worktree")),
            "verdict": verdict_of(rec), **({"outcome": outcome} if outcome else {})}
    land_lane.update({**rec, "_path": str(path)}, done)
    print(f"review lane: {rec['id']} ended exit {rc}{' ' + outcome if outcome else ''}: {done['verdict'] or 'no verdict'}", flush=True)
    return rc


# ── what the enqueue unit and next_unit.py read ────────────────────────────────────────────────────────────────────
@dataclasses.dataclass
class Lane:
    ticked_at: dt.datetime | None
    runs: list[dict[str, Any]]
    holds: list[dict[str, Any]]
    unreadable: list[str]

    def tick_age_min(self, now: dt.datetime | None = None) -> float | None:
        return None if self.ticked_at is None else ((now or land_lane.now_utc()) - self.ticked_at).total_seconds() / 60


def view(cdir: pathlib.Path) -> Lane:
    t = coord.read_json(lane_dir(cdir) / LAST_TICK, None)
    unreadable: list[str] = []
    return Lane(dt.datetime.fromisoformat(t["at"]) if isinstance(t, dict) and t.get("at") else None, runs(cdir, unreadable),
                holds(cdir), unreadable)


def agent_work(cdir: pathlib.Path, now: dt.datetime | None = None) -> list[str]:
    """What only the enqueue unit can do about the review lane's branches: its uncollected holds, and, while the lane is
    not ticking (no tick in LANE_STALE_MIN: the supervisor's lane is off or failing), the work it would start, which the
    unit starts by ticking it once. Empty when nothing is owed."""
    lane, out = view(cdir), []
    open_ = [h for h in lane.holds if not h.get("collected")]
    if open_:
        out.append("review lane holds: " + ", ".join(f"{h['branch']} ({h['why'][:80]})" for h in open_))
    age = lane.tick_age_min(now)
    surveyed = land_lane.view(cdir)
    fresh = surveyed.survey_age_min(now)
    if (age is None or age > LANE_STALE_MIN) and fresh is not None and fresh <= land_lane.SURVEY_MAX_AGE_MIN:
        waiting = [r["name"] for r in surveyed.rows if r.get("state") == "WAITING FOR REVIEW"
                   or (r.get("state") == "BOUNCED" and r.get("conflicts"))]
        if waiting:
            out.append(f"the review lane is not ticking and branches wait for it: {', '.join(waiting)}")
    return out


def collect(cdir: pathlib.Path, branches: list[str], say: Say = print) -> int:
    """The enqueue unit has handled these branches' holds."""
    rc = 0
    for b in branches:
        mine_ = [h for h in holds(cdir) if h["branch"] == b and not h.get("collected")]
        if not mine_:
            say(f"review lane: {b}: no uncollected hold")
            rc = 1
        for h in mine_:
            coord.write_json(pathlib.Path(h["_path"]), {k: v for k, v in h.items() if k != "_path"} | {"collected": True})
            say(f"review lane: {b}: hold collected")
    return rc


def status_lines(cdir: pathlib.Path, now: dt.datetime | None = None) -> list[str]:
    lane = view(cdir)
    age = lane.tick_age_min(now)
    alive_n = sum(1 for r in lane.runs if not finished(r))
    head = (f"=== REVIEW LANE: {alive_n} running · {len([h for h in lane.holds if not h.get('collected')])} held · " +
            ("never ticked: the lane is not running" if age is None else
             f"last tick {age:.0f} min ago" + (f" — STALE (over {LANE_STALE_MIN} min): run `review_lane.py tick`" if age > LANE_STALE_MIN else ""))
            + " ===")
    out = [head]
    out += [f"HOLD             {h['branch']} @{h['head'][:12]}{' [collected]' if h.get('collected') else ''}: {h['why']}"
            f" (findings file {h.get('findings') or findings_path(cdir, h['branch'])})" for h in lane.holds]
    latest: dict[tuple[str, str], dict[str, Any]] = {}
    for r in lane.runs:
        latest[(r["branch"], r["kind"])] = r
    for r in latest.values():
        state = ("RUNNING" if not finished(r) else (r.get("outcome") or f"exit {r.get('rc')}"))
        out.append(f"{r['kind'].upper():<16} {r['branch']} @{r['head'][:12]} round {r['round']}: {state}"
                   f"{' → ' + r['verdict'] if r.get('verdict') else ''} ({r['log']})")
    out += [f"UNREADABLE       {p}: not a run record" for p in lane.unreadable]
    return out


# ── the self-test ──────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    from scratch_repo import remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    import verdict_line  # noqa: PLC0415  (scripts/: the one home of the landing's verdict line, kb/Work PB3714)
    fails: list[str] = []

    def expect(what: str, ok: bool, detail: str = "") -> None:
        if not ok:
            fails.append(f"{what}{': ' + detail if detail else ''}")

    root = scratch_dir("review-lane-selftest-")
    n_case = [0]
    t0 = dt.datetime.now(dt.timezone.utc).replace(microsecond=0)   # the findings files stamp the real clock

    def fresh() -> pathlib.Path:
        n_case[0] += 1
        c = root / f"c{n_case[0]}"
        (c / "scratch").mkdir(parents=True)
        (c / "logs").mkdir()
        return c

    def row(name: str, state: str, tip: str, **over: Any) -> dict[str, Any]:
        return {"name": name, "state": state, "content": "UNIQUE", "tip": tip, "worktree": str(root / f"wt-{name}"),
                "why": "", "lead": None, "report": str(root / f"{name}-report.md"), "conflicts": False, **over}

    def survey(c: pathlib.Path, rows: list[dict[str, Any]], at: dt.datetime = t0) -> None:
        land_lane.lane_dir(c).mkdir(parents=True, exist_ok=True)
        coord.write_json(land_lane.lane_dir(c) / land_lane.SURVEY, {"at": at.isoformat(timespec="seconds"), "rows": rows,
                                                                  "landing_order": []})

    class Fake:
        """The tick's collaborators: spawn, liveness, the worktrees' tips, the budget."""

        def __init__(self, c: pathlib.Path, rows: list[dict[str, Any]], **kw: Any) -> None:
            self.c, self.rows, self.said, self.started, self.live, self.decision = c, rows, [], [], set(), "go"
            self.max_agents = kw.get("max_agents", MAX_AGENTS)
            self.pids = iter(range(50000, 60000))
            survey(c, rows)

        def start(self, argv: list[str], log: pathlib.Path) -> int:
            pid = next(self.pids)
            self.live.add(pid)
            self.started.append(argv)
            return pid

        def tick(self, now: dt.datetime = t0) -> list[tuple[str, str]]:
            before = {r["id"] for r in runs(self.c)}
            tips = {r["worktree"]: r["tip"] for r in self.rows}
            tick(self.c, root, max_agents=self.max_agents, budget=lambda: self.decision, start=self.start,
                 alive=lambda r: r.get("pid") in self.live, tip=tips.get, brief_for=lambda b: f"BRIEF of {b}",
                 now=now, say=self.said.append)
            return [(r["kind"], r["branch"]) for r in runs(self.c) if r["id"] not in before]

    def end(c: pathlib.Path, r: dict[str, Any], rc: int = 0, end_head: str | None = None, **more: Any) -> None:
        land_lane.update(r, {"rc": rc, "ended": t0.isoformat(), "end_head": end_head or r["head"], "verdict": None, **more})

    def prompt_of(r: dict[str, Any]) -> str:
        return pathlib.Path(r["prompt"]).read_text(encoding="utf-8")

    def runs_of(c: pathlib.Path, branch: str, kind: str = REVIEW) -> list[dict[str, Any]]:
        return mine(runs(c), branch, kind)

    try:
        # 1. A branch WAITING FOR REVIEW gets ONE reviewer: round 1, the lane's findings file, the role's own definition.
        c = fresh()
        f = Fake(c, [row("b1", "WAITING FOR REVIEW", "a1a1a1a"), row("b2", "WAITING TO LAND", "b2b2b2b"), row("b3", "IN FLIGHT", "c3c3c3c")])
        started = f.tick()
        expect("a branch WAITING FOR REVIEW starts ONE reviewer, and no other state does", started == [(REVIEW, "b1")], str(started))
        r = runs_of(c, "b1")[0]
        p = prompt_of(r)
        expect("the reviewer runs the role's own definition in a headless session, the prompt on stdin",
               r["argv"][:4] == ["claude", "-p", "--agent", "cobol-reviewer"] and "--session-id" in r["argv"]
               and "--permission-prompts" in r["argv"], str(r["argv"]))
        expect("the prompt names the branch, worktree, head, report, the lane's findings file, the label, round 1, the blocking "
               "severities, both stop files and the result line",
               all(s in p for s in (r["branch"], r["worktree"], r["head"], r["report"], r["findings"], r["label"], "round 1",
                                    "critical,high,medium", str(coord.global_stop(str(c))), "STOP-loop", "REVIEW LANE RESULT"))
               and "RE-REVIEW" not in p, p[:300])
        expect("the findings file is the one review_findings reads back to its branch",
               review_findings.NAMED_FINDINGS.match(pathlib.Path(r["findings"]).name)["branch"] == "b1", r["findings"])
        expect("a second tick while the reviewer runs starts none", f.tick() == [], str(f.said))

        # 2. Stops, the budget, the survey's age, the tip that moved, and the cap.
        for what, arrange in (("the loop's STOP", lambda c: (c / "STOP").write_text("", encoding="utf-8", newline="\n")),
                              ("the owner's global stop", lambda c: coord.global_stop(str(c)).write_text("", encoding="utf-8", newline="\n")),
                              ("the loop's fleet stop", lambda c: coord.fleet_stop(c / "scratch", "loop").write_text("", encoding="utf-8", newline="\n"))):
            c2 = fresh()
            f2 = Fake(c2, [row("s1", "WAITING FOR REVIEW", "a1a1a1a")])
            arrange(c2)
            expect(f"no agent starts under {what}", f2.tick() == [] and any("no agent starts" in s for s in f2.said), str(f2.said))
        c2 = fresh()
        f2 = Fake(c2, [row("s1", "WAITING FOR REVIEW", "a1a1a1a")])
        f2.decision = "hold-session"
        expect("no agent starts under a budget hold (the lane spends the loop's quota)", f2.tick() == [] and any("hold-session" in s for s in f2.said), str(f2.said))
        f2.decision = "go"
        survey(c2, f2.rows, t0 - dt.timedelta(minutes=land_lane.SURVEY_MAX_AGE_MIN + 1))
        expect("a survey older than land_lane.SURVEY_MAX_AGE_MIN launches nothing", f2.tick() == [] and any("old" in s for s in f2.said), str(f2.said))
        survey(c2, f2.rows)
        tips_moved = Fake(c2, [row("s1", "WAITING FOR REVIEW", "a1a1a1a")])
        tick(c2, root, budget=lambda: "go", start=tips_moved.start, alive=lambda r: False, tip=lambda wt: "ffffffff", now=t0, say=tips_moved.said.append)
        expect("a branch that moved since the survey is left for the next tick",
               not runs(c2) and any("moved since the survey" in s for s in tips_moved.said), str(tips_moved.said))
        c3 = fresh()
        f3 = Fake(c3, [row(f"m{i}", "WAITING FOR REVIEW", f"{i}{i}{i}{i}{i}{i}{i}") for i in range(1, 5)], max_agents=2)
        expect("at most max-agents agents run at once, in survey order", f3.tick() == [(REVIEW, "m1"), (REVIEW, "m2")], str(f3.said))
        expect("a full cap starts nothing more", f3.tick() == [], str(f3.said))

        # 3. THE LEASE: a rolling wave's reviewer, a verdict at the head, and a dead wave reviewer past the ceiling.
        c4 = fresh()
        wave = c4 / "scratch" / "review-w9-a.jsonl"
        review_findings.start(wave, 1, "L1", "a1a1a1a", "w9a-review-r1")
        f4 = Fake(c4, [row("L1", "WAITING FOR REVIEW", "a1a1a1a")])
        expect("a wave reviewer's `start` at the head (no verdict, young) is a lease: no second reviewer",
               f4.tick() == [] and any("started" in s and "no verdict yet" in s for s in f4.said), str(f4.said))
        started_at = dt.datetime.fromisoformat(review_findings.rounds(wave)[0].started)
        later = started_at + dt.timedelta(minutes=REVIEWER_CEILING_MIN + 1)
        survey(c4, f4.rows, later)
        expect("the same `start` past the reviewer ceiling is a dead reviewer's: the lane reviews",
               f4.tick(later) == [(REVIEW, "L1")], str(f4.said))
        c5 = fresh()
        done5 = c5 / "scratch" / "review-w9-b.jsonl"
        review_findings.start(done5, 1, "L2", "a1a1a1a", "w9b-review-r1")
        review_findings.verdict(done5, 1)
        f5 = Fake(c5, [row("L2", "WAITING FOR REVIEW", "a1a1a1a")])
        expect("a verdict at the head in any findings file means reviewed: nothing starts", f5.tick() == [] and any("reviewed" in s for s in f5.said), str(f5.said))
        c5s = fresh()
        stamped = root / "w9z-PB1-report.md"
        stamped.write_text("# PB1 — implementer report\n\n**Status:** DONE · **branch:** `S1`\n**diffstat:** x\n\n## Rule\n",
                           encoding="utf-8", newline="\n")
        own5 = findings_path(c5s, "S1")
        review_findings.start(own5, 1, "S1", "a1a1a1a", "lane-review-S1-r1-1")
        review_findings.verdict(own5, 1)   # the reviewer was killed between its verdict and its stamp
        f5s = Fake(c5s, [row("S1", "WAITING FOR REVIEW", "a1a1a1a", report=str(stamped))])
        expect("a verdict younger than REPORT_GRACE_MIN is left to its reviewer's own stamp",
               f5s.tick() == [] and "**Review:**" not in stamped.read_text(encoding="utf-8"), str(f5s.said))
        past_grace = t0 + dt.timedelta(minutes=REPORT_GRACE_MIN + 1)
        survey(c5s, f5s.rows, past_grace)
        expect("a verdict in the lane's own file with no stamp in the report is stamped by the lane, not reviewed again",
               f5s.tick(past_grace) == [] and "**Review:** CLEAN lane-review-S1-r1-1 a1a1a1a" in stamped.read_text(encoding="utf-8")
               and not holds(c5s), str(f5s.said))
        # R1-2: a rolling wave's reviewer recorded its verdict and was stopped before its stamp: the lane stamps it too.
        c5w = fresh()
        stamped_w = root / "w9y-PB2-report.md"
        stamped_w.write_text("# PB2 — implementer report\n\n**Status:** DONE · **branch:** `S2`\n**diffstat:** x\n\n## Rule\n",
                             encoding="utf-8", newline="\n")
        wave_w = c5w / "scratch" / "review-w9-y.jsonl"
        review_findings.start(wave_w, 1, "S2", "a1a1a1a", "w9y-review-r1")
        review_findings.verdict(wave_w, 1)
        f5w = Fake(c5w, [row("S2", "WAITING FOR REVIEW", "a1a1a1a", report=str(stamped_w))])
        survey(c5w, f5w.rows, past_grace)
        expect("a wave reviewer's verdict at the head whose stamp never came is stamped by the lane (the branch would wait for ever)",
               f5w.tick(past_grace) == [] and "**Review:** CLEAN w9y-review-r1 a1a1a1a" in stamped_w.read_text(encoding="utf-8")
               and not holds(c5w), str(f5w.said))
        c5g = fresh()
        young = root / "young-report.md"
        young.write_text("# report\n", encoding="utf-8", newline="\n")
        os.utime(young, (t0.timestamp(), t0.timestamp()))
        f5g = Fake(c5g, [row("G1", "WAITING FOR REVIEW", "a1a1a1a", report=str(young))])
        expect("a report younger than REPORT_GRACE_MIN waits: a rolling wave's reviewer may be about to write its `start`",
               f5g.tick() == [] and any("may be about to start" in s for s in f5g.said), str(f5g.said))
        os.utime(young, (t0.timestamp() - 600, t0.timestamp() - 600))
        expect("the same report past the grace is reviewed", f5g.tick() == [(REVIEW, "G1")], str(f5g.said))
        c5b = fresh()
        other = c5b / "scratch" / "review-w9-c.jsonl"
        review_findings.start(other, 1, "someone-else", "a1a1a1a", "w9c-review-r1")
        expect("a findings file that names another branch is no lease",
               Fake(c5b, [row("L3", "WAITING FOR REVIEW", "a1a1a1a")]).tick() == [(REVIEW, "L3")])

        # 4. RESTART-SAFE: a killed reviewer leaves its start (and maybe a cut-off line); the next tick starts the round again.
        c6 = fresh()
        f6 = Fake(c6, [row("K1", "WAITING FOR REVIEW", "a1a1a1a")])
        f6.tick()
        k = runs_of(c6, "K1")[0]
        own = pathlib.Path(k["findings"])
        review_findings.start(own, 1, "K1", "a1a1a1a", k["label"])
        review_findings.finding(own, 1, json.dumps({"file": "x.py", "line": 3, "category": "correctness", "severity": "high",
                                                    "summary": "s", "failure_scenario": "f"}))
        cut = json.dumps({"kind": "finding", "round": 1, "summary": "§ half"}, ensure_ascii=False).encode()
        own.write_bytes(own.read_bytes() + cut[:cut.index("§".encode()) + 1])
        f6.live.clear()   # the runner is gone (killed)
        expect("the killed reviewer's cut-off append reads TORN before the tick", review_findings.last_verdict(own)["verdict"] == "TORN")
        again = f6.tick()
        k2 = runs_of(c6, "K1")
        expect("the next tick reaps it DIED, mends the cut-off append and starts the SAME round again, with no hand repair",
               again == [(REVIEW, "K1")] and k2[0].get("outcome") == "DIED" and k2[1]["round"] == 1
               and review_findings.last_verdict(own) is None and any("mended" in s for s in f6.said), f"{again} {f6.said}")
        expect("the restart's prompt lists what the dead reviewer recorded and does not record it again",
               "STARTED BEFORE" in prompt_of(k2[1]) and "do not record those findings again" in prompt_of(k2[1]), prompt_of(k2[1])[-400:])
        expect("`start` of the unfinished round is allowed, and the standing finding counts at the verdict",
               review_findings.start(own, 1, "K1", "a1a1a1a", k2[1]["label"])["round"] == 1
               and review_findings.verdict(own, 1)["findings"] == 1)
        c7 = fresh()
        f7 = Fake(c7, [row("K2", "WAITING FOR REVIEW", "a1a1a1a")])
        bad = findings_path(c7, "K2")
        review_findings.start(bad, 1, "K2", "a1a1a1a", "x")
        bad.write_bytes(bad.read_bytes() + b"{not json}\n" + bad.read_bytes())
        expect("a tear before more lines is the operator's: a HOLD, nothing started, the file untouched",
               f7.tick() == [] and any(h["branch"] == "K2" for h in holds(c7)) and b"{not json}" in bad.read_bytes(), str(f7.said))
        expect("a hold names the findings file a hand-dispatched reviewer records in, and `status` prints it (R1-5)",
               holds(c7)[0].get("findings") == str(bad) and str(bad) in "\n".join(status_lines(c7)), str(holds(c7)))
        # The enqueue unit's hand-dispatched reviewer records into that SAME (lane-own) file; the operator mends the tear
        # and the reviewer's round is open there. The lane must not start a second reviewer of the round beside it (PB4049).
        bad.unlink()
        review_findings.start(bad, 1, "K2", "a1a1a1a", "enqueue-review-K2-r1")
        expect("a mended torn-file hold with the unit's reviewer open in the lane's own file starts NO second reviewer",
               f7.tick() == [] and not runs_of(c7, "K2")
               and "until the enqueue unit collects it" in plan_review(f7.rows[0], c7, [], t0, f7.said.append).why, str(f7.said))
        expect("the lane keeps waiting on later ticks too, the hold uncollected", f7.tick() == [] and not runs_of(c7, "K2"), str(f7.said))
        collect(c7, ["K2"], say=lambda s: None)
        expect("once the unit has collected the hold, its reviewer gone, the lane starts the open round again",
               f7.tick() == [(REVIEW, "K2")] and runs_of(c7, "K2")[0]["round"] == 1, str(f7.said))

        # 5. ROUNDS: round 2 reviews the finisher's diff; past REVIEW_FIX_ROUNDS the blocking set is the cap's.
        c8 = fresh()
        own8 = findings_path(c8, "R1")
        review_findings.start(own8, 1, "R1", "a1a1a1a", "l1")
        review_findings.finding(own8, 1, json.dumps({"file": "x.py", "line": 3, "category": "c", "severity": "high", "summary": "s", "failure_scenario": "f"}))
        review_findings.verdict(own8, 1)
        f8 = Fake(c8, [row("R1", "WAITING FOR REVIEW", "b2b2b2b")])
        f8.tick()
        r8 = runs_of(c8, "R1")[0]
        expect("a branch that moved past a reviewed head is round 2, a RE-REVIEW of the diff since that head",
               r8["round"] == 2 and r8["prev_head"] == "a1a1a1a" and "RE-REVIEW" in prompt_of(r8)
               and "git diff a1a1a1a..HEAD" in prompt_of(r8) and "critical,high,medium" in prompt_of(r8), prompt_of(r8)[:400])
        expect("the blocking severities are the cap's after the wave's review finisher rounds",
               blocking_for(REVIEW_FIX_ROUNDS) == "critical,high,medium" and blocking_for(REVIEW_FIX_ROUNDS + 1) == "critical,high")

        # 6. ATTEMPT_LIMIT: reviewers that end with no verdict are retried, then HELD.
        c9 = fresh()
        f9 = Fake(c9, [row("A1", "WAITING FOR REVIEW", "a1a1a1a")])
        for i in range(ATTEMPT_LIMIT):
            expect(f"review attempt {i + 1} starts", f9.tick() == [(REVIEW, "A1")], str(f9.said))
            end(c9, runs_of(c9, "A1")[-1], 1, outcome="STALLED")
            f9.live.clear()
        expect("after ATTEMPT_LIMIT reviewers with no verdict at one head the lane HOLDS, starts none, and asks for the unit",
               f9.tick() == [] and any(h["branch"] == "A1" for h in holds(c9))
               and any(w.startswith("review lane holds: A1") for w in agent_work(c9, t0)), f"{f9.said} {agent_work(c9, t0)}")
        expect("a new head is a new case: the hold is dropped and the lane reviews again",
               (f9.rows[0].__setitem__("tip", "d4d4d4d"), survey(c9, f9.rows), f9.tick())[2] == [(REVIEW, "A1")] and not holds(c9))
        # R1-3: a reviewer a LOOP STOP ended failed at nothing; however many stops came, the round starts again.
        c9s = fresh()
        f9s = Fake(c9s, [row("A2", "WAITING FOR REVIEW", "a1a1a1a")])
        for i in range(ATTEMPT_LIMIT + 1):
            expect(f"review {i + 1} after {i} loop stops starts again", f9s.tick() == [(REVIEW, "A2")] and not holds(c9s), str(f9s.said))
            end(c9s, runs_of(c9s, "A2")[-1], 1, outcome="STOPPED")
            f9s.live.clear()
        expect("loop stops never make a HOLD for the enqueue unit", not holds(c9s) and agent_work(c9s, t0) == [], str(agent_work(c9s, t0)))

        # 7. THE CONFLICT FINISHER: an exit 4 and a DIRTY bounce each get ONE per head, with the brief; the exit 4 is collected.
        c10 = fresh()
        att = land_lane.lane_dir(c10) / land_lane.ATTEMPTS
        att.mkdir(parents=True)
        rec4 = {"id": "000001-x-F1", "branch": "F1", "head": "a1a1a1a", "worktree": str(root / "wt-F1"), "rc": 4, "end_head": "a1a1a1a",
                "verdict": verdict_line.render(verdict_line.conflict_outcome(["x.py"]), "F1"), "collected": False}
        coord.write_json(att / "000001-x-F1.json", rec4)
        f10 = Fake(c10, [row("F1", "STOPPED", "a1a1a1a"), row("F2", "BOUNCED", "b2b2b2b", conflicts=True, why="pull request #9 conflicts with main (merge state DIRTY)"),
                         row("F3", "BOUNCED", "c3c3c3c", why="removed from the merge queue (FAILED_CHECKS)")])
        started = f10.tick()
        expect("an exit-4 stop and a DIRTY bounce each start a conflict finisher; a bounce for failed checks does not",
               started == [(CONFLICT, "F1"), (CONFLICT, "F2")], str(started))
        pf = prompt_of(runs_of(c10, "F1", CONFLICT)[0])
        expect("the finisher runs the cobol-finisher role; its prompt carries finisher-brief.md, the stop line and the brief verbatim",
               runs_of(c10, "F1", CONFLICT)[0]["argv"][3] == "cobol-finisher" and "finisher-brief.md" in pf and "ENQUEUE: CONFLICT" in pf
               and "BRIEF of F1" in pf and "FINISHER RESULT" in pf, pf[:400])
        expect("the lane collects the exit-4 attempt it started a finisher for", land_lane.attempts(c10)[0].get("collected") is True)
        expect("the unit's work is the failed-checks bounce only; the lane's rows are nobody's",
               land_lane.agent_work(c10, t0).reasons == ["bounced: F3"] and land_lane.stop_reason(c10) is None, str(land_lane.agent_work(c10, t0).reasons))
        end(c10, runs_of(c10, "F2", CONFLICT)[0], 0, end_head="e5e5e5e")   # F2's finisher committed
        f10.live.clear()                                                   # F1's runner is gone with no rc
        expect("ONE finisher per head: the same conflict at the same head starts none", f10.tick() == [], str(f10.said))
        expect("a finisher that ended (here DIED) with the head where it found it is a HOLD for the enqueue unit",
               any(h["branch"] == "F1" and "head unchanged" in h["why"] for h in holds(c10)), str(holds(c10)))
        expect("a finisher that moved the head is judged and holds nothing", not any(h["branch"] == "F2" for h in holds(c10)), str(holds(c10)))
        expect("`collect` clears a hold from the unit's work", collect(c10, ["F1"], say=lambda s: None) == 0
               and not [w for w in agent_work(c10, t0) if "F1" in w])
        # R1-1: the lane collects ONLY the exit-4 attempt; a DIRTY bounce's latest attempt is the ENQUEUED one, still owed
        # its leads and landing record. R1-3: a finisher a loop stop cut off is started again, not held.
        c10b = fresh()
        att_b = land_lane.lane_dir(c10b) / land_lane.ATTEMPTS
        att_b.mkdir(parents=True)
        enq = {"id": "000001-x-F4", "branch": "F4", "head": "a1a1a1a", "worktree": str(root / "wt-F4"), "rc": 0, "end_head": "a1a1a1a",
               "verdict": verdict_line.render("ENQUEUED", "F4"), "collected": False}
        coord.write_json(att_b / "000001-x-F4.json", enq)
        f10b = Fake(c10b, [row("F4", "BOUNCED", "a1a1a1a", conflicts=True, why="pull request #9 conflicts with main (merge state DIRTY)")])
        expect("a DIRTY bounce starts its finisher", f10b.tick() == [(CONFLICT, "F4")], str(f10b.said))
        expect("the ENQUEUED attempt is NOT collected: its leads and landing record are still the unit's",
               land_lane.attempts(c10b)[0].get("collected") is False
               and any("F4" in w for w in land_lane.agent_work(c10b, t0).reasons), str(land_lane.agent_work(c10b, t0).reasons))
        for i in range(2):
            end(c10b, runs_of(c10b, "F4", CONFLICT)[-1], 1, outcome="STOPPED")
            f10b.live.clear()
            expect(f"a finisher cut off by a loop stop (stop {i + 1}) is started again at the same head",
                   f10b.tick() == [(CONFLICT, "F4")] and not holds(c10b), str(f10b.said))

        # 8. agent_work and status: a lane that is not ticking hands its work to the unit; a ticking one hands none.
        c11 = fresh()
        f11 = Fake(c11, [row("W1", "WAITING FOR REVIEW", "a1a1a1a")])
        expect("never ticked and a branch waits: the unit ticks it once", any("not ticking" in w and "W1" in w for w in agent_work(c11, t0)), str(agent_work(c11, t0)))
        # R1-4: a survey older than land_lane.SURVEY_MAX_AGE_MIN (the unit's tick under -LandLaneSeconds 0, before step 3's
        # landing tick): nothing launches AND the tick is no TICK, so the branches stay reported to the unit; agent_work and
        # the tick read ONE threshold (a 15 min survey is fresh to both).
        stale = t0 - dt.timedelta(minutes=land_lane.SURVEY_MAX_AGE_MIN + 1)
        survey(c11, f11.rows, stale)
        f11.tick()
        expect("a tick that read a stale survey is not a tick: the lane still reports not ticking and the survey is the unit's to take",
               not runs(c11) and view(c11).ticked_at is None and not agent_work(c11, t0), str(agent_work(c11, t0)))
        survey(c11, f11.rows, t0 - dt.timedelta(minutes=15))
        expect("a 15 min old survey is fresh to agent_work and to the tick alike",
               any("W1" in w for w in agent_work(c11, t0)) and f11.tick() == [(REVIEW, "W1")], str(f11.said))
        c11 = fresh()
        f11 = Fake(c11, [row("W1", "WAITING FOR REVIEW", "a1a1a1a")])
        f11.tick()
        expect("a ticking lane owes the unit nothing for a branch it is reviewing", agent_work(c11, land_lane.now_utc()) == [], str(agent_work(c11, land_lane.now_utc())))
        expect("status counts the running reviewer and names the branch",
               "1 running" in status_lines(c11)[0] and any("W1" in ln and "RUNNING" in ln for ln in status_lines(c11)), "\n".join(status_lines(c11)))
        expect("status says a lane that is not ticking", "never ticked" in status_lines(fresh())[0])

        # 9. THE RUNNER: rc and the verdict recorded; the CEILING and a loop stop each end a hung agent.
        c12 = fresh()
        stub = root / "stub.py"
        stub.write_text("import sys, time\nmode = sys.argv[1]\nsys.stdin.read()\nif mode == 'hang':\n    time.sleep(600)\n"
                        "sys.exit(0 if mode == 'ok' else 3)\n", encoding="utf-8", newline="\n")
        f12 = Fake(c12, [row(f"U{i}", "WAITING FOR REVIEW", f"{i}a{i}a{i}a{i}") for i in (1, 2, 3)])
        tick(c12, root, budget=lambda: "go", start=lambda argv, log: 0, alive=lambda r: True,
             tip={r["worktree"]: r["tip"] for r in f12.rows}.get, argv_for=lambda k: [sys.executable, str(stub), "ok"], now=t0, say=lambda s: None)
        recs = {r["branch"]: r for r in runs(c12)}
        expect("the cap launched all three for the runner cases", len(recs) == 3, str(sorted(recs)))
        for (b, mode) in (("U1", "ok"), ("U2", "fail"), ("U3", "hang")):
            r = recs[b]
            r["argv"] = [sys.executable, str(stub), mode]
            coord.write_json(pathlib.Path(r["_path"]), {k: v for k, v in r.items() if k != "_path"} | {"poll_s": 0.05, "worktree": str(root / f"wt-{b}")})
        pathlib.Path(root / "wt-U1").mkdir(exist_ok=True)
        # U1 writes a verdict at the head as a reviewer would, then exits 0
        u1 = recs["U1"]
        review_findings.start(pathlib.Path(u1["findings"]), 1, "U1", u1["head"], u1["label"])
        review_findings.verdict(pathlib.Path(u1["findings"]), 1)
        rc1 = run_agent(pathlib.Path(u1["_path"]))
        done1 = runs_of(c12, "U1")[0]
        expect("the runner records rc 0 and the verdict the findings file holds for the run's round", rc1 == 0 and done1["rc"] == 0 and done1["verdict"] == "CLEAN", str(done1))
        rc2 = run_agent(pathlib.Path(recs["U2"]["_path"]))
        done2 = runs_of(c12, "U2")[0]
        expect("a failed agent records its rc and no verdict", rc2 == 3 and done2["rc"] == 3 and done2["verdict"] is None, str(done2))
        coord.write_json(pathlib.Path(recs["U3"]["_path"]), {k: v for k, v in runs_of(c12, "U3")[0].items() if k != "_path"} | {"ceiling_min": 0.0001})
        # The hang stub's own exit is 3 after its 600 s sleep, so rc 3 means the kill never reached it (kb/Work PB4072: on
        # Linux the agent led no process group, kill_tree passed over it, and both arms waited the sleep out and passed).
        run_agent(pathlib.Path(recs["U3"]["_path"]))
        expect("an agent past its CEILING is KILLED (its rc is the kill's, never the stub's own 3) and recorded STALLED",
               runs_of(c12, "U3")[0].get("outcome") == "STALLED" and runs_of(c12, "U3")[0].get("rc") not in (3, None),
               str(runs_of(c12, "U3")[0]))
        c13 = fresh()
        Fake(c13, [row("V1", "WAITING FOR REVIEW", "a1a1a1a")])
        tick(c13, root, budget=lambda: "go", start=lambda argv, log: 0, alive=lambda r: True, tip=lambda wt: "a1a1a1a",
             argv_for=lambda k: [sys.executable, str(stub), "hang"], now=t0, say=lambda s: None)
        v1 = runs(c13)[0]
        coord.write_json(pathlib.Path(v1["_path"]), {k: v for k, v in v1.items() if k != "_path"} | {"poll_s": 0.05})
        (c13 / "STOP").write_text("", encoding="utf-8", newline="\n")
        run_agent(pathlib.Path(v1["_path"]))
        expect("a loop stop KILLS a running agent (its rc is the kill's, never the stub's own 3), recorded STOPPED",
               runs(c13)[0].get("outcome") == "STOPPED" and runs(c13)[0].get("rc") not in (3, None), str(runs(c13)[0]))
    finally:
        remove_tree(root)
    for m in fails:
        print(f"FAIL: {m}")
    print(f"=== REVIEW LANE SELF-TEST: {'RED — ' + str(len(fails)) + ' failed' if fails else 'GREEN'} ===")
    return 1 if fails else 0


# ── the command line ───────────────────────────────────────────────────────────────────────────────────────────────
def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if argv[:1] == ["--self-test"]:
        return self_test()
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--coord", help="the coordination directory (default $COBOL_COORD_DIR or the default)")
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("tick", help="reap, judge, and start every eligible reviewer and conflict finisher")
    p.add_argument("--repo", type=pathlib.Path, default=coord.REPO)
    p.add_argument("--claude", action="append", help="the command that starts a headless session, one --claude=<piece> per "
                   "word (the supervisor resolves -ClaudeExe, a test seam, into them); default `claude`")
    p.add_argument("--permission-mode", default="bypassPermissions")
    p.add_argument("--max-agents", type=int, default=MAX_AGENTS)
    p.add_argument("--borrow-days", type=int, default=0)
    p.add_argument("--telemetry-dir")
    p = sub.add_parser("status"); p.add_argument("--json", action="store_true")
    p = sub.add_parser("collect"); p.add_argument("branches", nargs="+")
    p = sub.add_parser("run"); p.add_argument("record", type=pathlib.Path)
    a = ap.parse_args(argv)
    cdir = coord.coord_dir(a.coord)
    if a.cmd == "run":
        return run_agent(a.record)
    if a.cmd == "collect":
        return collect(cdir, a.branches)
    if a.cmd == "status":
        if a.json:
            lane = view(cdir)
            print(json.dumps({"ticked_at": lane.ticked_at.isoformat() if lane.ticked_at else None, "runs": lane.runs,
                              "holds": lane.holds, "agent_work": agent_work(cdir)}, indent=1))
        else:
            print("\n".join(status_lines(cdir)))
        return 0
    try:
        return tick(cdir, a.repo, claude=a.claude or ["claude"], mode=a.permission_mode, max_agents=a.max_agents,
                    budget=lambda: budget_decision(a.borrow_days, a.telemetry_dir))
    except (land_lane.LaneError, OSError, subprocess.SubprocessError) as exc:
        print(f"review lane: nothing launched: {exc}")
        return 1


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
