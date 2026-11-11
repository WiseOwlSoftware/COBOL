## 2026-11-11: the GitHub Actions outage, three merge-group runs, two zombie runs and PR #1510 (class (e), and a merge red behind it)

GitHub's incident "Impact across multiple GitHub products including Actions, Pages, and Copilot products" was created at
20:48:12Z; the Actions component read major_outage at 21:02Z and operational at 21:08Z (githubstatus.com, read by the
controller). Everything below is a red of class (e), GitHub's runners and services, with one exception named in the last
section.

### Three merge-group runs and the upload hang

Merge-group runs 43093640955 (PR #1511), 43093642718 (PR #1512) and 43094625899 (PR #1513) each completed `failure` at about
21:00Z. Every non-green job was `cancelled` with no failed step, at its job time limit, inside the `Upload test results`
step after its tests had passed; `ci-gate` still read `queued` after the run read `completed failure`. The upload step
bounds a failed upload (PB3140) but not a hung one: filed as `kb/Work/PB5328`, which carries the sampled jobs and their
timestamps.

### What became of each pull request

- #1512 and #1513 kept their queue places, and GitHub started new runs for them (43100500655 and 43100502325; the second
  completed success).
- #1511 was removed from the queue and queued once more at 21:08:49Z with `gh pr merge 1511 --auto` (run 43100581588).
- #1510 was re-queued at 21:08:47Z.

### Correction to `2026-11-11-pr1510-ci-gate-never-started.md`

That addendum says #1510 "is re-queued once, with no finisher". That held for the timeout bounce only. The queue then held
#1510 as UNMERGEABLE, because it conflicts with #1512, queued ahead of it, in `scripts/spec/merge_reconciliation.py`
(`git merge-tree --write-tree origin/spec-2002-reconcile-tools origin/spec-2002-catalog` reported the conflict; the
operator's finisher found it first). The controller dequeued #1510 at about 21:19Z. A finisher on account 2 resolves it
after #1512 merges. So one pull request carried two reds in one hour, an infrastructure one (class (e)) and a merge one
(class (a)), and the first hid the second.

### The zombie runs

Pull-request check runs 43097245139 (PR #1514) and 43098272601 (PR #1515) were created during the outage and still read
`queued` at 21:22Z, 14 minutes after Actions read operational. `gh run cancel` and `gh run rerun` both refused, and only a
close, reopen and `gh pr merge --auto` of each pull request cleared them (21:23Z). `enqueue.py requeue` cannot do that
while such a run reads `queued`: filed as `kb/Work/PB5329`.
