## PB2296 M5 and PB2358 — ConfigurationStep, the configuration slice, InheritedGlobals.Constants and ConstantEntryBinder

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
