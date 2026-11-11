## PB5327 filed: one BOUNCED row decided twice, an infrastructure re-arm and a conflict finisher

At 13:11 on 2026-11-11, PR #1510 (spec-2002-catalog) had two causes at once: GitHub's merge group timed out
(Actions outage) and the survey foresaw a conflict with #1512, queued ahead. The enqueue unit re-armed the branch as
infrastructure and the review lane started a conflict finisher within four seconds of each other. The re-arm's push was
refused (the PR was already re-queued) and the operator killed the finisher, which had been the right call; Mythos then
dequeued #1510 as UNMERGEABLE. kb/Work PB5327 records the defect: `land_lane.py rearm` overrides a head without asking the
review lane's conflict predicate. Notes only; no code changes.
