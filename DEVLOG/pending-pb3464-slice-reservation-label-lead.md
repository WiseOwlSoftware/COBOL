## PB3464 filed: the slice reservation's RUNNING label for a finished branch (PB3456 review lead)

The PB3456 branch review (round 1, lead R1-1, not blocking) found that `classify_inflight` in
`scripts/orchestrator/plan_wave.py` labels the slice reservation RUNNING whenever any non-reservation owner holds a
slice note, including a finished slice branch that is waiting for review, waiting to land or enqueued. The hold is
correct; only the word misleads the operator into waiting on work that is done. Filed as `kb/Work/PB3464.md`
(process-only, MINOR); the site is on branch `fix-pb3456-slice-reservation-20261020` (PR #329) and the fix follows it.
