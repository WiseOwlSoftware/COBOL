## PB3527: the landing lane watches each pull request into the merge queue, and a held gate lock is BUSY, never red

**Why.** On 2026-10-22 PR #385's `ci-gate` job completed success at 18:46:46Z while its check-run on the head commit
stayed `in_progress` for 50 minutes; the merge queue was empty, the pull request BLOCKED, and nothing looked, because a
landing ends at `=== ENQUEUE: ENQUEUED ===` and nothing after it checked that the pull request entered the queue. The
owner (12:45 PDT): "Why did it get stuck? That is a process problem." The same hour `enqueue.py` read build-local's
"NOT RUN — another gate holds this worktree's gate lock" as GATE RED (exit 5) and sent PB3511's branch to a finisher
with nothing to fix.

**Re-probe.** On this branch, before any change: a pull request with auto-merge on and not in the queue (PR #385's
shape) read ENQUEUED in `prune_worktrees.py`, its evidence claiming "is in the merge queue" (`PullRequest.queued`
counted auto-merge as queued); the held-lock verdict line was not GREEN to enqueue.py's own GREEN regex, so exit 5.
Live, `queue_facts.py` reads the queue's own entries (PR #398 IN THE QUEUE at the time of the probe), and GitHub's
check-runs for fb899d188e35 show the stuck one (120018552758) since resolved, beside the re-run's (120043423624).

**Mechanism 1 — the queue watch.** `scripts/orchestrator/queue_facts.py` is the one reader of GitHub's facts, moved
out of `prune_worktrees.py` with every caller changed: one GraphQL query for `mergeQueue(branch: "main")`'s entries and
each open pull request into main (merge state, auto-merge time, the newest removal from the queue, the head's
check-runs), and the jobs API only for a required context's (`ruleset.required_contexts`) unresolved check-run, so a
check-run in progress whose job completed reads stuck. The landing lane's tick reads those facts once (before its
fetch, so a pull request the queue merges in between is never missing from both) and watches every exit-0 attempt's
pull request (`land_lane.watch_step`, a pure state machine; `watch_queue`): in the queue or merged ends it; a failed
required check or a queue removal ends it as BOUNCED (the finisher's); past a 10-minute grace after the required
checks' work ended, read 1 re-runs the stuck check's workflow run, read 2 closes, reopens and re-queues the pull
request (`enqueue.rerun_check`, `enqueue.requeue`, behind `queue`'s ACTIVE guard: enqueue.py stays the only file that
queues), and read 3 sends the operator a mailbox question and acts no more. Under a stop it reads and acts on nothing.
Each read is recorded on the attempt and as a `stall` in `landing_measure.py` (cause and read number; a stall is not
a bounce, since `bounces` counts failed merge-group runs and `complete` re-reads them, so a stall in `bounce_causes`
would break the record's own consistency rule on the next completion). The branch survey gains NOT QUEUED (`--brief`
lists it with how long it has waited, GitHub's facts and the watch), and a required check that failed before the queue
reads BOUNCED, never ENQUEUED. The survey now tells an unobserved read (None, the git-only lifecycle) from an observed
"no pull request open" ({}): the self-review found that an exit-0 attempt whose pull request was closed without
merging read ENQUEUED ("no pull-request read says otherwise") although a read said otherwise; it reads NOT QUEUED now.
The silent-failure lens found a second: an unresolved check-run whose job the jobs API could not read counted as
"still running", so a jobs API that kept failing would have held the watch for ever; such a check now starts the clock
at the enqueue and, past the grace, is a stalled read that names the unread job. A sleep loop in enqueue.py was rejected: a landing that waits holds its runner for the
whole CI time, and a runner that died would leave its pull request unwatched.

**Mechanism 2 — one verdict reader.** The gate driver's verdict for a held gate lock is now `BUSY`, exit 3: it
measured nothing and nothing is wrong. `run_gate_legs.read_verdict` reads a verdict line as GREEN, BUSY or RED, and
`enqueue.py` reads through it: a BUSY gate ends `GATE BUSY`, exit 12, with no `BLOCKED:` report line, and the lane
retries it at the same head once the worktree's gate lock is free (`land_lane.wait_free`, the retry-when-free class
shared with exit 3, keyed by exit code in `WAITS`).

**Tests.** Self-test arms: `land_lane.py` (queued, merged, stuck then cleared by the re-run, stuck then cleared by the
reopen, the alert, a stop, unobserved facts, exit 12 held then free, the real lock probe), `queue_facts.py` (the queue's
entries, stuck, the jobs API only for an unresolved required check, a red, offline), `enqueue.py` (the remedies and
their guard, BUSY at land and in `run_gates`, the queued line), `landing_measure.py` (stalls), `run_gate_legs.py`
(BUSY and the verdict reader), `prune_worktrees.py` (NOT QUEUED with its wait, the lane's ENQUEUED attempt with
auto-merge off, a red before the queue, offline). The new arms are pinned in `LandingLaneDriftTests`,
`LandingPipelineDriftTests`, `LandingMeasureDriftTests` and `GateLegDriftTests`. Docs: DESIGN-orchestrator-loop.md
section 4.9 (step 4 and the table), DESIGN-test-build-ci.md sections 3.14.3 and 3.17.2, `units/enqueue.md` step 3,
`units/resume.md`, MANDATORY-PRACTICES L12, the workstream landing reference, DOC_INDEX, session-probe.
