## Entry 3275 — 2026-11-07 14:26 PST — PB4849: the wave 20261902 SPLIT branch is abandoned, its work already on main

A note-only change from a resume unit. The branch lifecycle listed `worktree-wf_a7a74442-4f6-1` (w20261902a, PB4849,
SPLIT) as a DECISION row. Its seven commits are all on main: w20261903a cherry-picked them before it finished the refuter
step, and PR 1291 landed that work (`git cherry origin/main` marks 7 of 7 `-`). The only file the branch holds that main
lacks is its pending DEVLOG entry, which the PR 1291 landing folded into its own. The PB4849 note now records the branch
as ABANDONED and why, so `prune_worktrees.py --apply --abandon` can remove it.

