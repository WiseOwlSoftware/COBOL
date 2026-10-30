## PB3245: a literal its line ends inside is the literal's own token, and COBOL5563 names it

**The defect.** A delimited literal left open at the end of a line, with no continuation line, drew only the parser's
cascade. Re-probed on this tree, `s07c.cob` (free form, `DISPLAY "HEL-` then `-LO".`) printed COBOL0001 `unexpected
'"'`, COBOL0001 at the `-`, COBOL0309 at `LO` and COBOL0001 at the second quotation mark. None of them said "this literal
is not closed". The lexer had no shape for an open literal: `STR_BODY` required its closing delimiter, so the opening one
fell to `ANY_CHAR` and the content lexed as words. A directive operand (`>>DEFINE X AS "ABC`) drew the generic COBOL1619
"malformed compile-time expression" for the same reason.

**The rule.** The general formats close every alphanumeric, boolean and national literal (ISO §8.3.3.2.2, §8.3.3.4.2,
§8.3.3.5.2). A literal may stay open at the end of a line only when a continuation line continues it: §6.3.5 1) with the
fixed indicator, §6.4.2 with the floating one ("When such a literal is incomplete at the end of a line, the incomplete
portion of the literal shall be terminated by a floating continuation indicator"). The reference-format stage joins
every continued literal before the lexer runs, so a literal still open at lex time was never closed. The stage itself
cannot refuse it, because a literal left open on a line an `>>IF` omits is legal (§7.3.16.4 3)) and conditional
compilation runs after it.

**The fix shape.** The R03 doctrine already in the lexer says the token must MATCH so that something is left to
diagnose. `STR_BODY`'s closing delimiter is now optional. Maximal munch takes the closed form whenever the line closes
the literal, a doubled delimiter included; otherwise the literal runs to the end of its line as STRINGLIT, NATLIT,
HEXLIT or BOOLLIT, prefix kept, and the parser reads the literal that was meant. The four tokens share one action,
`OnDelimitedLiteral`, which gives back an open literal's trailing spaces and one trailing period through `CutTokenTo`,
so `DISPLAY "HELLO.` still ends its sentence. `CobolLiteral.IsUnterminated` is the one question, shared with
`SplitLiteral` through `OpeningDelimiterAt`; `SplitLiteral` still declines an open literal, so the §8.3.3 rules and the
format gates never judge its content. `LiteralScreen.ReportUnterminated` reports COBOL5563 over the unit's token stream
in `Frontend.LexAndParseOnce`, first of the post-lex rules, because the open literal is usually why the parse fails and
a unit whose parse fails is never bound. It reports over each directive fragment in `ScreenFragment` as well, whose
verdict it joins. After the fix `s07c` draws COBOL5563 first. `DISPLAY "HELLO` followed by `STOP RUN.` draws COBOL5563
and nothing else, and the `>>DEFINE` draws COBOL5563 instead of COBOL1619.

**Sibling sweep.** The other literal scanners already name the case: pseudo-text (COBOL2449), a COPY operand (COBOL2450)
and the `>>COBOL-WORDS` tokenizer (its SR2 defect). The lexer was the one arm without a name for it. A fixed-form
debugging line that is a comment keeps its open literal unreported (the `ABSENT_DEBUG_LINE` channel is skipped).

**Witnesses.** The refusals are `6.3.5-Continuation-of-lines/pb3245-literal-not-continued-fixed` (all four editions),
`8.3.3.5-National-literals/pb3245-national-literal-unterminated-free` and
`8.3.3.2-Alphanumeric-literals/pb3245-define-literal-unterminated` (2002 on). The accepted golden is
`7.3.16-IF/pb3245_unterminated_literal_in_omitted_text`, which prints OK from 2002. Unit tests:
`UnterminatedLiteralTokenTests` (each kind, its extent and the DOT column after the cut), `CobolLiteralTests`
(`IsUnterminated`, and an open literal is no well-formed literal) and `LiteralScreenDriftTests` (the one question, the one
report, the two callers). No inventory row was claimed or re-verdicted.

**Review finding R1-1: a free-form comment-entry is commentary too.** The branch review found a regression. Free form
passed comment-entry text to the lexer, so with the new token `AUTHOR. Pat O'Brien.` (`--std 85 --source-format free`, or
`--permissive`) drew COBOL5563 and COBOL2633 where main had compiled it. The first implementer had recorded the root as a
lead: COBOL2631, COBOL2633, COBOL2630 and COBOL0900 already fired on entry text before this change, and
`SECURITY. J. Smith.` drew COBOL1971. Fixed form discards the entry in the §6.5 conversion. Free form now does the same:
`ReferenceFormatProcessor.FreeFormConverter` replaces the static `ConvertFreeLines`. In an IDENTIFICATION DIVISION, a line
whose first word is a comment-entry paragraph's word is written `AUTHOR. .`, as the fixed-form converter writes it, and
the rest of the line is discarded. No edition has both free form and a comment-entry, so where a free-form entry ends was
open. CLAUDE.md rule 1 sends it to GnuCOBOL 3.2. Probed with `cobc -free`: the paragraph word must be first on its line,
its period is optional, the entry is the rest of that line, and a second line of entry text is refused. That is
docs/CONFORMANCE.md D-CE1. One behavior changes on purpose: main accepted an entry continued onto further lines (it ran
to the parser's first period token), and that is now refused, as GnuCOBOL refuses it. The sibling sweep over the
conversion found a second defect. A free SOURCE FORMAT segment never passed the division it read on to the next segment.
So a fixed-form `REMARKS.` procedure paragraph after a free `PROCEDURE DIVISION.` header was read as a comment-entry, and
a legal 2002 program was refused with COBOL0001. `ConvertSegment` now carries the division across segments in either
format. Witnesses: `wiseowl/cases/6.5-Logical-conversion/pb3245_free_comment_entry_is_commentary` (85, prints OK: an
apostrophe, an unclosed quotation mark, `,`/`;` with no space, `X"ZZ"`, an inner period and a header with no period),
`pb3245-free-comment-entry-ends-at-line-end` (refused at 85, COBOL1971), and
`cases/6.5-Logical-conversion/pb3245_free_segment_hands_on_its_division` (2002, prints IN-REMARKS; GnuCOBOL prints the
same). Unit tests are in `ReferenceFormatLogicalConversionTests`.
