# COBOL.NET — Report Writer (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL → idiomatic
> typed-native C# via Roslyn; no byte substrate). This doc CLOSES the SSOT's "designed only to the seam"
> scope flag for Report Writer (`docs/COBOLNET_DESIGN.md` §14 verb table / §15.5): the subsystem is now
> implemented. The locked invariants and cross-cutting consistency live in the SSOT; spec authority is
> `specs/ISO_COBOL.md` (ISO/IEC 1989:2023) — every behavior below carries its § citation.

## Summary

The Report Writer Control System (RWCS): the REPORT SECTION (ISO §13.6/§13.14/§13.15 + the §13.18 report
clauses) and the INITIATE / GENERATE / TERMINATE verbs (§14.9.21/§14.9.16/§14.9.46), with the per-report
LINE-COUNTER / PAGE-COUNTER registers (§8.4.3.15) and USE BEFORE REPORTING declaratives (§14.9.49 Format 2).
Validated by NIST RW101A–RW104A (byte-match) **plus a spec-pinned conformance net for the report-file CONTENT
the NIST goldens never compare** (`ReportWriterConformanceTests`) — load-bearing because the legacy oracle's
report-file content is demonstrably WRONG in two places (see §7); the spec, not the oracle, governs.

**Edition status:** RW is an optional module in COBOL-85 (the NIST RW suite runs `--std 85`) and an optional
language element in 2023 (A.4.11). Its 2002 status is NOT derivable from the 2023 spec text — flagged as a
`VERSION_CHANGE_REFERENCE.md` follow-up row; the grammar is NOT edition-gated today.

## 1. Architecture (ONE mechanism — compose-at-presentation)

```
DataBinder.Reports.cs            ReportWriterBinder.cs                ReportWriterEmitter.cs
RD → ReportModel                 INITIATE/GENERATE/TERMINATE →        engine field __RPT_n + per-line
  geometry (§13.18.39 GR3        BoundInitiate/Generate/Terminate;    compose methods; construction in
  defaults), CONTROL list,       LINE-/PAGE-COUNTER →                 __Activate beside the file
  groups → lines → fields        BoundReportCounterRef                registration; verbs → engine calls
                ↘                                ↘                                  ↘
                         Cobol.Net.Runtime/IO/ReportWriter.cs — CobolReport
                         (the per-report RWCS engine: page geometry, counters,
                          page-fit/advance, control breaks, SUM, group hooks)
```

- **Every report line is ONE generated compose method** (`Func<string>` over the program instance's typed
  fields), invoked by the engine **at presentation time, after LINE-COUNTER is set to the line's number**.
  This realizes §13.18.53.4 GR1/GR3 (SOURCE is an implicit MOVE "executed before the associated report line
  is printed") and §13.18.35.4 GR6 (LINE-COUNTER set FIRST, then the line printed) **by construction** — a
  `SOURCE IS LINE-COUNTER` item on any group prints that line's own number (the RW103A page-heading check).
- The legacy split composition into TWO mechanisms (runtime-registered byte FieldPlans for auto groups vs
  code-composed byte buffers for details) — a singular-pattern violation and the proven source of its two
  §13.18.53 content bugs. The greenfield has no registration kinds, no byte buffers, no storage offsets.
- **Printable items are SYNTHETIC `DataItem`s** (PicInfo + JUSTIFIED/BLANK WHEN ZERO flags, never added to
  the storage forest — report groups are not storage, §13.6). The emitter renders every field through the
  orchestrator's ONE MOVE conversion (`MoveEmitter.ConvertSource`), so a numeric SOURCE edits through the
  printable PICTURE exactly like `MOVE src TO item` (alignment, truncation, editing, BLANK WHEN ZERO) —
  §13.18.53.4 GR1 verbatim. Numeric printable items are `StoreAsImage`, so the conversion yields the
  printable CHARACTER image directly; their `NumProfile` statics are emitted by the RW emitter
  (`ReportWriterEmitter`) — the field emitter only walks the storage forest.
- **Physical output** goes through the report file's ordinary connector (`CobolFile.WriteAdvancing` — the
  print-control stream). The engine tracks `_physLine` (physical position) separately from LINE-COUNTER so
  a future NEXT GROUP (which moves LINE-COUNTER, §8.4.3.15.4 GR4) cannot corrupt positioning.

