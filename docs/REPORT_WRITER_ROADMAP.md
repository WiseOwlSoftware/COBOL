# Report Writer — Implementation Roadmap

The one LIVE COBOL-85 module not yet implemented (ISO 2023 §A.4.11, optional element). Full design in
`docs/REPORT_WRITER_DESIGN.json` (3-facet parallel design). XL subsystem (~15 files). Implement in ordered
stages — each builds + guard-gates green before the next. NIST targets: RW101A…RW6 (+ RW301M flagging).

Spec: ISO §13.8 (Report Section), §13.14 (RD entry), §13.15 (report group entry), §13.18.12/14/16/28/37/39/46/
53/54/57/63 (CODE/COLUMN/CONTROL/GROUP-INDICATE/NEXT-GROUP/PAGE/REPORT/SOURCE/SUM/TYPE/VALUE clauses), §14 verbs.

## Stage 1 — Grammar + parse (foundation; highest regression risk → isolate + full-guard)
- `CobolData.g4`: add `reportClause : REPORT IS? reportName` to the FD/SD clause list (§13.18.46).
- `CobolReportWriter.g4` (replace the stub): `reportSection`, `reportDescriptionEntry` (RD + CODE/CONTROL/PAGE
  LIMIT [HEADING/FIRST DETAIL/LAST DETAIL/FOOTING]/IS GLOBAL), `reportGroupEntry` (level + name + `TYPE IS`
  {REPORT/PAGE/CONTROL HEADING|DETAIL|CONTROL/PAGE/REPORT FOOTING}), and report-group clauses (LINE NUMBER,
  NEXT GROUP, COLUMN NUMBER, SOURCE, SUM…UPON, VALUE, GROUP INDICATE, PRESENT WHEN, OCCURS, PIC/USAGE/SIGN/JUST/
  BLANK WHEN ZERO). Build auto-regenerates ANTLR (MSBuild `EnsureGeneratedFiles`). **DoD:** RW101A parses (no
  CBL grammar errors); full guard ALL GREEN (no parse regression).

## Stage 2 — Semantic model
- `SymbolKind.Report` / `SymbolKind.ReportGroup`; new `ReportSymbol` (name, hosting FILE, PAGE limits, CONTROL
  items, CODE, groups, implicit PAGE-COUNTER/LINE-COUNTER registers) + `ReportGroupSymbol` (type, control
  data-name, lines, columns, source/sum/value fields, GROUP INDICATE). `SemanticBuilder` visits the RD + groups,
  builds the model, links `REPORT IS` to its FILE, and synthesizes LINE-COUNTER/PAGE-COUNTER + DEBUG-free
  registers. **DoD:** RW101A binds (no undefined-name/CBL errors); guard green.

## Stage 3 — Runtime (`ReportWriterRuntime`, new isolated class)
- INITIATE: reset PAGE-COUNTER=1, LINE-COUNTER=0, save-area init. GENERATE detail (and summary form): control-
  break detection (compare CONTROL items to prior saved values; break minor→major→FINAL), print CONTROL FOOTINGs
  (inner→outer) then CONTROL HEADINGs (outer→inner), page-fit/advance via PAGE LIMIT + HEADING/FOOTING + PAGE
  HEADING/FOOTING groups, LINE NUMBER absolute/relative/NEXT-PAGE, COLUMN placement of SOURCE/SUM/VALUE into the
  print line, SUM accumulation (counters reset at their control level, UPON detail), GROUP INDICATE (first detail
  after break/page only). TERMINATE: final CONTROL FOOTINGs + REPORT FOOTING. Unit-test the control-break + SUM
  algorithms in isolation. **DoD:** runtime unit tests pass (not yet wired).

## Stage 4 — Verbs + codegen (wire it together)
- Bound nodes `BoundInitiate`/`BoundGenerate`/`BoundTerminate`/`BoundSuppress`; IR instructions; **CilEmitter
  dispatch cases for each new IrRuntimeCall** (GOTCHA: a missing case → InvalidProgramException at Main). Lower
  GENERATE to: evaluate SOURCE/SUM operands → call `ReportWriterRuntime.Generate` → write produced lines through
  the report's FILE (reuse the WRITE/ADVANCING path). `USE BEFORE REPORTING data-name` declarative (mirror the
  USE AFTER ERROR dispatch). **DoD:** RW101A runs and produces a CCVS report.

## Stage 5 — Baseline + spec-extra tests
- Verify RW101A…RW6 each from a clean dir (rc=0, 0 FAIL*, footer "NO TEST(S) FAILED", EXECUTED>0); baseline each;
  add to the guard. RW301M = flagging (correct diagnostics). Author `tests/nist/extra/` tests for options NIST
  under-covers (multiple CONTROLs, GROUP INDICATE, NEXT GROUP ON NEXT PAGE, summary reporting, SUM…UPON). Update
  `scripts/compliance.sh` (Report Writer → baselined). **DoD:** baseline target (live) → ~100%.

## Parallelism within the build
Stages 1→2→4 are a dependency chain (sequential). Stage 3 (runtime) is independent of 1/2 and can be built in
parallel (its interface is fixed by the design), then wired in Stage 4. Stage 5 tests parallelize per RW program.
