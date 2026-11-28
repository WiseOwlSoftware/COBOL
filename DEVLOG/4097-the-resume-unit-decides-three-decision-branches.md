## Entry 4097 — 2026-11-28 01:50 PST — The resume unit decides three DECISION branches

The resume unit of 2026-11-27 read the three branches the survey called superseded. `worktree-agent-a79c92744280df84a` is
a second implementation of the PB6232 rule that main landed as e1b2f3c4cf, and `worktree-agent-a2787d9fe53a79a18`'s one
commit reached main as 2bdc5525c0; both are recorded ABANDONED in addenda of PB6232 and PB3259. `closed-hierarchies-wave1`
still carries the PB6168 review fixes main lacks, so it stays a land decision for a conflict finisher (PB6168 addendum).

A later resume unit found a duplicate notes branch, `notes-resume-20261128`, recording the same two abandonments, and
recorded it ABANDONED beside them. It also found why two enqueue units in a row ended with an invalid handoff: each
wrote `"landed": []` where the schema has an object (kb/Work PB6348). The enqueue prompt now states the shape, and the
common unit rules give every unit the supervisor's own `Test-Json` check to run before it ends.

The resume unit of 2026-11-28 01:46 found this branch never queued and landed it. It recorded one more branch ABANDONED:
`worktree-wf_05bb8d55-c0a-6`, whose nine PB2328 commits the wave 20262182 finisher branch carries (PB2328 addendum).

