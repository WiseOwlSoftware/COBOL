# COBOL.NET — Report Writer (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL → idiomatic
> typed-native C# via Roslyn; no byte substrate). This doc CLOSES the SSOT's "designed only to the seam"
> scope flag for Report Writer (`docs/COBOLNET_DESIGN.md` §14 verb table / §15.5): the subsystem is now
> implemented. The locked invariants and cross-cutting consistency live in the SSOT; spec authority is
> `specs/ISO_COBOL.md` (ISO/IEC 1989:2023) — every behavior below carries its § citation.

## Summary

The Report Writer Control System (RWCS): the REPORT SECTION (ISO §13.8/§13.14/§13.15 + the §13.18 report
clauses) and the INITIATE / GENERATE / TERMINATE / SUPPRESS verbs (§14.9.21/§14.9.16/§14.9.46/§14.9.45), with
the per-report LINE-COUNTER / PAGE-COUNTER registers (§8.4.3.15) and USE BEFORE REPORTING declaratives
(§14.9.49 Format 2).
Validated by NIST RW101A–RW104A (byte-match) **plus a spec-pinned conformance net for the report-file CONTENT
the NIST goldens never compare** (`ReportWriterConformanceTests`) — load-bearing because the legacy oracle's
report-file content is demonstrably WRONG in two places (see §7); the spec, not the oracle, governs.

**Edition status:** RW is an optional module in COBOL-85 (the NIST RW suite runs `--std 85`) and an optional
language element in 2023 (A.4.11). Its module-level 2002 status is NOT derivable from the 2023 spec text —
flagged as a `VERSION_CHANGE_REFERENCE.md` follow-up row. The '85 clause surface is not edition-gated; the
2002 RW additions ARE (the VersionConformancePass parse arm, 0900 below 2002): PRESENT WHEN (§13.18.41 F1),
VARYING (§13.18.64), the multiple/relative COLUMN forms + COL/COLS/COLUMNS/NUMBERS/ARE spellings (§13.18.14 F1
— the '85 form was exactly `COLUMN NUMBER IS integer-1`), and the multiple-LINE form + LINES/NUMBERS/ARE
spellings (§13.18.35 F1). Matrix rows `report-present-when-2002` / `report-varying-2002` /
`report-multi-column-2002` (+ `report-multi-line-2002` pending).

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
  the storage forest — report items are not accessed as ordinary storage, §13.8.6.2.3). Numeric printable items
  are `StoreAsImage`, so every rendering path below yields the printable CHARACTER image directly; their
  `NumProfile` statics are emitted by the RW emitter (`ReportWriterEmitter`) — the field emitter only walks the
  storage forest.
- ⛔ **TWO CLAUSES FILL A PRINTABLE ITEM AND THEY ARE NOT THE SAME RULE** (kb/Work PB506). A **SOURCE** operand
  renders through the orchestrator's ONE MOVE conversion (`MoveEmitter.ConvertSource`), so a numeric SOURCE
  edits through the printable PICTURE exactly like `MOVE src TO item` (alignment, truncation, editing,
  JUSTIFIED, BLANK WHEN ZERO) — §13.18.53.4 GR1 verbatim. A **VALUE** operand is an INITIALIZATION and renders
  through the ONE §13.18.63 VALUE recipe the working-storage lane uses
  (`ValueInitializer.InitializerFrom`, reached via `DataEmitter.ValueImageOf`), because §13.18.63.4 GR21
  imports GR7 — "aligned … except that initialization is not affected by a JUSTIFIED clause and no editing
  takes place" — and GR8 (BLANK WHEN ZERO has no effect for an alphanumeric or national literal), and
  §13.18.63.3 SR34 imports SR11 ("Editing characters in a picture character-string for an alphanumeric-edited
  or national-edited data item do not cause editing of the initial value"). Routing a VALUE through the MOVE
  applied exactly those three excluded transforms: `PIC XXBXX VALUE "AB CD"` printed `AB  C`,
  `PIC X(5) JUSTIFIED VALUE "AB"` printed `   AB`, `PIC ZZZ9 BLANK WHEN ZERO VALUE "0000"` printed spaces —
  each while the IDENTICAL working-storage entry was right. `ReportOperandListDriftTests` keeps the MOVE out of
  the VALUE lane.
- **Physical output** goes through the report file's ordinary connector (`CobolFile.WriteAdvancing` — the
  print-control stream). The engine tracks `_physLine` (physical position) separately from LINE-COUNTER so
  a future NEXT GROUP (which moves LINE-COUNTER, §8.4.3.15.4 GR4) cannot corrupt positioning.

## 2. The engine (`CobolReport`) — spec-keyed behavior table

