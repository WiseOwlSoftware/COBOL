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
verdict at the head means reviewed) keeps a second reviewer off. The CONFLICT finisher (an exit-4 attempt, or a pull
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
