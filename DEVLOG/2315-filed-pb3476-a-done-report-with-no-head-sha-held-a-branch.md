## Entry 2315 — 2026-10-20 18:01 PDT — Filed PB3476: a DONE report with no HEAD sha held a branch reviewed CLEAN out of the landing lane

Wave unit 1137 planned 0 groups for the second unit running. PB3337, PB3460, PB1278, PB3338 and PB3340 all waited
on a file-set collision with `worktree-wf_722bb28c-219-2` (PB3471), which had been reviewed CLEAN at its tip but
still read IN FLIGHT. The cause was the report header `HEAD: see the trailing commit`. With no sha in it,
`prune_worktrees.lifecycle` treats the report as describing no commit and never checks the review stamp, so after
`--min-age` the branch would have become a DECISION. The unit wrote the tip sha into the scratch report, and the
survey now reads the branch WAITING TO LAND. PB3476 records the missing header check and the lifecycle's ordering
of evidence.

