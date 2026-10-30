## Entry 2795 — 2026-10-30 12:05 PDT — The conformance suite runs as 32 partitions: it crossed the 500-row bound at 24

The merge group of #811 (worktree-wf_f1d1c695-17b-15, PB3845; run 41107427092) went red on both operating systems in
`ConformancePartitioningDriftTests.TheSuiteRunner_KeepsItsPartitionCount_AndBoundsEveryPartition`. The message was:
"the suite has grown to 12005 rows, 501 per partition at 24 partitions (bound 500)". The guard did its job (kb/Work
PB2527): a serial collection may not quietly grow back into the Conformance leg's wall. But the bound is a property of
main plus the branch, not of the branch alone. Every later landing that adds suite rows would have bounced the same way
until the count was raised.

`ConformanceSuiteTestsBase.Partitions` is now 32, one partition per processor of the 32-way gate host. The change adds
`ConformanceSuiteTests_P24` to `_P31` and `Slot24` to `Slot31`; the slot ladder's top is set by the widest family, and
`TestPartitionAudit` checks the ladder and the exact cover. A partition now holds about 375 rows, and the bound is
reached again at 16,000 rows. CI selects the family by the name prefix (`FullyQualifiedName~ConformanceSuiteTests`), so
the new classes need no workflow change. The docs that state the count are updated: DESIGN-test-build-ci.md §3.11.1,
VERSION_TEST_MATRIX_DESIGN.md and DESIGN-conformance-suite.md.

Why no local gate saw it (THE CI INVARIANT): 17b-15's gate measured the branch at its head. Its base had fewer rows, so
the drift test passed there. The merge with main, which crossed the bound, is the merge queue's to test (owner
2026-10-23, kb/Work PB3612). A bound on the sum of main and a branch can only be crossed in that merge. The remedy is the
headroom above, raised before the bound is reached again; it is not a local re-gate of every merge.