| Operation | Rules encoded (all cited in code) |
|---|---|
| `Initiate` | §14.9.21.4 GR1a–c (sums←0, LC←0, PC←1), GR2 (active re-INITIATE **raises EC-REPORT-ACTIVE**, no other effect), GR3 (the file is NOT opened here — it shall ALREADY be open OUTPUT/EXTEND, else **EC-REPORT-FILE-MODE** and no action is taken on the report; the detection half of §14.9.27.4 GR7), GR4 (→active). §14.9.49.4 GR10 outranks all three — see the RANGE row |
| `Generate(detail?)` | GR4 first-GENERATE sequence (RH once → PH → CHs major→minor → detail); GR5 subsequent (break: CFs minor→break with PRIOR control values per §13.18.16.4 GR4a, then CHs break→minor); GR2 summary (null detail); GR7 inactive **raises EC-REPORT-INACTIVE** and does nothing; SUM accumulation per §13.18.54.4 GR7c (after break processing) |
| page fit | §13.18.35.4 GR4b absolute (integer-1 > LC) / GR4c relative (trial = LC + Σ relative values ≤ the §13.18.57.4 GR8 lower limit: DE→LAST DETAIL, CH→LAST CH, CF→FOOTING); the chronologically FIRST body group since INITIATE is exempt (GR4); only body groups test (§13.18.57.3 SR15) |
| page advance | §14.9.16.4 GR6 in order: PF → physical advance (form feed) → CODE re-eval (staged) → PC+1 → LC←0 → PH |
| line placement | §13.18.35.4 GR5a (absolute → integer-1), GR5b1 RH (HEADING+n−1), GR5b2 PH (RH-on-page aware), **GR5b3 body (FIRST body group on page → FIRST DETAIL, relative value IGNORED; else LC+n)**, GR5b4 PF (FOOTING+n), GR5b5 RF (PF-on-page aware), GR7 subsequent lines, GR6 LC-before-compose, GR8 final LC = last line printed |
| `Terminate` | §14.9.46.4 GR1 (inactive → **EC-REPORT-INACTIVE**, the statement is unsuccessful), **GR2 (no GENERATE ⇒ NO groups print — only →inactive)**, GR3a–d (controls→prior, CFs minor→major, restore), §13.18.57.4 GR6f (final-page PF, "immediately followed by" the RF), GR3c (RF), GR6 (file NOT closed) |
| controls | §13.18.16.4 GR1 (operand order = hierarchy), GR2 (FINAL highest, never breaks mid-report), GR3 (first GENERATE saves priors; major→minor compare), GR4a (CF composes under restored prior values), GR5 (TERMINATE = most-major break). Break key = the item's CHARACTER IMAGE via generated get/set delegates (representation-faithful for every category; restore decodes via `CobolNum.StoreDisplay` for native numeric leaves) |
| SUM | §13.18.54.4 GR1 (ONE counter per ENTRY, scale from the entry's PICTURE), GR2 (reset where printed / RESET ON level), GR4 (the counter is the printable entry's source item — `BoundReportSumRef`), GR7c1/c2 (accumulate per GENERATE / per the term's OWN UPON filter), GR9 (multi-addend). The counter carries a LIST of `SumTerm`s — one per `SUM … [UPON …]` group, because §13.18.54.3 SR1 lets the SUM keyword "appear more than once" in one clause and GR7c2 attaches each UPON phrase to ITS group |
| GROUP INDICATE | §13.18.28 — indicated items print on the first presentation after INITIATE / page advance / control break, blanked otherwise (engine-side, post-compose); one blank span per ABSOLUTE COLUMN operand |
| USE BEFORE REPORTING | §14.9.49 Format 2 GR8/SR9 — the declarative section binds to the named group (`BoundDeclarative.ReportGroup`) and runs via the group's `BeforeReporting` hook (a `__RunUse` bounded dispatch) just before the group is produced |
| the REPORT-GROUP REFERENCE | ⛔ **ONE funnel, `Binding/ReportGroupResolution.cs`** — THREE sites name a report group by name (`GENERATE data-name-1` §14.9.16.3 SR1, `USE BEFORE REPORTING identifier-1` §14.9.49.3 SR9, and the SUM clause's `UPON data-name-2` §13.18.54.3 SR7 — the DATA-division one, resolved in `ResolveReports`). The two statement sites share the parse rule `reportGroupReference` (`cobolWord ((IN|OF) reportName)?` — §8.4.2.2.2 Format 1's file-report-qualifier, the only qualifier a level-01 group has; §8.4.2.2.3 SR3 makes IN ≡ OF) and the one resolver. It collects EVERY candidate and reports **COBOLNET1920** when more than one survives (§8.4.2.2.1 / §8.4.2.2.3 SR1). Before kb/Work PB365 both sites returned on the FIRST match, so the same 01-level name in two RDs bound to whichever report was written first, silently — and GENERATE's qualifier did not parse at all. `ReportGroupResolutionDriftTests` keeps the search out of every other file |
| the BEFORE REPORTING RANGE | §14.9.49.4 GR10 — a GENERATE, INITIATE or TERMINATE executed **within the range of** a USE BEFORE REPORTING declarative sets EC-FLOW-REPORT, is **unsuccessful**, and leaves **the state of the report unchanged**. `RunBeforeReporting` — the ONE funnel every presentation path uses — brackets `group.BeforeReporting` with `RunUnit.ReportFlow.Enter()/Exit()` (a `finally`, so a fatal EC out of the declarative cannot latch the range), and the three verbs consult `InBeforeReporting` first. ⛔ **The range is the RUN UNIT's, not one report's and not one program's**: GR10 attaches no element qualifier where §14.9.18.4 GR6 attaches one explicitly for EC-FLOW-GLOBAL-GOBACK ("… in the same program as the GOBACK statement"), and the standard's other flow rules read the same way (§14.9.32.4 GR1's "within the range of an input procedure"). A per-`CobolReport` flag would pass `2023/pb326_flow_report_cross_report` while being wrong. A DEPTH, not a bool — two different groups' declaratives nest |
| SUPPRESS | §14.9.45 — `SUPPRESS PRINTING` sets the engine's one-shot `_suppressCurrent` flag (`__RPT_n.SuppressPrinting()`); `RunBeforeReporting` consumes it on the presentation whose GR8 hook set it (GR2 — current instance only). The target group is the lexically-enclosing USE BEFORE REPORTING group, resolved at bind from `BindCursor` ∈ the declarative's pc range (GR1); a SUPPRESS outside such a procedure is COBOLNET1581 (SR1). GR3 a–d inhibit printing / page advance / NEXT GROUP / LINE-COUNTER, but NOT sum accumulation (GR7, already done in Generate) nor the end-of-group sum reset (GR2) — so a suppressed control footing's totals stay correct (only PRESENT WHEN / ODO absence skips the reset, GR10). Body groups run `EndOfGroupSumReset` on the suppressed path; heading/footing groups (no reset) return after the hook |
| PRESENT WHEN | §13.18.41 Format 1 — `EvaluatePresent` evaluates every line's condition chain ONCE per presentation, BEFORE any LINE processing (GR2) and AFTER the `BeforeReporting` hook. ⛔ **That order is a DETERMINATION, not a reading** (kb/Work PB367b): §14.9.49.4 GR9 d) performs the declarative "Before the processing of any LINE clauses defined for the report group" and GR2 evaluates condition-1 "before the processing of any LINE clauses for the report group" — the SAME boundary, with no rule ordering them, and both precede the page fit test (GR9 c); §13.18.41.4 GR3 d). It is settled this way because the reverse makes a declarative's execution depend on data the declarative exists to set: condition-1 is any condition (§13.18.41.2), typically over the very items §14.9.49.4 GR8 lets the procedure prepare ("just before the named report group is produced"), and a level-01 PRESENT WHEN would otherwise silently suppress the procedure that would have made the group present. Witnessed by `ReportWriterConformanceTests.UseBeforeReporting_PresentWhen_DeclarativeRunsBeforeTheConditionIsEvaluated`; an absent line is SKIPPED so the next relative line re-anchors on LINE-COUNTER (GR2b — the line collapse); the fit-test form, the trial sum, and the GR5 first-line placement key on the first PRESENT line (§13.18.35.4 GR4/GR5; absent relative lines excluded from the trial, §13.18.41.4 GR3d); ALL lines absent ⇒ return-before-flags, as though the whole description were omitted (GR2b — no counters, no fit, no sum reset); an absent SUM entry is neither printed (the compose guard) nor reset (`EndOfGroupSumReset` consults `SumEntry.Present` — GR3g/§13.18.54.4 GR10); absent printable items place nothing and never advance the horizontal counter (GR3e/GR3f) |
| VARYING | §13.18.64 — per-repetition counters over the multiple-COLUMN repetition vehicle (SR1): compose-local `long`s, first occurrence ← FROM (default 1, GR3a — re-evaluated per presentation), += BY per repetition (default 1, GR3b); each value persists through its occurrence (GR4 — `SOURCE IS counter` renders it, GR4 NOTE); a noninteger FROM/BY truncates via `Rescale` (the GR5 EC-REPORT-VARYING seam, checking default-off §18.16) |
| multiple/relative COLUMN | §13.18.14 F1 — a multiple COLUMN clause defines one printable item per operand (GR12); relative (PLUS) operands place at `horizontal counter + integer-2` (GR8) with the counter starting at 0 (GR7) and set to each placed item's rightmost column (GR9) |
| the VALUE / SOURCE OPERAND LIST | ⛔ **ONE list, ONE cycling reader, ONE syntax screen** (kb/Work PB506). ISO writes the same two rules twice, once per clause: §13.18.63.3 SR35 = §13.18.53.3 SR6 (a multi-operand clause requires a repeating entry — §13.15.4 GR3 — and an operand count equal to its repetitions, or that number multiplied by the repetitions of successive higher repeating entries) and §13.18.63.4 GR23 = §13.18.53.4 GR4 ("successive operands are assigned to successive repeating printable items, horizontally and then vertically … If no further operands remain, assignment begins again from the first operand"). So `ReportFieldModel.Sources` is a LIST (a single-operand clause is a one-element list), `ReportFieldModel.SourceAt(rep)` is the only per-repetition reader — indexed by the repetition ORDINAL, which is what makes GR23's last sentence true by construction ("If any of the printable items are suppressed as a result of a PRESENT WHEN clause … operands are nevertheless assigned to them"), and `DataBinder.Reports.ScreenRepeatingOperandCount` is the ONE screen, fed by both clauses with its own diagnostic each (**COBOLNET2012** VALUE / **COBOLNET2013** SOURCE) and the repetition chain read off the entry scope stack. Before PB506 `FieldValueSource` held ONE glued string (`ExtractValue`'s `GetText()` over the whole list, so `VALUE "XX" "YY"` reached the emitter as `"XX""YY"` and printed `XX"YY`) and the SOURCE clause had no multi-operand grammar surface at all. GR23's wrap-around sentence has no reachable COBOL source while the higher-level repetition vehicles stage loud, so `ReportOperandListDriftTests` asserts it on the model |

**The four statement-precondition conditions, and what `>>TURN` does and does not gate.** EC-FLOW-REPORT
(§14.9.49.4 GR10), EC-REPORT-ACTIVE (§14.9.21.4 GR2), EC-REPORT-FILE-MODE (§14.9.21.4 GR3) and
EC-REPORT-INACTIVE (§14.9.16.4 GR7 / §14.9.46.4 GR1) are Table 13 **Fatal**, and each rule states its LENIENT
outcome outright — "no other effect", "no action is taken on the report", "the execution of the statement is
unsuccessful", "the state of the report is unchanged". So the engine's **return is unconditional** and only the
**raise** is gated by checking (§14.6.13.1.1), exactly as for EC-FLOW-SEARCH / EC-BOUND-TABLE-LIMIT. The raise
channel is the ordinary one: `EcBinder` binds a PRECISE `BoundEcChecked` wrapper on `BoundInitiate` /
`BoundGenerate` / `BoundTerminate`, `EcEmitter.FatalAmbientGates` sets the `ExceptionState.…Checking` flag
around the statement, and `CobolReport` calls the matching `ExceptionState.…Error` helper — which raises a
`CobolFatalException` the statement guard catches for USE-F3 dispatch / RESUME. All four were catalogue rows
with no raise site anywhere in `src` before kb/Work PB326.

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
- **A REPEATING ENTRY IS A SUBTREE REPLAY, AND THAT IS THE ONLY REPETITION MECHANISM** (§13.18.38 format 3 —
  `OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]`; kb/Work PB565).
  `ReportOccursOf` reads the clause and enforces its syntax rules (one bundled code, `COBOLNET2021`: SR1a no
  OCCURS on an 01 entry · SR10 nesting only without DEPENDING · SR16 bounds · SR17 integer data-name-1 · SR24
  TO⊥DEPENDING together · SR25 STEP required over an absolute COLUMN · SR26 integer-3 ≥ the repeated item's
  span · SR27 a DEPENDING entry is followed only by its subordinates · and the DYNAMIC/KEY/INDEXED phrases,
  which belong to formats 1, 2 and 4). `BindReportEntries` then binds the entry AND every entry subordinate
  to it once per repetition. **Replaying the subtree is what makes GR11 free** — "any PICTURE, USAGE, SIGN,
  VALUE, JUSTIFIED, BLANK WHEN ZERO, or GROUP INDICATE clauses have the same effect on each repetition as
  they would on a single data item without the OCCURS clause" is satisfied by running the whole clause binder
  again, not by teaching each clause about repetition; §13.18.63.4 GR21's import of GR9 (a VALUE reaches
  every occurrence, both the *contains* and the *subordinate to* leg) rides it unchanged.
  - **Placement** (§13.18.38.4 GR12) is `ReportColumnKindModel`: the displacement Σ ordinal × integer-3 is
    ADDITIVE over the enclosing repeating entries, so an `Absolute` operand simply moves by it. A relative
    operand has no compile-time column, so its first repetition is an `AnchorSeed` — it places as GR8 says
    and remembers the column in a compose-local `__raN` — and the later ones are `AnchorStep`, placing at
    anchor + displacement. The anchor and not the horizontal counter is the datum because GR12 measures from
    the preceding occurrence's LEFTMOST while §13.18.14.4 GR9's counter holds its RIGHTMOST. With no STEP
    phrase nothing is displaced — GR12's closing sentence gives the interval to the relative COLUMN numbers,
    which the replayed operand reproduces against the counter.
  - **DEPENDING** (GR13) is a presence test composed into the SAME guard as the PRESENT WHEN chain, so the
    two suppressors §13.18.63.4 GR22 names cannot drift: repetition *n* appears iff
    `n < (data-name-1 ∈ [integer-1, integer-2−1] ? data-name-1 : integer-2)`. Every repetition is BOUND
    either way, which is GR23's "VALUE operands are nevertheless assigned to them, even though they are not
    printed".
  - **VARYING over a replay**: `ReportFieldModel.RepetitionOrdinal` counts placements PER ENTRY, so the
    §13.18.64.4 GR3 counter is emitted as the closed form `FROM + n × BY` rather than an accumulator — a
    replayed entry becomes one field per repetition and a field-local accumulator could not span them. The
    forms are equal, not approximate: GR3 adds arithmetic-expression-2 itself and both operands truncate to
    scale 0 once.
  - **RESIDUE, named**: VERTICAL repetition — an OCCURS on an entry that contains, or has subordinate to it,
    a LINE clause (GR10c/GR10d, GR12c/GR12d) — still stages LOUD on `COBOLNET0899 report-occurs-in-group`,
    beside its sibling the multiple LINE clause. `ReportRepeatingEntryDriftTests` holds that diagnostic to
    naming the AXIS, so it can never be re-broadened over the live horizontal one.
