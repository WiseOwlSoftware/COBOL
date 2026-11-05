## kb/Work PB4769: a prerequisite of a harmed note carries that note's harm

`work.py next` and the fix lane read only a note's own harm flags, so a `process_only` prerequisite was never listed or
planned, and the harmed notes waiting on it waited for ever. The MCS slices PB4734 and PB4739 were such prerequisites:
PB4736 (a wrong answer) and PB4743/PB4744 (rejected legal source) depend on them. `work.inherited_harm` now gives every
open blocker of an open, half or held-`blocked` harmed note that note's harm. `next`, the register's views and the
planner's ready view all read it, and the planner's copy of the note carries the owed flags while the register's file
stays as it is. PB4734 and PB4739 join `next` (17 to 19 actionable). Self-tests cover the direct and chained cases and
the `owner` hold, which passes nothing on.
