## PB2296 — the DataBinder extract lands as one: steps 1-10 (PB2352-PB2360, PB2296 M5, PB2358) and M6/M7, the value slices of DataBindState

One landing carries the whole wave-20261834 extract (integrated by wave 20261837 A, first queued as #1045, which the merge group bounced and this landing supersedes) and its last step, M6 and M7 (wave 20261837 B).

### PB2296 M6 and M7 — the data binder's tables become value slices of DataBindState, and the file binder takes the FILE half

kb/Work PB2296 is step 1 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), census
R0-0001). This wave (20261837 group B) ends it, on top of group A's integration branch (499a65da5, reviewed CLEAN): M6 regroups the
remaining binder fields into value slices of `DataBindState`, and M7, which group A's review finding R1-1 added, moves the FILE half
that `FileControlBinder` (PB2356) left on the binder. Behavior-neutral by contract, so the proof is the §4 oracle:
`python scripts/arch/compare_oracle.py --base 499a65da5` answered `=== ARCH-ORACLE: IDENTICAL — 16147 cases ===` after M6 and again
after M7.

**What the re-probe showed.** Nothing to reproduce: an Extract wave has no defect. The design's list of slices (the usage facts, the
sign facts, the REDEFINES classes, the OCCURS facts) was an estimate that did not survive the code: those facts are fields of the
`DataItem` forest, written by the ordered pass list and held by no binder field. What the binder DID hold were its tables, and
§8.3 (1) now names the five slices they became (the estimate was corrected in the design in this change).

**M6, the fix shape.** Five new slices, each a class with private storage, `internal` mutators and read-only views; every reader of
the old `DataBinder` members changed to read through `State` (no forwarder remains):
- `ForestSlice` (`State.Forest`): `Roots`, `WorkingStorageRoots`, `LocalStorageRoots`, `ByName`, `Conditions`, `IndexNames`,
  `CapacityRegisters` (gated; `CapacityRegistersSoFar` is the ungated read of the declaration funnel), `TypeDecls`, `ScreenNames` and
  the inheritance tier (`DeclaringDepth`, `InheritsGlobalData`, `DeclaresOwnDataName`). Its inserts take the method scope that owns
  the record, so a name still joins the index of its source element (§11.7.4 GR5; PB3192). `AddCapacityRegister` moved into it.
- `FileSlice` (`State.Files`): `Declared`, `FilesByName`, `Declare`; `DataBindState.VisibleFiles` reads it with the inherited files.
- `StorageTierSlice` (`State.StorageTier`): the four static channels with `IsStaticMember`, and the pointer pass's cell backings,
  BASED bridges and addressable-cell map (they were `Ptr*` members of `DataBinder.Ptr.cs`).
- `ClauseFacts` (`State.Clauses`): the SIGN-written, VALUE-written and refused-operand sets, the written level-88s and the
  lapsed-type-member index (written once, enforced).
- `ImplicitRegisters` (`State.Implicit`): the DEBUG-ITEM family and `EXCEPTION-OBJECT`.
- `Reports`, `SumCounters` and the pass-completion watermark (`Watermark`, `MarkProduced`, `Require`) moved onto `DataBindState`.
  `DataBinder.TakeFillerOrdinal` is the filler counter's one door, beside `TakeUid`.
The drift test is `DataBindStateBoundaryDriftTests`, SEEN RED before it was green: a `JunkTable` collection added to `DataBinder` failed
`TheDataBinder_DeclaresNoCollectionOtherReadersSee_EveryTableIsASliceView`, and a `.Files.Declare(` written in `ProgramEmitter.cs`
failed `TheDataBindersSlices_AreWrittenOnlyByTheirWriters`. The older drift tests that read `DataBinder`'s source or members by
name now follow the slices (`GlobalNameClassDriftTests` walks `ForestSlice.Inherit`, `MethodNameRegistrationDriftTests` pins the
`ForestSlice` writers, `CapacityRegisterReferenceDriftTests`, `ReportScopeBoundaryDriftTests`, `GlobalReportScopeTests`,
`ClauseOperandCaptureDriftTests`, `ReportItemReferenceTests`, `BindPassBoundaryDriftTests`).

**M7, the fix shape.** `FileControlBinder` gains two partials. `FileControlBinder.FileSection.cs` holds `BindFileSection`, the RECORD,
BLOCK CONTAINS, LINAGE and CODE-SET clauses, the implied record of a record-less FD, `LinkImplicitRecordArea`, the CODE-SET and
record-name screens and `ScreenSelectsHaveDescriptions`. `FileControlBinder.Resolve.cs` holds `ResolveFiles` and its FILE STATUS,
LINAGE, ASSIGN USING, key and SUPPRESS WHEN resolutions. `BindPassBoundaryDriftTests.ConcernOwned` and the test that required an
open note to name it are gone: no pass row names a method of `DataBinder` any more, and the test says exactly that. The clause-operand
resolvers every section shares (`ResolveClauseOperand`, `ClauseDataName`) stay on the data binder.

