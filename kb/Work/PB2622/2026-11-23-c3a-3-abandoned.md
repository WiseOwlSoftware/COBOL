## 2026-11-23 - Branch worktree-wf_bd30c11f-c3a-3 is abandoned

`worktree-wf_bd30c11f-c3a-3` ABANDONED, superseded by `worktree-wf_b65a448b-ca0-1`. Its report
(w20262154c-PB2622) was SPLIT: PB2627 was done there and gated GREEN; PB2622, PB2724 and PB3231 were left. Wave 20262161
group A cherry-picked its four commits into `worktree-wf_b65a448b-ca0-1` (report w20262161a-PB2622, line 8); every file
the branch changed is byte-identical on that branch or carried further by its later commits (a per-file `git diff`
of the two tips, resume unit 2026-11-23). The planner already plans nothing from a replayed branch (plan_wave rule 10,
kb/Work PB3170), but the survey listed it as a DECISION row and the planner counted it as a same-member collision
holding PB5575. PB2622 and PB3231 continue on `worktree-wf_b65a448b-ca0-1`'s successor.
