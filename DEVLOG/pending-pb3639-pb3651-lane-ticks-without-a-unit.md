## PB3639, PB3651: the landing lane ticks without a unit, and a replay tie goes to the later report

**PB3639.** The supervisor ticked `land_lane.py tick` only from `Invoke-Unit`. Between units it slept in `Wait-Until`
(a budget hold, a failure backoff) and never ticked, so a branch that became WAITING TO LAND or was re-armed in such a
window waited for the next unit's start. The stall survey (class LANE_TICK_UNIT_BOUND) had traced the code claim and
left the minutes unmeasured. Re-probed on this tree with a fake `python` that records each tick: three failed units and
a 30-second backoff produced 6 ticks, all from unit starts and the top of the loop, and the backoff ran its full length.

Fix: one lane timer, `Step-Lane` in `orchestrate.ps1`, replaces the unit-local `$lastLaneTick`. `Invoke-Unit` calls it
as before (once forced as the unit starts, then on the period), `Wait-Until` calls it after its STOP check and bounds its
nap by the time to the next tick, and the top of the loop calls it for the gap between units (not in a dry run). The lane
now ticks for as long as the supervisor lives, and no tick starts once STOP exists. The lane still ends with the process:
the circuit breaker (exit 4) and an owner question (exit 5) exit the supervisor, and keeping it alive in a lane-only mode
would change the restart protocol, so that residual goes to the owner as a lead.

Tests: new `test_orchestrate_lane.ps1` (the 8th tick creates STOP; the backoff ends on it with exactly 8 ticks and one
unit; `-LandLaneSeconds 0` records none), red on the old `Wait-Until`; `fake-python.ps1` records `land_lane.py tick`;
`LandingLaneDriftTests` now holds `Step-Lane` in `Invoke-Unit`, in `Wait-Until` after `Test-Stop` and in the loop.
Docs: DESIGN-orchestrator-loop 4.9, MANDATORY-PRACTICES O11, the supervisor header, `land_lane.py` comments.

**PB3651.** `prune_worktrees.replays` broke a tie between two carriers that replay the same number of a branch's commits
by `key(branch)`, whose first element is the number of distinct patches the carrier holds, so the larger branch won before
the report order was read. The `Replay` docstring, the inline comment, plan_wave item 10 and DESIGN item 9 all say the later
report wins, so the code was brought to them: the tie key is `(replayed count, rank, name)`. A `test_plan_wave.py` case
(a larger earlier carrier and a smaller later one) was red with the old key and passes, as does its reversed order.

Self-review: sibling arms checked (`plan_wave.branch_replays` is the only caller and passes the report order as `rank`; the
other timers that sleep, `Wait-Until`'s two callers, share the one function).
