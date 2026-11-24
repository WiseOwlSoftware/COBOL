## PB6167 filed: a SPLIT carrier deadlocks with the DONE branch it contains

The loop's resume unit of 2026-11-24 found eight DECISION rows that four units in a row had decided FINISH without
any wave planning them. Two DONE branches, `0ee-22` (PB5674) and `c3a-20` (PB3227), are ancestors of SPLIT branches
(`c3a-1`, `ca0-1`). The survey holds each DONE branch IN FLIGHT until its successor lands (PB4396). The planner's
`inflight.branch_carrier` counts only finished branches as carriers. So the successor's own finisher collides with
the predecessor it contains and is never planned. kb/Work PB6167 records the mechanism and the fix wanted: one
"lands through" predicate for the survey and the planner, plus a drift test. The unit re-armed `26c-14`, `0ee-22` and
`c3a-20` for the land lane. Each was DONE, CLEAN and GREEN at its tip, and landing them clears the hold.
