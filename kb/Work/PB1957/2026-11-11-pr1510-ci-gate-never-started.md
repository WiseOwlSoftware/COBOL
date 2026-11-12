## 2026-11-11: PR #1510 removed from the merge queue on `checks_timed_out` with every job green (class (e), GitHub's runners)

Pull request #1510 (branch `spec-2002-catalog`, head 6d58b037ac, kb/Work PB5256) was removed from the merge queue at
20:08:06Z, sixty minutes after it entered, with the reason `checks_timed_out`. No job failed.

What GitHub recorded (read 20:30Z to 20:35Z):
- Merge-group run 43084093039 on `gh-readonly-queue/main/pr-1510-e3c9e8551a`, merge commit 78544570d6, created 19:06:38Z.
- `commits/78544570d6/check-runs`: 19 check runs, each `success` or `skipped`; the last completed at 19:39:03Z.
  `ci-gate`, the one required check, is not among them: the job never started.
- The matrix jobs started at 19:28:47Z, 16 minutes after `changes` completed at 19:12:29Z.
- At 20:32Z the run still read `in_progress`, and `actions/runs/43084093039/jobs` answered 502 or 504 on five reads.
- Run 43084616737 (#1511 stacked on that same merge commit) completed `success`.
- The three live runs at 20:32Z (43093640955, 43093642718, 43094625899) showed jobs whose limits are 15 and 20 minutes
  reading `in_progress` 24 minutes after their 20:08:47Z start. githubstatus.com read "All Systems Operational".

Classification: infrastructure. The branch's cases all ran green in the merge group; what did not happen is GitHub
starting the `ci-gate` job. The branch is unchanged and is re-queued once, with no finisher.

What this adds to the parity record:
- **Class (e), a required check that never starts.** `ci-gate` is `if: always()` with `needs:` on every other job, so
  it cannot report until GitHub schedules it. When the scheduler stalls, the queue's sixty-minute limit removes the pull
  request and the branch reads BOUNCED with the same words a hung test would produce. The signature that tells them
  apart is mechanical: every check run on the merge commit is `success` or `skipped` and `ci-gate` is absent.
- The session probe printed this bounce as "ONE finisher fixes the branch". A finisher has nothing to fix here. The
  landing lane's queue watch classifies INFRA and re-queues once (`land_lane.py`); the probe's branch line does not
  apply that classification when the lane is stopped, as it was for the reboot.
