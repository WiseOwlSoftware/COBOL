## The loop's review lane: a branch WAITING FOR REVIEW gets its reviewer within a tick, and a CONFLICT stop its finisher (kb/Work PB3608)

**Mechanism.** A finished branch waited up to an hour for its reviewer: the loop reviewed only inside an `enqueue` unit,
units run one at a time (3,308 s, 4,579 s and over two hours on 2026-10-23), and `fix-self-test-wall-leads`, DONE at
~14:00, was still WAITING FOR REVIEW at 16:00 until the operator started its reviewer by hand (CLEAN in 187 s). The
landing lane already started every WAITING TO LAND branch within its tick; the two steps before it that need a model had
no lane.

**Re-probe.** On this tree `prune_worktrees.py --brief` still lists a branch with no CLEAN stamp WAITING FOR REVIEW and
`next_unit.py` still named the `enqueue` unit for it; nothing in `orchestrate.ps1` started a reviewer. Reproduced as the
note says; no fix had landed (`git log -S"review_lane"` finds none).

**Fix shape (design section 4.9.1).** `scripts/orchestrator/review_lane.py`, built like `land_lane.py`: deterministic,
state on disk (`<coord>/review-lane/{runs,prompts,holds}`), one detached runner per agent. The supervisor starts
`review_lane.py tick` in the background right after each landing-lane tick completes (`Start-ReviewTick`, called from
`Complete-LaneTick`; `-ReviewAgents`, default 3, `0` runs no review lane), because it reads the survey that tick wrote
(`land-lane/survey.json`; its rows now carry the `report` path and `conflicts`), so no second survey is taken. A tick
reaps and judges finished runs; starts nothing under the loop's STOP, its fleet stop or the owner's global stop
(`land_lane.loop_stop`, now the one reader of the three) or unless `budget.py` says `go`; then launches at most
`-ReviewAgents` runs, conflict finishers first: `claude -p --agent cobol-reviewer|cobol-finisher` in the main checkout,
prompt on stdin, the role's own definition carrying model, hooks and turn cap, killed at the role's ceiling (90 / 240
min, STALLED) or on a loop stop. A lease per branch head (the lane's live runner; any findings file naming the branch
with a `start` at the head and no verdict younger than the reviewer ceiling, which is a rolling wave's reviewer; a
verdict at the head means reviewed; a report under 5 minutes old waits, because a wave's reviewer has not written its
`start` yet) keeps a second reviewer off. PB3283 (the survey reads a running wave reviewer as WAITING FOR REVIEW) stays
open: the lane works around it by the lease and does not touch the survey's state. The CONFLICT finisher (an exit-4 attempt, or a pull
request the survey reads BOUNCED with merge state DIRTY) gets one `cobol-finisher` per head with `land_lane.py
conflict-brief` verbatim in its prompt; the lane collects the exit-4 attempt, and `land_lane.agent_work` leaves DIRTY
bounces to it. `review_lane.agent_work` is `next_unit.py`'s second reader: holds, or the lane's own work while it has
not ticked in 30 minutes. `units/enqueue.md` step 2 now reads the lane (`review_lane.py status|tick|collect`) and
starts no reviewer except for a hold or an UNREVIEWED STOPPED row; step 4 leaves conflicts to the lane.

**Restart-safe (the standing requirement).** A reviewer killed mid-review leaves its `start`, perhaps findings, perhaps a
cut-off last line. `review_findings.py start` refused a second `start` of a round, so a killed round could never be
started again: it now allows it when the round has no verdict (findings already recorded stand and count; the verdict
uses the newest `start`). `review_findings.repair_interrupted_append` mends a file torn at its LAST line only (a
tear before more lines, a byte-order mark and an unreadable file stay the operator's); the tick calls it, reads the file
through `rounds` (intact files only, the rule `last_verdict` keeps), and starts the same round again with a prompt that
lists the standing findings. A reviewer killed between its verdict and its stamp is stamped by the lane from the verdict
on disk. Two runs with no verdict at one head are a HOLD, not a third.

**Siblings swept.** The same "second `start` of a round" refusal had no other writer. `land_lane._now`/`_slug` were
imported across modules by the new lane, so they are now `now_utc`/`slug` (every caller, the self-test's monkeypatch and
design section 16.7's module table changed together); `detached._kill_tree` became `kill_tree` (the runner uses it).
`DESIGN-test-build-ci.md`, `MANDATORY-PRACTICES.md` (I10, L12, O11), `finisher-brief.md`, `cobol-reviewer.md` and the
review skill's branch form say who reviews and who finishes a conflict now. `script_layering.py` declares
`review_lane` above `land_lane`.

**Tests.** `review_lane.py --self-test` (about 40 arms: tick over planted surveys with stubbed spawn, liveness, tips and budget;
the lease; the killed reviewer's restart with a cut-off append; rounds; the attempt limit; the conflict finisher; the
runner over a stub agent for rc, verdict, ceiling and a loop stop), `review_findings.py --self-test` (restart,
`rounds`, `repair_interrupted_append`), `test_next_unit.py` (rule 6 with the lane ticking, holding, collected, stale),
`test_orchestrate_lane.ps1` (a review tick follows the landing ticks, carries the cap and the resolved claude launch,
and none at `-ReviewAgents 0` or `-LandLaneSeconds 0`), `ReviewLaneDriftTests` (the supervisor's chaining, enqueue.md
step 2, every self-test arm named). No inventory row is claimed or closed.

**Branch review, round 1 (four blocking findings, all fixed at the root).** R1-1: starting a conflict finisher collected
every finished attempt of the branch, so a DIRTY bounce lost the enqueue unit's work on its ENQUEUED attempt (leads, landing
record); `conflict_attempt` now names the one exit-4 attempt that IS the stop, and only it is collected. R1-2: a verdict
at the head in a rolling wave's findings file whose stamp never came (the wave stopped before its step 5) read as
reviewed for ever; any file's verdict older than the 5-minute grace is now stamped by the lane (only the file's newest
round, `Round.ended` carries the verdict's time). R1-3: runs a loop stop ended (STOPPED) counted toward ATTEMPT_LIMIT, so
two stops turned the restart into a HOLD; they count for neither reviewers nor conflict finishers now (the finisher was the
sibling: `plan_conflict` and `judge`). R1-4: the lane's survey threshold was 10 minutes while `agent_work` used 30, and
`tick` recorded itself as ticking before it read the survey, so under `-LandLaneSeconds 0` a stale-survey tick launched
nothing and hid the waiting branches; one threshold (`land_lane.SURVEY_MAX_AGE_MIN`), the tick is recorded only after a
fresh survey, and enqueue.md step 2 takes the survey (`land_lane.py tick`) before the review tick. Leads fixed in files
already changed: R1-5 (a hold carries and `status` prints its findings file; step 2 names the lane's file for an
UNREVIEWED branch), R1-6 (`review_findings.py summary` reads every `review-*.jsonl` and times a restarted round from its
newest `start`). Arms for each in `review_lane.py` and `review_findings.py` self-tests, named in `ReviewLaneDriftTests`.
