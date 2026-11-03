## PB2296 — the DataBinder extract's step 1 to 10 branches land as one (integration of wave 20261834)

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

**Gate.** Filled in at the head by the next commit.
