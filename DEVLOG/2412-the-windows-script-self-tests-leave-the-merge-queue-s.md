## Entry 2412 — 2026-10-23 13:20 PDT — The Windows script self-tests leave the merge queue's critical path, and prune_worktrees stops spawning git per pair (PB3581, PB3583; PB3582, PB3584, PB3585 filed)

The owner asked for the Windows script self-tests to be analyzed for duplication, redundancy and inefficiency, and
asked why the data was not read from GitHub Actions, where they run. It was: merge group run 40004159292 ran the same
`self_tests.py` on both runners. Windows: 95 self-tests in 440 s of wall (1,444 s summed). Linux: 81 in 37 s (144 s
summed). The same self-test runs 10 to 50 times slower on Windows (prune_worktrees.py 168.3 s against 7.2,
landing_check.py 127.4 against 3.2, devlog.py 47.2 against 1.9, line_endings.py 10.1 against 0.2).

**PB3581, landed.** The Windows self-tests were the last step of `windows-build-test`, 7.4 of its 16.5 minutes, and
that job is the merge queue's longest (the NIST guard, next, ended four minutes earlier). The default phase never builds,
so the step waited for nothing: it is its own job, `windows-self-tests`, with the line-ending check over a Windows
checkout, wired into `ci-gate`. `SelfTestDiscoveryDriftTests` holds the runner to the new job and refuses it back in
`windows-build-test`. Expected: the queue's wall falls to the guard's 12.5 minutes; to be measured on the first merge
groups.

**PB3583, landed.** A `GIT_TRACE2` of `prune_worktrees.py --self-test` counted 4,636 git processes; 1,746 were
`contained()`'s `merge-base --is-ancestor <sha> <sha>`, one per (candidate, carrier) pair. It now reads one `rev-list
<carrier> ^origin/main` per carrier into a set (exact: a candidate is never MERGED, so its tip is no ancestor of
origin/main). Same trace after: 3,140 processes, 154 sha-to-sha ancestry calls (the per-row checks), self-test GREEN.

**Filed for the fix lane.** PB3582: the self-tests are git-process-bound, a Windows process start costing tens of
milliseconds; trace each hot one and batch its git reads. PB3584: fifteen self-tests each build a scratch repository,
seven through a git helper of their own; one shared fixture. PB3585 (analysis, blocked on PB3582): the budget cap is
`os.cpu_count()` for work that waits on process starts; measure a larger cap on CI.

