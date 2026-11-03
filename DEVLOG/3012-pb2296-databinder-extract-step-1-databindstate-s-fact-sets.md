## Entry 3012 — 2026-11-02 17:14 PST — PB2296 — DataBinder extract step 1: DataBindState's fact sets, ProcedureBindState, CheckingFold and InheritStep; PB2357 — DataBinder extract step 7: ReportSectionBinder, ReportScope and ReportModel replace the Reports partial

### PB2296 — DataBinder extract step 1: DataBindState's fact sets, ProcedureBindState, CheckingFold and InheritStep

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

### PB2357 — DataBinder extract step 7: ReportSectionBinder, ReportScope and ReportModel replace the Reports partial

kb/Work PB2357 is step 7 of the `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)), run beside
the other steps of wave 20261834 on top of PB2296 step 1's branch (its `DataBindState` and `ProcedureBindState`, merged first
with `merge_drivers.py`). Behavior-neutral by contract: no re-probe of a defect applies, the probe is the §4 oracle.

**What moved.** `DataBinder.Reports.cs` was 5,341 lines: the first thousand were the report model types, the rest 100 methods
of the data binder. It is now three files. `Binding/ReportModel.cs` holds the model types, unchanged. `Binding/ReportSectionBinder.cs`
holds the binder (`BindReportSection`, `ResolveReports`, `InheritContainerReports`, `ReportEntryReferenceRefused`, every clause and
entry screen); one instance per `DataBinder`, `DataBinder.ReportSection`, built with the binder it serves for the shared helpers
(clause-operand resolution, picture screens, `NextUid`), whose members it needed it made `internal`. `Binding/ReportScope.cs`
(`DataBinder.Reports`) holds the facts the binder writes and every later phase reads: `Declared`, `Visible`, `Depth`, the sum
counters, the refused PRESENT WHEN conditions, and the lookups over them (`EntriesNamed`, `NearerEntryHides`, `NearestInScope`).
The binder is the only writer. `DataBinder.SumCounters` stays as the watermark-gated view of the scope's counters. The rest of the
re-architecture (the clause-operand helpers leaving `DataBinder`) belongs to the other steps; the design's §8.3 says so.

**Callers.** Every reader of the old `Reports`, `VisibleReports`, `ReportDepth(...)`, `NearestInScope`, `PresentWhenRefused`,
`NearerReportEntryHides`, `ReportEntriesNamed` and `ReportEntryReferenceRefused` changed in the same commit (emitters,
`SymbolTable`, `StatementBinder`, the resolvers, `BindPipeline`, the storage and conformance passes); no forwarder remains. One
edit was not mechanical: a printable item's `_rptItem` name and uid came from one counter read before and after the item was built;
the uid is now taken once first, which is the same number.

**Drift and register.** New `ReportScopeBoundaryDriftTests` (a scope is mutated only in `ReportSectionBinder.cs`; no
`DataBinder.Reports*.cs` returns; seen red on a planted `Reports.Declare(...)`). Ten existing drift tests that read
`DataBinder.Reports.cs` by path now read the file that holds what they scan. `architecture-edges.json` tolerates the new pairs onto
`ReportScope` (CodeGen, `StatementBinder`, `SymbolTable`) with the removers of their `ReportModel` pairs, and moves the two
`Binding` to `Validation` pairs to `ReportSectionBinder`. 158 inventory rows were re-sited by record_verdicts
(`pb2357-reportsectionbinder-code-locations`, witnesses retired with reasons) and nine rows' prose restated
(`pb2357-notes-prose`); GAP 19 to 19, no row closed (`closes_rows: []`, behavior-neutral). The REPORT_WRITER_DESIGN, DATA_MODEL and
architecture documents, comments, the grammar's comments, the constructs source and five open notes that named the old file now name the
new ones. The note's `blocked_by` listed the 11-step census order; it is now PB2296, the result it depends on.

**Self-review.** Architecture: one writer per fact, the read side holds no binder, phase order untouched. Full code: the
move is verbatim apart from the host prefix and the uid; the cited rules were not re-derived (no rule changed). Performance: no new
allocation on a hot path (the binder is built once per unit). Duplication: none added; the three `DataBinder` helpers
the binder shares are the single copies.

**Proof.** `compare_oracle.py --base bd9a034e0`: `=== ARCH-ORACLE: IDENTICAL — 16143 cases ===`.

**Gate.** Run 000001 was RED on the evidence-supersession audit (two frozen PHASE-13 citations named the split file; each now
carries a `superseded_by` marker). Run 000002-20261102T221759Z: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit
39,398/39,398 · Characterization 37/37 cases ran (skipped 0) ===`; `ReportScopeBoundaryDriftTests` 3/3 passed in its Unit.trx;
`semgrep/verify.py` PASS (no count rose). Linux: not part of the per-change gate (PB4327).

