**Landed (wave 20262135 group F, 2026-11-22).**

**Re-probe.** The first consumer the note names is gone: `tools/impact/` (ImpactTestFramework.cs, ImpactRecording.targets,
ImpactInstrumenter.csproj) was deleted by a8c569fc19 (kb/Work PB4328, the impact map deleted). On 608ccf507c the only
tracked C# project outside WiseOwl.Cobol.sln is the host `tools/ArchCensus/ArchCensus.csproj` (no ProjectReference) and
the two frozen evidence probes under `docs/rearchitecture/evidence/impact-map-pb1708/`. Nothing refused a NEW one: the
member index's `tree_key` hashed no MSBuild file outside src/ and tests/, and neither caller of the scope checked the
tree. The second consumer still reproduced in the code: `TestArtifacts.LocatorPrefixes` matched classes by the text
`TestRepo` and returned an empty map when none matched.

**Fix.** THE WALK BOUNDARY, `census.walk_scope` (the one scope function `census.measure` and `member_index.record` both
call): it refuses a tracked C# project that is not a solution project, the host or frozen evidence; a host with a
ProjectReference; and a `<Compile Include>` outside src/ and tests/ or one it cannot resolve. This is stronger than the
target's ".cs under tools/ uses no WiseOwl.Cobol namespace" rule: C# compiles only through an MSBuild project, so the
rule reads the projects and their Compile items, and it catches both shapes the impact recorder had (an out-of-solution
project and a linked source). `member_index.KEY_INPUTS` adds every tracked `*.csproj`, `*.props` and `*.targets`, so a
project added outside the solution changes the key and the next index regenerates and refuses it.
`TestArtifacts.LocatorMetadataName` resolves the locator as the project assembly's symbol, binds each drift-test call
semantically, and throws CensusRefusal when drift tests exist but no project compiles the locator, or when the locator
has no method of a shape the census reads. `ArchCensusDriftTests.CensusHost_NamesTheLocatorTheDriftTestsCall` pins the
constant to `typeof(TestRepo).FullName`, so a rename is red at the gate.
