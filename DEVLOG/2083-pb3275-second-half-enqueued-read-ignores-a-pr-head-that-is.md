## Entry 2083 — 2026-10-16 09:12 PDT — PB3275 second half: ENQUEUED read ignores a PR head that is not the branch tip

Appended to kb/Work/PB3275 the second half of the survey defect: prune_worktrees.py:438 reads ENQUEUED whenever auto-merge is on without comparing the PR head with the branch tip (witness worktree-wf_00451f9d-3d9-14, finisher commit 8b7796394 against PR #107 head 98ba99813), with the fix shape and self-test placement.

