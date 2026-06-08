# Report Writer — Implementation Roadmap

> **STATUS BANNER (2026-06-07).** Design reference + staged roadmap for the COBOL Report Writer subsystem.
> **Implementation status: IMPLEMENTED / largely complete** — not "not yet implemented" (that opening claim below is
> STALE and retained only for historical context). The runtime exists at `src/CobolSharp.Runtime/ReportWriterRuntime.cs`
> (control-break detection, CONTROL HEADING/FOOTING, SUM accumulators, GROUP INDICATE, page/line mechanics); RW101A–104A
> are NIST-baselined and the control-break + SUM tier is live (DEVLOG 349–352). Integration tests:
> `tests/CobolSharp.Tests.Integration/ReportWriterSpecTests.cs`. Remaining deferrals: numeric-edited/COMP SUM addends,
> sum-of-sums rollup, special-register SOURCE in DETAIL, full page-fit interaction of CF/CH with FIRST DETAIL/overflow.
> **Stack: .NET 10 / C# 14.** Backend is CIL-only via Mono.Cecil (no custom VM / no bytecode interpreter; Roslyn C#
> backend is a future additive option, Cecil = oracle). The companion LIVE engine design is
> `docs/REPORT_WRITER_CONTROL_DESIGN.md`. Plan SSOT = `docs/MASTER_PLAN.md`; doctrine = `PROMPT.md`.
> Consolidated from 4 prior docs (this roadmap + 3 long-titled "Report Writer … Architecture" essays), 2026-06-07.
>
> NOTE on data model: report fields render through the standard data-item path. As the typed-native data model
> (`docs/DATA_MODEL_ARCHITECTURE.md`) lands (char→string, numeric→long/decimal), SOURCE/SUM field rendering tracks
> it; SUM accumulation is decimal. No 8-byte pointer handle / no custom VM is involved anywhere in this subsystem.

The one LIVE COBOL-85 module not yet implemented (ISO 2023 §A.4.11, optional element). Full design in
`docs/REPORT_WRITER_DESIGN.json` (3-facet parallel design). XL subsystem (~15 files). Implement in ordered
stages — each builds + guard-gates green before the next. NIST targets: RW101A…RW6 (+ RW301M flagging).

Spec: ISO §13.8 (Report Section), §13.14 (RD entry), §13.15 (report group entry), §13.18.12/14/16/28/37/39/46/
53/54/57/63 (CODE/COLUMN/CONTROL/GROUP-INDICATE/NEXT-GROUP/PAGE/REPORT/SOURCE/SUM/TYPE/VALUE clauses), §14 verbs.

## Stage 0 — Unblock the header (a WS-DIALECT prerequisite, NOT report-writer code)
**Finding (2026-06-04):** RW101A fails at line 11 — `error COBOL0001: unexpected '5203'` — in the obsolete
`INSTALLATION` paragraph's free-text address block, BEFORE any REPORT SECTION. The RW lexer tokens (RD, SUM,
DETAIL, …) and `reportSection` ALREADY exist. The real blocker is the identification-division **comment-entry**
paragraphs: `authorContent`/`installationContent`/… are `~DOT+` (CobolParserCore.g4:172-210), which stops at the
FIRST embedded period (`…SERVICE.`), so a multi-line comment-entry with embedded periods / a number-starting line
(`5203 LEESBURG PIKE`) / a quoted line (`" HIGH "`) breaks. Per ISO §13.x a comment-entry is *any* characters
across one or more lines until the next paragraph-name or division header — embedded periods are text, not
terminators. **Fix (a WS-DIALECT item, likely the cleanest):** consume the obsolete comment paragraphs
(`AUTHOR`/`INSTALLATION`/`DATE-WRITTEN`/`DATE-COMPILED`/`SECURITY`/`REMARKS`) and their free text in the
preprocessor (`ReferenceFormatProcessor`) when an Area-A paragraph header is seen — blank/comment the content to
the next Area-A header. (Column-aware so it never eats real code — regression surface = every program's ID
division, so full-guard carefully.) An ANTLR-only non-greedy `.*?` content rule is the fragile alternative.
This must land before Report Writer Stages 1-5 can even compile RW101A. **DoD:** RW101A parses past its header.

