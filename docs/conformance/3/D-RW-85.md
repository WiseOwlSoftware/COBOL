- **D-RW-85 — Report Writer at `--std 85` — four rules the 1985 text states differently from 2002 and 2023 (kb/Work PB5245, PB5246, PB5248),
  one rule neither edition words for a report of several details (PB5249), and the clause-combination table (PB5243)**.
  **Pinned choice: PER EDITION, each edition's own text.** (a) *SUM size error (PB5248, VCR row 7.58)*: §2023:13.18.54.4 GR4 fills the
  printable item with spaces when the size error indicator is set; §1985:XIII.3.19.4 GR4 has no indicator and MOVEs the counter, so at
  `--std 85` the item holds the counter's content (the value after an overflow is undefined, its presentation is not) and nothing is
  raised (`CobolReport.SumPresentable`, `DialectBehavior.SumSizeErrorBlanksPrintableItem`). (b) *TERMINATE after a saved NEXT GROUP
  (PB5245, row 7.59)*: §2023:13.18.37.4 GR4 a) discards the saved integer; at `--std 85` the first control footing TERMINATE produces meets
  it in its fit test (§1985:XIII.3.10.8 GR3; `CobolReport.TerminateCore`). (c) *GROUP INDICATE and SUPPRESS (PB5246, row 7.60)*: from 2002
  every GENERATE of the detail uses the occasion up; at `--std 85` only a GENERATE that presented (§1985:XIII.3.13.4 GR1, XIII.4.6.4 GR3;
  `CobolReport.GenerateCore`). (d) *GENERATE report-name and SUM ... UPON (PB5249)*: §1985:XIII.3.20.4 GR11 processes the statement of
  a report with exactly one detail group "as though a GENERATE data-name statement were being executed", its step a) subtotalling for that
  detail; §2023:14.9.16.4 GR2 says "the same processing as for a GENERATE data-name statement for the same report, except that no detail
  is printed" and names no detail. **Pinned choice, every edition:** a report with exactly ONE named detail group subtotals for it
  (`BoundGenerate.SummaryDetail`, `CobolReport.GenerateSummary`); a report with several details names none, so only the UPON-less terms
  add. The same reading runs that one detail's USE BEFORE REPORTING procedure, after the subtotalling and with no presentation after it
  (§1985:XIII.3.20.4 GR11 b) and d); at every edition, since §2023:14.9.49.4 GR9 performs the procedure "on each occasion that the named
  report group is processed" and GR2's summary reporting is the detail's processing without its printing; GnuCOBOL runs it too, though
  before the subtotal), and a report with several details runs none; the processed detail's entries are added into the SUM counters naming
  them, §2023:13.18.54.4 GR7 a) (kb/Work PB5337; `CobolReport.BeginGroup`'s `print: false`). (e) *Table 1 (PB5243)*: at `--std 85` an elementary format 3 entry's clauses must be the cells of one row of §1985:XIII.3.9.3 SR11
  Table 1 (`ReportEntryClauseTable`): SOURCE with JUSTIFIED, BLANK WHEN ZERO or GROUP INDICATE and no COLUMN clause is legal there (the entry
  prints nothing), and VALUE with BLANK WHEN ZERO and SUM with JUSTIFIED are refused; at every edition SOURCE, SUM and VALUE are the
  alternatives of one brace (§1985:XIII.3.9.2, §2023:13.15.2) and an entry writing two is refused COBOL2247. Witnesses:
  `conformance:cases/13.18.54-SUM/pb5248_sum_size_error_presentation`, `.../13.18.37-NEXT-GROUP/pb5245_terminate_saved_next_group` (each `.85.out` and `.out`),
  `.../13.18.28-GROUP-INDICATE/pb5246_group_indicate_suppressed_generate` and `.../XIII.3.13@1985-GROUP-INDICATE-CLAUSE/pb5246_group_indicate_suppressed_generate_85`, `.../13.18.54-SUM/pb5249_generate_report_name_sum_upon`, `.../14.9.49-USE/pb5337_generate_report_name_runs_detail_use` and `.../pb5337_generate_report_name_several_details`, `.../13.18.54-SUM/pb5337_generate_report_name_sums_detail_entry`,
  `.../XIII.3.9@1985-REPORT-GROUP-DESCRIPTION-ENTRY/pb5243_table1_permitted_rows` and the three `pb5243-*` refusals.
