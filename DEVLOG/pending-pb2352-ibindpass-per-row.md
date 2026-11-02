## PB2352, PB2353, PB2354 — DataBinder extract steps 2-4: one IBindPass class per pipeline row

Steps 2, 3 and 4 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), census
R0-0001) ran as one change by owner decision (2026-11-02: do the steps of one mechanism together, several groups at once). They
sit on PB2296 step 1 (`DataBindState`, `ProcedureBindState`), which was merged first; their `blocked_by` now names that real
dependency instead of the census plan's order. The contract is behavior-neutral, so the proof is the §4 oracle:
`python scripts/arch/compare_oracle.py` against the base 2e1a4eed8377 answered `=== ARCH-ORACLE: IDENTICAL — 16143 cases ===`.

**What the rows are now.** `BindPipeline.Build` listed 29 `BindPass` lambda rows (`d => d.ExpandTypes()`); it lists 29 classes,
`Binding/Passes/<Name>Pass.cs`, and the `BindPass` record is deleted. A class holds its name, `Requires`/`Produces`, its
`PassViews` and the rule code only it asks: the entry became `Run(DataBinder binder)` and every `DataBinder` member in the
entry's exclusive call closure moved with it (41 + 49 members, as `private static` helpers taking the binder). Rule code that
another asker shares stays on `DataBinder` and was widened from private to internal (45 members) until that asker's concern
binder lands; `SynthesizeImpliedPicturesPass.SynthesizeImpliedPicturesUnder` is the one moved rule with outside callers
(`CompleteDescriptionAhead`, the TYPE expansion), which now call the pass class. Four rows keep their rule code with a concern
binder another group of the wave moves and only schedule it: `ResolveFilesPass` (`FileControlBinder`), `ResolveReportsPass`
(`ReportSectionBinder`; its closure alone is 36 members of `DataBinder.Reports.cs`), `CallBindExternalAndGlobalPass`
(`LinkageBinder`) and `OoRouteMethodRedefinesBackingsPass` (`OoDataBinder`). The move was done with a Roslyn mover (symbol
based, so a `this` member becomes `binder.X`, a moved callee gets the binder argument, a private member a pass reaches is
widened), not by hand.

**Declared views.** `IPassInfo.Views` is new (`PassViews`: the fact sets a pass appends to, reads early, reads whole), with
`FactSetKind` naming the four sets and `FactSetContract` holding each set's appending phases. `BindPipeline.ValidateDag`
refuses an append in a phase the set does not admit and a whole read that is not after the last appender; the group tail's rows
declare theirs (`ProcedureBinding`, `UsageCollectionPass`, `StorageFormPass`). A declaration that the code does not honour would
only be a comment, so `AppendGate` checks it while a per-unit pass runs: `FactSet.Add` throws when the running pass did not
declare the set, which also covers the binder code a pass calls. It found two declarations the design had wrong or missed: the
Tier-B REDEFINES leaves are image-forced by `ClassifyRedefinesClasses` at RedefinesClassified, not at FilesResolved
(`FactSetContract`, `DataBindState` and §8.3 (1) say so now), and the pointer pass `PtrBindBasedAndAddressables` appends to
`ImageForcedItems` too (found by `RepositoryCodecTests` on the first run). `BindPassBoundaryDriftTests` pins the boundary: the
rows of `Build` are exactly the `IBindPass` classes, each in a file named for it; no `DataBinder` method carries a moved pass's
entry name except the four concern-owned rows (a closed list that fails when one lands); a pass's own source appends only to
what it declares; and the gate refuses an undeclared append. `BindPipelineTests` gained the three view-order cases and a test
double for the deleted record.

**Neighbours moved with it.** 112 inventory rows cited the old files and symbols (`DataBinder.Odo.cs#OdoResolve` ...): two
`record_verdicts` batches re-sited them (`rows changed 111 + 1`, witnesses `+142` / `142` retired, GAP 19 → 19). The tests that
scan source by file and method name (`TemplateClauseResolutionDriftTests`, `UsageDeclarationPlacementDriftTests`,
`StorageClaimHeadDriftTests`, `OdoDependingAdmissionDriftTests`, `CapacityRegisterReferenceDriftTests`,
`DisplayUsageUnionDriftTests`, `ConditionNameAssociationDriftTests`) now read the pass files. 47 comments and design docs that
named `DataBinder.<moved member>` or a deleted partial name the pass class now. Five partials emptied out and were deleted
(`Aligned`, `ConstantRecord`, `CodeSetRecords`, `RedefinesEntry`, `TableValue`); `PB2296` and `PB2358` listed some of them as
sites and lost those lines.

**semgrep.** `scripts/semgrep/verify.py` raised `raw-diagnostic-code-literal` from 225 to 229 and the baseline was moved with
this explanation, not by a new violation: semgrep's C# parser reports `PartialParsing` for lines 14-4851 of the old
`DataBinder.cs` (and most of `Reports.cs` and `Constants.cs`), so every rule was blind there; the literals in the moved code
(`binder.Edition.Error("COBOL0850", ...)`, 19 codes with no catalog descriptor) were always present and are visible now that
they sit in files semgrep parses. Registering those descriptors is a separate item.

**Self-review.** Architecture: one class per row, a boundary test, no forwarder (the four concern-owned rows call a method that
stays until its binder lands, which the test names). Full code: the oracle is identical over 16143 cases and the rule text moved
verbatim; the license header of 22 files was dropped by the unused-using pruner and restored before the gate. Performance:
`BindPipeline.Build` allocates 29 small objects per unit as it allocated 29 lambdas; `AppendGate` costs one reference test per
`FactSet.Add`. Duplication: none added; the group tail's view declarations are the only second statement of a fact set's
phases, and `ValidateDag` checks them against `FactSetContract`.
