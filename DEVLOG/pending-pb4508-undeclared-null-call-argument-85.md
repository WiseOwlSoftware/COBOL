## PB4508 — an undeclared NULL in a CALL argument at 85 draws the undefined name alone

**Re-probe (my build, `--std 85`).** `CALL "X" USING NULL`, `USING BY CONTENT NULL` and the pb4155 golden's three CALLs each drew
COBOL0900 ("a literal or expression as a CALL argument requires COBOL-2002") AND COBOL1639 ("'NULL' is not defined"); the
bare and BY CONTENT spellings carried the second, misleading refusal, `BY REFERENCE NULL` only the right one. The cause was the
one the note named: PB4503's up-front judgement (`judgedUpFront`) ran `Format2OnlySpellingOf` on every argument before the
loop's NULL screen (PB4155), and read the NULL token as a literal-2.

**Fix shape.** The order was a convention kept by hand between two passes; the question "what IS this word" now sits in the
judge itself. `CallBinder.IsUndeclaredNullWord` is the one reader of "the argument writes a NULL the edition leaves free
(§8.9 reserves NULL from COBOL-2002; at 85 it is identifier-2 naming nothing, §8.4.2.1)". The argument loop reports it;
`Format2OnlySpellingOf` reads it for the two operand-derived spellings (BY CONTENT expression operand, bare literal or
expression) and names no Format-2 spelling, so neither pass judges the word as a literal, in whichever order they run. A BY
VALUE phrase is still the 2002 introduction whatever its operand is, so `BY VALUE NULL` keeps its phrase refusal beside the
undefined name (two true statements). At 2002 and later nothing changed.

**Witnesses.** `FormatOneVendorCallPermissiveTests.UndeclaredNull_At85_DrawsTheUndefinedNameAlone` (strict and
`--permissive`: four CALLs, exactly four COBOL1639, no other error, no COBOL0899 warning) and
`UndeclaredNull_ByValue_At85_KeepsThePhraseRefusalBesideTheUndefinedName`. The existing golden
`8.3.2-COBOL-words/pb4155-call-null-undeclared-at-85` stays the refused program (a presence check on the text; the exact count is
the xunit test's); the 2002 half is `pb1630_null_argument_crossing`.

**Rows.** None claimed, none closed. Sweep: the other `IsFreeAt("NULL")` readers (`PredefinedNullRule`, `ExpressionBinder`,
`SetBinder`) report the undefined reference alone already; INVOKE's argument NULL is a 2002 construct.
