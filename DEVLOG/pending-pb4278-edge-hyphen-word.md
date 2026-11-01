## PB4278: a hyphen at a word's edge is named by ISO 8.3.2.1, and the NAME_BODY comment states the current rule

**Re-probe (base 2ce2e70a2).** `01 A- PIC X.` drew only COBOL1941 at `PIC` and `01 -A PIC X.` only COBOL5506 at `A`,
at every `--std`. §8.3.2.1 ("The hyphen or underscore shall not appear as the first or last character in such words")
was never named. `COMPUTE C = A -B` drew COBOL2993 (§8.7.1), which is correct.

**Mechanism.** §8.3.1 says "A character-string is delimited by separators", and the hyphen is not a separator, so
`A-` and `-A` are each one COBOL word. The lexer cannot take that word whole the way PB4234 took `_A`: the `-` is also
the arithmetic minus and a literal's sign, and only the parse knows whether `A -B` is a subtraction missing a space or
`-B` a word. The fix leaves the lexer splitting it (IDENTIFIER MINUS) and puts the join in the screen that already
owns the word's character rule. `WordCharacterScreen` now has one `WordScanner`, and both word sources feed it: the
token stream on a failed parse and the tree's terminals on a parsed one. The scanner groups ADJACENT tokens (nothing
between them) into chains. A run of word-character tokens that holds a MINUS is one word, judged by the same
`CobolWordRule.CharacterViolation`, so PB4234's `SeparatorPositionViolation` names it (COBOL2773, every edition, the
cause before the parse error). Two kinds of run are not words. A run whose MINUS the tree made an `addOp` belongs to
§8.7.1, and `SeparatorRule.ArithmeticOperatorViolations` reports it, so each boundary gets one report. A hyphen
followed only by digits is a literal's sign. A word token inside a joined run is judged only as part of that run, so
`MY_NAME-` draws one report. The screen also covers the success path: `LINE -X` in a screen item is named on the
binder channel. No new code was needed; COBOL2773 is reused, so COBOL5815-5817 are returned.

**The stale comment.** The NAME_BODY block in `CobolLexer.g4` made three claims that PB4234's underscore alternatives
had made false: NOT-FIRST is structural, NOR LAST is enforced for both separators, and only hyphen and underscore are
barred from the ends. It also named `VersionConformancePass.VisitCobolWord` as the site of the underscore gate, which
`WordCharacterScreen` has owned since PB3845. The block now describes three cases: well-formed words, the ill-formed
underscore edge the lexer takes whole, and the hyphen edge the screen joins. `DESIGN-frontend-grammar.md` §3.13,
`SeparatorRule` and `SeparatorPositionViolation` say the same.

**Goldens.** `cases/8.3.5-Separators/pb4278-hyphen-inside-a-word` (accepted at 85/2002/2014/2023: interior hyphens, a
spaced minus, signs; 007/002/012/WORD from the arithmetic), and `cases/8.1.3-COBOL-character-repertoire/pb4278-trailing-
hyphen-in-a-word` and `-leading-hyphen-in-a-word` (COBOL2773 at every edition). `WordCharacterScreenTests` adds both
positions, every edition, a reserved word, an integer, a word that also holds an underscore, the location and order
of the cause, operators and signs that are not word edges (on both word sources), the parsed source, and a literal,
a comment-entry and a comment debugging line.

**Rows.** None claimed, none closed. The rule catalog has no row for the §8.3.2.1 general paragraph (the report's
lead).

**Self-review.** Architecture: one scanner, one rule site, and the operator/word split is made on one `addOp` test
that the separator rule mirrors. Performance: one reused chain list, and token text is read only for candidates and
for chains that hold a MINUS. Duplication: no second statement of the position rule. A hyphen next to a literal's
opening delimiter (`-X"41"`) is not joined, because it is §8.3.5 5)'s case (COBOL2633).
