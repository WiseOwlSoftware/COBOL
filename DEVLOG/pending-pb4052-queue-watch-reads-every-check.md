## The queue watch reads every check on the head, and a superseded run's cancelled ci-gate is no bounce (kb/Work PB4052)

The landing lane's queue watch requeued pull requests whose CI was still running. `ci-gate`, the one required check,
`needs:` every other job and has no check-run until they end, so while `audits` ran the watch saw no check at all, dated the
work from the enqueue and, ten minutes later, closed and reopened the pull request. The reopen's run cancelled the one in
flight, whose `ci-gate` then failed "audits=cancelled", and the branch read BOUNCED with nothing wrong in it. Once
`audits` grew to 22 minutes (kb/Work PB4072) every landing hit it: #773 to #777 each read BOUNCED within 45 minutes on the
afternoon of 2026-10-29.

`queue_facts.read` now keeps every context's newest check-run on the head, read only from the newest workflow run of each
workflow there, so a superseded run's checks are history: the required ones stay `checks`, the rest are `others`, and an
unresolved one carries its job's status. `land_lane.watch_step` holds the watch while any of them runs and dates the grace
from the newest one's end. `enqueue.requeue` takes the head and refuses, touching nothing, while a workflow run on it has not
completed. Self-test arms in all three scripts plant PR #777's shape; DESIGN-orchestrator-loop.md §4.9 says the same.
