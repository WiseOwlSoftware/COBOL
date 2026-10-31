## Entry 2871 — 2026-10-31 12:58 PDT — CI's concurrent-job budget is 60 on GitHub Team: the workflow comment and the design say so, and the merge queue's width of 5 is re-measured and kept (PB3773)

The org moved to GitHub Team on 2026-10-25, which gives standard hosted runners 60 concurrent jobs where the Free plan gave
20. The workflow's comment at the `pull_request` trigger and `DESIGN-test-build-ci.md` section 3.17.2 step 5 still described
"a public repository's 20-concurrent-job limit"; both now name the organization's 60.

Re-probe, on the live repository: `gh api orgs/WiseOwlSoftware` reports plan `team`. Over the day's 60 merge-group runs a
full group is 23 jobs and peaks at 18-19 running at once; an inert-lane group is 13 jobs. The busiest moment, five groups
at once (two full, three inert-lane), peaked at 40 running jobs; every job started within a second of its run's creation, and
a full group's wall was 13-16 minutes, the time it takes alone. The budget holds three full groups at once (about 57
jobs). The day never put more than two full groups in flight, because a wave that adds refusals lands one branch at a time on
the driver-file rule (PB4147).

Decision: `max_entries_to_build` stays 5 in `scripts/orchestrator/ruleset-main.json`. A fourth or fifth simultaneous full
group would queue part of its jobs behind the budget, which costs no more than the serial order width 3 would impose, and
width 5 keeps inert-lane groups parallel. Section 3.17.6 of the design records the measurement and the condition for
narrowing (a measured day in which full groups wait for runners). No ruleset change, so nothing to apply; `ruleset.py check`
reports ACTIVE with the committed value.

No code changed, so no golden; the note claims no inventory row and closes none.

