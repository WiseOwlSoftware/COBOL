## Entry 3916 — 2026-11-22 11:57 PST — PB3201: the census refuses C# compiled outside its walk, and reads the drift tests' path locator as a symbol

kb/Work PB3201 (an R2 finding of the PB1754 review) named two consumers of solution types that the member index and the
census cannot see. The member index computes every Move or rename wave's file set from the census's Roslyn walk of
WiseOwl.Cobol.sln, so a consumer outside that walk is outside every wave's file set.

**Re-probe.** The first consumer is gone. `tools/impact/` (ImpactTestFramework.cs, which derived `GateTestFramework`,
its ImpactRecording.targets and ImpactInstrumenter.csproj) was deleted by a8c569fc19 (PB4328). On 608ccf507c the only
tracked C# projects outside the solution are the census host and two frozen evidence probes. Nothing would have stopped
a new one: `member_index.tree_key` hashed no MSBuild file outside src/ and tests/, and neither caller of the scope
checked the tree. The second consumer was still live: `TestArtifacts.LocatorPrefixes` matched classes by the text
`TestRepo` and returned an empty map when none matched, so after a rename every path a drift test builds would have
been recorded as a plain literal that `census.py dangling_literals` never checks.

**Fix.** THE WALK BOUNDARY, `census.walk_scope`, is now the one scope function. Both `census.measure` and
`member_index.record` call it. It refuses three things: a tracked C# project that is not a solution project, the host
or frozen evidence; a host with a ProjectReference; and a `<Compile Include>` that names sources outside src/ and tests/
or a path it cannot resolve. C# compiles only through an MSBuild project, so this rule covers both shapes the impact
recorder had, the out-of-solution project and the linked source. The note's target proposed a check on the namespaces
of .cs files under tools/; this rule is stronger. `member_index.KEY_INPUTS` now hashes every tracked `*.csproj`,
`*.props` and `*.targets`. A project added outside the solution therefore changes the key, and the next index
regenerates and refuses it. `TestArtifacts.LocatorMetadataName` resolves the locator as a symbol in the project's own
assembly and binds each drift-test call semantically, so an alias or a `using static` call is read. It throws
CensusRefusal when drift tests exist but no project compiles the locator, or when the locator has no method of a shape
the census reads. `ArchCensusDriftTests.CensusHost_NamesTheLocatorTheDriftTestsCall` pins the constant to
`typeof(TestRepo).FullName`, so a rename fails at the gate. DESIGN-architecture-review §8.7 now states the boundary
and the wider key.

**Evidence.** census.py --self-test gains five boundary arms (28 arms, PASS). member_index.py --self-test gains
`msbuild-dirty-no-key` and `msbuild-in-key` (38 checks, PASS). The census host's base and new builds were run in
measure mode on this tree, after a Release build of the analyzer (see below). The base host recorded 606 drift-test
paths and the new host 609; every other field of the raw record is equal except the walk's elapsed seconds. The three extra paths are fully qualified
calls (`WiseOwl.Cobol.Tests.Shared.TestRepo.Src(...)` in ExceptionCheckingPerformDirectiveBanDriftTests,
HostEntryShapeDriftTests and PlaceDenotedItemDriftTests) that the text matcher never read, and all three exist. A
planted host whose locator name matches no type exited 2 with "ArchCensus: REFUSED - no project of the solution
compiles the drift tests' path locator WiseOwl.Cobol.Tests.Shared.RenamedRepo". The base code would have recorded 0
paths in that case and said nothing.

**Found on the way, left to their owners.** The census fails at the base in a fresh worktree. `census.measure` builds
the solution in Debug, but Tests.Unit references the analyzer with `SetConfiguration=Release`, so the bin/Release
analyzer dll is missing, and the host crashes in `Reachability.StrongestUseAsync` with UnresolvedAnalyzerReference.
This is the same root as PB6043. Also, `scripts/buildgraph.py` restates TestRepo's helper prefixes by hand
(`TESTREPO_BASES`) and finds calls by the text `TestRepo.`.

**Gate.** Run 1 was RED in the INERT LANE audit: the new drift test reads tools/ArchCensus/TestArtifacts.cs, so
`lane_classification.CODE_READS` now names that reader. Run 2 was RED in SELF-TESTS: the walk-boundary arm reads every
tracked MSBuild file, so census.py declares `# SELF-TEST-POPULATION: tree`. Self-review also found that the boundary's
patterns read only double-quoted attributes, so a single-quoted Include would have passed; both quote characters are
now read, with a planted arm. Run 000003-20261122T191952Z: `=== BUILD-LOCAL GATE: GREEN — ... RepoDrift 231/231 ran
... ===`, with `ArchCensusDriftTests.CensusHost_NamesTheLocatorTheDriftTestsCall` Passed.

