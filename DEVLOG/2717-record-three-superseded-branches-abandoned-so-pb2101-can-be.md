## Entry 2717 — 2026-10-28 19:15 PDT — Record three superseded branches ABANDONED, so PB2101 can be planned

The wave unit of 18:44 held PB2101 (PB1086's next link) because its files meet three undecided branches. `git cherry
origin/main` marks every commit of each `-`, so each carries nothing main lacks except a pending DEVLOG file its landed
entry replaced. `worktree-wf_aee78301-ec3-2` and `worktree-wf_f781f6f8-448-1` are older PB2100 branches carried into #736,
recorded in PB2100. `worktree-wf_0c0f6eee-9f8-1` is PB3975 and PB3990's SPLIT branch, carried into #745 and recorded in
PB3975. With the records on main, `prune_worktrees.py --apply --abandon` removes all three.

