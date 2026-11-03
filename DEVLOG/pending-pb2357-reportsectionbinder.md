## PB2357 — DataBinder extract step 7: ReportSectionBinder, ReportScope and ReportModel replace the Reports partial

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
