## Entry 3661 — 2026-11-15 00:00 PST — PB5572, PB5573, PB5564: three --std 85 under-rejections in the version conformance pass (a declarative RELEASE/RETURN with no range, a reference-modified control name, a subscripted subscript)

**PB5572, a RELEASE or RETURN in the declaratives.** Re-probe on ed86914443: `r10c.cob` (a USE AFTER ERROR procedure that
RELEASEs, no SORT in the program) printed `RELEASED IN DECLARATIVE`, `AFTER OPEN`, and its RETURN twin took AT END, both exit 0
at --std 85. `VersionConformancePass.GateReleaseReturnRange` collected only statements at or after `EntryPc`, because the range
of an input or output procedure includes "all statements in declarative procedures that are executed as a result of" its
statements (§1985:XI.4.4.4 GR7, GR10), a run-time fact. That reason holds only where such a procedure exists. The gate now
collects the declarative statements too and refuses one (COBOL7081) only where its file's range is empty; where a procedure
exists it is still not asked. Goldens: `pb5572_release_in_declarative_of_input_procedure_85` (accepted, AAA DDD: the declarative
RELEASE runs inside the input procedure) and two refusals (`pb5572-release-in-declarative-no-input-procedure-85`,
`pb5572-return-in-declarative-no-output-procedure-85`, the second a SORT with an input procedure and GIVING). The rows
`1985-SR-XI.4.2.3-2` and `1985-SR-XI.4.3.3-3` stay PARTIAL: the range's CALL is not followed into a contained program, a lead
of this landing's report. docs/conformance/3/D-SORT-RELEASE-RETURN-RANGE-85.md states the declarative rule.

**PB5564, a subscripted subscript.** Re-probe: `DISPLAY ELEM (IE (2))` printed `B` and `ELEM (IE (2) + 1)` printed `C` at 85.
`Cobol85OperandForms.IsSubscript` took any data reference as data-name-2. §1985:III.2 bars a subscripted 'data-name' unless the
format permits it, and §1985:IV.4.3.8.2.3 SR5 permits qualification only, so the new `IsSubscripted` asks the word and every
qualifier for a subscript, and both repros are the 2002 arithmetic-expression subscript, COBOL0900. Only the subscript is
asked: a reference-modified subscript is alphanumeric and keeps its every-edition COBOL0844 alone (the first draft asked the
modifier too and drew a second, misleading COBOL0900 beside it; the probe caught it). Goldens: two refusals and
`pb5564_qualified_subscript_data_name_85` (K OF G1, K IN G2 + 1, K OF G1 - 1: B D A). `1985-FMT-IV.4.3.8.2.2` PARTIAL to
CONFORMS.

**PB5573, a reference-modified control name.** Re-probe: `CONTROLS ARE WS-KEY OF WS-REC (1:1)` with `TYPE CH WS-KEY OF WS-REC
(1:1)` compiled and ran at 85. One parse-arm helper, `GateCobol85ControlName`, now asks every control name, from the new
`VisitReportControlClause` and from `VisitReportControlName`; a reference modifier on the word or a qualifier (read by
`ReferenceResolver.ReadWritten`) is COBOL0900 under the new construct row `report-control-reference-modification-2002`
(ISO/IEC 1989:2002 13.16.15.2 SR4 and 13.16.55.2 SR10 add it; VCR row 7.45 (g)). A subscripted control name is refused at every
edition already and is not asked again; the SUM clause's RESET ON operand refuses a modifier at every edition (probed). The
note's open half is decided as a reading: a qualified TYPE operand stays COBOL-85 (folio XIII-55 is missing, PB4952; the
operand must match a CONTROL operand, which SR1 permits to be qualified). Goldens: `pb5573_control_name_ref_mod_2002` (the
break on WK (1:1) alone), `pb5573_qualified_control_name_85` (CH 1 CF 1 CH 2 CF 2 through USE BEFORE REPORTING) and two
refusals. `1985-FMT-XIII.3.20.2` PARTIAL to CONFORMS. The 1985 GAP moves 63 to 61.

**Self-review.** Architecture: each rule has one home (the range gate, the 1985 operand-form reader, one control-name helper);
the report gate reuses the reference reader the UNSTRING gate uses rather than a second refmod predicate. Duplication: none
added. Performance: parse-arm checks over the suffixes of one reference, no allocation on a hot path. No new diagnostic code
(COBOL7324-7326 returned).

**Gate.** The first full gate was RED on one case, three tests: `8.4.3.1-Identifier/pb1455_qualified_subscripted_subscript`,
accepted at 85, writes `X (E OF T OF G (2))`, a subscripted subscript, so PB5564 now refuses it at 85, as §1985:IV.4.3.8.2.3
SR5 requires. It moves to accept 2002 with the same output (the change's edition-gate sweep). Run 000003-20261115T072658Z:
`=== BUILD-LOCAL GATE: GREEN — Conformance 25,187/25,187 ran · Unit 40,785/40,785 ran · RepoDrift 177/177 ran ·
Characterization up to date ... ===`.

