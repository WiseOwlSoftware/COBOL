## The partition bound's next crossing is filed (PB4114), and the partition comments say only what is true now

The branch review of the 32-partition change (tooling-suite-partitions) left three leads. Two are filed as kb/Work
PB4114. First, nothing warns before main plus a branch crosses `Partitions × 500` rows again: at 32 partitions that is
16,000 rows, about five weeks away. The crossing can only happen in the merge queue, where every golden-adding landing
then bounces. Second, the 32-partition Conformance leg was not re-measured, although the drift test's message asks for
it. The third lead is fixed here. `DESIGN-conformance-suite.md` and the two partition summaries in
`ConformanceSuiteTests.cs` and `ConformancePartitioningDriftTests.cs` now state the current count and bound. The dated
history of the change lives in the DEVLOG entry of the change itself.
