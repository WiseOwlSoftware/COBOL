## PB3275: a pull request's state is read against the head it was observed at (BOUNCED after a push, NOT QUEUED past a dead head)

**Re-probe.** The note's variants 1 and 2 (ENQUEUED read from auto-merge alone; a conflicting or red pull request that never
enters the queue) had already been fixed by PB3527: `queue_facts.PullRequest.in_queue` is the only ENQUEUED fact, and
`conflicts` / `red` read BOUNCED. Their self-test cases stand. Two arms of the same mechanism, a pull-request state read
against a head it was not observed at, were still open on this build:

1. **Variant 3 (BOUNCED forever after a push).** `PullRequest.bounced` was true while the queue had removed the pull
   request and auto-merge was off, and `lifecycle` compared the branch tip with `pr.head`, the CURRENT `headRefOid`.
   A finisher that pushes its fix moves `headRefOid` to the tip, so tip == head held again and the row read BOUNCED for
   ever. Witness: PR #268 (live timeline read with `gh api graphql`): the `RemovedFromMergeQueueEvent` at 03:26:03Z is
   followed by `PullRequestCommit` items cc441f527, 891f8604e, c1421bf44.
2. **Variant 1's second half (NOT QUEUED forever on a dead head).** A pull request with auto-merge on whose head
   CONFLICTS with main or has a failed required check can never be queued at that head. `lifecycle` sent every
   auto-merge-on pull request to NOT QUEUED whatever the tip, so a finisher's tip that had moved past that head never
   read WAITING FOR REVIEW or WAITING TO LAND. Reproduced first by the new self-test case (DIRTY and red both read
   NOT QUEUED).

**Fix.** `queue_facts.py`: the GraphQL read asks for the last 100 removal and commit timeline items (`pushes`), and
`PullRequest.pushed_since_removal` is true when a commit follows the newest removal (`pushed_since_removal()`, the one
reader of the window; a window with no removal holds only commits newer than the removal). `PullRequest.bounced` is empty
once a push followed the removal, so every consumer (the survey, the landing lane's watch, `queue_facts.py`'s own listing)
reads one answer. `prune_worktrees.lifecycle`: the NOT QUEUED arm requires auto-merge on AND (the head is the tip OR the head
is queueable); a conflicting or red head promises nothing for a tip that moved past it. The existing healthy-head behavior is
kept (a case pins it).

**Tests.** `queue_facts.py --self-test`: bounced only at the removal head, the timeline-window reader over seven shapes.
`prune_worktrees.py --self-test`: a removed pull request whose headRefOid is the tip reads BOUNCED, and WAITING TO LAND
once a commit follows the removal (mutation-checked: removing the new clause turns it RED); a tip past a DIRTY, a red and
a healthy auto-merge head reads WAITING TO LAND, WAITING TO LAND and NOT QUEUED.

**Sibling sweep.** Consumers of `PullRequest.bounced/removed/head`: `prune_worktrees.lifecycle` (fixed), `land_lane.watch_step`
(compares the head with the attempt's end head first, so a pushed head already ends its watch), `queue_facts.main` (labels
the new state). Arms that read the CURRENT head's facts (`red`, `conflicts`) are right for a pushed head: the checks and the
merge state belong to it.

**Gate.** See the branch report.
