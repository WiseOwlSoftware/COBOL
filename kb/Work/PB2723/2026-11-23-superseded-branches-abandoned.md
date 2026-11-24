# PB2723 — worktree-wf_6f9eca3a-cc1-19 and worktree-wf_46d3a156-23e-10 ABANDONED (superseded)

Branch `worktree-wf_6f9eca3a-cc1-19` ABANDONED: wave 20262154 group B cherry-picked its work onto
`worktree-wf_bd30c11f-c3a-2`, which landed. `git cherry origin/main worktree-wf_6f9eca3a-cc1-19` at origin/main
02e4815d89 marks all six of its commits `-` (patch-equivalent on main), so nothing unique remains.

Branch `worktree-wf_46d3a156-23e-10` ABANDONED: its four commits were replayed by cc1-19, and
`git cherry origin/main worktree-wf_46d3a156-23e-10` at 02e4815d89 marks all four `-` as well.

Both are removed with `prune_worktrees.py --apply --abandon <branch>` once this note is on main. PB2723's open
remainder is planned from its newest report, not from these branches.
