## PB4325: the long serial tests are split (step 2 of the pipeline redesign PB4323)

xUnit 2.9.2 runs one class as one serial collection, so seven classes each set a floor under their assembly's wall clock
(PIPELINE-REVIEW section 2, measure-M3 F4). This entry splits each at the place its cost sits, test code only, no
inventory row.

**The runner-drift verdict comparison** (`BothRunners_GiveEverySuiteTestTheSameVerdict`, one `[Fact]`, 109 to 232 s local,
352 s on Windows CI) is now the partitioned family `BothRunnersVerdictDriftTestsBase<TSlot>` (8 partitions over the 89
in-suite tests, about 11 each). A partition runs `run.py` once per edition its share names, with `--test` per id, under
`ObservationBudget.ForChildren`; it asserts that `run.py` ran exactly the ids it was asked for and that each verdict matches
the in-process runner's. The whole-population assertion (the per-edition suites hold exactly the manifest's in-suite
tests) stays once, as `BothRunners_RunExactlyTheSameTests`, because an inherited `[Fact]` would run in every partition.
`SuiteManifest.Generated` now generates the suites once per test host (10 s) and every partition and the manifest share
them; the old test generated its own copy. The family is enrolled in `ArchOracle.Sources` (`ArchOracleDriftTests` demands
every partitioned row source be), mapped to the suite-test case it is a subset of.

**StorageForm NIST sweep**: `Partitions` 8 to 16 (the slowest of the eight, `_P6`, took 131 s on a loaded gate). **Run-unit
file lock pairs**: the 768 (sharing x open mode x organization) pairs are `RunUnitFileLockPairDriftTestsBase<TSlot>`, 8
partitions of 96; the three other cases stay in `RunUnitFileLockDriftTests`. The `[Trait("Host", "ReFS")]` rides the
concrete partitions, not the generic base: the planner's source scan names a generic definition without its arity and the
compiled assembly with it, and `HostWitnessTraitDriftTests` holds the two equal (it went red on the first try).

**Four classes have no row set to slice**, so they are split along their cases over an abstract harness holding the helpers
(`private protected`): the 30-case keyed-store witness (`KeyedStoreLoadWitnessTests` per file shape and sharing mode, 6
classes, whose one costly theory is 12 s a cell; `KeyedStoreStatementWitnessTests` per shape, 3); `CommitRollbackRuntimeTests`
(258 cases, three classes cut at test boundaries, `CommitRollbackRuntimeHarness` and `ReplaceOutsideTheRunUnit` shared);
`SequentialCommitControlTests` (the fault sweeps are 27 of its 28 s; three classes); `UsageInheritanceDriftTests` (the usage
equivalence, the phrase equivalence and the rest, three classes over `UsageInheritanceEquivalence`). The test-ref resolver
finds a method by the class's file name, so every split stays in the file the inventory rows cite. UsageInheritance is not a
`TestPartitioning` family because every partitioned row source of the Conformance assembly must also be an `ArchOracle` source,
and its rows are programs the oracle would have to rebuild.

**Measured here (this host, other fleets running, so wall seconds are noisy):** `CommitRollbackRuntimeTests` 27 s serial to 14 s for
its three classes; the keyed-store witness's largest class 78 s to 37 s; `SequentialCommitControlTests` 28 s to about 10 s a class;
the run-unit lock pairs ran 13 s serial and are now eight partitions of 96 pairs (not timed apart from the host's load). The Windows CI figures in the note were
not re-measured (this is not that runner). Left alone, because outside the note's list: other Unit classes the same review named
(RequiredImperativeStatementDriftTests, FileAuthorityPresenceTests, RunUnitProcessBoundaryTests, CommitRollbackRefusalWitnessTests).

**Self-review.** Architecture: one mechanism per shape (`TestPartitioning` for rows, harness plus classes for programs); no copy of
a helper. Full code: the partition asserts the ids `run.py` ran, so a lost test reads "only in-process", not green. Performance:
generation shared, `--jobs` bounded by the partition's test count. Duplication: none added; the drift-test doc references moved to
the new class names.
