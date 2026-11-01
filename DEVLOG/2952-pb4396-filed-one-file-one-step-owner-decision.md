## Entry 2952 — 2026-11-01 10:10 PST — PB4396 filed: one file, one step (owner decision)

The owner asked why work on a file is not done in one step. PB4395 had just been filed `blocked_by` steps 8 and 11 of the
pipeline redesign, which were in flight on the same files, so the same files would have been gated, reviewed and landed
twice. Three earlier owner rules made this the planner's habit too. The group cap of 4 notes (PB3704) split a busy file
into several groups. Same-file successors (PB3670, MANDATORY-PRACTICES O2) made each group a separate implementer, one link
per wave (PB3638). "Nothing is held" (PB3381) landed each link as soon as it was clean, so the next link gated, reviewed
and landed the same files again.

The owner chose "One step per file". PB4396 records the decision and the change. Notes that share files form one group,
sized by the turns an implementer holds rather than a note count. A note on a file an in-flight branch holds joins that
branch. Only true overflow chains, the chain continues on one branch, and it lands once. The combined branch of steps 8,
11 and PB4395 went to the Mythos lane the same morning (worktree-wf_01545e8c-f44-1). The operator held the landing lane so
the step 8 and 11 half could not land alone.

