## Entry 2888 — 2026-10-31 16:10 PDT — A word that starts or ends on an underscore is named by §8.3.2.1, not split at the underscore (PB4234)

ISO §8.3.2.1 ends its first paragraph with "The hyphen or underscore shall not appear as the first or last character in
such words." The lexer's word class (`NAME_BODY`) could not start or end a word on an underscore, so `_A` lexed as the
lone `ANY_CHAR` `_` followed by the word `A`, and `A_` as `A` followed by `_`. The user read `COBOL0001: unexpected '_'`
or `cannot parse construct near '_'`, while `A€` (a character no Annex B holds) was named with COBOL2773.

**Re-probe (Debug build, 0/0).** `01 _A PIC X.` drew `COBOL0001 unexpected '_'` and `01 A_ PIC X.` drew
`COBOL0001 cannot parse construct near '_'` at every edition. After the fix both draw COBOL2773 with the rule's own
sentence and the position (`has the underscore as its first character`), and at `--std 85` COBOL0900 (the underscore is
a 2002 introduction) comes first, as for any underscore word.

**Fix shape (superset-parse, bind-narrow, the shape `EXT_CHAR` already has).** The lexer takes the whole character-string
as one ill-formed IDENTIFIER (two new `NAME_BODY` alternatives over a new `WORD_CHAR` fragment: `'_' WORD_CHAR*` and
`(letter|digit|EXT_CHAR) WORD_CHAR* '_'`). `CobolCharacterRepertoire.SeparatorPositionViolation` is the one statement of
the position rule (edition-free, hyphen and underscore); `CobolWordRule.CharacterViolation` asks it first for every
edition, so `WordCharacterScreen` (both word sources) and the directive stages share it. `IsWordShape` and
`IsWordAtSomeEdition` ask the same method instead of repeating the test. `RemovedAt2023` now asks the whole character rule
at 2014: before, `Violation(word, 2014) is null` would have called an edge underscore a 2023 removal (COBOL0902, a warning
under `--permissive`). The `>>DEFINE` and `>>COBOL-WORDS` stages already refused such a word through `IsWordShape`
("not a COBOL word") and now carry the rule's sentence. The COBOL2773 description and `DESIGN-frontend-grammar.md` §3.13 say so.

**Not fixed.** A hyphen at a word edge (`A-`, `-A`) is also the arithmetic minus and the sign of a signed number, so the
lexer cannot take it whole without deciding the operator-spacing rules; it still reads COBOL1941 / COBOL5506. Left as a lead.

**Goldens.** `pb4234-underscore-inside-a-word` (accepted 2002/2014/2023, `.out` computed from §8.3.2.1: `CREME 42`),
`pb4234-leading-underscore-in-a-word` and `pb4234-trailing-underscore-in-a-word` (refused at 85/2002/2014/2023, COBOL2773).
Tests: `WordCharacterScreenTests` (both word sources, every edition, `--permissive`, a comment-entry, directive words) and
`WordCharacterEveryEditionTests`. No inventory row is claimed or closed.

