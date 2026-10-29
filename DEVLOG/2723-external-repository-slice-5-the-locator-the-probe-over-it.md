## Entry 2723 — 2026-10-29 10:26 PDT — External repository slice 5: the locator, the probe over it, the per-kind name dictionaries and the load-fault classification (PB2101)

**What the re-probe showed.** The probe still found a sibling module by file name alone (a case-insensitive scan of `<name>.dll`),
loaded it before reading its record, scanned `_order` linearly at every CALL, and let two faults escape the CALL as the CLR's own
exceptions: a registrar that failed when reached through a host's `EnsureRegistered()` or the emitted `Main`, and a failure of
`n.Factory(...)` for a registered program whose class or dependency would not load. The throwing-registrar witness
(`FunctionLocateResourceWitnessTests`) had been passing without reaching the registrar: its hand-made module carried no `Units`,
and the miss it produced was EC-FUNCTION-NOT-FOUND for the wrong reason. It now carries its row and does reach it.

**The fix.** `RepositoryLocator` over `RepositoryIndex` (named file first, nothing else read when it answers; else the directory
index, cached per process by path, length and mtime; MVID collision; schema check; 256 MiB skip; files only, exact `.dll`).
`ProgramTable.ProbeSiblingModule` reads the record before loading. `_programs` / `_functions` replace `_order`. `RegisterModule`
classifies EVERY registrar fault as EC-PROGRAM-NOT-FOUND and registers nothing of the module (static resets are adopted before
nodes commit; `AdoptStaticStorage` no longer adopts a reset that throws). `ProgramTable.Create` classifies a CLR load or
initialization failure at the instance creation (EC-FUNCTION-NOT-FOUND for a function); `RunOnThisThread` does the same for the
entry's own registration member. `RuntimeFeatures.DynamicProbe` (a `FeatureSwitchDefinition`) guards the one reflection; the
runtime is `IsTrimmable` with the trim analyzer on. Design corrections made in the same change: `FeatureGuard` cannot be stated
(IL4000), so one justified `UnconditionalSuppressMessage`; `RuntimeFeature.IsDynamicCodeSupported` as a default is NOT usable (a
trimmed self-contained app threw `TypeLoadException` on it), the switch defaults on and the application sets it off;
multi-unit case units are `.cbl` (the generator reads every `*.cob` as a test) and `build` items are quoted; the one runner of a
`wiseowl/cases/` case is the in-process runner, so no `compile-unit` adapter operation exists.

**Multi-unit cases.** `build` header field and `<id>/<step file>` option keys in the generator (with self-test arms), the manifest
entry, `SuiteManifest`, and `ConformanceSuiteRunner.UnitOptions`; three cases witness DOC-A.1-14 (a program in an assembly not
named after it, two assemblies carrying one name, `X - Copy.dll` beside `X.dll`). `RunTimeLocateFaultTests` holds the C# fixtures.

**Measured by hand, not yet automated.** A self-contained `PublishTrimmed` host: probe off with the runtime rooted, no `warning IL`,
no `LoadFromAssemblyPath` in the trimmed runtime, a CALL nothing registered reports "the dynamic probe is disabled"; probe on with
the runtime rooted, an untrimmed callee beside the host is located and runs. Not automated because the local box lacks the
SDK-matched ILLink package (a publish hangs on NuGet) and because a host project cannot reference an emitted COBOL assembly:
the emitted assemblies reference `System.Private.CoreLib` directly (CS0012).

**Left for PB2103.** The per-step `diagnostics.json` key (six readers take a key for a test's own file).

**Review round 1 (R1-1).** The docs said a native-AOT application has the switch off by default; the code defaults it ON (the
`IsDynamicCodeSupported` default was dropped), so an AOT application that leaves it on reached `LoadFromAssemblyPath`, whose
`PlatformNotSupportedException` escaped the CALL. Both halves fixed: the docs (CONFORMANCE DOC-A.1-14, INTERPROGRAM_DESIGN,
DESIGN-external-repository 9.4 and 15.5) now say the switch is on unless the application sets it and a native-AOT application must;
the probe's catch is one classifier, `ProgramTable.ProbeFaultReason`, which also takes `NotSupportedException` as "not located" with a
reason naming the switch (`ProbeFaultClassificationTests`). A CALL with no sibling file in such an application stays the plain miss.

