## Entry 2360 — 2026-10-22 12:54 PDT — A landing is not verified to enter the merge queue: filed as PB3527

On 2026-10-22 pull request #385 (PB3512's fix) sat BLOCKED for 50 minutes with an empty merge queue. Its ci-gate job had completed successfully at 18:46:46Z, but the check-run on the head commit stayed in_progress until a manual `gh run rerun`. The landing lane ends at ENQUEUED and never checks that the pull request entered the queue. The owner called it a process problem. PB3527 holds the facts and the fix shape: a post-enqueue watch, one automatic rerun of a stuck check-run recorded as a bounce cause, an operator alert on a second occurrence, and a row in the worktree brief.

