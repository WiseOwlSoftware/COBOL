## Entry 2730 — 2026-10-29 12:10 PDT — The queue watch requeued PR #760 while its CI still ran, cancelling the run and faking a bounce: filed as PB4052

The loop's resume unit found branch `worktree-wf_e06e2377-556-4` (PR #760) turned from NOT QUEUED to BOUNCED with no defect of
its own. Before `ci-gate` starts (it `needs:` every other job) no required check-run exists on the head, so the landing lane's
queue watch dated the checks' work from the enqueue. Eleven minutes later it closed and reopened the pull request while `audits`
was still running. The reopen's new run cancelled the in-flight one (`cancel-in-progress` on `pull_request`), and the cancelled
run's `ci-gate` failure then read as a bounce. kb/Work PB4052 records the sites, the timeline and the direction of the fix; the
branch's new CI run continues, and auto-merge queues it once that run passes.