## Stage 1 — Grammar + parse (foundation; highest regression risk → isolate + full-guard)
**Note:** much of this already exists (`CobolReportWriter.g4` has `reportSection`/`RD`/`reportGroupEntry`/SUM).
After Stage 0, re-probe RW101A to see how far the *existing* report-writer grammar gets, and fill only the gaps
(the design's full clause set: LINE/COLUMN/NEXT GROUP/TYPE variants/PAGE LIMIT/CONTROL/GROUP INDICATE).
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

---

## Appendix A — Consolidated subsystem reference (salvaged from the 3 essays, 2026-06-07)

These items are the unique, still-relevant conceptual content from the three long-titled "Report Writer … Architecture"
generation-pass essays (now merged here and marked for deletion). They are TARGET DESIGN, not a claim of completeness —
verify any specific clause against `src/CobolSharp.Runtime/ReportWriterRuntime.cs`, `docs/REPORT_WRITER_CONTROL_DESIGN.md`,
and the stages above before relying on it. (Stale forward statements in the essays — ".NET 9", "C# 13", "custom VM",
"AOT/WASM target", any "fully implements" — are corrected per the status banner; AOT/WASM and printer-stream output are
DESIGN-ONLY product-surface aspirations, Phase E.)

### A.1 Report group taxonomy (TYPE clauses)
The full group set the subsystem must render: REPORT HEADING, PAGE HEADING, CONTROL HEADING, DETAIL, CONTROL FOOTING,
PAGE FOOTING, REPORT FOOTING (plus SUMMARY-style reporting via SUM…UPON). Groups may contain elementary report fields,
nested groups, and SOURCE / VALUE / SUM / COUNT / AVERAGE / MIN / MAX fields. Multiple reports per program, each with
independent state. (ISO §13.15 report-group entry; §13.18.63 TYPE.)

### A.2 Report field kinds (rendering semantics)
- **SOURCE data-item** — copies the data item's value into the report at its COLUMN; numeric → DISPLAY conversion;
  NATIONAL → DISPLAY. (§13.18.53.)
- **VALUE literal** — prints literal text verbatim at its COLUMN. (§13.18.63 VALUE in report context.)
- **SUM data-item** — decimal accumulator; reset at its RESET / control level. (§13.18.54.)
- **COUNT** — increments a counter (DETAIL count); reset at control break.
- **AVERAGE** — maintains sum and count; computes SUM/COUNT at print time (zero count → zero).
- **MIN / MAX** — track minimum / maximum value; reset at control break.
- **COMPUTE field = expression** — computed report field.
Accumulator lifetime: DETAIL-level resets each detail; CONTROL-level resets at its break; SUMMARY-level resets at
end of report.

### A.3 Page / line control
- PAGE LIMIT n (RD) sets max lines per page; a configurable default (the essays cite 60 and 132-column line width as
  examples — actual defaults follow `docs/REPORT_WRITER_CONTROL_DESIGN.md` / the RD HEADING/FIRST DETAIL/LAST DETAIL/
  FOOTING regions). ReportEngine maintains CurrentLine / CurrentPage plus the implicit LINE-COUNTER / PAGE-COUNTER
  special registers.
- LINE NUMBER n = absolute line on page; LINE PLUS n / LINE +n = relative advance; NEXT GROUP ON NEXT PAGE forces a page.
- Writing a line that would exceed PAGE LIMIT → emit PAGE FOOTING, advance page, emit PAGE HEADING, reset line counter.
- COLUMN n = 1-based starting column; multiple fields render in order of appearance; later COLUMN placements overwrite.

### A.4 Control-break ordering (canonical sequence)
CONTROL IS field1 field2 … (field1 = most-major; "FINAL" if present is the highest level). On each GENERATE/DETAIL,
compare current control values to the saved prior values; the most-major non-FINAL changed level is the break level L.
On a break at L the presentation order is (matching `docs/REPORT_WRITER_CONTROL_DESIGN.md`):
1. CONTROL FOOTINGs minor→L (the *ending* group's control values are restored first so CF SOURCE shows them);
2. CONTROL HEADINGs L→minor;
then the per-page order at the top of a page is REPORT HEADING, PAGE HEADING, CONTROL HEADING, DETAIL (§14.9.16.4 GR4).
At TERMINATE: present all CONTROL FOOTINGs minor→major (the final break), then REPORT FOOTING, then PAGE FOOTING.
GROUP INDICATE: an indicated field prints only on the first detail after a control break or page advance.

### A.5 Runtime engine + integration (as-built)
The engine is `ReportWriterRuntime` (managed; deterministic; no `unsafe`, no pointers, no `stackalloc`). It owns
page/line control, control-break detection, accumulator management, line rendering (pad-to-column then field text),
and emission. INITIATE / GENERATE / TERMINATE are the verbs; output is written through the report's FILE using the
standard WRITE / ADVANCING path (FileManager). Report descriptors + current report state live in the execution context.
`USE BEFORE REPORTING data-name` declaratives mirror the USE-AFTER dispatch.

### A.6 Lowering (compiler side, summary)
RD → report descriptor; each report group → a render method called by the engine; SOURCE → load + convert + place;
VALUE → place literal; SUM/COUNT/AVERAGE/MIN/MAX → accumulator update; control-break → key-comparison + branch to
break handlers; page control → line-counter check + page transition. SUM/AVERAGE accumulators are decimal locals,
COUNT is integer. See the IR + CilEmitter detail in `docs/REPORT_WRITER_CONTROL_DESIGN.md` (IrReportRegisterControl /
IrReportRegisterDataField, and the **mandatory** CilEmitter dispatch case per new IrRuntimeCall — a missing case →
InvalidProgramException at Main).

### A.7 Exception handling (DESIGN — full EC model is Phase C)
`USE AFTER EXCEPTION ON REPORT` is triggered by page-overflow / invalid LINE-COLUMN / output-file errors;
`ON EXCEPTION` in SOURCE by invalid conversion / overflow; ExceptionState is populated with report group, line,
column, and message. This rides on the broader exception/EC subsystem (Phase C in the plan).

### A.8 Debugger view (DESIGN — debugger is Phase E)
A future debugger surfaces: current report name/group, current page/line, control fields (current vs prior),
accumulator values, the rendered-line preview, and ExceptionState; sequence points at each report group / field /
page eject. Debugger is design-only (Phase E).

### A.9 Edge-case behavior catalog (target semantics — verify vs spec)
- DETAIL with no CONTROL fields → printed sequentially, no break logic.
- DETAIL on first record → triggers PAGE HEADING then (FINAL/first) CONTROL HEADING.
- SUM of a non-numeric field → compile-time error.
- AVERAGE with zero count → zero.
- CONTROL break at end of file → all CONTROL FOOTING groups emitted (then REPORT FOOTING).
- SUMMARY with no DETAIL → printed with zero accumulators.
- LINE / COLUMN beyond page/line width → page advance (LINE) / truncation (COLUMN); VALUE literal too long → truncation.
- Nested / multiple reports → independent state each.
(The essays also asserted hard rules like "PAGE LIMIT minimum = 5 lines", "COLUMN < 1 illegal", "missing CONTROL field
= runtime error" — treat these as design intent; the authoritative behavior is whatever the NIST RW baselines + ISO
§13/§14 require, which the runtime follows.)