- **A CONTROL-clause OPERAND is a written reference, and there is ONE of it** (`ReportControlRef` — name +
  IN/OF qualifiers + the reference modification, captured by the one helper `ControlOperandRef`). THREE clauses
  write such an operand and all three permit the ref-mod with the same integer-literal restriction: the CONTROL
  clause (§13.18.16.3 SR4), a TYPE CH/CF operand (§13.18.57.3 SR10) and `SUM … RESET ON` (§13.18.54.3 SR8). SR10
  and SR8 then ask the SAME question — "the same as one of the operands of the CONTROL clause" — which is
  `ControlLevelOf`, one comparison over the whole written reference. It has to be the written reference and not
  the resolved item, because §13.18.16.3 SR6's second sentence lets two operands "refer to the same physical data
  item or to overlapping data items"; `CONTROLS ARE CX(1:3) CX(4:3)` is the legal shape that proves it.
  A ref-modded operand is emitted through `ReferenceResolver.ResolveItemRefMod` — the SAME ref-mod view builder
  the procedure division uses (`RefModView`), reached by item instead of by parse context — so §13.18.16.4 GR3's
  prior control is the §8.4.3.3.4 GR5 unique data item, the SLICE. §13.18.16.3's operand rules are all screened
  in one place: SR2 (asked only after resolution fails — a name also in ordinary storage resolves there), SR3,
  SR4, SR5, SR6, SR7 and the subscript that SR3 + §8.4.2.3.3 SR2 leave no legal spelling for; §8.4.3.3.3 SR1 on a
  ref-modded operand reuses `RefModExclusion`, the one SR1 reader. (kb/Work PB205 — before it, `KeyReference`
  kept only the qualification suffix and TYPE/RESET kept only the base word, so `CX(1:3)` and `CX(4:3)` were one
  operand and every report broke on the whole item.)
