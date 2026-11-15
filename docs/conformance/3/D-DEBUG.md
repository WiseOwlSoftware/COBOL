- **D-DEBUG — the COBOL-85 debug facility's debugging lines (indicator `D`) and `WITH DEBUGGING MODE`** (kb/Work
  PB1705, PB1802, PB1088; owner decisions R56 and R61). ISO/IEC 1989:2023 no longer carries the facility: §2023:6.2.2 lists no
  `D` fixed indicator (`cite.py --check 6.2.2 "The following are fixed indicators:"` → OK), so the earlier-edition
  behavior is X3.23-1985's, taken as owner decisions and not from the 2023 text. **Determination:** a debugging line is
  SOURCE — compiled as if the `D` were absent — when the program's SOURCE-COMPUTER paragraph says `WITH DEBUGGING MODE`,
  and a COMMENT line otherwise. The clause is a SOURCE-COMPUTER clause, so it applies to the source unit that specifies
  it and to every source unit contained in it (§2023:12.3.5.4 GR1, `cite.py --check 12.3.5.4` → OK 1), and to nothing else: a
  separate program in the same source does not inherit it. **Edition windows (R61, GnuCOBOL's per-standard dialect
  files, medium confidence on the 2002/2014 edge — derived before the ISO 2002 and 2014 texts were in the repository, not yet checked against them):** accepted at
  85; OBSOLETE at 2002 (COBOL0903 on the clause and on the first line); removed from 2014 (COBOL0902, an error
  under strict, a warning under `--permissive`, where the pre-removal behavior above is kept). **Mechanism (R56, the
  pre-parse token rewriter):** the §2023:6.5 logical conversion cannot decide, because the clause is further down the text
  than the line, so it writes the line as `ReferenceFormatProcessor.DebugLineCarrier` plus its program text; COPY
  REPLACING and REPLACE match over that text as if the `D` were absent; the lexer reads the carrier as the hidden
  `DEBUG_LINE` marker and the text as ordinary tokens; and `DebuggingLineRewriter` keeps the line's tokens or moves
  them to the `ABSENT_DEBUG_LINE` channel, per source unit, before any other token pass runs. A continuation line of a
  debugging line is part of it (§2023:6.5 6) joins it to its latest logical line), and the literal state it ends in is
  tracked like any source line's, so a debugging line that opens a continued literal gets that literal's continuation
  (kb/Work PB1914; before, the line's open literal was forgotten and its continuation line was appended as loose text).
  **A debugging line between the parts of a continued literal** (kb/Work PB1914; the 1985 text is not in the repository,
  so the choice follows rule 1's precedence: ISO 2023 has no `D` indicator, then GnuCOBOL): a debugging line is a comment
  line unless the clause says otherwise, and "Comment lines and blank lines may be interspersed among lines containing
  the parts of a literal" (§2023:6.3.5 2), `cite.py --check 6.3.5 "Comment lines and blank lines may be interspersed"` → OK),
  so when the line before it leaves a literal open — an open literal, or a floating literal continuation indicator —
  the debugging line does NOT become the latest logical line: the continuation line after it continues the literal, and
  the debugging line keeps a logical line of its own, after the joined one (GnuCOBOL reads the same program without
  `-fdebugging-line` as the joined literal, and rejects it with `-fdebugging-line`, "continuation character expected").
  Under WITH DEBUGGING MODE the line is source, and a source line between the parts of a literal is outside the
  continuation rule (§2023:6.3.5 2) lets only comment and blank lines stand there); that program is non-conforming and is
  REJECTED (COBOL3473, an error at every edition, kb/Work PB2490), at the debugging line's indicator: the walk
  cannot tell the two modes apart, so it passes the line over as it would a comment and NOTES it to the per-compilation
  reference-format diagnostics, `DebuggingLineRewriter` names the lines it keeps as source, and the front end reports
  the ones that were noted (GnuCOBOL 3.2.0 `-fdebugging-line`: "continuation character expected"; witnesses
  `conformance:cases/_unassigned/pb2490-debugging-line-between-literal-parts-source`,
  `unit:LogicalLineBuilderTests.DebuggingLine_PassedOverInALiteral_IsDiagnosedOnlyWhenItIsSource`,
  `unit:DebuggingLineRewriterTests.Rewrite_NamesTheDebuggingLinesThatAreSource_PerUnit`). Any other debugging line is the
  latest logical line, so a continuation line after it continues it ("continuation of debugging lines is permitted"); a
  debugging line of spaces is a blank line (§2023:6.5 2)), never a join target. Witnesses:
  `conformance:cases/_unassigned/pb1914_debugging_line_between_literal_parts`, `conformance:cases/_unassigned/pb1914_debugging_line_continued_under_clause`,
  `unit:LogicalLineBuilderTests.DebuggingLine_*`. The NIST `S` / `Y` letters are the
  `--nist` dialect, not the `D` indicator: they stay comment lines whatever the clause says. A debugging line that
  stands inside an open PICTURE clause or an open subscript or reference-modification region is decided the same way
  (kb/Work PB1913): the lexer skips its text in those two modes, and when the line's unit declares the mode the frontend
  blanks the line's carrier and lexes the text again, so the line is source under the clause (`MOVE A(1` / `D    + 2` /
  `) TO B` is `A(1 + 2)`; a `D` line holding the PICTURE string supplies it) and a comment without it. `USE FOR
  DEBUGGING` keeps its own row (accepted at 85, rejected from 2002) and its `--std 85` posture is unchanged. Witnesses:
  `conformance:cases/_unassigned/pb1913_debugging_lines_in_open_regions`, `conformance:cases/_unassigned/pb1913_debugging_lines_in_open_regions_comment`,
  `conformance:cases/_unassigned/pb1705_debugging_lines_compiled`, `conformance:cases/_unassigned/pb1705_debugging_lines_per_unit`,
  `conformance:cases/_unassigned/pb1705_debugging_lines_copy_replacing`, `conformance:cases/_unassigned/pb1494_debugging_line_85` (no clause: a
  comment), `conformance:cases/_unassigned/pb1705_debugging_mode_obsolete_2002`, `conformance:cases/_unassigned/debugging-mode`,
  `conformance:cases/_unassigned/pb1494-debugging-line-at-2014`, `unit:DebuggingLineRewriterTests`.

  **What a COPY statement on a debugging line introduces, and the two rules of the debugging-line presentation (--std 85; kb/Work
  PB5214, PB5212).** §1985:XII.2.4 GR9: "each text word introduced appears on a debugging line if the COPY statement begins on a
  debugging line" (`cite.py --check "§1985:XII.2.4 GR9"` → OK), so the library text of a COPY that begins on a debugging line is
  debugging lines, source under the clause and comment without it (§1985:VI.4.3.4 GR4, GR5). `CopyProcessor.ExpandCopiesOneLevel`
  is the one place text is introduced: it writes each non-blank line of the (replaced, nested-expanded) library text behind the
  carrier, and the text after the statement's period on a debugging line keeps its line's kind. Only the carrier is written, so
  `DebuggingLineRewriter` still makes the one decision. §1985:VI.7.3 adds two rules `DebuggingLineRewriter` and the front end now
  check: "A debugging line is only permitted in the separately compiled program after the OBJECT-COMPUTER paragraph" (COBOL7060,
  at the line, with or without the mode: a line that begins before the end of the unit's OBJECT-COMPUTER paragraph, or, in a unit
  with none, before its Data or Procedure Division) and "The content of a debugging line must be such that a syntactically
  correct program is formed with or without the debugging lines being considered as comment lines" (COBOL7061: a program that
  parses with its debugging lines as source is parsed again with them hidden, and reported at the last debugging line before the
  first syntax error of that parse; the converse direction, a unit without the mode whose debugging lines would not parse as source, is
  not asked). Both are 1985 rules and bind below COBOL-2002 only: the later editions carry the facility as
  obsolete or removed (the edition windows above). Witnesses:
  `conformance:cases/VI.4.3@1985-SOURCE-COMPUTER-PARAGRAPH/pb5214-debugging-line-copy-no-mode`,
  `conformance:cases/VI.4.3@1985-SOURCE-COMPUTER-PARAGRAPH/pb5214-debugging-line-copy-mode`,
  `conformance:cases/VI.4.3@1985-SOURCE-COMPUTER-PARAGRAPH/pb5212-debugging-line-before-object-computer`,
  `conformance:cases/VI.4.3@1985-SOURCE-COMPUTER-PARAGRAPH/pb5212-debugging-line-syntax-both-ways`.

  **The FLOATING spelling `>>D`** (ISO/IEC 1989:2002 clause 6.1.2 "`>>D` debugging indicator — a debugging line when followed by
  at least one space character", clause 6.3.4, clause 6.1.2.1 SR7 and SR8; the 2002 text is not in the repository and is read from the
  archived draft, so `cite.py` cannot check those clauses; kb/Work PB4298). ISO/IEC 1989:2023 §2023:6.2.3.1 lets a floating
  indicator stand "in fixed-form or free-form reference format" (`cite.py --check 6.2.3.1 "Floating indicators may be used
  in fixed-form or free-form reference format"` → OK) and lists none for debugging, so, like the fixed `D`, the spelling is
  the earlier editions'. It is the SAME facility in a second spelling, so it is the same construct all the way down: the
  §2023:6.5 conversion's `LogicalLineBuilder.TryEmitFloatingDebugging` (called by both reference formats' classifiers) writes
  the indicator as three spaces, so every later character keeps its column, and carries the line as the fixed `D` line is
  carried — `DebuggingLineRewriter`, the PICTURE / subscript re-lex and the between-literal-parts rule (COBOL3473, at the
  indicator's own column) read the carrier, not the indicator. **Determinations:** (a) the indicator is the first
  character-string of the line, after spaces only (SR7), spelled `>>D` or `>>d` (as the fixed indicator's letter is
  read in either case) with NO space inside it and at least one space after it; `>>DISPLAY`, `>> D` and a `>>D` that
  follows program text are no debugging line. (b) "Followed by at least one space" is read literally: in free form a line
  that ENDS in `>>D` is not a debugging line (its next character is the end of the line, not a space) and draws the
  parser's `>>` error as any unknown directive does, while a fixed-form program-text area is space-filled to margin R
  (Annex A.1 item 157), so a bare `>>D` there is followed by spaces and is a debugging line of spaces, a blank line (§2023:6.5 2)).
  (c) SR8: `>>D` as the first character-string of a line whose indicator area holds the fixed `D` or the continuation
  hyphen is refused, COBOL5770, at the indicator; the line is then read as its fixed indicator says. SR8 is a 2002 rule
  and binds from `--std 2002`: COBOL-85 has no floating debugging indicator, so a `D` line there is a comment line whose
  text `>>D` is commentary (witness `conformance:cases/_unassigned/pb4298_floating_debugging_text_on_d_line_85`). **Edition window
  (registry row `floating-debugging-indicator-2002`):** introduced WITH the floating indicators at 2002 (COBOL0900 below,
  an error under `--permissive` too), obsolete from 2002 (COBOL0903), removed from 2014 (COBOL0902, an error under strict,
  a warning under `--permissive`, where the facility keeps working); GnuCOBOL 3.2 agrees at both edges (`-std=cobol2002`
  accepts all three of the review's probes, `-std=cobol2014` refuses "debugging indicator does not conform"). Witnesses:
  `conformance:cases/_unassigned/pb4298_floating_debugging_free` (under the clause: runs),
  `conformance:cases/_unassigned/pb4298_floating_debugging_fixed` (no clause: a comment),
  `conformance:cases/_unassigned/pb4298-floating-debugging-below-2002`, `conformance:cases/_unassigned/pb4298-floating-debugging-at-2014`,
  `conformance:cases/_unassigned/pb4298-floating-debugging-on-fixed-indicator`, `unit:LogicalLineBuilderTests.FloatingDebuggingIndicator_*`.
