## PB3769: CI cancels the merge-group runs the queue has superseded

The owner, 2026-10-25 ~13:45 PDT: "cancelling superseded runs automatically" (PB3765's second requirement, filed and
landed on its own as PB3769; the fast lane and the bounded bypass stay open in PB3765).

**The mechanism.** When a merge-queue entry fails or leaves, GitHub rebuilds every group behind it with new commits and
new runs, and the old runs ran on to the end. After #591's ejection at 20:18Z five superseded runs (40228927401,
40228888256, 40230142896, 40230142122, 40230639741) held runners under the concurrent-job limit until they were
cancelled by hand. The workflow's `concurrency` group cannot catch them: each group's ref is unique.

**Re-probe.** A dry run of the new sweep against the live repository (own run 40231561214) listed 6 unfinished
merge-group runs and 5 live entries: the five entries' runs KEEP, and run 40230639741 (pr-596, still `queued`, its commit
heading no entry) CANCEL. That confirmed the shapes the script reads: an entry's `headCommit.oid` is its run's
`head_sha`, and a run's branch is `gh-readonly-queue/main/pr-<n>-<base sha>`.

**The fix.** A new `supersede` job, the first job of every merge-group run (no `needs`, `merge_group` only), and the one
job granted `actions: write`. It runs `scripts/orchestrator/cancel_superseded.py`, which lists the unfinished
merge-group runs FIRST and then reads the queue's live head commits through the new `queue_facts.live_heads`. The order
matters: a run created after the read would otherwise look superseded. It cancels each run of the queue on main whose
head commit heads no entry, never its own run or a live entry's. It cancels nothing when the queue cannot be read whole,
reads empty, or does not hold the run's own head commit, and each of those cases is a warning annotation. Every outcome
exits 0 and the step is `continue-on-error`, so housekeeping never decides a verdict. A separate job was chosen over a
step in `changes` so that `actions: write` never reaches the job that runs the branch's own classification code.

**Tests.** `cancel_superseded.py --self-test` covers 16 arms: keep, cancel, own run, the three fail-safe holds,
completed, other events, other branches, malformed items, list-before-read order, dry run, a refused cancel, an
offline or absent queue, a partial run list, and exit codes. `queue_facts.py --self-test` adds two `live_heads` arms.
`LandingPipelineDriftTests.TheMergeGroupRun_CancelsTheRunsTheQueueSuperseded_AndNothingElse` holds the job's wiring and
both scripts' arms. DESIGN-test-build-ci.md sections 3.17.2 step 4, 3.17.6 and 3.17.8, plus DOC_INDEX, describe the job.

**Rows.** None: process tooling, no ISO rule.

**Self-review.** One mechanism, one reader of the queue's facts (`live_heads` lives in `queue_facts.py`), and least
privilege. Found and left as a lead: six drift-test classes each carry their own workflow job splitter, with
different semantics.
