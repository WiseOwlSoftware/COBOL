## Entry 2902 — 2026-10-31 19:52 PDT — PB380: one of two parallel fixes lands, and the other is abandoned

Two finishers fixed PB380 (the collation runtime's one-time initialization race) from the same round-1 branch, both
reviewed CLEAN. The loop's wave wrote an instance `CacheConfigurationLatch` on `worktree-wf_15d43d1b-129-9`. The Mythos
lane's wave 20261792 wrote a reset under `s_initLock` on `worktree-wf_e6e34c8b-638-1`, which is PR #934 in the merge queue.
The planner had planned a branch that a live successor already contained, and the loop's clerk files that defect. Rather
than spend a conflict finisher reconciling a second design of one fix, the operator keeps #934 and records 129-9 and its
round-1 predecessor 129-2 as ABANDONED in kb/Work/PB380. This landing changes only the register.

