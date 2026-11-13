## COBOL-85 arms of the report binder, the PICTURE reader, ALTER and the segment rule

Wave 20262019 group A continued the 1985 code-read notes on top of wave 20262013 group A's branch (merged, not
replayed). Nine of its ten notes landed; PB5171 (the SPECIAL-NAMES clause sequence and once-only clauses) is left to a
successor, because its row is a ClauseCardinalities row whose 2023 figure needs the order decision kb/Work PB1578 still
holds open. Every note was re-probed on this branch's build before the fix, and every probe reproduced.

**PB5423, the CONTROL level a RESET ON or TYPE CF operand names.** `CONTROLS ARE FINAL WS-A OF G1 WS-B` with `RESET ON
WS-A` and `TYPE CONTROL FOOTING WS-A` drew COBOL0899: `ReportSectionBinder.ControlLevelOf` compared the written
references. It now matches the written reference first (two operands §2023:13.18.16.3 SR6 lets name one item stay two
levels) and then the item the reference names with the same reference modification (§2023:8.4.2.2.3 SR2). Golden
`13.18.54-SUM/pb5423_reset_on_qualified_control`, its report derived first and matched at 85 and 2023.

**PB5424, the USE half of §1985:XIII.3.20.3 SR5.** A USE BEFORE REPORTING procedure for a page heading could DISPLAY the
control item, and one for a control footing the group holding it. The SR5 relation is now one member,
`ReportSectionBinder.ControlDataReferenceOf`, asked by the SOURCE screen and by `ReportWriterBinder.CheckControlDataReferences`,
which `StatementBinder.BindStatement` asks of every statement of a USE BEFORE REPORTING procedure (COBOL6825, 85 only).

**PB5426, three hierarchic levels.** `ScreenReportEntryFormats` now refuses a format 2 entry whose parent is not the
format 1 entry (§1985:XIII.3.9.3 SR4 b; COBOL2247).

**PB5118 and PB5174, the PICTURE character-string reader.** At 85 a repetition factor may follow only the symbols
§1985:VI.5.9.4 GR7 lists (`TryExpandRepeats` asks `RepeatableSymbol`; new code COBOL7078), and only the letters of A, B,
P, S, V, X, Z, CR and DB fold to uppercase (`FoldPictureCase`, §1985:VI.5.9.3 SR3), so `PIC w99` under CURRENCY SIGN
IS "W" is COBOL0808 there. The currency scan no longer folds a second time.

**PB5191, BLANK WHEN ZERO on a signed picture at 85.** `Validate` bars only the asterisk below 2002 (§1985:VI.5.9.3
SR7), and the 85 promotion to numeric edited (§1985:VI.5.4.4 GR2) takes the S out of the edit mask: `PIC S9(4) BLANK
WHEN ZERO` is four positions, blank at zero. A PICTURE refused by its own rule no longer draws the misleading COBOL2405
"category alphanumeric" cascade, in the data division and in the Report Section.

**PB5175, the trailing currency place.** `Precedence` offers a fixed currency symbol the trailing role from 2002
(§2002:13.16.38.2 SR23); at 85 PIC 999$, +999$ and 999$CR are COBOL1935, whose 85 message names §1985:VI.5.9.6.

**PB5195, ALTER of a section.** `PcRange.IsParagraph` (Start == End) is deleted: a section of one paragraph met it, so
`ALTER S7` altered S7's paragraph. `BindAlter` and the switch prescan ask `ResolvedProcedure.IsSection`.

**PB5442, the segment rule over the executed range.** `Segmentation.CheckProcedures` narrows a failing physical span
to the sections reached by fall-through and GO TO (ALTER destinations included), so a section a GO TO jumps over is not
asked; nested PERFORM, SORT and MERGE ranges stay their own statements'. `docs/CONFORMANCE.md` §3.8's row is rewritten
around §1985:VI.6.21.4 GR11 and §1985:I.1.6, and `docs/CONTROL_FLOW_DESIGN.md` D19 says why this rule follows GO TO.

**Rows.** Fourteen 1985 rows re-verdicted CONFORMS through `record_verdicts --edition 1985` (1985 GAP 288 → 276); no 2023
row changed. Goldens for each note, refusals with their own JSON. The self-review read the diff for one mechanism per
job (the SR5 relation and the PICTURE fold each have one home), cited-rule fidelity (every § checked with `cite.py`),
hot-path cost (the segment walk runs only on the failure path; the fold allocates once per PICTURE, as before) and
second copies (none added).
