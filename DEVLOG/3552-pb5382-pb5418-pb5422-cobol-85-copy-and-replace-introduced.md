## Entry 3552 — 2026-11-12 16:07 PST — PB5382, PB5418, PB5422: COBOL-85 COPY and REPLACE - introduced debugging lines, word operands BY / LEADING / TRAILING, REPLACE after a COPY; PB5214 PB5212: a COPY on a debugging line introduces debugging lines; the two rules of the 1985 debugging-line presentation

### PB5382, PB5418, PB5422: COBOL-85 COPY and REPLACE - introduced debugging lines, word operands BY / LEADING / TRAILING, REPLACE after a COPY

Three defects from the 1985 code read, each in `CopyProcessor` and each re-probed on this branch's build first.

**PB5382 (MAJOR, wrong answer).** A replacement wrote its multi-line pseudo-text-2 verbatim, so only the first introduced line sat on the
debugging line's carrier and `DebuggingLineRewriter` kept every later line as source. Re-probe before the fix: p11g printed THREE to SEVEN and
END, p11h and p11i printed 3, and p11b and p11f compiled a 100-character literal onto a debugging line. Fix at the one place the text is
written: `ApplyReplacements` decides the line each whole match begins on and writes `IntroducedText`. Every introduced line is a debugging
line when the replaced text begins on one (§1985:XII.3.4 GR11, §1985:XII.2.4 GR9, §2002:7.1.3.3 GR11). On a source line, COPY keeps
pseudo-text-2's own debugging lines (GR9, §2002:7.1.2.3 GR13), as does REPLACE from 2002, while COBOL-85's REPLACE gives them the replaced
line's indicator (`ResultRule.KeepsPseudoTextDebuggingLines`, `EditionInfo.ReplacedTextIsLaidOutInFixedForm`). A replacement that ends on a
debugging line the replaced line is not now puts the rest of that line on a line of its own, where before the rest joined the debugging line.
At 85, a literal placed on a debugging line that the program-text area cannot hold from its area's margin (65 positions in area A, 61 in
area B) draws the new COBOL7039. After the fix: p11g prints END, p11h and p11i print 1, p11b and p11f draw COBOL7039. `DebuggingLineRewriter` is
unchanged. Goldens `pb5382-replace-debugging-introduced-lines` and `pb5382-copy-debugging-introduced-lines`, refusals
`pb5382-replace-literal-on-debugging-line` and `pb5382-copy-literal-on-debugging-line`; design `DESIGN-cc-in-copy.md` §3.7. Row
1985-GR-XII.3.4-11 PARTIAL to CONFORMS.

**PB5418 (rejects legal source).** `ReadOperand` took any BY in an operand position as a missing operand, and `ParseReplacingOperands` took
LEADING and TRAILING as the 2002 partial-word phrase at every edition. §1985:XII.2.3 SR6 admits any COBOL word but COPY. The operands are
now read by position: a BY where an operand stands is the operand, and LEADING or TRAILING opens the phrase only before ==partial-word-1==.
Re-probe: s14 and s16 print AB, s7 runs, the control s11 is unchanged. Golden `pb5418-replacing-reserved-words`. Row 1985-SR-XII.2.3-6
PARTIAL to CONFORMS.

**PB5422 (under-rejects).** `FollowsSeparatorPeriod` accepted a REPLACE whose preceding character came from another file, so
`DISPLAY "X" COPY LIBR.` with a REPLACE at the start of LIBR compiled at 85. It now reads the last text-word of the COPY-expanded
resultant text before the REPLACE. The sibling sweep found a false refusal in the same function: a REPLACE on a debugging line after a
period drew COBOL0900, because the carrier read as text. The first gate was RED on `pb4241_copy_operand_names_division` (a REPLACE after a
COPY whose library text is a comment-entry paragraph): a character walk read the comment-entry carrier, so the check became a text-word
scan, to which every carrier and comment is a space. Goldens `pb5422-replace-library-text-not-after-period` (refused at 85) and
`pb5422-replace-library-text-after-period`. Row 1985-SR-XII.3.3-1 PARTIAL to CONFORMS.

1985 GAP 318 to 315. Self-review looked at the one writer of replacement text (no second copy of the indicator rule), the allocation in
`IntroducedText` (only for a multi-line or carrier-led replacement that the indicator decides) and the shared peek of `StatementCursor`.
`semgrep/verify.py` PASS with no count raised.

### PB5214 PB5212: a COPY on a debugging line introduces debugging lines; the two rules of the 1985 debugging-line presentation

Finished from a predecessor's checkpoint (be960f036b, cherry-picked onto main with PB5382's branch merged in). Its codes COBOL7015/7016 became this wave's COBOL7060/7061.

PB5214. Re-probe on the branch build: without WITH DEBUGGING MODE a `D    COPY` still ran the copied statements before the fix. Fix at the one place text is introduced, `CopyProcessor.ExpandCopiesOneLevel`: when the COPY begins on a carrier line (`LineIsDebugging`), each non-blank line of the library text is written behind the carrier (`AppendAsDebuggingLines`, after nested expansion and REPLACING), and the text after the statement's period on a debugging line keeps the carrier (§1985:XII.2.4 GR9). `DebuggingLineRewriter` still makes the one decision. Goldens `pb5214-debugging-line-copy-no-mode` and `-mode` (REPLACING, tail text) at 85. The predecessor's goldens nested a COPY in the library text, which COBOL-85 forbids (§1985:XII.2.4 GR12, COBOL0900 on the merged tree); the nesting moved to `7.1.2.3@2002-COPY-STATEMENT-GENERAL-RULES/pb5214-debugging-line-copy-nested-no-mode` and `-nested-mode` (accept 2002).

PB5212. `DebuggingLineRewriter.Rewrite` also names the lines that begin before the end of the unit's OBJECT-COMPUTER paragraph (or, with none, before its Data or Procedure Division): COBOL7060, at 85 only. A program that parses with kept debugging lines is parsed again with them hidden (`Frontend.ParseCopyOf`) and a program that no longer parses is COBOL7061, at the last kept line before the first syntax error (§1985:VI.7.3). Goldens `pb5212-debugging-line-*` plus their `wiseowl/diagnostics` refusals.

Review finding R1-1 on PB5382 (a multi-line match crossing between a debugging line and a source line gave the rest of the end line the begin line's indicator): `ApplyReplacements` now compares the line the replacement ends on with the line the match ends on and starts a line of its own for the rest, carrier included, when they differ. Golden `pb5382-replace-match-crossing-debugging-lines`; `FixedFormDebuggingLine_TextWordsTakePartInMatching` now expects the period on its debugging line.

Rows: 1985-GR-VI.4.3.4-4 PARTIAL to CONFORMS and 1985-GR-VI.4.3.4-5 DIVERGES to CONFORMS (GAP 313 to 311).

Review round 1 (finisher): a contained program inherits its container's OBJECT-COMPUTER position for COBOL7060 (kb/Work/PB5212/2026-11-12-review-r1-nested-programs.md), and the golden XII.3@1985-REPLACE-STATEMENT/pb5382-replace-match-crossing-debugging-lines (S0, S1, R2, S2) now exists to witness the end-line indicator rule.