## 2. The engine (`CobolReport`) — spec-keyed behavior table

| Operation | Rules encoded (all cited in code) |
|---|---|
| `Initiate` | §14.9.21.4 GR1a–c (sums←0, LC←0, PC←1), GR2 (active re-INITIATE = EC-REPORT-ACTIVE seam, no effect), GR3 (file NOT opened — EC-REPORT-FILE-MODE seam), GR4 (→active) |
| `Generate(detail?)` | GR4 first-GENERATE sequence (RH once → PH → CHs major→minor → detail); GR5 subsequent (break: CFs minor→break with PRIOR control values per §13.18.16.4 GR4a, then CHs break→minor); GR2 summary (null detail); GR7 inactive = EC-REPORT-INACTIVE seam; SUM accumulation per §13.18.54.4 GR7c (after break processing) |
| page fit | §13.18.35.4 GR4b absolute (integer-1 > LC) / GR4c relative (trial = LC + Σ relative values ≤ the §13.18.57.4 GR8 lower limit: DE→LAST DETAIL, CH→LAST CH, CF→FOOTING); the chronologically FIRST body group since INITIATE is exempt (GR4); only body groups test (§13.18.57.3 SR15) |
| page advance | §14.9.16.4 GR6 in order: PF → physical advance (form feed) → CODE re-eval (staged) → PC+1 → LC←0 → PH |
| line placement | §13.18.35.4 GR5a (absolute → integer-1), GR5b1 RH (HEADING+n−1), GR5b2 PH (RH-on-page aware), **GR5b3 body (FIRST body group on page → FIRST DETAIL, relative value IGNORED; else LC+n)**, GR5b4 PF (FOOTING+n), GR5b5 RF (PF-on-page aware), GR7 subsequent lines, GR6 LC-before-compose, GR8 final LC = last line printed |
| `Terminate` | §14.9.46.4 GR1 (inactive seam), **GR2 (no GENERATE ⇒ NO groups print — only →inactive)**, GR3a–d (controls→prior, CFs minor→major, restore), §13.18.57.4 GR6f (final-page PF, "immediately followed by" the RF), GR3c (RF), GR6 (file NOT closed) |
| controls | §13.18.16.4 GR1 (operand order = hierarchy), GR2 (FINAL highest, never breaks mid-report), GR3 (first GENERATE saves priors; major→minor compare), GR4a (CF composes under restored prior values), GR5 (TERMINATE = most-major break). Break key = the item's CHARACTER IMAGE via generated get/set delegates (representation-faithful for every category; restore decodes via `CobolNum.StoreDisplay` for native numeric leaves) |
| SUM | §13.18.54.4 GR1 (counter scale from the entry's PICTURE), GR2 (reset where printed / RESET ON level), GR4 (the counter is the printable entry's source item — `BoundReportSumRef`), GR7c1/c2 (accumulate per GENERATE / UPON detail filter), GR9 (multi-addend) |
| GROUP INDICATE | §13.18.29 — indicated items print on the first presentation after INITIATE / page advance / control break, blanked otherwise (engine-side, post-compose) |
| USE BEFORE REPORTING | §14.9.49 Format 2 GR8/SR9 — the declarative section binds to the named group (`BoundDeclarative.ReportGroup`) and runs via the group's `BeforeReporting` hook (a `__RunUse` bounded dispatch) just before the group is produced |

**The GR4c trial-sum ambiguity (decided):** the 2023 wording "incremented by integer-2 for each *subsequent*
LINE clause" is ambiguous for the FIRST relative line's integer-2. The NIST goldens + the legacy resolve it
as **trial = LINE-COUNTER + Σ integer-2 over ALL relative lines** (RW103A overflows exactly at LC=25 with
LAST DETAIL 25 and one `PLUS 1` line); GR5b3 then ignores the first line's relative value on the new page
anyway. Encoded as Σ over all; the alternative reading prints one detail past LAST DETAIL and cascades
off-by-one through every later counter check.

## 3. Binding (`DataBinder.Reports.cs`)

