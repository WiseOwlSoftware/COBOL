## Entry 2304 — 2026-10-20 15:17 PDT — PB3465: filed the planner defect that makes every worktree collide on .claude/settings.local.json

The wave 1134 unit read `plan_wave.py`'s table and found PB3337 (the campaign's `next` rank 1), PB3460 and PB1278
held by a "file-set collision" on `.claude/settings.local.json`, a file every agent worktree has modified and that is
never work. `inflight_file_sets` counts each worktree's uncommitted files with no exclusion, and a finisher inherits
the files of the branch it continues, so it meets every other worktree on that one path. `prune_worktrees.py` already
excludes the path in its own constant; the fix puts the exclusion in one place every reader applies, with a drift
test. Filed as kb/Work PB3465 (MAJOR, process-only, silent); no code change in this landing.