**Registers.** PB2296 is flipped to landed (`closes_rows: []` with its reason: no inventory row). Rows re-sited by `record_verdicts`
batches (`GAP 19 → 19`, nothing closed): 3 rows for M6 and 56 rows for M7 (59 witnesses, nine of them re-sited a second time because
the first pass named the partial that merely mentions the symbol in a comment), each a `retire-witnesses` with its reason, because the
symbol moved files. Twenty-two edge pairs (`StatementBinder`, `SymbolTable`, `BoundUnit` and the emitters onto the slice types) and two
file-binder pairs are tolerated in `architecture-edges.json`, removed by PB2361 with its `DataBindResult`; PB2361's **Removes** line
names the new `Bound → Binding` row.

**Self-review (four lenses).** Architecture: one mechanism per job, each slice with one writer set pinned by a test; no C# text in
a bound node; the watermark gate stays with the state it gates. Full code: the citations written into the new docs were
`cite.py --check`ed (§13.16.3 SR10 and SR24, §13.4.5.3 SR1, §13.4.6.3 SR1, §8.4.3.6.3 SR2, §11.7.4 GR5, §13.5.4 GR1, §13.18.52.3 SR1, §8.4.2.2.1 GR4);
one inherited wording ("rule 4's other definitions") was not in the standard and was rewritten to the rule's real text. Performance:
no hot path changed (a property chain `State.Forest.X`). Duplication: `AddCapacityRegister` and `DeclaringDepth` have one home. Left for
step 11: the entry walk's private working memory, and the members widened to `internal` for the file binder (`LiteralPosition`,
`RawValueOperandText`).

**Review round 1 (R1-1, blocking, answered).** The branch review found that two of the new slices are not frozen when the data
phases end, against the contract the change had written ("frozen when its producer is done", design §8.3 (1) "binders read the frozen
value slices"): the PROCEDURE binders append compiler temporaries to `ForestSlice.Roots` (`CompilerTemps.Create`, called by the
subscript, sending-value, ACCEPT/DISPLAY and function-result temporaries) and a user-defined function's result temporary claims a cell
on `StorageTierSlice` (`UdfBinder`), both after `RouteStaticUnitStoragePass`. The drift test pinned writers by FILE, so a procedure-phase
call through an allowed file passed unseen. The fix names the doors instead of hiding them: `ForestSlice.AddTemporaryRoot`,
`StorageTierSlice.ClaimTemporaryCell` and `DataBinder.ClaimTemporarySlotCarriedCell` (the shared test of the two cell claims is
`SlotCarriedCellClass`), each pinned by member and by its one caller in `TheDataBindersSlices_AreWrittenOnlyByTheirWriters` (seen red
with `CompilerTemps` writing `AddRoot` again); the slice summaries, `DataBindState` and the design §8.3 (1) now say the two slices are
frozen only when the procedure binders are done, and PB2361 is told to build `DataBindResult` after the group tail. The merge with
main took PB2104's changes into `OoDataBinder`, `DataBinder.Ptr` and `RepositoryCodec` (`m.Definition`, `FormalName`, the
`ExpansionIdentity` records).

### PB2360 — DataBinder extract step 10: OoDataBinder, CompilerTemps and the dissolved Oo partial

kb/Work PB2360 is step 10 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3
(1), census R0-0001). It is behavior-neutral by contract, so the proof is the §4 oracle:
`python scripts/arch/compare_oracle.py --base 7b0ec6eb1` (the merge base with PB2296 step 1) answered
`=== ARCH-ORACLE: IDENTICAL — 16143 cases ===`. It ran beside five other DataBinder groups (owner, 2026-11-02, kb/Work
PB4452), each on its own partials, so `blocked_by` is rewritten from the census order (PB2359) to the real result dependency,
PB2296 step 1.

**What the re-probe showed.** Nothing to reproduce: an Extract wave has no defect. The `DataBinder.Oo.cs` partial held seven
unrelated mechanisms behind one name, and the work was to give each its own home rather than to rename the file.

**The fix shape.**
- `Binding/OoDataBinder.cs` (`DataBinder.Oo`): a class unit's OO half, one per binder: the method roster, the method-scoped
  roots, the root-to-method owner map, the method declaration context, `BindMethodData`, `BindPropertyClauses`,
  `BindObjectRefDescriptor` and the class-member-name reservation. `OoIsClassUnit` and `OoOwnerClassName` (two init
  properties that could disagree) became one init-only `DataBinder.OoRole` (`OoUnitRole`: null for a program, an owner
  class name or null for an interface), which `OoDriver.ElementBinder` sets.
