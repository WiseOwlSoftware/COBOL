## Four binder god classes split by concern: EVALUATE, SORT/MERGE, INITIALIZE and the procedure table

Wave 20262168 group H ran four Extract notes of the architecture program (PB2119; DESIGN-architecture-review §8.3) as one
branch, one checkpoint commit per note. Each split moved members by line range with their bodies unchanged, so no
statement or comment was retyped; each adds a boundary drift test; each re-sited the traceability inventory's witnesses
through record_verdicts batches with the GAP unchanged at every edition.

**PB2338, EvaluateBinder (1,093 lines).** Now four types: `EvaluateBinder` (70) binds the statement (WHEN clauses, bodies,
WHEN OTHER, the 1985 object-evaluation rule); `EvaluateSubjectBinder` (323) the selection subjects, one `SubjectSlot` each
(§2023:14.9.13.4 GR3); `WhenPhraseBinder` (740) one WHEN phrase (the SR2 count, SR6 classification, SR10 Table 15 screen
and the GR4 terms); `EvaluateOperands` (68) the bare-operand classification and operand bind both sides share.
`EvaluateBinderBoundaryDriftTests`. Witnesses: 2023 17 rows, 1985 4, 2002 11.

**PB2341, SortBinder (1,157 lines).** The phrases and file screens SORT and MERGE both print are now
`SortMergePhraseBinder` (546): placement, refused-statement ranges, SD record facts, KEY (through `SortKeyAdmission`),
COLLATING, USING/GIVING, procedure ranges, and the SAME-clause, record-size, indexed-GIVING-key and reel screens.
`SortBinder` (641) binds the four statements. `MergeBinder` is deferred to PB2347 by decision: the dispatch is
`StatementBinder`'s, in `compiler.bound`, whose tolerated edge row may not grow, and a forwarding `BindMerge` is barred
(addendum `kb/Work/PB2347/2026-11-24-mergebinder-waits-on-the-dispatch-move.md`). `SortBinderBoundaryDriftTests`.
Witnesses: 2023 32, 1985 27, 2002 8.

**PB2344, InitializeBinder (1,064 lines).** `ReplacingPhraseBinder` (202) binds the REPLACING phrase with its SR3, SR4 and
SR6 screens and returns the `InitializeReplacingItem` node, which the statement spec now carries directly; the item and
slice category rows moved to `InitializeCategories` (names kept so inventory prose stays true). `InitializeValueOperand`
stays: its `CodeGen.FigurativeConstants` read is PB2395's tolerated edge. `InitializeBinder` is 858 lines.
`InitializeBinderBoundaryDriftTests`. Witnesses: 2023 7, 1985 1, 2002 1.

**PB2345, ProcedureTableBuilder (1,141 lines).** `UseStatementBinder` (268) binds a USE statement's trigger scope with the
SR7/SR8/SR9/SR14 registers; `DebugDeclarativeResolver` (371) resolves the 1985 USE FOR DEBUGGING subjects and targets into
lists the table owns, so `StatementBinder`'s reads are unchanged. The table (574) keeps the pc space, sections, declarative
scopes and procedure-name resolution. `ProcedureTableBoundaryDriftTests`. Witnesses: 2023 12, 1985 27, 2002 5.

**Neutrality.** The §4 oracle (`scripts/arch/compare_oracle.py`) and the gate are recorded in the wave report
(`E:\COBOL-coord\scratch\reports\w20262168h-PB2338-report.md`). The architecture edges walk measures no new pair: every new
type is reached from its old owner, never from `StatementBinder`.
