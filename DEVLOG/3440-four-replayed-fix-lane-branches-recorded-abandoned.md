## Entry 3440 — 2026-11-10 10:15 PST — Four replayed fix-lane branches recorded ABANDONED

A resume unit decided the five DECISION rows `prune_worktrees.py` listed. `worktree-wf_82a7f4cd-626-1` (w20261979a)
had cherry-picked every unlanded commit of `worktree-wf_7ae59db5-4ef-2`, `-4ef-5` and `-4ef-17` onto main, resolved
their conflicts, and fixed those branches' review findings (d1ff4cc). Its gate was GREEN at 02d7ae008e. So those three
branches, and `-4ef-4` (merged into `-4ef-17`), carry nothing that is not on `626-1`. kb/Work PB5074, PB5076 and PB5078
now record each one ABANDONED with that reason, and `prune_worktrees.py --apply --abandon` can remove them.
`626-1` (SPLIT) and `626-20` (DONE, one low review finding) stay open for a finisher.

