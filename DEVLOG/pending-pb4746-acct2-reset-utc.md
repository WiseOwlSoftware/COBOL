## kb/Work PB4746: both accounts' weekly resets are kept in UTC

Account 2's weekly reset was configured as Saturday 10:00 Los Angeles time. The reset is a fixed instant, so when
daylight saving time ended on 2026-11-01 it moved to 09:00 Pacific, and `budget.py` began the week and each pacing day an
hour late. The owner pointed it out on 2026-11-05. The row in `model_rules.json` is now Saturday 17:00 UTC. The
account and budget self-tests pin it, with a new check for the first week after daylight saving time ends. The
orchestrator design doc states the instant. Account 1 moved too (its /usage read "Resets Nov 8, 2am"), so its row is now Sunday
10:00 UTC, and the self-test that had pinned the old 03:00 belief now pins 02:00.