- `BindReportSection` runs in `DataBinder.Bind` right after `BindFileSection`; `ResolveReports` runs
  post-build after `ResolveFiles` (the FILE STATUS capture-then-resolve pattern — ONE resolution point).
- **Geometry defaults** per §13.18.39.4 GR3 (HEADING→1; FIRST DETAIL→HEADING; LAST CH→LD else FOOTING else
  limit; LD→FOOTING else limit; FOOTING→LD else limit). No PAGE clause ⇒ unpaged (GR2a — one page of
  indefinite length; fit/advance machinery inert).
- **Line-building rule** (§13.15): walking a group's entries in declaration order, an entry with a LINE
  clause OPENS a new report line (**LINE is legal at ANY level** — RW101A puts `LINE PLUS 1` on an 03; a
  binder that reads LINE only at the 01 produces a lineless group and a never-moving LINE-COUNTER); an entry
  with a COLUMN clause appends a printable field to the CURRENT line. TYPE abbreviations per §13.18.57.3 SR9.
- **The FD side**: `FileModel.ReportNames` (the §13.18.46 REPORT clause, captured in `BindFileSection`);
  a report file is an FD with a non-empty list — legally record-less (§9.1.22). `FileModel.RecordContains`
  captures the fixed Format-1 RECORD CONTAINS for the line width; otherwise the width is the widest field
  extent (column + image width − 1) — the §13.18.39.4 GR5 page-width default 999 is a maximum, not a record
  length, and the legacy's hardcoded 132 was arbitrary.
- **Counters in the PD** (§8.4.3.15): `ReportWriterBinder.CounterExpr` intercepts LINE-/PAGE-COUNTER in `FieldOperand`/`RefExpr`
  ahead of name resolution (the LINAGE-COUNTER idiom); the OF/IN `cobolWord` is the report-name qualifier;
  unqualified resolves only against a sole report (SR2/§8.4.2.2). `ReferenceResolver.Resolve` early-returns
  for the counter tokens — LOAD-BEARING for the qualified form, where `cobolWord()` is the qualifier and
  would otherwise mis-resolve as a base data-name.
- **Receiving guard**: ALL receiving resolution (MOVE targets, arithmetic resultants ×3, SET targets) goes
  through ONE chokepoint, `ResolveReceiving` — a counter receiver is rejected at bind (LINE-COUNTER illegal
  per §8.4.3.15.3 SR3; PAGE-COUNTER legal-but-staged) instead of being silently dropped by
  `.OfType<Place>()` (the silent-miscompile hazard).

## 4. Emission (`CodeGen/Verbs/ReportWriterEmitter.cs`)

- Per report: `private CobolReport __RPT_n` + construction inside `__Activate`'s `if (!__filesRegistered)`
  block, **after** `EmitFileRegistration` — the registration order is load-bearing (§7 hazard 1). Report FDs
  with `Records.Count == 0` register in `EmitFileRegistration` with the report's line width (without this
  the OPEN falls into the keyed-organization else-branch and every report write silently no-ops).
- Per line: `private string __RPT_C_{r}_{g}_{l}()` — a space-filled `char[LineWidth]`
  (`CobolReport.NewLine`), each field placed at its COLUMN (`CobolReport.Place`) with the `ConvertSource`
  image. SOURCE counters/sums render through `NumericRenderer` (`BoundReportCounterRef` /
  `BoundReportSumRef` — one case each; both relation conditions and MOVE sources route through the renderer).
- Verbs: `__RPT_n.Initiate()/.Generate("DETAIL-NAME" | null)/.Terminate()`; multi-name statements unroll in
  written order (§14.9.21.4 GR5 / §14.9.46.4 GR4).
- Multi-unit: engine fields are per-instance; the engine's file name is the SAME emit-qualified
  `"PROG::FILE"` name `EmitFileRegistration` registers (the IC114A connector precedent).

## 5. The full §13/§14 RW surface — implemented vs staged LOUD

