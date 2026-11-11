## Entry 3485 — 2026-11-11 12:48 PST — PB5324 filed: prune_worktrees.py --apply reports a failed delete of a branch it had just deleted

kb/Work PB5324 (new, `kind: defect`, `process_only: true`, area tooling/landing), no code.

Cleaning up the landed branches after the reboot (`python scripts/prune_worktrees.py --apply --no-fetch` at origin/main
e3c9e8551a; 24 branches and their worktrees removed, two kept under the 180-minute age rule), the script printed
`deleted origin/worktree-wf_82a7f4cd-626-20` and then `could not delete origin/worktree-wf_82a7f4cd-626-20: ... remote
ref does not exist`. The survey held two LANDED rows for that branch, the local one and the `origin/<branch>` row
kb/Work PB3413 added, and each arm of the apply deleted origin's copy. The branch is gone from origin (checked with
`git fetch --prune` and `git branch -r`), so the second line reports a failure that did not happen. It is the sibling
kb/Work PB3409 did not reach: there the ref was stale before the apply, here it goes stale during it.

The note records the two survey rows, the four output lines, what was and was not verified about the mechanism, and the
fix with its self-test case.

