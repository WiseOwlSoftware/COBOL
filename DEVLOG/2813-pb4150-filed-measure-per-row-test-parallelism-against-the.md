## Entry 2813 — 2026-10-30 14:43 PDT — PB4150 filed: measure per-row test parallelism against the partition classes

The owner asked whether one test at a time per class is a bottleneck. It is: xUnit 2.9.2 runs a class as one serial collection, which the partition classes work around. Section 3.11 of DESIGN-test-build-ci called more parallelism exhausted, but measured that under the lexer lock M6 has since removed. kb/Work PB4150 is the analysis: measure an in-class parallel framework (Meziantou.Xunit.ParallelTestFramework) on the Conformance assembly against the 32 partitions, then adopt it or keep the partitions with the measured reason.

