## Two pre-rewrite fleet branches recorded abandoned

After the history rewrite of main (2026-11-05), every fleet branch from before it was checked by content against the new main. All but two had landed. worktree-wf_d77cc414-ca7-1 (PB4739, superseded by the PB4739 branch that landed from wave 20261881) and worktree-wf_d77cc414-ca7-4 (PB4759, a note-only record replaced by the PB4810/PB4811 split) are recorded ABANDONED in their notes, so `prune_worktrees.py --apply --abandon` can remove them.
