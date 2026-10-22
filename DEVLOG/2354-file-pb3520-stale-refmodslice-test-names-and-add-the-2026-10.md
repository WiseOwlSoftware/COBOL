## Entry 2354 — 2026-10-22 11:58 PDT — File PB3520 (stale RefModSlice test names) and add the 2026-10-22 witness to PB3310

Register-only change. PB3520 files the review lead R1-3/R2-1 of PB3512 (PR #385): three conformance tests still spell the deleted `MoveKind.RefModSlice`. PB3310 gains a dated witness: a hand-run `enqueue.py land` overlapped the landing lane's attempt on the same branch, the lane's gate reported NOT RUN on the worktree's gate lock, and `enqueue.py` classified that as `GATE RED (Windows)`, wrote a false BLOCKED line and made the survey read the branch STOPPED. The note's planned REFUSED outcome now covers a gate NOT RUN on the gate lock. No code changed.

