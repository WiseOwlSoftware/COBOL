## A sign touching the next literal is that literal's sign (PB3962), and four report-section 2002 forms get their COBOL-85 gate (PB3964)

Wave 20261732, group A (the DataBinder group). Two mechanisms, two checkpoint commits.

**PB3962 - `ZERO -1` in an operand list.** Re-probe on base `6974abd43`: `DISPLAY ZERO -1` was a bare COBOL0001, `DISPLAY N ZERO -1 N`
a COBOL2269, and the level-88 list `VALUE ZERO -1` and `ADD ZERO -1 TO X` read the ZERO as the numeric 0 or did not parse.
`ZeroTokenRewriter` took any `+` / `-` right of a ZERO as a binary operator and made the ZERO arithmetic. The lexer's default mode
splits `-1` into MINUS and INTEGERLIT outside a list region, so the token type cannot tell `ZERO - 1` from `ZERO -1`; the source
positions can, and the standard states the test: 8.7.1 makes a binary operator a character "preceded by a space and followed by
a space", 8.3.3.3.2 2) makes a sign "the leftmost character of the literal". `ZeroTokenRewriter.IsSignOfNextLiteral` vetoes the
following-operator arm when the sign touches the digits of an integer, decimal, floating-point or DECIMAL-POINT IS COMMA literal;
operators on the ZERO's left, spaced operators and the paren and colon arms are unchanged. The reader-side patch PB3846 added for
the one list that parsed (`DataBinder.RewrittenAllZeroRawText`) is dead and deleted. A consequence taken on purpose: an arithmetic
`COMPUTE X = ZERO -1` is now a COBOL0001 at the sign, where it was COBOL2993; it is invalid source either way. Golden
`8.3.3.6-Figurative-constant-values/pb3962_figurative_zero_before_a_signed_literal` (ADD, ADD GIVING, SUBTRACT, two VALUE lists, the
spaced `ZERO - 1` control; all four editions), unit theories `CobolLexerModeDriftTests.Zero_BeforeASignTouchingItsLiteral_StaysFigurative`
and `Zero_BesideAnOperator_IsStillRewritten`. Inventory: the retired witness `RewrittenAllZeroRawText` moved off SR-8.3.3.6.3-1;
witnesses added to SR-8.3.3.3.2-2 and GR-8.3.3.6.4-4; no row closed.

**PB3964 - report-section 2002 introductions.** FILLER as a report group entry name, the `+` symbol for PLUS in a relative LINE or
NEXT GROUP operand, a SOURCE operand that is not a bare identifier, and the SOURCE ROUNDED phrase all compiled clean at `--std 85`.
The edge is quoted from ISO/IEC 1989:2002 Annex F.2 items 53 and 115 (the archived post-ballot text). Four construct rows
(`report-filler-2002`, `report-plus-symbol-2002`, `report-source-expression-2002`, `report-source-rounded-2002`), three recognition
arms in `VersionConformancePass` (`VisitReportGroupEntry`, `VisitReportRelativeSign`, `VisitReportSourceClause`), VCR row 7.45. The
'-' of "PLUS or MINUS" names nothing (no 2023 relative format has MINUS). Three goldens that had been filed `accept: [85]` used these
forms (`pb1226_report_filler_entry_name`, `pb957_next_group_forms`, `pb852_report_source_expression`) and now accept from 2002; four
negatives `pb3964-report-*-85`. A sweep of every REPORT SECTION program in the suite and the NIST set at each accepted edition found
exactly those three. Item 115's other bullets (CODE IS identifier, OR PAGE, an omitted control name, optional ON / FOR, PAGE LIMIT
COLUMNS, SUM with an expression or ROUNDED) still compile clean at 85; they are reported as leads, not fixed here.

Self-review (architecture, full code, performance, duplication): the sign test reads token positions already in hand (no text
re-scan); the relative-sign gate is one visit of the one shared production, not a copy per clause; the SOURCE form test reuses
`SoleOperand.DataRef`, the reduction the binder's classifier uses.

Gate (batched, leg 1 plus the Linux half): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` on head e727195b3, run 000001-20261028T011525Z; the added goldens (`pb3962_figurative_zero_before_a_signed_literal` at 85, 2002, 2014 and 2023; the four `pb3964-report-*-85` refusals) all Passed in `leg-1-Conformance.trx`; Linux half (WSL clone of HEAD) GREEN; `semgrep/verify.py` PASS.
