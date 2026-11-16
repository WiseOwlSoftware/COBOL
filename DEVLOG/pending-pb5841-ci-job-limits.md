## CI job limits sit at twice the longest recent run, and the nightly reports a job at half its limit (PB5841)

Merge-group run 43843342653 (pull request 1769, the PB5801 fix, gate green and review clean) was removed from the queue
because the `audits` job was cancelled at its 10-minute limit. The job was not hung: it ran 354 to 530 s and had grown with
the inventory. The limits had been set once, at about twice a p90 of 2026-10-31, and nothing kept that true.

Measured over the last 20 runs of both workflows, the longest successful run of every job; the limits now sit at least
twice it: `audits` 25 minutes (longest 9.6), `linux-shard` 25 (10.8), `windows-shard` 25 (10.7), `linux-built-self-tests` 20
(7.7), `windows-self-tests` 35 (14.7), and the nightly `differential` 90 (31.9; `MAX_JOB_MINUTES` is now 90). Every other
job was already at least twice its longest run.

To keep it true, `scripts/ci_bounds.py --durations` reads the last 20 runs of each workflow with `gh` and is red for a job
whose longest run times two passes its limit, or when the newest run has a job no workflow job names (so the measurement
cannot silently skip one). It runs as the nightly's new `job-limits` job, not in the tree audits, which every gate runs
and which make no network call. The self-test gained an arm that fails without the check and one that fails if the nightly
stops running it. DESIGN-test-build-ci.md sections 1.4 and 1.4a state the rule, the measurements and the check.

The slow audits inside `audits` are `Script self-tests` (211 s), `Code citations` (66 s) and `Rendered views` (25 s in CI,
150 s in the local gate); making them cheaper is left as a lead in the note.