- `Binding/OoMethodDataScope.cs`: the per-method name scope, moved unchanged.
- `Binding/CompilerTemps.cs` (`DataBinder.Temps`): THE ONE constructor of a compiler temporary (`Create`, `CreateTable`,
  `CloneNode`) and the object-reference families that go through it (`ForProperty`, `ForInvocation`, `ForObjectView`,
  `ForSelf`, `ForIdentifiedObject`), with the clone pairs `StorageFormPass` re-syncs. `DataBinder` gives it a uid and a root
  list through `NextUid`, `TakeUid`, `AddRoot`, and `CopyDescriptionToTemp` (the one copy in its `CompilerTemp` scope, so
  `CopyEntryDescription` and its private `PictureAsWritten` stay private).
- `Binding/OoPendingPropertyOp.cs`: the entry type of `ProcedureBindState.PropertyOps`, no longer nested in the binder;
  `ProcedureBindState.ClaimInterleavedReceiver`. `ReceiverOpensAtRunTime` moved to `MoveBinder`, its only asker, which
  already referenced `PlaceIdentification`; left on the state type it would have added a Binding to CodeGen edge.
- The scope-aware resolver's accessors (`Symbols`, `ActiveScope`, `ScopeOf`, `NameScopeOf`), the four static-storage channels
  and `IndexDeclarationsUnder` moved, text unchanged, into `DataBinder.cs`. The static channels are written by
  `RouteStaticUnitStorage` (PB2354), which takes them with it; changing those writer lines here would only conflict with that
  step.
- Five `DataBinder` members went from `private` to `internal` because the OO binder asks for the entry binding every element
  kind shares: `BindEntries`, `DeclareDataEntries`, `BindRemainingConstants`, `CompleteDescriptionAhead`, `SealLinkageRules`.

**Goldens and tests.** None: behavior-neutral (oracle IDENTICAL). New `OoDataBinderBoundaryDriftTests` (3 facts): no
`DataBinder.Oo*` partial and no `Oo…` member on `DataBinder` beyond its door; the OO binder is made only by `DataBinder`; a
compiler temporary (`IsCompilerTemp = true`) is built only by `CompilerTemps`. Seen red with a planted `DataBinder.OoTmp.cs`.
Six existing drift tests read the old file by name and were re-pointed. One of them,
`DescriptionCopyCompletenessDriftTests.NoCodeOutsideTheOneCopy`, matched `DataBinder.cs` by `EndsWith`, which `OoDataBinder.cs`
also satisfies; it now matches the file name.

**Rows.** 48 inventory rows cited `DataBinder.Oo.cs`; one `record_verdicts` batch re-pointed them (`+48 code-location added;
48 retired`, `CONFORMS 48`, `GAP 19 → 19`) and restated the notes that named the renamed members.

**Edges.** `architecture-edges.json`: the pair `DataBinder -> PlaceIdentification` fell (ratcheted); the tolerated row
`Bound -> Binding` grew two pairs, `StatementBinder -> OoDataBinder` and `-> CompilerTemps` (the method declaration scope and
the subscript temporaries), both removed by PB2347 with the rest of that row's `StatementBinder` pairs. The census-measured
`uses` stays 39; `walkUses` is 41.

**Self-review.** Architecture: one mechanism per type, the door pinned by a drift test. Full code: the moved bodies are
byte-identical apart from `data.` qualification and renamed members; `RootOwner` is still written only in
`DataBinder.AttachToHierarchy`. Performance: `Oo` and `Temps` are lazy fields, no new hot-path allocation. Duplication: none
added; `IsClassUnit`/`OwnerClassName` no longer come from two properties.

**Gate.** At head 2c5a0843f the gate answered `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,399/39,399 ·
Characterization 37/37 cases ran (skipped 0) … (run 000001-20261102T221940Z) ===`, first time, no red on the way. The three
`OoDataBinderBoundaryDriftTests` facts are in that run's `Unit.trx`. `python scripts/semgrep/verify.py`: PASS, no rule's count rose.

### PB2359 — DataBinder extract step 9: LinkageBinder, the Linkage partial as a type

kb/Work PB2359 is step 9 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3
(1), census R0-0001), on the critical path of the backend seam (PB2293). It ran in wave 20261834 beside five other
DataBinder groups, each on its own partial, all starting from PB2296 step 1's branch, so its `blocked_by` was rewritten
from the census plan's ORDER (PB2358) to the real result dependency (PB2296: `DataBindState`, `InheritedGlobals`). It is
behavior-neutral by contract, so the proof is the §4 oracle: `python scripts/arch/compare_oracle.py` against the merge
base 2e1a4eed8377 answered `=== ARCH-ORACLE: IDENTICAL — 16143 cases ===` on the finished tree. There is no probe to
re-run: nothing the note measured is a behavior.

