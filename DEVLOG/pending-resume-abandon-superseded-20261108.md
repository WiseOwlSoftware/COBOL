## Four SPLIT branches recorded ABANDONED: their finishers landed the work

The resume unit of 2026-11-08 found five DECISION rows. Four are superseded: every commit on
`worktree-wf_a62b5670-5f6-2` (PB4981, PB4975, PB4979), `-5f6-3` (PB4973), `-5f6-5` (PB4980) and
`worktree-wf_c99e2069-d41-1` (PB4917, PB4813) has a cherry-picked equivalent on main, and every file where a branch
still differs from main is newer on main. Each is recorded ABANDONED in its kb/Work note (PB4975, PB4973, PB4980,
PB4813), so `prune_worktrees.py --apply --abandon` can remove it. The fifth, `-5f6-6` (PB4929), is unlanded work and
goes to a finisher.
