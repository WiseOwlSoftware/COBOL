## PB5443 filed: progress.py's orphan test misses a GATE-PENDING group's live gate

The branch review of wave 20262008 group A (review R1-1, low, non-blocking at the CLEAN verdict of
worktree-wf_ee94fd83-ad2-1 @06aeb645f3) found that the orphan test PB5097 added counts a GATE-PENDING report as settled even
when its worktree holds a live gate. A wave whose only started group awaits a handed-over gate can then have its queued
groups answered NOT STARTED while the rolling wave still holds them. PB5443 records the site, the scenario, why it is rare and
the fix direction; no code changed.