- **A SUM clause OPERAND is a written reference too, and its VALUE is bound in the PROCEDURE phase**
  (`ReportSumAddend` — the `dataReference` parse context + base name + qualifiers, captured by the one helper
  `SumAddendRef`; kb/Work PB482). §13.18.54.3 SR1 makes *identifier-1* an addend and §8.4.3.1.2 Format 2
  makes an identifier a *qualified-data-name-with-subscripts*, so the SUBSCRIPT is part of the operand — and a
  subscript may be an integer literal, an index-name or an arithmetic expression (§8.4.2.3), none of which has
  a value in the data division. The addend is therefore bound in `ReportWriterBinder.BindReportGroupClauses`
  through the ONE `ExpressionBinder.BindExpr` (the `PRESENT WHEN` / `VARYING` precedent) and rendered by the ONE
  `NumericRenderer`; `ReferenceResolver` does the subscript arithmetic exactly as it does for a procedure-division
  reference. The arm choice is made once, in `ResolveSumAddend`: SR4's *data-name-1* (a report-section item —
  a rolled total, GR6) and SR4 g)'s cross-report form stage loud, and SR5's *identifier-1* is screened for BOTH
  halves of its sentence — resolution outside the report section AND the CATEGORY (**COBOLNET2045**), which
  also refuses every reference-modified spelling, since §8.4.3.3.4 GR6 c) makes a ref-mod's unique data item
  alphanumeric and SR5 requires a numeric one. `UPON data-name-2` (SR7) is captured by `UponDetailRef` and
  resolved through the report-group funnel — it "shall be the name of a detail" and "may be qualified only by a
  report-name" (**COBOLNET2046**); a detail of ANOTHER report stages loud, because GR7 c) 2) accumulates on a
  GENERATE run against that report's engine. **ONE counter per ENTRY** (GR1) however many times the SUM keyword
  appears (SR1): the entry binder collects every `reportSumClause` and each becomes a `ReportSumTerm` with its
  OWN UPON list, emitted as one `AddSumTerm` call. (Before PB482 the addend went through `KeyReference` — the
  FILE STATUS key helper — so `SUM WS-CELL(2)` compiled and ABORTED at the first GENERATE, `SUM WS-TXT(1:2)`
  silently summed the whole item, `SUM WS-TXT` over a `PIC X(6)` summed its digits, `UPON <a control footing>`
  and `UPON <an undeclared word>` were accepted and totalled nothing, and a second `SUM … UPON …` group
  overwrote the first. `SUM OF` — the format's optional word, §8.3.2.4.3 — was a parse error.)
  ⚠ The SOURCE clause's operand (§13.18.53) is the SAME mechanism's other arm and is NOT converted: a
  subscripted or reference-modified SOURCE still stages COBOLNET0899. `ReportSumOperandCaptureDriftTests`
  carries that as its one adjudicated `KeyReference` caller, so the residue is visible rather than remembered.
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
- **PRESENT WHEN chains** (§13.18.41 F1): conditions accumulate down a level-number stack while the flat entry
  list walks — §13.18.41.4 GR2b makes an absent ancestor absent every subordinate "irrespective of any PRESENT
  WHEN clauses they may also contain", so presence = the AND of INDEPENDENT chain conditions. A LINE carries
  the chain 01→line-entry; a printable field the slice strictly BELOW its line entry (the line's own chain
  already gates the whole line); a SUM entry the FULL chain (the GR3g print/reset suppression). Conditions are
  captured as parse contexts at data bind and bound in the procedure phase through the ONE `ConditionBinder`
  (`ReportWriterBinder.BindReportGroupClauses`, memoized per distinct context, called at the top of
  `StatementBinder.Bind`); VARYING FROM/BY bind the same way through the ONE expression binder.
- **The §13.15.3/§13.18.64.3 SR family = COBOLNET1559** (`report-group-clause-rule`, one bundled code): SR16 —
  condition-1 shall not reference LINE-/PAGE-COUNTER or a report-section data item (token scan in
  `ResolveReports` over report-section-EXCLUSIVE names; a name also in ordinary storage resolves there and is
  exempt); SR17 — GROUP INDICATE ⊥ PRESENT WHEN in one entry; VARYING SR1 — the entry needs OCCURS / multiple
  LINE / multiple COLUMN (OCCURS and multiple COLUMN are LIVE vehicles; multiple LINE is still 0899-staged);
  SR2 — the counter shall not be defined elsewhere (`ByName` probe); SR3 — the counter shall not appear in its
  own FROM. `SOURCE IS counter` (same entry, unqualified) rebinds to `FieldVaryingSource` (§13.18.64.4 GR4 NOTE).

## 4. Emission (`CodeGen/Verbs/ReportWriterEmitter.cs`)

- Per report: `private CobolReport __RPT_n` + construction inside `__Activate`'s `if (!__filesRegistered)`
  block, **after** `EmitFileRegistration` — the registration order is load-bearing (§7 hazard 1). Report FDs
  with `Records.Count == 0` register in `EmitFileRegistration` with the report's line width (without this
  the OPEN falls into the keyed-organization else-branch and every report write silently no-ops).
