## PB2296 M6 and M7 — the data binder's tables become value slices of DataBindState, and the file binder takes the FILE half

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
