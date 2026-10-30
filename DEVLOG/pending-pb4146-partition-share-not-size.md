## PB4146: test partitions are bounded by their share, never by size; the optional-word sweep's row is one spelling

**The defect (kb/Work PB4146, process only).** The conformance suite's runner and the optional-word sweep were guarded by
absolute size bounds in `ConformancePartitioningDriftTests` (500 rows a suite partition, 3 formats an optional-word
partition). The suite only grows, so every crossing turned the drift test red on every golden-adding branch until
someone raised `Partitions` by hand (#811's merge group at 12,005 rows, 24 × 500), and it pushed an implementer to trim
a golden to one edition to stay under the bound (PB4096). Re-probe at base 90da780ef, after the operator's bump to 32:
3,113 test rows and 8,900 refusal rows, 12,013 in all, 376 a partition, so the bound would turn red again 3,987 rows
later.

**The design correction (DESIGN-test-build-ci.md §3.11 "How many partitions").** A partition's time is its family's
time over P; the leg's ideal wall is the leg's time over the host's T workers; so a partition stays under the wall
while P ≥ T × (the family's share of the leg). Neither side moves when a family grows with the leg, so the row total
was never the right input: the count is chosen from the host's parallelism and the family's share, kept at its
measured floor, and re-measured when the host or the shares change.
- **The size bounds are gone.** `ConformancePartitioningDriftTests` keeps the measured floors (24 suite, 8 sweep,
  kb/Work PB2527) and asserts the sweep's row is one spelling.
- **`TestPartitionAudit` gained a share check** for every partitioned family in both test assemblies: each partition
  holds ⌊n/P⌋ or ⌈n/P⌉ of each source's n rows, which the stride gives by construction. It bounds a partition
  relatively, so growth alone never turns it red. Seen red twice: a planted count in the new Unit fact
  `TheShareCheck_NamesEveryPartitionOutsideItsEvenShare`, and a planted block split of the sweep (P0 holding 200 of 380
  rows; eight violations, the audit red), reverted.
- **The optional-word sweep's row is `(format, mask)`, one spelling**, compared with the format's bare spelling. The
  bare spelling's output is observed once per test process for every partition by `OptionalWordBaselines` (outside the
  generic family, whose statics are one per partition). As a row per format, a format's whole power set (128 compiles
  for `special-names-85`, 41-59 s) landed in one partition, which is why the sweep needed its 3-formats bound; now the
  stride spreads every format. 9 partitions became PB2527's measured 8. A failing spelling now reports itself instead
  of stopping its format at the first failure. The architecture oracle's case is per spelling
  (`optword/<format>/<unit>`), so `compare_oracle.py` against a base before this change reports those ids as added and
  removed once.
- **The partition classes stay written, not generated.** The count changes only with the host, and deriving it from
  the row total is impossible in xUnit v2's class-per-collection model without a second copy of the partition rule: the
  suite's rows exist only at test time (`SuiteManifest` runs `generate.py`), and `scripts/gate_plan.py` models a
  collection as a class. The note proposed generation; this departs from it on that evidence, and its "Done when" holds.
- **Docs.** The summaries of `ConformanceSuiteTestsBase.Partitions` and `OptionalWordSubsetDriftTestsBase.Partitions`
  state the current design without dated history (review lead R1-3 of #828); §3.11's check table and §3.11.1's rows,
  and DESIGN-architecture-review.md's `optword/` case, are current.
- **PB4096's golden is restored to `accept: [2002, 2014, 2023]`.** It matches its `.out` at all three editions (CLI
  re-probe, and the three suite rows green).

Self-review: architecture (one partition rule, `TestPartitioning.Slice`; one share rule, `TestPartitionAudit.Share`;
the baseline cache in one non-generic home), full code (each spelling compiled once per process, as before; the
`PROGRAM-ID` uniqueness assertion kept), performance (the sweep compiles the same programs, now spread over 8
partitions instead of 9 coarse ones), duplication (no second copy of the stride or the share).
