## Entry 2879 — 2026-10-31 14:02 PDT — kb/Work PB4011: a second branch stranded by a prose `head <sha>` in its report header

The round-1 review finisher of `worktree-wf_a064f67b-ab6-14` (PB4196, PB4113, PB4199) wrote its report header as
`Base: … (reviewed head 4462da5c8, …)   Head: ec168a1d2…`. `reports.parse_report` reads the predecessor's
`4462da5c8` as the report's HEAD, so the branch survey would have turned this branch, reviewed CLEAN at its tip and
gated GREEN, into a DECISION that the landing lane never takes. The loop's resume unit recorded this third occurrence of
the layout in PB4011 and decided the branch LAND through `enqueue.py land`. That path reads the review stamp's sha and
admits the branch. The note's fix shape is unchanged.

