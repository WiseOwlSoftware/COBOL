## CI shard limits to 30 minutes (PB6044)

The nightly's job-limits check (`ci_bounds.py --durations`, PB5841) was red on 2026-11-21 and 2026-11-22: the longest
shard runs had grown to 13.5 min (Windows) and 12.7 min (Linux), past half of the 25-minute limit. Both shard jobs now
stop at 30 minutes. The check reads GREEN at the change. The test population's growth stays with PB4333.

The first gate of this branch also found a stale case spelling on the main checkout's disk (PB6045, filed open): a
case-only folder rename of 2026-11-15 survived on disk, and the generator's output differed from another worktree's at
one input key.
