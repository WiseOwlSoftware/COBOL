## CI shard limits to 30 minutes (PB6044)

The nightly's job-limits check (`ci_bounds.py --durations`, PB5841) was red on 2026-11-21 and 2026-11-22: the longest
shard runs had grown to 13.5 min (Windows) and 12.7 min (Linux), past half of the 25-minute limit. Both shard jobs now
stop at 30 minutes. The check reads GREEN at the change. The test population's growth stays with PB4333.
