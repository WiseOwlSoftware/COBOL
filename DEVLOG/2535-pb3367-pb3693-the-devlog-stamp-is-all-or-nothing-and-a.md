## Entry 2535 — 2026-10-25 10:49 PDT — PB3367, PB3693: the DEVLOG stamp is all or nothing, and a branch is landed once

Two faults of the landing path, one root file (`scripts/devlog.py`), fixed together (fix-lane wave 20261107 group F).

**PB3367, the stamp was not atomic.** `devlog.py stamp` wrote the numbered entry and unlinked the pending file before
`git add`. Three landings (2026-10-18 twice, 2026-10-24 once) lost a branch whose gates were GREEN to one transient
`git add` exit 128; the numbered file was left untracked, the pending file gone, so every rerun said "has no pending
entry to stamp" and `enqueue.py` blamed the branch ("needs one `DEVLOG/pending-<slug>.md`"). `_git` hid git's stderr.
Re-probe: a planted `index.lock` in a linked worktree reproduced it exactly. Fix, at the one writer: `_git(check=True)`
raises `GitError` with git's own stderr; `_git_write` retries `index.lock` clashes (0.2 s to 4 s) and nothing else;
`stamp` stages the entry and the removals while the pending files are still on disk, unlinks them only then, commits,
and on any failure restores the pending bytes, resets the index and deletes the numbered file (exit 3, which `land`
maps to exit 2, retried once by the lane). A stamp killed after the unlink is `half_stamp`: `stamp_due` counts it, the
next run completes it under the number the entry holds, and `land`'s dirty check sets its paths aside. A failed stamp
burns an allocator number (a gap the landing rule allows).

**PB3693, a carried branch was landed twice.** A predecessor reviewed CLEAN lands on its own (PB3381), but its carrier
(a branch that merged it) was still WAITING TO LAND, stamped with the predecessor's pending entry folded in, and queued:
PB3665's PR #541 carried PB3613's title through a second pull request on a second merge base, and the `merge=union`
rule kept `CobolNum.GroupDigits` twice (PB3694), bouncing the queue. One rule, read at every place: a branch is landed
once. `prune_worktrees.carrying_landing_branch` holds a WAITING TO LAND carrier behind a WAITING TO LAND or ENQUEUED
predecessor (`waits_on`) and lands a predecessor inside an ENQUEUED branch through it (`carrier`); `enqueue.
open_predecessors` (read by `land` and `land_lane.predecessor_free`) gained the pending arm and `land` waits, exit 15,
before it stamps; `devlog.stamp` drops a pending entry whose title is already a stamped entry of the tree.

Goldens are the three scripts' self-tests (devlog 51/51, enqueue, prune_worktrees), each failing on the old behaviour.
Rows closed: none (process tooling). Self-review found and fixed: `worktree_of`'s git failure sat outside the new
handler; `enqueue.py` imported `devlog` locally three times and now imports it once.

