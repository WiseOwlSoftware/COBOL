## PB3772, PB3849, PB3413, PB3723: the queue watch keeps reading, a notes-only commit is covered, the survey lists origin's branches and foresees a queue conflict

Four defects of the landing pipeline's tooling, fixed in one worktree (wave 20261764, group A), each at the place its rule
lives.

**PB3772, the watch ended at IN_QUEUE.** Re-probe: `WATCH_DONE` still held `IN_QUEUE`, so a pull request the lane saw
queued was never read again; PR #593 sat out of the queue (a ruleset toggle dropped it, auto-merge off, no failed run) for
an hour and the survey showed its last reading as current. `WATCH_DONE` is now `(MERGED, LEFT, ALERTED)`; a queued pull
request is read on every tick and records `queued_at`. One the watch saw queued and finds out of the queue again (no failed
run, no conflict) waits the grace from that last reading, then goes straight to the requeue (its checks passed, so a re-run
is no remedy) and on to the alert as before. `watch_text` says when the reading was made and that an ended watch reads
nothing again, `prune_worktrees.not_queued` says a QUEUED reading under a pull request found out of the queue is the last
one, and `rearm_hold` names what acts. Seen RED with `IN_QUEUE` back in `WATCH_DONE`.

**PB3849, a notes-only commit paid a full gate and a full review.** `prune_worktrees.mechanical` (the one coverage rule the
review gate, the landing's reading of the gate record, the report coverage and the survey all call) counted only main's
commits, clean merges of main and DEVLOG-only commits. `register_path` is now the one definition of a register path, a
`DEVLOG/` entry or a `kb/Work/*.md` note, and a commit changing only such paths is covered. The frontmatter decision: a note's
status, harm flags and `closes_rows` are covered too, because the landing's `work.py check` and CI's `audits` job run
`work_inventory.py` over the claims and the closed rows; a row whose verdict really changed carries an inventory edit, which
is no register path, so that commit is reviewed. Written in DESIGN-test-build-ci.md section 3.17.2 step 2, in
MANDATORY-PRACTICES L2 and L9, the finisher brief and the landing reference. Seen RED (survey arms `reviewed-noted`,
`reviewed-noted-src`; `enqueue.py` `tested_locally` and `review_gate`).

**PB3413, origin's branches were invisible.** `survey()` now reads `refs/remotes/origin/*` (not main, not the merge queue's
`gh-readonly-queue/*`): a branch with no local branch is a row named for it, and one origin moved on past or away from is a
row `origin/<branch>`. It is measured by the same `classify()` and given its state by the same `lifecycle()`, with no
worktree, lock or report; `--apply` deletes a LANDED one on origin under a lease on the judged tip (`delete_origin`, which
`drop_remote` uses too). Every reader fetches with `--prune` (`fetch_origin`) so a branch GitHub deleted leaves no row. The
verdict line counts them, which the session probe prints.

**PB3723, queue conflicts were seen only after GitHub removed the entry.** Fix direction 2 is decided against: PB3643's
trial of exactly the insert-only rule resolved 1 of 32 conflicted merges, in another order, and the wave 20261107 pair that
read insert/insert was revise/revise (the design says so, so it is measured again, not rebuilt). Direction 4 is built:
`merge_drivers.foresee_queue` replays the queue's builds with git's own line merge and no driver (one `probe`, the
`merge-tree` that `merged_tree` and `merge_conflicts` share, `drivers=False` here), skipping the entries the queue would remove; `queue_facts.PullRequest.position`
carries the queue's order; `prune_worktrees.foresee_queued` asks it on every survey and a queued head that conflicts reads
BOUNCED (a FORESEEN CONFLICT naming the entry ahead) with `Row.foreseen`. The review lane's conflict finisher takes it while
the entry ahead is still queued, and `land_lane.py conflict-brief` is against that entry's head. The measurement baseline
(767 landing logs, 139 conflicted files) is in the note; the "after" is read once PB3726 is on main.

What the self-review found: the C# drift tests pinned eight arm labels of the scripts' self-tests by prefix
(`LandingLaneDriftTests`, `LandingPipelineDriftTests`, `MergeDriverDriftTests`); they name the new arms. No diagnostic code
was used.

Gate (run `TestResults/build-local/000002-20261030T043315Z`, head bb3029ec4): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN — leg 2 left to CI's whole-population run — Conformance 67/19,363 · Unit 5,632/38,436 · Characterization 36/36
cases ran (skipped 0) in 1 of 2 leg(s) · Linux half (WSL clone of HEAD) GREEN ===`. The first gate (000001, head 5e20763f8) was
GREEN too; the follow-up commit added only a stderr note to `foresee_queue`.

Review round 1 (R1-1, blocking): the first build replayed the queue THROUGH the drivers, but GitHub's merge group runs
none, so two entries that each add a key to one keyed catalogue (diagnostics.json, the inventory, constructs.json, kb/Work
notes) were read as clean while the queue removes the second, and a third entry was then foreseen against the removed
one. `probe` takes `drivers` (False for the simulation) in place of `attributes_of`, and a self-test arm with the reviewer's
scenario (seen RED first: k3 foreseen against the removed k2) pins it. R1-2 (the conflict brief's claim that bringing the
head in keeps the pull request queued) is a lead for the clerk.
