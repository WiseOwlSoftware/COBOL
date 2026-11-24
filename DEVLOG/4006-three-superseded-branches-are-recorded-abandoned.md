## Entry 4006 — 2026-11-23 13:20 PST — Three superseded branches are recorded ABANDONED

The resume unit of 2026-11-23 found six DECISION rows in `prune_worktrees.py`. Three carry nothing main lacks:
`git cherry origin/main <branch>` at 02e4815d89 marks every commit of `worktree-wf_6f9eca3a-cc1-19` (6),
`worktree-wf_46d3a156-23e-10` (4) and `worktree-wf_ee682338-d7f-1` (6) as patch-equivalent. The first two were
carried to main by `worktree-wf_bd30c11f-c3a-2` (PB2723/PB2725); the third's PB3178 work landed earlier. Notes
`kb/Work/PB2723/2026-11-23-superseded-branches-abandoned.md` and `kb/Work/PB3178/2026-11-23-pilot-branch-abandoned.md`
record each ABANDONED so `prune_worktrees.py --apply --abandon` can remove them. The other four SPLIT rows
(PB5658, PB5575, PB5580, PB5704 branches) keep open notes and go to the planner's finishers.

