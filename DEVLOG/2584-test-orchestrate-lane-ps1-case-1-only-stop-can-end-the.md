## Entry 2584 — 2026-10-25 16:27 PDT — test_orchestrate_lane.ps1 case 1: only STOP can end the backoff (PB3708)

Case 1 of the lane self-test asserts one unit ran and the STOP exit, which held only if the fake python's 8th lane tick (the one that
creates STOP) landed inside the fixed 30 s backoff. Each tick is a `pwsh` start; on a host where a tick takes over about 5 s the backoff
ended first, a second failing unit ran and the check failed (got 2, want 1): a wall-clock limit deciding a verdict, a red that passes alone.

Re-probe: the part passed on an idle host before (26 s) and after (19 s); the race is in the assertion's shape, so it does not reproduce
idle. Fix: `-BackoffBaseSeconds 600` (the supervisor caps a backoff at 1800 s) so only STOP can end the wait, and `-MaxUnits 2` so a
regression of the lane-ticks-in-backoff mechanism fails after the second unit instead of looping. Sibling sweep: case 2's 2 s backoff is
the subject (the timer must elapse) and only gets later on a slow host; no other `test_orchestrate_*.ps1` suite depends on a backoff or
hold expiry (all run `-BackoffBaseSeconds 0`). No goldens (a script self-test), no inventory rows (`closes_rows: []` with its reason).

