## PB5932: the source listing, step 6b slice 5 - the REPORT SECTION names move into SymbolTable with their resolution

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands slice 5, from
`listing-step-6b-5`: the report class of the progress table (report-names, report groups, sum counters, VARYING counters, report
entries), shown behaviour-neutral by the architecture oracle, and the baseline lowered from 279 to 234.

**The REPORT SECTION names.** `SymbolTable.Reports.cs` owns the report-names (this element's RDs, then its containers' GLOBAL reports,
nearest first, a name already present hiding a farther one), the sum counters (a list per name: §2023:13.18.54.4 GR1 gives every SUM
entry its own counter), the report group description entries as the RD entries write them, and every walk over them. `ReportScope`
keeps the reports in order, their depths and the refused PRESENT WHEN conditions, and names nothing; `DataBindState.SumCounters`,
`DataBinder._reportEntryLocations` and the dictionary `ReferenceResolver.VaryingScope` are gone. `NameClass` gains `ReportGroup`,
`SumCounter`, `ReportVarying`, `ReportEntry` and `ReportSumOperand`, and every reference is `SymbolTable.Identify`: INITIATE, TERMINATE,
GENERATE, USE BEFORE REPORTING, a SUM clause's UPON phrase and data-name-1, a LINE-COUNTER or PAGE-COUNTER qualifier, a sum counter, a
VARYING counter, a constant's LENGTH OF report item, an FD's REPORT clause, FLAG-02 f's TERMINATE and the CODE-SET report items.

**What the code proved about the design** (section 6.7.1, "Step 6b, slice 5"):

- A report group, a sum counter and a report entry may legally share a name, and each class's funnel words the ambiguity in its own
  terms, so `Identify` answers every candidate (`ReportCandidates<T>`) and the funnel decides; `ReportGroupResolution.Resolve` now takes
  the written reference instead of its words.
- The VARYING counters in scope were a name table the clause binder set per entry. They are the reference's site, as a procedure-name's
  method and section are: `ReportSite` carries the enclosing report (§2023:13.18.53.3 SR4) and the counters in scope (§2023:13.18.64.3
  SR2), and the table finds the nearest declaration of a spelling.
- A report entry is named before the report section binds (a constant's LENGTH OF, §2023:13.10.3 SR11) and after (a SUM clause's
  data-name-1, §2023:13.18.54.3 SR4); each stage keeps its own walk, so the oracle is identical.
- The data division reaches report entries with the name as text, because the data-name class still holds its clause operands so. What
  it asks is a declaration fact, so `Declarations.ReportEntriesNamed` answers `DeclarationFact`s. At COBOL-85 a sum counter IS such an
  operand (§1985:XIII.3.9.3 SR10d), so `SymbolTable.SumCounterOperand85` returns the register; it moved into the table with the counters,
  its baseline key with it, and it leaves the baseline with the data-name class.
- An FD's REPORT clause is identified once where the clauses are screened (`FileControlBinder` keeps the written word; the model keeps
  the name), and which file each RD is written to is the agreement of two declarations, `Declarations.NamesReport`.
- `DataBinder.ReportPrintablePicture`, counted as a report lookup by the first measurement, resolves no name: classified.

**Proved.** `python scripts/arch/compare_oracle.py`: `=== ARCH-ORACLE: IDENTICAL — 22643 cases (baseline b039f2b67e62) ===` after the
move. `NameTableOwnershipDriftTests` was red on the move before the baseline was lowered, naming the 45 entries it retired (rule (1) 5,
rule (3) 9, rule (5) 31) and 2 findings in neither file; the baseline is 234 (35 tables, 42 lookups, 157 sites) and
`string-parameter-members.json` 125. `ReportScopeBoundaryDriftTests.TheSumCounterReference_IsGatedByTheWatermark` replaces the gated-view
test and was shown red with the watermark `Require` removed.
The `None` row of the front end and bind against the parent b039f2b67e (DESIGN-listing.md section 9) caught a cost the oracle cannot
see: the first build allocated 0.6 to 1.3 MB more per compilation on every program (IX113A 69.98 and 69.95 MB against 68.92 and
68.95), because the moved sum-counter arm, which every data-name reference asks first, read the reference's word and qualifiers before
asking whether any counter carries the name. The arm now asks the table first (2fad1cf810); re-measured, every branch `None` row lies
within 0.13 MB of the parent's three runs but one NC218A run whose other rows lie inside the parent's (the benchmark README's step-6b
table, slice 5).

The two review leads of slices 2 and 3 that the controller filed against this slice are closed: the design's four `ProcedureNameMap`
mentions name `SymbolTable`'s `ProcedureNames<T>`, and `NamesConstant` and `IsFailedConstant`, constant-name resolutions that took the
name as text, take the written word; the slice-3 claim "no string reaches the table" now says which declaration questions still take
text and why. The oracle against the merged main stays IDENTICAL.

kb/Work/PB5932/2026-11-19-step-6b-slice-5.md holds what moved and what remains: the data-name class (with the two text-held report
operands), the program, function and repository names, and the OO names.