**What moved.** `DataBinder.Linkage.cs` is now `Binding/LinkageBinder.cs`, renamed in place so the records it also holds
(`LinkageFormal`, `CallExternalBacking`, `CalleeSignature`, `UserFunctionSignature`, `ProgramPrototype`) keep their history
and a concurrent edit to them merges. `LinkageBinder` (internal, `DataBinder.Linkage`, created on first use because it binds
over its binder's forest, as `SymbolTable` does) owns the linkage roots, the USING formals, the RETURNING item, the
EXTERNAL backings, the GLOBAL roots, the root fields the emitter must not declare, and the §13.7.3 SR4/SR5 sets, with
every writer: `CallBindLinkage`, the ANY LENGTH sweep, `SealLinkageRules` (the method arm of `OoBindMethodData` calls it
too), `CallBindExternalAndGlobal`, the externalized-name screen, and the bridge lists. Its views are
`IReadOnlyList`/`IReadOnlySet`; `SuppressRootField` is the one write the OO binder makes. Every reader changed in the same
change and the old names are deleted (`LinkageFormals` is `Linkage.Formals`, `CallGlobalRoots` is `Linkage.GlobalRoots`,
and so on): emitter, repository codec, resolver, call and condition binders, `InheritStep`, `BinderDriver`, the tests.

**What did not belong.** The partial held three things that were never the linkage binder's, and each went where its
neighbours are rather than into the new type: the inherited-depth tier of the name tables (`DeclaringDepth`,
`InheritsGlobalData`, `DeclaresOwnDataName`, `InheritGlobalSubtree`, `ReserveInheritedMemberNames`) to `DataBinder.cs`
beside `ByName`, `Conditions` and the capacity registers it orders (39 callers, none changed); the cell-backing forcer
(`ForceStringCanonical`, `CellCanCarry`, `AssignCellComponents`) to `DataBinder.Ptr.cs` beside its BASED / ADDRESS OF
callers; the unit-kind facts and `CallSeedUids` to `DataBinder.cs`. That is the stated scope corrected (CLAUDE.md rule 5):
the note said "the Linkage partial", and moving all of it would have put name-resolution state behind `Linkage.`.

**Pinned.** `LinkageBinderBoundaryDriftTests` (3 facts): every view `LinkageBinder` exposes is read-only by its declared
type, `DataBinder` declares no member of a linkage fact type (a forwarder or a second home would be one), and no
`DataBinder.Linkage.cs` partial exists. Seen red on a planted `List<DataItem>` view and a planted `DataBinder` member
returning `IReadOnlyList<LinkageFormal>`. The architecture-edges file tolerates four new `CodeGen -> LinkageBinder`
pairs (`ProgramEmitter`, `OoEmitter`, `HostEntryEmitter`, `RecordStructEmitter`), removed by PB2361 with the `DataBinder`
pairs they sit beside. Four drift tests that read the partial by path now read its successors
(`GlobalNameClassDriftTests`, `MethodNameRegistrationDriftTests`, `ByValueFormalScreenDriftTests`,
`StorageClaimHeadDriftTests`).

**Rows.** 35 inventory rows re-keyed from `DataBinder.Linkage.cs#X` to the member's new file by a `record_verdicts` batch
(35 records, 35 rows changed, GAP 19 to 19); none closed. Design §8.3 (1), `DESIGN.md`, `DESIGN-module-topology.md` and
three open notes' file paths follow the move. PB2359 is landed.

**Gate.** Run 000002-20261102T215833Z: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,396/39,396 ·
Characterization 37/37 cases ran (skipped 0) …`. Run 000001 was red on the rendered-views audit: the new boundary test
carried the `Sweep=tree` trait though it scans no tree. `semgrep` counts did not rise (one rule fell 225 to 216).

**Self-review.** Architecture: one writer per fact and a pin for it; the type takes its binder rather than the
binder's tables being copied. Full code: the moved bodies are textually the old ones with `data.` for the binder members;
the two arms of `SealLinkageRules` and `FormalIsCarrierResident` (program and method) agree. Performance: `Linkage` adds one
null test per access, and `DeclaringDepth` did not move. Duplication: none added; `ExternalizedSubjects` stays the one
population both screens read.

### PB2352, PB2353, PB2354 — DataBinder extract steps 2-4: one IBindPass class per pipeline row

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

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,428/39,428 · Characterization 37/37 cases ran (skipped 0) …` (run 000006-20261102T235249Z at 6cdfa8113006); earlier reds were found and fixed on the way: the line-ending audit (a script wrote CRLF into four files), the rendered-views audit (a `Sweep` trait on a test that is no tree-wide sweep), and three text-scan tests that still read the partials (`UsageInheritanceDriftTests`, `EditionMutedDriftTests`, `ReportItemReferenceTests`). The oracle at that head: `=== ARCH-ORACLE: IDENTICAL — 16143 cases (baseline 2e1a4eed8377, new 6cdfa8113006) ===`.

### PB2355, PB2356 — DataBinder extract steps 5 and 6: SpecialNamesBinder and FileControlBinder

Steps 5 and 6 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), census
R0-0001; kb/Work PB2355, PB2356), run as one group on PB2296 step 1's branch (the owner's 2026-11-02 rule: several steps
in parallel, each on its own partials). Both are behavior-neutral by contract, so the proof is the §4 oracle:
`python scripts/arch/compare_oracle.py` answered `=== ARCH-ORACLE: IDENTICAL — 16143 cases ===` against main's
2e1a4eed8377 after step 5 and again at the final head.