- Per line: `private string __RPT_C_{r}_{g}_{l}()` — a space-filled `char[LineWidth]`
  (`CobolReport.NewLine`), each field placed at its COLUMN (`CobolReport.Place`) with the image its own clause
  gives it (`ConvertSource` for a SOURCE operand, the VALUE recipe for a VALUE operand — see §3), taken from
  `ReportFieldModel.SourceAt(rep)` so each repetition of a multiple COLUMN entry gets its own operand. SOURCE counters/sums/VARYING counters render through `NumericRenderer` (`BoundReportCounterRef` /
  `BoundReportSumRef` / `BoundReportVaryingRef` — one case each; both relation conditions and MOVE sources
  route through the renderer).
- Compose-side 2002 decoration (`EmitFieldPlacements`; the plain '85 shape — one absolute operand,
  unconditional, no VARYING, all-absolute line — keeps its exact single-statement emission, the
  characterization-pinned text): a field's PRESENT WHEN chain wraps its placements in `if (…)` (the
  `ConditionRenderer` AND — §13.18.41.4 GR2b/GR3f: an absent item places nothing and never advances the
  horizontal counter), joined by one OCCURS … DEPENDING presence test per enclosing repeating entry (§13.18.38.4
  GR13 / §13.18.63.4 GR22 — §3's replay); a multiple COLUMN entry unrolls one `Place` per operand, each with
  its VARYING counter as the closed form `__rv{uid}_{k} + ordinal × __rv{uid}_{k}b` (§13.18.64.4 GR3; FROM/BY
  align to scale 0 via `Rescale` truncation — the GR5 EC-REPORT-VARYING seam); `int __hc` (emitted only when a
  relative operand exists) realizes the §13.18.14.4 GR7/GR8/GR9 horizontal counter, and `int __raN` the
  §13.18.38.4 GR12 step anchors of this line's repeating entries.
- Construction decoration: a conditioned line appends `, () => chain` to its `ReportGroupLine` (the engine
  evaluates it once per presentation, GR2); a conditioned SUM entry appends its chain to `AddSum` (the GR3g
  reset suppression). Both parameters are optional — unconditioned emission is byte-identical.
- Verbs: `__RPT_n.Initiate()/.Generate("DETAIL-NAME" | null)/.Terminate()`; multi-name statements unroll in
  written order (§14.9.21.4 GR5 / §14.9.46.4 GR4).
- Multi-unit: engine fields are per-instance; the engine's file name is the SAME emit-qualified
  `"PROG::FILE"` name `EmitFileRegistration` registers (the IC114A connector precedent).

## 5. The full §13/§14 RW surface — implemented vs staged LOUD

**Implemented:** PAGE LIMIT geometry + GR3 defaults; RH/PH/CH/DE/CF/PF/RF groups; absolute + relative LINE
(any level); COLUMN/PIC/SOURCE/VALUE/JUSTIFIED/BLANK WHEN ZERO/SIGN printable items, **with the multi-operand
VALUE and SOURCE clauses and the SOURCES/ARE spellings** (§13.18.63.2 format 4 / §13.18.53.2, edition-gated 2002
for the SOURCE forms — `report-multi-source-2002`); SOURCE
LINE-/PAGE-COUNTER; CONTROL/CONTROLS incl. FINAL (breaks, prior-value CF composition, TERMINATE final
break) **and REFERENCE-MODIFIED control operands** (§13.18.16.3 SR4 — the break is sensed on the slice, and the
TYPE CH/CF and SUM RESET ON operands that name the level carry the same ref-mod, §13.18.57.3 SR10 /
§13.18.54.3 SR8); **SUM with SUBSCRIPTED addends** (§13.18.54.3 SR5's identifier-1 is §8.4.3.1.2 Format 2's qualified-data-name-with-subscripts — a literal, index-name or expression subscript, bound in the procedure phase) + UPON (SR7-screened against the report-group funnel) + RESET, the repeated `SUM … UPON …` group into the ONE counter of the entry (SR1 / GR1 / GR7c2) and the `SUM OF` optional word; GROUP INDICATE; summary `GENERATE report-name`; multi-name INITIATE/TERMINATE;
**the four RWCS statement-precondition exception conditions** (EC-FLOW-REPORT §14.9.49.4 GR10,
EC-REPORT-ACTIVE §14.9.21.4 GR2, EC-REPORT-FILE-MODE §14.9.21.4 GR3 / §14.9.27.4 GR7, EC-REPORT-INACTIVE
§14.9.16.4 GR7 and §14.9.46.4 GR1 — the unsuccessful/state-unchanged half unconditional, the raise `>>TURN`-gated);
**SUPPRESS PRINTING** (§14.9.45 — inhibit the current instance's printing/advance/NEXT GROUP/LINE-COUNTER,
NOT the sum accumulation or end-of-group reset; SR1/GR1 bind-resolve the enclosing USE BEFORE REPORTING group,
COBOLNET1581 on a misplaced SUPPRESS); PD counter references incl. qualified; USE BEFORE REPORTING (Format 2
declaratives); unpaged reports;
**PRESENT WHEN Format 1** (§13.18.41 — any entry level, chain semantics per GR2b, the GR3a–g LINE/COLUMN/SUM
interactions, edition-gated 2002); **VARYING** (§13.18.64 — over the multiple-COLUMN repetition vehicle,
counter-as-SOURCE, per-presentation FROM); **multiple + relative (PLUS) COLUMN** (§13.18.14 F1 incl. the
COL/COLS/COLUMNS/NUMBERS/ARE spellings and the GR7–GR9 horizontal counter).

**Staged LOUD at bind (`COBOLNET0899`, Edition.Error — legal-but-unimplemented, never silent):** NEXT GROUP
(§13.18.37, incl. the WITH RESET PAGE-COUNTER form); CODE (§13.18.12); LINE … NEXT PAGE / ON NEXT PAGE;
**VERTICAL** repetition of a report group entry (§13.18.38.4 GR10c/GR10d — an OCCURS over a LINE clause;
HORIZONTAL repetition is LIVE, see §3; the multi-operand VALUE and SOURCE clauses themselves RIDE over the
multiple-COLUMN vehicle, kb/Work PB506); **multiple LINE
(§13.18.35.3 SR10 — GR9-equivalent to LINE + a simple OCCURS, staged with the OCCURS repetition family;
`report-multiple-line`)**; **a VARYING counter inside a FROM/BY expression (the §13.18.64.3 SR3-legal BY
self-reference; `report-varying-counter-in-expression`)**; **FUNCTION inside a PRESENT WHEN condition
(`report-condition-function` — the UDF activation-hoist is statement-context machinery)**; **GROUP INDICATE on
an entry with a relative COLUMN operand (`report-indicate-relative-column` — the engine's blank spans need
static columns)**; GLOBAL RD (§13.18.27); multi-report FDs (`REPORTS ARE r1 r2`); subscripted/ref-modified
SOURCE; SOURCE of another report's counter; rolled SUM totals (§13.18.54.3 SR4 / §13.18.54.4 GR6 — a
report-section addend); cross-report SUM (a SUM addend qualified by a report-name, SR4 g); a cross-report
`UPON` detail (GR7 c 2); an arithmetic-expression-1 SUM addend (SR1/SR6 — no grammar surface, see §6);
non-DISPLAY printable items; PAGE-COUNTER as a receiving operand. PAGE
`COLS`/width, LAST CONTROL HEADING (the GR3c default applies), and **the COLUMN LEFT/CENTER/RIGHT alignment
phrase (§13.18.14 F1 — the SR9 LEFT default is what the grammar parses)** have no grammar surface. The §13.18.14.3 SR4/SR5 IS/ARE-spelling pairings and the
SR7/SR8/SR10b operand-order-vs-PRESENT-WHEN arrangement rules are not enforced (over-acceptance; the runtime
overlap seams are EC-REPORT-COLUMN-OVERLAP/-LINE-OVERLAP, default-off). EC-REPORT-* checking is default-off
(SSOT §18.16). The CONTENT/GEOMETRY conditions are still seams with no raise site —
EC-REPORT-COLUMN-OVERLAP (§13.18.14.4 GR4), EC-REPORT-PAGE-WIDTH (§13.18.14.4 GR5), EC-REPORT-PAGE-LIMIT
(§13.18.35.4 GR2), EC-REPORT-LINE-OVERLAP (§13.18.35.4 GR3), EC-REPORT-SUM-SIZE (§13.18.54.4 GR3),
EC-REPORT-VARYING (§13.18.64.4 GR5) — each with a cited seam comment at the
point that would raise it; they need per-line/per-item state the engine does not yet carry, which is a different
mechanism from the four statement preconditions PB326 closed.

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
- `tests/Cobol.Net.Tests.Conformance/ReportWriterConformanceTests.cs` (15 spec-pinned tests): INITIATE GR1;
  GR5b3 first-body placement; GR4c trial-sum overflow; the §13.18.53.4 GR1 content pins (the two legacy
  bugs); PH composes its own line (GR6); RH-once/PH-per-page (GR4a/GR6f); PF at advance + final-page PF→RF
  (GR6a/GR6f/GR3c); TERMINATE-without-GENERATE (GR2); control-break prior/new values (GR4a); SUM
  accumulate/reset/UPON (GR2/GR7); USE BEFORE REPORTING (GR8); LINE-COUNTER receiving rejection (SR3);
  the §13.18.41.4 GR3g absent-SUM neither-prints-nor-resets pin.
