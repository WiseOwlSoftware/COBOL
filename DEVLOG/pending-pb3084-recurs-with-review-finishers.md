## PB3084 recurs with review finishers: three carried predecessors would read DECISION once they age

The loop's resume unit on 2026-10-25 surveyed every branch with `prune_worktrees.py --min-age 0` and found three DECISION
rows ("DONE, but the report's HEAD ... is not a commit of this branch"): `worktree-wf_6e0a917c-500-4`,
`worktree-wf_6e0a917c-500-9` and `worktree-wf_1341dc87-1c4-1`. Each is wholly contained in the branch its report is
about (500-26, 500-24 and 1c4-5). The review finisher that wrote each report named the branch it merged in the report
header ("(branch `...-500-4`)", "includes the reviewed branch", "Merged the reviewed branch"), and the survey's loose
`BRANCH_FIELD` read that prose as a branch field. `plan_wave.BRANCH_FIELD`, the field-position rule the note's fix
shape adopts, returns only each report's own branch on all three headers. Because the predecessor holds a report,
`contained()` never names its carrier. The rows read IN FLIGHT only while they are under three hours old, and their
carriers wait behind the PB3308 CI outage. kb/Work PB3084 records the recurrence and this new harm. No code changed.
