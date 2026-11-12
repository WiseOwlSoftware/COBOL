**Lead from the branch review of build-stage6-ci, round 1 (R1-1, low, not blocking at round 1; filed by the clerk 2026-11-12).**

`scripts/buildgraph.py:637` (`write_records`, on the build-stage6-ci branch; the function is not on main until that branch lands) runs one `git hash-object -w --stdin` subprocess per record, for EVERY record of the union (the ref's own records included, whose blob ids `ls-tree` already gave), on every publish attempt. `publish` runs it on the landing's path (enqueue step 4) between its fetch and its leased push, so the cost is paid inside the lease window and grows with the store.

**Fix direction.** Write only the records the ref does not already hold (reuse the blob ids `ls-tree` returned), and batch the new ones through one `git hash-object -w --stdin-paths` (or a `git fast-import`) call, with a buildgraph self-test arm that counts the subprocesses of a publish over a ref that already holds N records (expected: 0 for the held ones).

Source: report `E:/COBOL-coord/scratch/reports/w20261996b-PB5235-report.md`, `## Review findings`, R1-1; not answered there.
