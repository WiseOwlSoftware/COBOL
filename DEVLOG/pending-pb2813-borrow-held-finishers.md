## PB2813 amended: a unit's bare budget read held two finishers the owner's borrow allowed

The loop runs with `-BorrowDays 2`, and the supervisor's reading was "go" (weekly 29.4 % of 57.2 %). The enqueue unit
that ended at 18:51 on 2026-11-01 ran `budget.py` without the flag, read hold-day, and deferred two Opus finishers
(0aa-5, 54d-26) by about 15 hours. The operator steered the next unit to read the budget with `--borrow-days 2`.
PB2813, open since 2026-10-08, records this harm and is raised to MAJOR. Its fix makes the borrow loop state that
`budget.py` reads, so every caller gets one verdict.
