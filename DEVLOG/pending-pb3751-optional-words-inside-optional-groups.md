## PB3751: PAGE IS and RECEIVE CONTINUE 5 compile; the audit judges a flat group on its whole language

kb/Work PB3751 named three places where the grammar demanded a plain word inside an optional group. Each was
re-probed on this branch's own build first, and each printed format was rendered (pages 468, 546, 634 and 732).

**PAGE (§13.18.39.2).** `RD RP PAGE IS 10 LINES.` failed with `COBOL0001: unexpected 'IS'`. On the printed page
only PAGE carries an underline, so all four words of `[ LIMIT IS | LIMITS ARE ]` are optional words (§8.3.2.4.3).
`reportPageClause` is now `PAGE (LIMIT IS? | IS | LIMITS ARE? | ARE)?`. Either word of the chosen alternative may
be dropped, and a word from each alternative (`PAGE LIMIT ARE`) is still refused (§5.2.6.2). GnuCOBOL demands
LIMIT, but the printed page controls (CLAUDE.md rule 1), and the note records this as a determination.

**VALUE `[ IN alphabet-name-1 ]` (§13.18.63.2).** This site did not reproduce. `88 CN VALUE "A" THRU "Z" AL1.`
compiles and runs, because the IN-less spelling parses as one more literal operand and the binder decides by
symbol (`DataBinder.RangeAlphabetPhraseOf`, kb/Work PB983). The grammar has to keep `(IN cobolWord)?`, so the
audit could only ever report it. The rule now carries a checked marker line,
`// audit:optional-word IN decided-by DataBinder.RangeAlphabetPhraseOf`. The audit reports that word as DECIDED,
and a full run fails when a marker names a member no source file has, or matches no candidate.

**RECEIVE's CONTINUE phrase (§14.9.31.2).** `CONTINUE 5 SECONDS` and `CONTINUE AFTER 5` failed with COBOL0307.
The grammar comment and `specs/ISO_COBOL.md` had made page 634 govern. Page 634 is the CONTINUE statement's
format, where AFTER and SECONDS are underlined. This change reverses that reading and records the reversal as a
determination. §8.3.2.4.3 judges optional words within each format, and §14.9.31.4 GR2 gives RECEIVE's "CONTINUE
phrase" its own semantics, so on page 732 AFTER and SECONDS are plain words. The rule is now `CONTINUE AFTER?
(arithmeticExpression SECONDS? | MESSAGE cobolWord)`. The CONTINUE statement keeps both words. The §14.9.9.2 figure
note is corrected, and its figure now underlines CONTINUE, as the C2 correction it cites already said.

**The audit.** `demanded` compared mandatory token sequences, so it never saw that `(LIMIT IS? | IS | …)?` accepts
`IS` alone. A group whose alternatives are all flat token lists is now judged on its whole language
(`_flat_spellings`), while `(LIMIT IS?)?` still demands LIMIT. Self-test arms cover both cases and the marker. The
full run went from 11 candidates to 6, all of them kb/Work PB3752's, plus 1 DECIDED.

**Goldens.** `13.18.39-PAGE/pb3751_page_limit_optional_words` (accept 85) writes seven spellings with page limit 3.
§13.18.39.4 GR3 d) and §13.18.35.4 GR4 give `1/01 1/02 1/03 2/01` for each. The negative
`pb3751-page-limit-mixed-alternatives` expects COBOL0001 at all four editions. `optional_words_mcs` gains the four
AFTER/SECONDS spellings. `OptionalWordSubsetDriftTests` gains three rows, which made 26 rows, so its partitions go
from 8 to 9 to keep the bound of three rows per partition. The `DESIGN-test-build-ci.md` table now names the class
that enforces that bound.

**Rows.** FMT-13.18.39.2 already read CONFORMS while `PAGE IS` was refused. It is re-affirmed with the new
witnesses (batch `w20261505c-PB3751`, GAP 31 → 31). The corrected §14.9.9.2 figure changed FMT-14.9.9.2's
digest, so the rule catalog was regenerated and that row re-affirmed in batch `w20261505c-PB3751-continue`. Its
notes had recorded the page-604-versus-page-732 "contradiction", and that reading is now withdrawn. No row closes.

**Left as a lead.** A declared data item named MESSAGE, which is not reserved at 2023, is retyped to IDENTIFIER, so
`CONTINUE [AFTER] MESSAGE RECEIVED` fails. That happens with AFTER written too, so it predates this change.
