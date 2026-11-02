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
(PB2361, `DataBindResult`), `EmitContext` onto `OptionsModel` (PB2402, the OPTIONS types move to
compiler.model) and `StatementBinder` onto `ProcedureBindState` (PB2347, `StatementBinder` leaves `Bound/`). The
inventory's three code-locations that named moved members were re-sited by a `record_verdicts` batch (GAP 19 → 19),
and the second gate's `BackendContractDriftTests` — which walks compiler.model as backend input — classified
`FileNameScope`'s two maps as file-names and refused the delegate the first `Scoped<T>` held for its root: the override
now holds only its pushed values, and the reader passes the root (`Over(root)`; `EmitContext.CurrentOptions`).

**Found on the way.** `DataBinder.IsVisibleFileName` had no caller and is deleted; `CheckExternalFileConsistency`'s
summary sat stacked above `ExternalMaskAt`'s and now documents its own method; an inherited "§13.18.22 NOTE 1" citation
is §13.18.22.4 GR1's NOTE 1.

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
