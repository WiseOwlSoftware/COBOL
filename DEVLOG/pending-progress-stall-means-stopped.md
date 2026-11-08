## The loop's STALL means an agent stopped; over-size and the review rate join the PROGRESS line (PB4944, PB4888 lever 5)

Re-probe. `python scripts/orchestrator/progress.py --coord E:/COBOL-coord` on this tree raised STALL on elapsed minutes alone
(60 for a running group, 15 for a DONE report awaiting review), so a healthy 90-minute campaign implementer drew the same mail
as a dead one. PB4888's lever 5 (the round-1 FINDINGS rate and the queue's empty minutes in the PROGRESS line) was the one lever
the earlier wave left undone.

Fix. `progress.py` now reads two signals. A STALL is a group (running, or DONE and awaiting review) with no act in the
coordination directory for `STALL_IDLE_MIN` (20) minutes; `group_activity` takes the newest mtime of its scratch directory,
the scratch files and reports named for its slug, and for a DONE report the worktree's last act. A gate waiting for a slot
writes its log (17 minutes at worst), so a slot wait is not silence. OVER-SIZE is a group still moving but past
`RUNNING_LIMIT_MIN` (60) or its campaign's own limit, or a DONE report past `REVIEW_LIMIT_MIN` (15); it is counted per wave in
the PROGRESS line and never mailed. The campaign limit is data: `running_limit_min:` on a cluster's lead note, read by
`work.campaign_running_limit` and validated by `work.py check` (`running_limit_problems`: a positive whole number, on a note
some note's `cluster` names). Lever 5 added `round-1 FINDINGS x of y reviewed` (`review_round` reads the reviewer id's `-rN`)
and `merge queue empty m of n min` (`queue_empty_minutes`, the window less the union of the enqueued-to-merged intervals; an
unmerged record counts as queued, so PB3725 understates the figure).

The constants were renamed (`RUNNING_STALL_MIN` to `RUNNING_LIMIT_MIN`, `REVIEW_STALL_MIN` to `REVIEW_LIMIT_MIN`) because they
no longer mean a stall; no caller outside the file used them. Self-test arms were added to `progress.py` and registered in
`self_test_arms.py` (`DECLARED`). Not done, and kept in PB4944: the planner reading the over-size counts to split a campaign
that always runs over (nothing records the counts per unit yet), and a lead note carrying `running_limit_min:`. No goldens:
process-only, no compiled program changes. PB4888 stays open for the post-lever measurement.

Review round 1 (3 blocking, all fixed in progress.py): group_activity now takes the ledger group and also reads one directory below scratch/ (a campaign wave keeps its groups under the wave scratch, so Mythos groups no longer showed no act) and the branch review hyphenated review-w<wave>-<letter> findings file and scratch (the slug never matched it). queue_empty_minutes counts a record with no merged stamp as queued for at most 60 minutes after its enqueue (12 closed, never-merged records from October had made the figure 0 for every window). The self-test plants a wave-scratch group, a review findings file and a closed record.
