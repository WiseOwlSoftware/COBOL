## Entry 2472 — 2026-10-24 13:05 PDT — PB3628: NOT QUEUED reads only a pull request at the branch's tip, so a re-armed branch is no longer stranded

**What happened.** On 2026-10-24 branch `worktree-wf_0e51b14b-ced-6` (PB3155) bounced in the merge queue (PR #467, ci-gate
failed). A finisher fixed it, a reviewer stamped the new tip CLEAN and `land_lane.py rearm` re-armed it at 08:12Z. By 09:45Z
no attempt had started. `land_lane.py status` still said "the next tick lands it again" while the survey read the branch
NOT QUEUED and the queue watch said LEFT (BOUNCED).

**Re-probe.** The witness had moved on, so the repro is the survey's own self-test fixture: a pull request at an older head
than the reviewed tip, auto-merge still on there, a red required check, merge state DIRTY. On the unfixed tree
`prune_worktrees.py --self-test` fails three new checks, all reading `NOT QUEUED` (the PR #467 shape).

**Root cause.** `prune_worktrees.lifecycle` read NOT QUEUED from `pr.auto_merge_at` alone, commented "GitHub's promise to
queue the pull request at ITS head, whatever the local tip". `enqueue.py land` force-pushes the tip onto the same pull
request and queues it again, so a pull request at an older head belongs to the OLD landing. The queue watch had already
ended it, and the survey's own BOUNCED arm skips it for the same reason (`at_head`: "a tip that moved past the bounced head
is the fix, and reads by its report again"). Only the NOT QUEUED arm still claimed it. NOT QUEUED is a state `candidates`
and the enqueue unit do not act on, so the branch waited for good. The note proposed widening `land_lane.candidates`; that
would have launched a landing for a pull request at the tip as well. The defect is the survey's, not the lane's.

**Fix.** `lifecycle` reads NOT QUEUED only for a pull request at the branch's tip. One at an older head falls through to
the report arms, so a branch reviewed CLEAN reads WAITING TO LAND and the next tick lands it, re-armed or not. Two siblings
in the same change: `landing_state` said "no pull request is open" of a branch whose pull request is open at another head
(it now names the head), and the REARMED status line promised a landing the lane would never start (`rearm_effect` reads
the last survey, and `land_lane.QUEUE_OWNED` replaces two hand-written state tuples in `candidates`). Design doc
`DESIGN-orchestrator-loop.md` section 4.9 and the `prune_worktrees.py` state table say it.

**Tests.** `prune_worktrees.py --self-test`: the `unreviewed` fixture now reads WAITING FOR REVIEW; the NOT QUEUED evidence
check runs on a pull request at the tip; new checks for the stranded shape, the re-armed variant through
`land_lane.candidates`, and the exit-0-at-another-head evidence. `land_lane.py --self-test` case 12c: the finisher's tip
launches, a re-armed NOT QUEUED row does not, and its status line says why.

**Left alone.** A pull request IN the merge queue reads ENQUEUED whatever the local tip. A commit made after the queue took
it cannot be pushed without ejecting it, so the queue merges first and the branch then reads by its content.

