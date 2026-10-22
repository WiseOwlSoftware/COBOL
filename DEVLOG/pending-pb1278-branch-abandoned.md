## The superseded wave 1140 PB1278 branch is recorded ABANDONED on main

The resume unit of 2026-10-22 classified `worktree-wf_36fbfc86-472-1` (wave 1140 A, PB1278 + PB3460, SPLIT) as a
DECISION row. `git cherry worktree-wf_31aba3ec-d06-1 worktree-wf_36fbfc86-472-1` lists all three of its commits with
`-`: wave 1141's PB3338 branch carries them as patch-equivalent cherry-picks, so the work lands with that branch. An
earlier resume unit wrote the ABANDONED line only on `worktree-wf_31aba3ec-d06-1`, where `prune_worktrees.py
--abandon` (which reads the main checkout's kb/Work) cannot see it; this landing puts the same line in
`kb/Work/PB1278.md` on main so the worktree can be removed. No code changes. PB1278 stays open: its fix lands with
the PB3338 branch's finisher.
