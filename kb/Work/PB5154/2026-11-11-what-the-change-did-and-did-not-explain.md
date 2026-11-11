## Correction 2026-11-11 (wave 20261998 A, review R2-1 and R1-1) - what this change really did

The title and "The root cause" section above say the one-unit budget "left 30 s for load" and that this was the cause of
the 120 s kills. That was not shown. What the change did: `ObservationBudget` gained a term for a program's OWN clock
(`OneProcessWaiting`, `For(..., ownWait)`), so CM102M's three 30-second `DELAY-FOR-30-SECS` spins (90 s of CPU,
measured 1:1 with the clock) are stated in its budget (210 s) instead of being crowded into one unit of 120 s. That is a
true statement of what the program does and it stays.

What it did not do: explain the runner kills. Both killed attempts used only 31.3-31.5 s of CPU on a host 13-15 % busy,
so after one delay the process was off the processor for about 88 s, which a 90 s own-clock term does not account for.
That stall is UNEXPLAINED and is tracked by kb/Work PB5147 (OPEN), whose addendum records the 544 durable device flushes
per run and the flush-time probe now in a timed-out observation's line. Read PB5154 as "the budget states CM102M's own
wait", not as "load was the cause".
