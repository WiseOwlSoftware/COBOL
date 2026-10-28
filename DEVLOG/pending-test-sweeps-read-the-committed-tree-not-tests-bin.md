## PB3814: the test-source sweeps read the committed tree, and a drift test refuses a walk of tests/ from the file system

PB3677's merge group failed on `SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest`: it resolved a
`unit:` test-ref with `Directory.GetFiles(tests/WiseOwl.Cobol.Tests.Unit, "<Class>.cs", AllDirectories)`, which enters
the running test host's own `bin/Release/net10.0`, where `CobolFileLockTests` had left sidecar lock directories; a
delete-pending `.wolk-gone-<guid>` directory answered the walk with access denied. PB3677's finisher removed the one known
writer. This change removes the reader side.

**Re-probe.** The race has no deterministic repro (it needs a delete-pending directory under a walked `bin/`), so the
probe was the population: `git grep -n AllDirectories -- tests` lists 231 recursive listings in the test projects, and
12 classes root one at `tests/` (the note named 11; `FileHarnessOneHomeDriftTests`, `VacuousTestDriftTests` and
`SourceEncodingDriftTests` were missing from its list). Several filtered `bin/` and `obj/` only after the enumeration had
entered them, and `GateLegDriftTests` filtered `obj/` alone.

**Fix.** Each of them reads the COMMITTED tree through a declared `TreeScope` (`TrackedTree.Files`, the one reader that
PB3531 built for the whole-tree sweeps), so a build output is excluded structurally and the drift-rule index lists the
class for the files it reads. `UnresolvedTestRefs` narrows the `tests/` C# scope by the schema's `test-dir` and the
`<Class>.cs` name. `SourceEncodingDriftTests` keeps its three theory rows and takes each row's population from a declared
scope instead of a directory.

**Guard.** `TrackedTreeScopeDriftTests.NoTest_WalksTheTestsTreeFromTheFileSystem` reads every committed test source and
refuses a recursive listing (`SearchOption.AllDirectories`, `RecurseSubdirectories`) whose root is `TestRepo.Tests(..)`,
`TestRepo.Root` or `TestRepo.At("tests"..)`; `TheWalkCheck_FindsEachShapeOfAWalkOfTheTestsTree` fires it on each shape and
on its clean twins (the same listing of `src/`, a non-recursive listing of `tests/`, a mention outside a listing).
`docs/rearchitecture/DESIGN-test-build-ci.md` (tier 0d) states the rule.

**Left alone.** The 217 other recursive listings root at `src/` (about 190, each with its own `bin/obj` filter), `docs/`,
`scripts/`, `.github/` and `conformance-suite/`; no failure measured the `src/` ones, so they are a separate population and
the report registers them as a lead. The guard sees a root spelled in the call, not one assigned to a variable first.

`closes_rows: []`: process tooling, no inventory row.
