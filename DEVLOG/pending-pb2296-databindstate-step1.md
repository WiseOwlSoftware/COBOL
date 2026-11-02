## PB2296 — DataBinder extract step 1: DataBindState's fact sets, ProcedureBindState, CheckingFold and InheritStep

kb/Work PB2296 is step 1 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3
(1), census R0-0001), on the critical path of the backend seam (PB2293). It is behavior-neutral by contract, so the proof
is the §4 oracle: `python scripts/arch/compare_oracle.py` against the base a91db12e2212 answered
`=== ARCH-ORACLE: IDENTICAL — 16123 cases ===` after each mechanism below.

**The four fact sets.** `WholeGroupReferenced`, `ImageForcedItems`, `CharacterChannelItems` and `ActivationSites`
accumulate across phases (FilesResolved, ProcedureBound, UsageCollected), so no single-producer slice can hold them. They
left `DataBinder` for `DataBindState` (`DataBinder.State`), each a `FactSet<T>` with the three views the design names:
`IAppends<T>` (add only), `IReadsEarly<T>` (`RecordedSoFar`, the mid-bind read whose statement-order visibility decides
the `NumericImagePlace` wrap) and `IReads<T>` (the whole set). `DataBinder.MarkImageForced` and `IsImageBackedEarly` are
deleted, not forwarded; the `UsageCollectionPass` visitor holds only the append views.

**ProcedureBindState.** The state procedure binding keeps that is no fact at all moved into its three shapes:
`Scoped<OptionsModel>` (a method's own OPTIONS pushed over the unit's, §11.9.4 GR1), two `DrainQueue<T>`s
(`PendingPreOps`, `PropertyOps`) and two `Cursor<T>`s (`OperandEvaluations`, `ActiveMethodScope`). Building it corrected
the design in three places, now in §8.3 (1): it is held per unit binder (`DataBinder.Procedure`), not on `BindSession`,
because the resolver and the binder's own name lookup reach it through the unit's binder and the method-scope cursor is
also entered by the storage-claim resolution (a data pass) and by `FlagConformancePass`; the drain queue needs
`InsertAt` (a freeze's evaluation-order placement, §14.6.4) and `Since` (the enclosing-occurrence capture reads an
operand's entries without taking them); and the cursor needs `Advance`, because the operand-evaluation count grows
inside its statement's scope. `DataBinder.Options` is now written once; the emitter's method override, which used to
assign `DataBinder.Options` around a method's LOCAL-STORAGE initializers (kb/Work PB1215), is its own
`EmitContext.Options` scope.

**CheckingFold.** The five unit checking flags became the write-once `UnitChecking` slice and the method's three the
`MethodChecking` slice (`OoMethodSymbol.Checking`), both produced only by `CheckingFold` (`Unit`, `ClassHalf`,
`Method`), which took `BinderDriver.ExternalMaskAt` with it so the program and method halves of §14.8.4.1 share one
fold.

**InheritStep.** A contained program's inheritance is written in one place: `InheritStep.Seed` before the unit binds
(GLOBAL constants, type declarations, file-names, record areas, the report and data-root offers, the reserved bridge
names) and `InheritStep.Merge` after (`MarkFormals`, the GLOBAL bridges, `DropShadowedConstants`), into the
`InheritedGlobals` slice. The merge of the containers' GLOBAL files into `FilesByName` is gone: `FilesByName` holds the
unit's own files and every file-name reader asks `DataBinder.VisibleFiles` (own first, then the containers' GLOBAL files,
nearest first), so §8.4.6.2.1 3) a)'s local-shadows rule is the lookup's structure, not a `TryAdd` after the bind.
`DataBindStateBoundaryDriftTests` pins who writes the new slices (seen to fail on a planted violation), and
`GlobalNameClassDriftTests` now walks the slices for name indexes, so a name index that moves into one stays checked.

**Layering.** The gate's `ArchitectureEdgesDriftTests` refused the first cut: code generation and the statement binder
(still in `Binding.Bound`) had gained pairs onto the new binding types. The restoring shapes, the two checking records
and `FileNameScope` hold no binder type, so they moved to compiler.model, below both users. Three pairs remain and are
tolerated by hand on their rows with the notes that remove them: `ProgramEmitter` and `OoEmitter` onto `DataBindState`
(PB2361, `DataBindResult`), `EmitContext` and `PhysicalModel` onto `OptionsModel` (PB2402, the OPTIONS types move to
compiler.model) and `StatementBinder` onto `ProcedureBindState` (PB2347, `StatementBinder` leaves `Bound/`). The
inventory's three code-locations that named moved members were re-sited by a `record_verdicts` batch (GAP 19 → 19),
and `OptionsModelConsumerDriftTests` now counts a read through a scoped override's `Current`.

**Found on the way.** `DataBinder.IsVisibleFileName` had no caller and is deleted; `CheckExternalFileConsistency`'s
summary sat stacked above `ExternalMaskAt`'s and now documents its own method; an inherited "§13.18.22 NOTE 1" citation
is §13.18.22.4 GR1's NOTE 1.
