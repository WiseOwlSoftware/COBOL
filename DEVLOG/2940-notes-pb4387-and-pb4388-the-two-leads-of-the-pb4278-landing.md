## Entry 2940 — 2026-11-01 05:31 PST — Notes PB4387 and PB4388: the two leads of the PB4278 landing gate reds

The PB4278 finisher found that the landing gate of `worktree-wf_ca25d124-0aa-5` went RED twice for reasons in main, not in the branch. PB4387 files the first: an uncovered sequential writer takes and retires the sidecar entry mutex directory on every WRITE (`UncoveredChangeGuard`, `RetireDirectory`), about ten metadata operations per record. PB4388 files the second: `ProcessObserver.Observe` ignores the result of its post-kill `WaitForExit(5_000)`, so the retry is refused on the held files. No code changed; both notes are open and the branch is re-armed once main carries a fix.

