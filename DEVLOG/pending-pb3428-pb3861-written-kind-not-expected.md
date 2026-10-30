## PB3428, PB3861: a syntax error names what the parse expects at the token (COBOL5506)

Wave 20261769 group C. Both notes are one mechanism: at a token of a kind its position does not take, the parse
error carried no account of what the position expects, and the reason was that every reading of the parse at the
token was predicate-blind.

**Re-probe (802352531).** `OBJECT-COMPUTER. XYZ MEMORY SIZE 12 FOO.` and `XYZ SEGMENT-LIMIT IS.` at `--std 85`, and
`SET W-N TO "A".` at 2023, each drew a bare `COBOL0001: cannot parse construct near …`. `WRITE R AFTER ADVANCING "A".`
(PB3861's second example) did not reproduce: it has been refused by name, COBOL2365, since a374ee660 (2026-09-23).

**The root, in two places.** (1) The expected set: `(CHARACTERS | {memoryUnitAhead()}? IDENTIFIER)` shows the
IDENTIFIER edge the predicate guards, so FOO looked taken, and WORDS and MODULES, no lexer tokens, could not be named.
(2) The re-simulation walk the hints read: it evaluated no predicate at any token, so past `SEGMENT-LIMIT` the
CHARACTER CLASSIFICATION clause's path stayed open (its left-edge predicate refuses SEGMENT-LIMIT) and the period
seemed to take a locale-name. PB3861's literal case was hint 15 ("a data-name is expected here, not a literal"),
silenced by PB906 where a literal IS expected, with nothing to say instead.

**The fix.** `ResimulationWalk.AdmittedAt` runs the same full-context walk with every closure evaluating its
predicates at the token it precedes, the way ANTLR's own full-context prediction does (`PredTransition` seeks to
`startIndex`; the walk sets it per closure). Its `EvalSemanticContext` runs each predicate through the parser base's
one word funnel, `CobolParserCoreBase.EvaluateTestingWords`, which records the words compared against the token: a
predicate that refuses with no word matching contributes its words, so the context words a format prints are read by
running the predicate, never listed. `AdmittedKinds` names the result in the standard's terms (keywords, context words,
the §8.3.3.1 literal categories through `LiteralTokens.CategoryOf`, literal families read from the grammar's literal
rules, figurative constants) in three shapes: a closed position names everything expected; a literal where an operand
is taken names the literal categories expected, or none; a user-defined word where only literals are taken says both.
Hint 0i reports it as COBOL5506, after 0h and behind `HasStructuralHint`, and yields where a statement may begin or a
forgotten period would let the next entry follow (`01 W-X PIC X` then `05 W-Y` stays COBOL0307). The sentence says
"expected", because the grammar parses a superset of each format wherever a later check names the departure (INSPECT
tallying FIRST/TRAILING for COBOL2269, a later edition's words at every edition). Hints 15 and 16 (COBOL0308, COBOL0309)
named a kind in two shapes only; both are deleted, and the guesses 3 and 17 now stand behind a structural diagnosis.
The literal-token set moved to `LiteralTokens.Whole`, its one home beside the category.

Now: `MEMORY SIZE 12 FOO` "expected CHARACTERS, MODULES or WORDS"; `SEGMENT-LIMIT IS.` "expected an integer literal";
`SET IX TO "A"` "of the literals, only a numeric literal or the figurative constant ZERO is expected" (§14.9.39.2
Format 1 with §8.8.1.1); `OPEN F` "expected EXTEND, I-O, INPUT or OUTPUT"; `CALL "P" RETURNING 5` "no literal is
expected". Twenty-four existing negatives and test rows that pinned the bare COBOL0001 (or COBOL0308, COBOL0312) now
pin the named COBOL5506; `RequiredImperativeStatementDriftTests` keeps its one-diagnostic-at-every-depth check with
the new code.

**Goldens.** `12.3.6-OBJECT-COMPUTER/pb3428-memory-size-unit-word` and `pb3428-segment-limit-integer-omitted` (refused
at every edition), `pb3428_memory_size_modules_85` (accepted at 85); `14.9.39-SET/pb3861-set-index-to-alphanumeric-literal`
(refused at every edition) and `pb3861_set_index_to_integer_literal` (accepted at every edition). Unit:
`WrittenKindRefusalTests`, with the drift fact that every whole-literal token has a category. Documented as
`docs/CONFORMANCE.md` §3 D-EXPECT and in DESIGN-frontend-grammar.md beside the 0g arm. No inventory row changes
verdict (FMT-12.3.6.2 and FMT-14.9.39.2 were already CONFORMS). Code COBOL5506 used; COBOL5507 and COBOL5508 returned.
