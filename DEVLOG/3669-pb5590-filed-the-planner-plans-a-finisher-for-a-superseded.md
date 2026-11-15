## Entry 3669 — 2026-11-15 01:58 PST — PB5590 filed: the planner plans a finisher for a superseded branch's landed review finding

Wave unit 20262040 found `plan_wave.py` planning group A as a FINISH-FIRST finisher for PB5501's review finding R1-1,
though PB5501 is landed, R1-1 is fixed on main (2e217802d6, `ArithmeticReach` in `ConditionRenderer.SizeErrorGuarded`),
and `prune_worktrees.py` already calls the branch superseded. The unit released the group (`plan_wave.py --release
20262040a`) and launched groups B to H. kb/Work PB5590 records the mechanism: the planner and the lifecycle classifier
answer "is this branch superseded" in two places. The same root keeps PB5569 and PB5554 held behind an abandoned
DECISION branch.

