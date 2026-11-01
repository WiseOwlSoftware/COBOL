## PB4353 — shard_weights.py's self-test read its planted runs against the wall clock, and every merge group after 2026-11-01 09:00 UTC went red on it

The merge queue bounced #960 (PB4298) and #961 (PB2555) within fifteen minutes of each other, both on one self-test,
`scripts/shard_weights.py`, on Linux and Windows alike, at a main whose own CI was green. Neither branch touches that
script. PB4332's self-test plants merge-group runs dated 2026-10-31 07:00 to 09:00 UTC against a fixed `now` of 12:00 that
day, and its `measure_window` arms pass that `now`. `report()`, though, took no clock and called `measure_window` without
one, so its arm read the planted runs through the real 24-hour window. At 2026-11-01T09:00Z the newest planted run left
the window, `report()` answered `NOT MEASURED`, and the arm failed for every pull request the queue tested from then on.

`report()` now takes `now` and passes it to `measure_window`. The self-test passes its fixed clock, and a new arm reads
the same runs two days later and requires `NOT MEASURED`. That arm proves the window ends at the clock passed in, not at
the wall clock. The local gates could not see this: a self-test whose planted data is dated near its authoring day passes
every gate until the day it expires. The sibling sweep ran all 137 self-tests with Python's clock shifted forward 3 and
45 days. It found no second expiring fixture. Its 6 and 7 reds were all false: each test compares Python's clock with
a real one the shift could not move (file mtimes, git commit times, a process start time, a timestamp PowerShell
wrote). A one-clock shift is therefore no standing guard, and kb/Work PB4358 holds the design question of the guard that
would catch the next one on the day it is written. Process only: no compiler code, no diagnostic, no golden, no
inventory row.
