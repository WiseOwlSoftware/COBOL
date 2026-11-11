## PB5328 and PB5329 filed from the 2026-11-11 GitHub Actions outage, with a PB1957 addendum

GitHub Actions had an incident on 2026-11-11 (created 20:48:12Z, major_outage at 21:02Z, operational at 21:08Z, read from
githubstatus.com). Three merge-group runs (43093640955, 43093642718, 43094625899) failed with every non-green job cancelled
at its time limit inside the `Upload test results` step, after the tests had passed. PB5328 records that the shared upload
action retries a failed upload (PB3140) but nothing bounds an upload that hangs, and that the job's time limit was the only
bound in `action.yml` and both callers.

Two pull-request runs (43097245139, 43098272601) stayed `queued` after the incident ended and refused both `gh run cancel`
and `gh run rerun`. PB5329 records that `enqueue.py requeue` calls a run live on its `status` alone, so such a run would make
it refuse that pull request on every tick.

The addendum `kb/Work/PB1957/2026-11-11-actions-outage-three-runs-and-1510.md` files the runs as class (e) findings, records
what became of each pull request, and corrects the earlier PR #1510 addendum: #1510 was also UNMERGEABLE against #1512, a
merge red the infrastructure red had hidden. No code changed.
