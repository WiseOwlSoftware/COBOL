## kb/Work PB4807: budget.py's fallback anchors days back and undercounts

The operator reported (mail 20261105-175031) that account 2's loop read about 8 points low whenever the status-line tap was older
than 30 minutes. Reproduced: the fallback anchors on the readings.json entry of 2026-11-02 (45 %), because `current` never stores
a fresh live reading, and the 2026-10-07 `tokens_per_point` counted 32.8 points where the meter moved 41. Filed as PB4807 (persist
the live anchor, refit the calibration from the brackets it then provides). Note only; no code changes here.