- **⛔ EVERY corpus golden and unit fact below that observes report CONTENT lives at `--std 2023`, and the reason
  is not the Report Writer** (an 85 subsystem): the only way to read a report file back is a second SELECT with
  `ORGANIZATION LINE SEQUENTIAL`, a COBOL-2023 introduction (§12.4.5.10.3 GR2; kb/Work PB688), and a COBOL.NET
  report file is CRLF-delimited text whatever its own ORGANIZATION, so a record-sequential read-back of it is
  misaligned. `ReportWriterConformanceTests` compiles whole at 2023 for that reason, and the nine 2002 goldens
  moved to `tests/conformance/2023/`. The ONE golden that stays at 85 —
  `85/pb326_flow_report_unconditional`, whose subject IS the oldest edition — dropped its read-back instead:
  §14.9.49.4 GR8/GR9d run the USE BEFORE REPORTING declarative once per PRESENTATION, so its `WS-N` already
  counts the detail lines the read-back was re-counting.
- Corpus goldens for the four statement-precondition conditions (kb/Work PB326), all byte-exact and verified by
  RUNNING: `85/pb326_flow_report_unconditional` (the §14.9.49.4 GR10 *unsuccessful / state unchanged* half at the
  edition that has no `>>TURN` at all — the nested GENERATE produces nothing); `2023/pb326_ec_flow_report` (its
  checked twin — the F3 declarative sees EC-FLOW-REPORT and RESUMEs); `2023/pb326_flow_report_cross_report` (the
  range is the RUN UNIT's — a GENERATE of a SECOND report from inside report R-A's declarative is refused, and
  the same GENERATE succeeds once MAIN has left the range); `2023/pb326_ec_report_file_mode` (three arms — not
  open, open INPUT, and the two modes §14.9.21.4 GR3 permits — with the follow-on EC-REPORT-INACTIVE proving that
  GR3's "no action is taken" left the report inactive); `2023/pb326_ec_report_active_inactive` (GR2's "no other
  effect" witnessed by a SUM counter that keeps its total across a refused second INITIATE).
- Corpus golden `tests/conformance/2023/rw_present_when.cob` (+`.out`, byte-exact, verified by RUNNING): the
  PRESENT WHEN line collapse across a present and an absent presentation (the relative TAIL line re-anchors on
  LINE-COUNTER, §13.18.41.4 GR2b), field-level suppression within a present line, and `COLUMNS ARE 12 16 20 …
  VARYING RV-IDX FROM WS-SEQ BY 2` with the counter as SOURCE (per-presentation FROM). Negatives:
  `present-when-at-85` (0900), `report-varying-no-repetition` (1559 §13.18.64.3 SR1),
  `present-when-group-indicate` (1559 §13.15.3 SR17). Matrix rows `report-present-when-2002` /
  `report-varying-2002` / `report-multi-column-2002` ACTIVE (+ `report-multi-line-2002` pending).