**SpecialNamesBinder (PB2355).** `DataBinder.Switches.cs` (2,414 lines, the SPECIAL-NAMES paragraph and the
OBJECT-COMPUTER alphabets and classification, ISO §12.3.6-§12.3.7) is a class of its own, made once per unit as
`DataBinder.SpecialNames`, over six partial files by clause family (the core walk and the name tables;
`.Alphabets`; `.Literals`, the shared literal-operand screens; `.Classes`, CLASS and SYMBOLIC CHARACTERS; `.Currency`;
`.Locales`). It owns the state it declares and asks the data binder only for what is not SPECIAL-NAMES (the §8.3.2.2
declaration funnel, the constant readings, the method tier of a lookup). Every reader asks `Data.SpecialNames`;
there is no forwarder on `DataBinder`. What the old partial held that is not SPECIAL-NAMES stayed on the data binder in
`DataBinder.Configuration.cs`: the contained-unit seed `InheritConfiguration` (which now asks
`SpecialNamesBinder.InheritFrom` for its half), the REPOSITORY specifier screens and the configuration-literal constant
readings. `DropShadowedConstants` asks `SpecialNames.HideShadowedSymbolics()` instead of reaching into the binder's
private set. The design had put the I-O-CONTROL partial under this binder too; it is §12.4.6, the input-output
section, so it went with step 6 (the design's own module-topology rows 10 and 15 had it so).

**FileControlBinder (PB2356).** The FILE-CONTROL SELECT entries (`BindFileControl`), the RECORD KEY and ALTERNATE RECORD KEY
source phrase, the key-level and file-level COLLATING SEQUENCE clauses with their post-build resolution, and the
ORGANIZATION, ACCESS MODE, SHARING and LOCK MODE mappers left `DataBinder.cs`; the I-O-CONTROL paragraph (the SAME
clauses and APPLY COMMIT) moved from `DataBinder.IoControl.cs` to `FileControlBinder.IoControl.cs`. The binder is made once
per unit as `DataBinder.FileControl` and owns `SelectedFiles`, `SuppressWhenOperands` and `ApplyCommit`; the files
themselves stay on the data binder, where the FD, the report section and the file resolution reach them. The COLLATING
SEQUENCE alphabet class test (`CollatingAlphabetFault`), shared by the file clause, PROGRAM COLLATING SEQUENCE and
SORT/MERGE, is an alphabet question and lives in `SpecialNamesBinder`. Not moved: the post-build half (`ResolveFiles` and its
FILE STATUS, LINAGE, ASSIGN USING and key resolutions) and the FD/SD binding, named in §8.3 (1) as the next FILE step.

**Guards and bookkeeping.** `ConfigurationBindersBoundaryDriftTests` (new) pins both binders: the data binder declares no
member (name and parameter types) a binder declares, which is the rule against forwarders, and each binder is made in one
place; it was seen red on the first run, when `SpecialNamesBinder.Bind` and `DataBinder.Bind(ProgramUnitContext)`
collided, which is why the binder's entry point is `BindSpecialNames`. Six existing source-scan drift tests that read
`DataBinder.Switches.cs` now read the `SpecialNamesBinder*.cs` partials through the new `SpecialNamesSource` helper.
The emitters' and the statement binder's reads of the binders' state are tolerated edge pairs onto `SpecialNamesBinder`
and `FileControlBinder` in `architecture-edges.json`, each with the removers of its pair onto `DataBinder`
(`removedBy` PB2361 and its siblings); they fall when the backend input stops carrying the binder. 182 inventory rows had
their code-location witnesses re-pointed through `record_verdicts` (three batches; verdicts unchanged, GAP 19 to 19). The
frozen evidence that cites `DataBinder.Switches.cs` carries a `superseded_by` marker. The design docs name the new
homes (DESIGN-architecture-review §8.3 (1), DESIGN-module-topology rows 10 and 15, FILES_DESIGN, DESIGN, DATA_MODEL_DESIGN).

**What the run found.** `wiseowl-cobol-raw-diagnostic-code-literal` went from 225 to 226 in `scripts/semgrep/baseline.json`:
the `COBOL1512` literal in the LOCK MODE rule moved with its code and is now matched, where the old site's
interpolated string with `…` in it never was. `COBOL1512` has no catalog descriptor and three rules share it
(LOCK MODE with sequential access, a lock phrase under LOCK MODE AUTOMATIC, OPEN SHARING ALL OTHER without a LOCK MODE),
so the finding is a defect in the diagnostic catalog, not in this move; it is a lead for the fix lane.
The gate's verdict on the code head 2f23f8d93: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,404/39,404 · Characterization 37/37 cases ran (skipped 0) ... (run 000003-20261102T232400Z) ===`.
Two reds on the way, both mine: gate 1 stopped on the evidence-supersession audit (the frozen record cites the deleted
file), gate 2 on `ReportItemReferenceTests`, which names `ResolveClauseOperand` by its visibility.

### PB2296 — the DataBinder extract's step 1 to 10 branches land as one (integration of wave 20261834)

Six branches of wave 20261834 had each been reviewed (five CLEAN, one SPLIT) and each stopped on a CONFLICT against main (which
by then carried the ReferenceResolver chain, #1041) and against the others, because they cut different blocks out of the same
files (`DataBinder.cs`, `DataBinder.Ptr.cs`, `ReferenceResolver.cs`, `ProgramEmitter.cs`, `architecture-edges.json`, the
inventory rows). This entry is the integration branch that lands them as one pull request: D (PB2296 step 1 and PB2357
`ReportSectionBinder`) fast-forwarded, then B (PB2352 to PB2354, one `IBindPass` class per pipeline row), C (PB2355
`SpecialNamesBinder`, PB2356 `FileControlBinder`), F (PB2359 `LinkageBinder`), G (PB2360 `OoDataBinder`, `CompilerTemps`) and A
(PB2296 M5 `ConfigurationStep`, inherited constants into `InheritedGlobals`, PB2358 `ConstantEntryBinder`) were merged in that
order with `merge_drivers.py merge --keep`; the per-branch entries (`pending-pb2352-ibindpass-per-row`,
`pending-pb2355-pb2356-special-names-and-file-control-binders`, `pending-pb2359-linkagebinder`,
`pending-pb2360-oo-data-binder-step10`, `pending-pb2296-m5-configuration-step-constant-entry-binder`, and the stamped 3012 for D)
stay as their authors wrote them. PB2296 stays OPEN: its M6 (the value slices of step 11) follows this landing.

**The rule for a conflict.** A member one group moved and another edited goes to its new home with the edit. Where a block was
deleted on one side (moved into a pass, a binder or a partial of its own) and edited on the other, the deleted side won and the
edit was re-applied at the new home by name (the cref and call renames of the ReferenceResolver chain, `Oo.*`, `Temps.*`,
`Linkage.*`, `Constants.*`, `State.Configuration.*`); the compiler's error list found every one, and the code-location witnesses
of the inventory rows followed through three `record_verdicts` batches (GAP 19 to 19, no row closed or opened).

**Where two groups moved the SAME state to different homes** (the one real design merge). A made the configuration facts
(decimal point, currency set, the seven `ConfigurationNames` tables, collating sequences, classification, mnemonic-names, the
REPOSITORY specifiers, the OPTIONS model) a value slice, `ConfigurationSlice`, with one producer per unit, `ConfigurationStep`
(`Own | Contained | ClassHalf`); C made `SpecialNamesBinder` own the same facts and seed a contained unit through its own
`InheritFrom`. The two cannot both hold. The merged shape is A's state with C's logic, which is what C's design text already
called for (its facts "become a value slice ... and the binder's state stops being read through the binder"):
`SpecialNamesBinder` declares into the unit's slice (`Cfg`, `DataBindState.Configuration`) and holds none of the facts, its
`InheritFrom`, `HideShadowedSymbolics` and `_shadowedSymbolics` are deleted (`ConfigurationStep` and `InheritStep.DropShadowed`
own them), `DataBinder.InheritConfiguration` is deleted, and every reader reads `State.Configuration`. A new fact in
`ConfigurationBindersBoundaryDriftTests` pins that the binder declares no member the slice declares. A's SPECIAL-NAMES mnemonic
declaration (`DeclareMnemonic`), which lived in the deleted `DataBinder.Switches.cs`, was carried into the binder; without it
`SET switch TO ON` was refused (COBOL1757, seen red in `DecidedRuleStageDriftTests` and `HostEntryShapeDriftTests`).

**Two mechanisms found twice and made one.** D added `DataBinder.NextUid()` for the report section's counters and G added
`TakeUid()` beside a `NextUid` peek property for the compiler temporaries: both advance the same counter, so
`ReportSectionBinder` now takes `TakeUid()` and the second method is deleted. G's `DataBinder.SuppressRootField` forwarder
(F's `LinkageBinder.SuppressRootField` owns it) was deleted as well. The two CONSTANT RECORD predicates that more than one
asker puts to the flag (`IsConstantRecordItem`, `ConstantStoreProhibition`) had slid into the `CheckConstantRecordSubtreesPass`
class through a rename match; they are back on the data binder in `DataBinder.ConstantRecord.cs`.

**Ratchets.** `architecture-edges.json` was merged per tolerated pair (a three-way merge by row and pair, not by line) and
ratcheted with `WISEOWL_COBOL_ARCH_EDGES_RATCHET=1`; PB2375, landed, was dropped from every `removedBy` the merge had re-added it to.
`wiseowl-cobol-raw-diagnostic-code-literal` is 230 in the semgrep baseline (B 229, C 230: code that moved out of `DataBinder.cs`
into files semgrep parses, so findings that were there before are counted now; `verify.py` PASS).
`BindPassBoundaryDriftTests.ConcernOwned` shrank to `ResolveFiles`: the `ResolveReports`, `CallBindExternalAndGlobal` and
`OoRouteMethodRedefinesBackings` rows now call `ReportSectionBinder`, `LinkageBinder` and `OoDataBinder`. The stale crefs the
reviews listed (`OoOoDataBinder`, `LinkageRoots`, `CallGlobalRoots` in `docs/DESIGN.md`, `RefModBinder.HasVariableLengthSubordinate`
which is `DataItem.HasVariableLengthSubordinate`, the I-O-CONTROL paragraph cited as §12.4.1 where §12.4.6 is the paragraph and
§12.4 the section: `cite.py --check`) are fixed; the report section is §13.8 (the 13.6 citation was already gone).

**What the self-review looked for.** Architecture: one owner per fact (the two state homes above), no forwarder left by a merge,
passes call binders, no C# text in a bound node. Full code: the cited rule (the mnemonic declaration above is the one rule
a text merge dropped). Performance: no hot-path allocation changed (the slice is one object per unit). Duplication: the uid
takers and the suppressed-root-field door above.

**Gate and oracle.** The code head 53683769c: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,976/20,976 · Unit 39,472/39,472 ·
Characterization 37/37 cases ran (skipped 0) ... (run 000001-20261103T015321Z) ===`, first run, no red on the way; the integration
fixes above were found by the compiler's error list and by `DecidedRuleStageDriftTests`, `HostEntryShapeDriftTests`,
`DecimalPointModeDriftTests`, `ReportScopeBoundaryDriftTests`, `ArchitectureEdgesDriftTests` and
`SpecTraceabilityInventoryDriftTests` run by hand before it. `compare_oracle.py --base 1f60f2503` (main): DIFFERENT, 5 of 16144
cases, and all five are accounted for: the ADDED golden `suite/2023/pb2296_method_options_arithmetic_reaches_body` (A's defect
fix, `UnitTestResult` Passed in `Conformance.trx`), and four DIAGNOSTICS cases (`report-multi-line-2002@85`, its `+permissive`
twin, `pb4109-level-01-multiple-line-at-85`, `pb565-report-multiple-line-at-85`) whose message differs only in the C# site the
construct's `citation` names, `DataBinder.ReportRepetitionOf` before and `ReportSectionBinder.ReportRepetitionOf` after PB2357
moved the member. Every behavior-neutral branch measured IDENTICAL against its own base; this is the one user-visible text
a member move reaches (a lead is filed in the report: a citation should name the ISO rule, not a C# member).

Review round 1 (finisher). One blocking finding: the `ConcernOwned` exception in `BindPassBoundaryDriftTests` kept
`ResolveFiles` waiting for a binder that had already landed (`FileControlBinder` took only the SELECT / I-O-CONTROL half),
and no open note owned the rest. Fix: kb/Work PB2296 names the next FILE step as M7 (`ResolveFiles` and `BindFileSection` leave
`DataBinder`, the entry is deleted in the same change), and M7 landed with M6 in this landing, so the exception list is gone
(a first version that checked its owners from the drift test was refused by the merge group of #1045: a compiler test may not
read `kb/Work`, PB4453); design §8.3 (1)
no longer says the test "fails when it lands". Leads fixed in files already touched: nine stale `DataBinder.X` crefs, the
seed/`InheritConfiguration` names in the design and the `ConfigurationSlice`/`DataBinder.Configuration` summaries,
`LinkageBinder.SealLinkageRules` in INTERPROGRAM_DESIGN, and `DynamicResolvePass` takes its uid through `TakeUid()` (the
`_uidCounter` field is private again). Main moved under the branch (PB2103's metadata provider, §12.3.8.3 SR15 skip): the merge
kept both, and carried `ProgramSpecifier.At` into `DataBinder.Configuration` and `Linkage.Formals/Returning` into
`RepositoryCheck`. Gate: GREEN, run 000002-20261103T030808Z.

### PB2296 M5 and PB2358 — ConfigurationStep, the configuration slice, InheritedGlobals.Constants and ConstantEntryBinder

**M5: ConfigurationStep and the configuration slice (wave 20261834 A).** The configuration-derived state of `DataBinder`
(SPECIAL-NAMES: decimal point, currency signs, the seven `ConfigurationNames` tables; the OBJECT-COMPUTER collating
sequences and classification; DEBUGGING MODE; the REPOSITORY specifiers; the OPTIONS model; and now the mnemonic-names)
is `ConfigurationSlice` (`DataBindState.Configuration`), and `ConfigurationStep` is its one seeding producer: a closed
union `Own | Contained(container slice) | ClassHalf(ClassConfiguration, half OPTIONS)` (ISO §12.3.3 SR1/SR3, §12.3.4 GR1,
§11.9.4 GR1), run by `BinderDriver.BindUnitData` for every unit and by `OoDriver` for the class definition and both
halves. `DataBinder.InheritConfiguration`, `UseSkeletonOptions` and the second writer of the OPTIONS baseline are deleted;
a half holds the class's `ClassConfiguration` value, never the class's binder. `MnemonicRegistry` is a reader
(`ConfigurationSlice.TryGetMnemonic`: own entries, then the container's, the nearest winning) and its parse-tree ancestor
walk is gone. The design's estimate was wrong in two places, corrected in §8.3 (1): `OoRepositoryScope.Build` cannot read a
binder-side slice (the class table resolves it in pass 1, before any binder exists), and the class definition's own binder
still binds the class's section until `SpecialNamesBinder` (PB2355) moves that binding behind the step. 196 references in
41 files followed the moved members (rewritten from the compiler's own error list, `fix_refs.py` in the wave scratch, kept
so a merge with PB2355's `SpecialNamesBinder` can re-run it); 28 CodeGen and Emit pairs onto `DataBindState` and
`ConfigurationSlice` are tolerated on their rows with the remover PB2361. `DataBindStateBoundaryDriftTests` gained
`TheConfigurationSlice_IsSeededOnlyByConfigurationStep`.

**Inherited constants into InheritedGlobals.** `InheritGlobalConstants` and `DropShadowedConstants` left `DataBinder`: a
container's GLOBAL constants are `InheritedGlobals.Constants` with their containment depth (§13.18.27.4 GR1-GR2), read
after the unit's own entries (`ConstantEntryBinder.BoundConstantOf`: a declared local entry hides them structurally, so the
merge into the unit's own table and the per-name removal at declaration are gone), and the hidden symbolic-characters are
`InheritedGlobals.HidesSymbolic`. `InheritStep.DropShadowed` is the one writer, run by `Merge` and by `OoDriver` for a class
half (§8.4.6.2.1 3)).

**PB2358: ConstantEntryBinder.** The `DataBinder.Constants` partial became `ConstantEntryBinder` (`DataBinder.Constants`,
one per unit): the constant tables, the declaration-before-binding walk, the entry evaluation, `FindConstant` and the
integer-position, PICTURE-repetition and VALUE readers; `ConstantDef` is the top-level `Binding/ConstantDef.cs`. The
CONSTANT RECORD predicates stay with the data binder (`DataBinder.ConstantRecord.cs`) and `DeclaredNearerThan` moved to
`DataBinder.NonDataNames.cs`. About sixty callers changed with no forwarder; `ConstantEntryBinder.cs` was rewritten from the
compiler's error list too (`fix_ce.py`), and the private services it needs became `internal` on `DataBinder`, which step 11
is to narrow. 29 inventory rows were re-sited (`record_verdicts`, GAP 19 → 19). Oracle IDENTICAL (16143 cases) after M5,
after the constants and after the extract.

**A defect found on the way, fixed.** `OoEmitter.EmitMethod` pushed a method's own OPTIONS model over the emitter's scoped
override only around its LOCAL-STORAGE initializers, but `NumericRenderer`, `ConditionRenderer` and `IntrinsicRenderer`
read it for the whole body: a method alone writing `ARITHMETIC IS STANDARD-DECIMAL` computed `FUNCTION SQRT(2)` as
1.414213562373095145474621858738 (binary64) instead of §15.84.4 2)'s 34-digit 1.414213562373095048801688724209. The scope
now spans the body (§11.9.4 GR1); golden `11.9-OPTIONS/pb2296_method_options_arithmetic_reaches_body`; the oracle differs
only by that added case. Suspected sibling, unprobed: the method's LOCAL-STORAGE items take `FLOAT-BINARY` endianness from
the class half's OPTIONS (`DataBinder.cs`, the usage default), not the method's own.

