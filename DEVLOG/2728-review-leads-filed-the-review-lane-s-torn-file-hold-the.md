## Entry 2728 — 2026-10-29 12:00 PDT — Review leads filed: the review lane's torn-file hold, the landing lane's ghost retry rows

The enqueue unit's clerk filed two review leads as notes and retired a third. kb/Work PB4049 comes from review round 2 (R2-1) of
pull request #760 (kb/Work PB3608): a torn-file hold sends the hand-dispatched reviewer into the review lane's own findings file, and
once that file is mended `review_lane.plan_review` starts a second reviewer of the same round. kb/Work PB4051 comes from the enqueue
unit's own read of `land_lane.py status` on 2026-10-29: `status_lines` and `agent_work` read the latest attempt of every branch the
lane ever attempted, so deleted branches print RETRY-ONCE / RETRY-WHEN-FREE rows and inflate the header's "to retry" count.
PB4050 (review R1-1 of pull request #759, collapsed space runs in two notes' Development line quotes) was not filed: the branch head
already carries the fix (commit 7f4bf7007), so there is no open defect to record.

