# 2026-10-24 — stall-class survey: which mechanical stops left finished work idle

> Frozen evidence (kb/Work PB785). Raw data: `2026-10-24-stall-class-survey.json` (per-agent metrics, workflow run
> `wf_261e7afb-19e`). The working files (measurer and refuter jsonl, the classifier's 32 classes) were in the operator
> session's scratchpad and are not kept.

## Question

On 2026-10-24 the attended operator landed four stranded branches by hand within minutes, each idle for a mechanical
reason: PR #467 reviewed CLEAN but stranded by `land_lane.candidates` skipping NOT QUEUED (PB3628); PR #493 bounced on
a `blocked: true` flag that went stale when its blocker landed mid-queue; two notes-only branches waiting for the
serial enqueue unit; PB3112 refused exit 6 for carrying a merged predecessor's stamped DEVLOG entry. The owner: "We
should always use such approaches when available. Don't keep simple things blocked for long periods for stupid
reasons". The question: what EVERY class of such stop is, measured from the loop's own records since 2026-10-12, so each
can be made automatic.

## Hypothesis

Most of the idle after DONE is a small number of mechanical classes a script could resolve, several already described
by open notes that no lane plans.

## Design

A Workflow (`stall-class-survey`, 9 agents): four read-only `cobol-locator` measurers, one per record source
(`landing-measurements.jsonl` 489 rows; `land-lane/attempts` 491 attempts, 174 with rc != 0; the supervisor logs, 739
handoffs and `units.jsonl` 425 units; the kb/Work register and DEVLOG 1967-2462, 485 notes screened); one Opus
`cobol-adjudicator` classifier grouping the de-duplicated incidents by MECHANISM; four Opus `cobol-refuter` verifiers,
each told to re-open two examples in the raw files, recompute an idle figure, argue the automation unsafe, find any
note already covering the class, and default to refuted when the evidence did not hold.

## Attempts

One run. The measurers returned 712 incident lines (302 folded as duplicates, 410 kept) in 54 minutes of wall time.
One refuter ran `git fetch -q` in `E:\COBOL` (it wrote FETCH_HEAD and remote refs, a metadata write against the read-only
rule; no working-tree change) and reported it.

## Result

The classifier proposed 32 classes and about 50,500 branch-minutes of mechanical idle. The refuters kept 16 classes
and overturned 16, and the overturned set includes the three largest proposed classes:

- **LANE_TICK_UNIT_BOUND** (proposed 22,299 min): the code claim is TRUE (`orchestrate.ps1` `Wait-Until` never ticks the
  landing lane), but the cited minutes belonged to night STOPs the lane honours by design, PB3181 survey misreads, PB3186
  over-holds and the WSL disk outage; the residual (~1,000 min of holds and breaker pauses with no STOP) is unmeasured
  as landable time. Filed as PB3639.
- **SERIAL_ENQUEUE_REVIEW** (proposed 8,235 min): the mechanism is real, but `landing_measure.done_times` takes the
  EARLIEST DONE report per lead, so a WIP report inflated the wait (1,571 min max was really 14 min); recomputed
  attempted-head commit to first attempt is 2,797 min over 59 incidents (max 188), mostly windows holding review-fix
  commits. Skipping review for notes-only branches is unsafe: PR 155 (kb/Work and DEVLOG only) had three accuracy
  errors found in review. PB3608's review lane removes the genuine serial waits.
- **ENV_LINUX_GATE_RED** (proposed 6,452 min): real (ENOSPC on WSL), but the idle was the owner-only WSL repair while the
  owner question had stopped the loop; the cause was fixed by PB3194 after the incidents.

Kept classes, by measured idle (minutes): REAL_RED_FINISHER 5,137 (not mechanical; PB3107 covers its misfiled exit-7
members); ORACLE_ONE_BASELINE 5,064 (covered and fixed by PB3107, 2026-10-14); OWNER_POLICY_LOOP_DOWN 4,848 (owner STOPs;
not mechanical, and landing under the owner's STOP needs the owner); NOT_QUEUED_STRANDED 602 (PB3628, PB3275);
STALE_STORED_BLOCKED 601 (PB3389); SOURCE_CONFLICT_FINISHER 454 (not mechanical); DEVLOG_STAMP_NOT_ATOMIC 125
(PB3367); LANE_TRANSIENT_RETRIED 118 (fixed by PB3526, PB3527, PB3536); EXIT6_PREDECESSOR_STAMPED_ENTRY 117 (PB3145; today's
0af-11 measured at 59.8 min); and, with idle unmeasured or zero: STOP_UNIT_SUPPRESSES_LANE (fixed, PB3402),
SURVEY_MISREADS_FINISHED (PB3476), ABANDON_DECISION (PB3386, PB3170), GATE_EXCLUSION_HANG (PB3157, PB3310, PB3113),
REGATE_AND_GATE_DURATION (PB3612, PB3315), OPERATOR_INBOX_WATCHER (fixed, PB3387), DEVLOG_ORDER_REFUSED (fixed, PB3123).

Overturned classes that still named real idle: PLANNER_NEVER_PLANS (PB3118: ~1,657 min for a FINDINGS branch only a
resume unit answered; PB3509, PB3446, PB3404 open with no running campaign); PLANNER_OVERHOLDS (the PB3186 hand-entry
over-hold held seven finished branches 961-1,193 min each on 10-14/15; fixed); NOT_AN_IDLE (PB3180 did idle finished
work, now landed; PB3250, PB3474, PB3409, PB3135 are one-line script fixes that cause no idle); BREAKER_METER (39 of 53
meter units failed since 10-12, 36 on "Browser extension is not connected"; one breaker trip, ~352 min; PB2665).

## Limits

Idle is measured in branch-minutes, so overlapping branches double-count wall time, and a branch's idle during an
owner STOP is not idle the loop could have spent. Classes taken only from the register are unmeasured. The landing
records' `done` time is the earliest DONE report per lead, which over-states waits whenever a WIP report preceded the
final one (a measurement defect for PB3638's per-stage measure to avoid).

## Decision

The operator (owner instruction "file the no-lane check" and O11, "nothing simple waits") put every open note that
covers a kept or real class into PB1957: PB3389, PB3145, PB3476, PB3509, PB3118, PB2665, PB3367, PB3386, PB3404, PB3157,
PB3113, PB3446 at tier 2; PB3250, PB3474, PB3409, PB3135 at tier 3; PB3628 and PB3275 were already in flight (wave
20261030). New note PB3639 (the lane bound to a unit's life), tier 2. The owner decides OWNER_POLICY_LOOP_DOWN (may the
tokenless lane land under an owner STOP), not the operator.

## Links

kb/Work PB1957, PB3636, PB3638, PB3639; DEVLOG pending-pb1957-stall-class-survey; workflow run `wf_261e7afb-19e`.