**Implemented:** PAGE LIMIT geometry + GR3 defaults; RH/PH/CH/DE/CF/PF/RF groups; absolute + relative LINE
(any level); COLUMN/PIC/SOURCE/VALUE/JUSTIFIED/BLANK WHEN ZERO/SIGN printable items; SOURCE
LINE-/PAGE-COUNTER; CONTROL/CONTROLS incl. FINAL (breaks, prior-value CF composition, TERMINATE final
break); SUM + UPON + RESET; GROUP INDICATE; summary `GENERATE report-name`; multi-name INITIATE/TERMINATE;
PD counter references incl. qualified; USE BEFORE REPORTING (Format 2 declaratives); unpaged reports.

**Staged LOUD at bind (`COBOLNET0899`, Edition.Error — legal-but-unimplemented, never silent):** NEXT GROUP
(§13.18.37, incl. the WITH RESET PAGE-COUNTER form); CODE (§13.18.12); LINE … NEXT PAGE / ON NEXT PAGE;
OCCURS in report groups (§13.18.38 repeating entries, multi-operand SOURCE §13.18.53 SR6); GLOBAL RD
(§13.18.27); multi-report FDs (`REPORTS ARE r1 r2`); subscripted/ref-modified SOURCE; SOURCE of another
report's counter; rolled SUM totals (§13.18.54.4 GR6 — a report-section addend); cross-report SUM
(`SUM x OF report`); non-DISPLAY printable items; PAGE-COUNTER as a receiving operand. PAGE `COLS`/width
and LAST CONTROL HEADING have no grammar surface (the GR3c default applies); **SUPPRESS (§14.9.45) has no
grammar rule** — it cannot parse (a frontend change, which this wave may not make: shared-frontend guard).
EC-REPORT-* checking is default-off (SSOT §18.16) — cited seam comments at every raise point.

## 6. Design authority (this doc + the cited GRs, not the legacy)

The implementation follows THIS doc + the cited GRs rather than a §-by-§ port of the legacy (whose
report-file content is wrong — §7). The SSOT (§14 verb table, §15.5) records RW's locked scope and its §0.5
deep-dive table points here.

## 7. Hazards & oracle holes (validated)

1. **The legacy report-file content is NOT a content reference.** Two proven bugs: RW102A's
   `PIC 9(3) SOURCE IS WS-COUNTER` (a `PIC 9(6)` holding 1) printed `000` — a raw left-justified byte copy
   truncated to 3 — where §13.18.53.4 GR1's implicit MOVE yields `001`; and `SOURCE IS LINE-COUNTER` printed
   blank. Only the CCVS print file is golden-compared; the conformance net pins the spec content.
2. **Registration order** (§4): report FDs must register with the connectors, before any engine write.
3. **GR6 ordering**: `PresentLine` is the single method that sets LINE-COUNTER before composing — do not
   per-group reorder.
4. **First-GENERATE exemption** (§13.18.35.4 GR4): no page-fit for the chronologically first body group —
   RW101A's first detail lands at line 1, never triggers an advance.
5. **Trial-sum reading** (§2): Σ over ALL relative lines; the other reading shifts every page boundary.
6. **`\f` and the print stream**: a page advance appends a form feed to the stream after the page's last
   record; read-back style tests must keep markers clear of it (`String.ReplaceLineEndings` treats FF as a
   line ending).

## 8. Verification

- NIST: RW101A, RW102A, RW103A, RW104A GREEN (byte-match, `--std 85 --nist`) — counters (INITIATE values,
  per-GENERATE LINE-COUNTER, page-advance PAGE-COUNTER/FIRST-DETAIL placement over 3-page runs).
- `tests/Cobol.Net.Tests.Conformance/ReportWriterConformanceTests.cs` (14 spec-pinned tests): INITIATE GR1;
  GR5b3 first-body placement; GR4c trial-sum overflow; the §13.18.53.4 GR1 content pins (the two legacy
  bugs); PH composes its own line (GR6); RH-once/PH-per-page (GR4a/GR6f); PF at advance + final-page PF→RF
  (GR6a/GR6f/GR3c); TERMINATE-without-GENERATE (GR2); control-break prior/new values (GR4a); SUM
  accumulate/reset/UPON (GR2/GR7); USE BEFORE REPORTING (GR8); LINE-COUNTER receiving rejection (SR3).
