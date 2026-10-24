## Entry 2453 — 2026-10-24 02:53 PDT — PB3628: file the stranded re-armed NOT QUEUED branch

`land_lane.candidates()` skips a re-armed branch whose pull request reads NOT QUEUED, while the queue watch has already left it as BOUNCED, so finished and reviewed work (PB3155, PR #467) never lands and `land_lane.py status` still says the next tick lands it. The lead is now kb/Work note PB3628 (MAJOR, tooling/orchestrator, process only). No code changed.

