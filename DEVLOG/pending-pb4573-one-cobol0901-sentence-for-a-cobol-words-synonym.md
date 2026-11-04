## PB4573: one COBOL0901 sentence for a COBOL-WORDS synonym, and the withdrawn-word sentence beside it

Wave 20261860, group A (PB4573). The parser's COBOL0901 named the ORIGINAL of a retyped synonym: `MOVE "A" TO SHOW` after
`>>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"` said "'DISPLAY' is a reserved word", a word the source never wrote.

**Re-probe (69270302d).** The note believed the funnel arm named the synonym. It does only for a synonym the lexer has no token for
(LOCALE, COPY, ...). The lexer's retype (`CobolWordsRewriter.Plan.Apply`) makes a synonym of a word it has a token for that token and
overwrites its `Text` with the original's, so every stage that quotes `Text` names the original: the parser (`MOVE ... TO SHOW`,
`PERFORM fetch` after `EQUATE "READ" WITH "Fetch"`) and the funnel (`01 CNST` after `EQUATE "CONSTANT" WITH "CNST"` said 'CONSTANT').

**Fix.** One builder, `ReservedWordSet.UserWordViolationMessage(written, map, edition)`; the synonym decision is the map's
(`CobolWordsMap.SynonymOf`) and both stages read the word from the source extent (`WrittenSource.Of(IToken)`, new). The instance
`UserWordViolation` is gone. Found on the way and fixed: the parser resolved the already-canonical token text a second time
(`DenotedBy` resolves the written word once), so `MOVE "A" TO SHOW` after `SUBSTITUTE "DISPLAY" BY "SHOW"` drew COBOL5506 and a
COBOL5926 note that called DISPLAY withdrawn when the source wrote SHOW; `NoteWithdrawnWord` now tests the written word.

**The three review leads of w20261859b (PB4572).** (1) `SpecialNamesBinder.SwitchBindDecimalPoint` said the clause "is not available"
under SUBSTITUTE too; 7.3.10.4 GR4 makes literal-5 usable wherever COMMA is required, so under SUBSTITUTE the refusal says the clause is
written with literal-5 and under UNDEFINE (GR3) that it is not available. (2) `SwitchBindCurrency` drew a self-contradictory COBOL0892
for an UNDEFINEd or SUBSTITUTEd SYMBOL; it names the directive. (3) The `OoIsExceptionObject` comment quotes GR2 word for word. The
withdrawn-word sentence is `CobolWordsOp.Withdrew` / `WithdrawnSyntax`; `CobolWordsOp.AsWritten` is the one spelling of a directive
(`CobolWordsMap.DirectiveText` and `IntrinsicBinder.RemovalClause` use it).

**Goldens.** `pb4573_cobol_words_substitute_comma_is_the_decimal_point_word` (accept 2023, 1,50),
`pb4573_cobol_words_substitute_symbol_is_the_currency_picture_word` (accept 2023, FU=[USD5.25]); refusals
`pb4573-cobol-words-synonym-of-display-is-no-operand-word`, `pb4573-cobol-words-substituted-comma-withdrawn-from-decimal-point`,
`pb4573-cobol-words-undefined-symbol-withdrawn-from-currency-picture`, each pinning the sentence. Unit tests in `CobolWordsDirectiveTests`
assert the three stages produce the builder's sentence and the clause refusals name the directive and what is left of the clause;
`EditionReadDriftTests` records `UserWordViolationOf`. Rows closed: none (GR-7.3.10.4-2, -3, -4 were CONFORMS).

**Registered, not fixed (outside the mechanism).** A SUBSTITUTE synonym of a lexer-token word that a gate or a keyword predicate admits
(`SUBSTITUTE "RECEIVE" BY "RCV"` then `01 RCV PIC X.`) is accepted as a user-defined word; and syntax-error messages quote the retyped
`Text` (`MAIN SECT.` ... `MOVE 1 TO W-X SECT.` after `EQUATE "SECTION" WITH "SECT"` says "unexpected 'SECTION'").
