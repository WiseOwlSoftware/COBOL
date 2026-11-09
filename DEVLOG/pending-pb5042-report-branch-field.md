## PB5042: a report with no header fields is finished to the wave but nameless to the landing

After the 2026-11-09 restart, the reviewed, gate-GREEN PB5031 branch (`worktree-wf_ae1d041e-326-1`, Review CLEAN at
8ead032f98) still would not land: `enqueue.py land` refused it with exit 11, 'no fix-lane report names it'. Its report
began with the bare `DONE` status word and had no template header line, so `Report.finished` accepted it while
`reports.BRANCH_FIELD` found no branch for the survey. PB5022's report had the same shape and was enqueued by hand.
The operator rebuilt the PB5031 header from facts on disk (worktree, merge-base, branch tip), and the survey then read
WAITING TO LAND, so the loop's landing lane takes it. Filed kb/Work PB5042 (sibling of PB3260) with the code sites and
a fix shape: the one finished predicate requires the branch and HEAD fields, and the wave sends the same agent back to
repair its header.
