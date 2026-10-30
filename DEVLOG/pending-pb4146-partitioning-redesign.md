## PB4146 filed: the conformance suite's partitioning should scale itself

The suite crossed its partition bound on 2026-10-30 (12,005 rows, 501 a partition at 24): #811's merge group went red,
and every branch that adds a golden waits until the operator raises `ConformanceSuiteTestsBase.Partitions` by hand
(branch tooling-suite-partitions). The PB4096 implementer cut its new golden to one edition to stay under the bound. The
bound itself is right in purpose (PB2527: no serial pole in the Conformance leg), but a hand-maintained constant with
hand-written partition classes guards a set that only grows. The owner asked for a redesign, designed in the Mythos lane
after the operator's bump lands: derive the partition count from the row total, generate the partition classes, and make
the drift test guard coverage and measured balance instead of total size.
