## PB4224, PB4225: COBOL5506 names a statement position and no longer offers a noise word as an alternative

Two defects in `AdmittedKinds` (hint 0i, COBOL5506), one file pair (`AdmittedKinds.cs`, `CobolErrorStrategy.cs`).

**PB4224 (re-probed on this build).** `IF W-N = 1 THEN 5 END-IF.` drew only `COBOL0001: unexpected '5'` at all four
editions, and so did a literal after `ELSE`, after `ON SIZE ERROR`, after `WHEN 1` in an EVALUATE, and a user-defined word
(`THEN FOO`) or a figurative constant (`THEN ZERO`) at the same places. The parse there expects all 51 statement-opening
words, so `Named.Open` was true (`BeginsAStatement`), no literal was expected and `Refusal` fell through to null. Fix: a
statement position (`Named.Statement`) draws COBOL5506 for a literal (figurative constants included, ISO 8.3.3.1, and
`ALL literal-1`, 8.3.3.6.2) or a user-defined word, saying that a statement may begin there and why the token cannot: a
statement begins with the first word of its statement name (ISO 14.5.3.1, `cite.py --check` OK), the names are Table 12's
(14.5.1, OK). Positions after a COMPLETE statement keep COBOL2269 (hint 0h, which stands first); that was not touched.

**Review round 1 (R1-1, blocking; R1-2, lead).** The first cut listed the words the position takes ("expected a
statement or WHEN", "expected a statement"). Where the position directly follows an operand (`IF W-N = 1 5`,
`EVALUATE W-N WHEN 1 5`) that list was false: the parser has already closed the condition or selection object, so its
admitted set holds the statements and not AND, OR, THRU or an operator, all of which `IF W-N = 1 AND ...` and
`WHEN 1 THRU 5` compile. Tried and discarded: run the admitted walk from the start of the construct closed just before
the token, so the set holds what would lengthen it. For `IF` it did (AND, OR, the operators join the list), but for
`EVALUATE ... WHEN 1 5` the closed construct is the whole WHEN phrase, whose re-opened walk admits a user-defined word
(the grammar's class-name alternative, a superset) and the sign words, which routed the refusal into the literal-category
arm ("of the literals, only the figurative constant ZERO") and buried the statement, with a 100-word list besides. The
root is that the refusal claimed to know what is expected where the parser had already left the alternatives behind, so
the sentence now claims only what is true wherever the arm fires and lists nothing: a statement may begin here, and the
token cannot begin one. R1-2: `ALL "A"` at a statement position kept the bare COBOL0001 because the figurative-word set
excludes ALL (it is not a word to NAME); `FigurativeStarts` adds it for recognising the token written.

**PB4225 (re-probed).** `USAGE 5` listed IS among the expected usages at every edition. IS is the optional word of
`[USAGE IS] usage` (ISO 8.3.2.4.3, OK). The classification is the parse's own, not a list: `ResimulationWalk.WordChangesNothing`
takes one step of the walk over a listed word and asks whether the parse then admits exactly the set it admitted without
the word. IS after USAGE does; WHEN after a selection object does not (a new object follows) and stays named. `Name` drops
such words unless nothing else would be left. First attempt, discarded: an ATN-shape classification (a word written only
as `word?` and never required) failed on IS itself, since three grammar sites require `(IS | ARE)` as a choice, and it
included END-IF and the other `END-x?` scope terminators, which are optional-shaped but not optional words.

**Goldens.** `14.9.19-IF/pb4224-literal-where-a-statement-begins` and `14.9.13-EVALUATE/pb4224-literal-after-a-when-selection`
(refused at 85, 2002, 2014, 2023); the PB4201 USAGE goldens lost the IS, and `13.18.60-USAGE/pb4225-usage-literal-names-no-noise-word-2002`
and `-2014` pin the other two editions. `WrittenKindRefusalTests` gained the statement-position theory (literal after THEN
at 85 and 2023, after ELSE, ZERO, after ON SIZE ERROR, after WHEN, a user word) and the 2002/2014 usage lists; the old
`literal-at-a-statement-start` control (which pinned the bare COBOL0001) is gone. `DESIGN-frontend-grammar.md` states both rules. After review round 1: the two PB4224 goldens carry the new sentence,
`14.9.19-IF/pb4224-literal-after-a-condition` is added (an operand directly before the refused literal), and the theory
gained `all-literal-after-then`, `literal-after-a-condition` and `literal-after-a-relation` (85).
