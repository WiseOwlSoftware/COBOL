# PB2652, 2026-11-23: the duplicate branch of account 2's wave 20262143 is abandoned

worktree-wf_6f9eca3a-cc1-21 ABANDONED: it duplicates worktree-wf_44cf5ea1-3ec-2 (wave 20262147 group B), which landed as pull
request #1996. Both branches were planned in parallel over the same predecessor (worktree-wf_46d3a156-23e-6, wave 20262139
group D) and did the same notes (PB2652, PB2742, PB2744, PB2763, PB2776, PB3213, PB3217, PB3223, PB3227) independently; account
2's planner took the PB1754 branches as finish-first rows of its PB3160 cluster by mistake and handed the branch back to account 1
(mailbox 2026-11-23 02:52). Nothing on it is owed: its two `2026-11-23-editions.md` addenda set `editions: []`, which PB2763 and
PB3213 already carry on main, and its code is a second implementation of what #1996 landed. The planner gap is account 2's PB6059.