**Merge with main and review round 1 (finisher).** The reviewed branch (6175b548c) was cut before main's PB2371-PB2375
split of `ReferenceResolver` and carried PB2296 step 1; merging them needed a semantic resolution, not a textual one.
`DataBinder` keeps `IPlaceFacts` (main's PB2375) by answering it from `State` (`IsImageBackedEarly` reads the
`ImageForcedItems` mid-bind view, `OdoReferenceChecking` the unit's checking slice); the `PendingPreOps`, `PropertyOps`
and `ActivationSites` call sites in `SubscriptBinder`, `ReferenceResolver` and `CounterResolver` read `Procedure` and `State`;
main's edits to `DataBinder.Reports.cs` (`RefModBinder`, `SubscriptBinder`, `DataItem.HasVariableLengthSubordinate`,
`ReportControlModel.Place`) were carried to `ReportSectionBinder.cs`, `ReportModel.cs` and `ReportScope.cs`. R1-2 (blocking):
the six tolerated pairs onto `ReportScope` named removers that never planned to remove them; PB2420 now names `ReportScope.cs`
in its Sites and its Removes line (the type moves to compiler.model with the report model types) and the `SymbolTable` pair
names PB2420 instead of PB2361. Leads fixed in the files this change touches: crefs requalified (R1-1), the property
`ReferenceResolver.ReportScope` renamed `EnclosingReport` so it no longer shares a name with the type (R1-3), the one-writer
drift test matches any receiver of the unambiguous mutators and any scope- or reports-named receiver of `Declare`/`Inherit`
(R1-4), the report section is cited as 13.8 (R1-5; `cite.py --check 13.8 "Report section"` OK), and `ReportScope.SumCounters`
is `internal` with a drift fact that only the two `DataBinder` files read it (R1-6).

**Gate after the merge.** Run 000001-20261103T002112Z: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,411/39,411 · Characterization 37/37 cases ran (skipped 0) ===`; `ReportScopeBoundaryDriftTests` now 4 facts; `semgrep/verify.py` PASS.

**Review round 2.** R2-1 (blocking): R1-2 was half fixed, because PB2420's move of `ReportScope.cs` waited on two cuts no note planned.
`ReportScope` holds a `Core.ConditionContext` set (`PresentWhenRefused`/`RefusePresentWhen`) and calls the static
`DataBinder.QualifierWalk`. PB2416, the cut of the report models' parse contexts, now names `ReportScope.cs:103-105`, its two
writers and its one reader in its title, Sites and target: the SR16 verdict becomes a fact on the model's PRESENT WHEN member.
PB2399, which ends the model's remaining binder reads, now names `QualifierWalk` (a stateless static, four callers) beside
`WrittenQualified`. PB2420's Removes line and DESIGN-architecture-review §8.3 (1) name both notes, and both are already in PB2420's
`blocked_by`. No code changed in this round: the cuts belong to those waves, whose Sites now carry them. `work.py check` clean.

