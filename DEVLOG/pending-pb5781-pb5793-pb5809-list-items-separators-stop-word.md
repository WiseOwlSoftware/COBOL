## PB5781, PB5793, PB5809: a list's items as the declarations read them, the separator as the space it is, STOP with a symbolic-character

Wave 20262074 group A, three 1985 refusals of legal source around the expression grammar's lists. Each re-probed on
ce3dceb0db before any change; each reproduced as filed.

**PB5781 (MAJOR).** `FUNCTION MIN (A (- B))`, A no table, was refused at every edition: COBOL2096 on `'A (- B)'` and, at
`--std 85`, COBOL0900 on the "arithmetic-expression subscript - B". The lexer types the '(' after a data-name-capable word
as that reference's, so the parse hangs it on A; only the subscript list ever asked the declarations whether A could own
it (kb/Work PB136's cut, read by the direct position walk alone). §1985:VI.6.2.3 SR4 (§2023:8.8.1.2 GR5) puts a unary-led
expression that follows an identifier in its own parenthesis, and §2023:8.4.2.3.3 SR2 allows a subscript only on a table
element, so the paren is the second argument. The cut is now ONE list reader, `SubscriptBinder.GiveAwayClosingList`, asked
by the subscript list and by both function-identifier forms' argument lists (`IntrinsicBinder.ArgumentsOf`), and the paren
it gives away is marked on the parse node (`SubscriptPartOwnership.GiveToNextItem`): `ReferenceResolver.ReadWritten` reads
the reference without it and `VersionConformancePass` asks it no subscript form, so every reader agrees. `PositionSegment.Cut`
is deleted, and a materialized subscript whose head carries a cut (`D2 (X / 1 (B - 1))`) now binds. A given-away list that
holds no expression is COBOL1543 (§2023:8.4.3.2.3 SR8). Goldens: `g85_bf15_01` relanded (E8=-0003, E9= 0004),
`pb5781_argument_after_non_table_name` (2023), negative `pb5781-parenthesized-argument-not-an-expression`; drift fact
`EveryArgumentListReader_AsksTheOneListReader`. GR-8.8.1.2-5 re-affirmed with the argument witnesses; 1985-SR-VI.6.2.3-4
closed (1985 GAP 23 → 22). Left open, reported for a note: CALL and INVOKE USING and an inline invocation's argument list
do not ask the reader (`CALL "C2" USING BY CONTENT A (- B)` is still COBOL2096 at 2023).

**PB5793.** `W-E (I +, 1)`, `W-E (, I)` and `W-E (I, )` were COBOL0001 at every edition: inside a list region the lexer
kept every `, ` / `; ` as FNARG_SEPARATOR, which the grammar admits only between two items. The separator is the space
(§1985:IV.4.2.1 SR2, §2023:8.3.5 SR2), so FNARG_SEPARATOR's action now keeps it only at a boundary between two items
(`CobolLexer.ListItemBoundary`: not after an opening paren, operator, colon, OF/IN or separator, the set `ListOperandFollows`;
not before ')' or a spaced operator) and skips it as the space elsewhere. The argument list and the reference modifier had
the same shape and are fixed by the same decision. Goldens `pb5793_list_separator_where_space` (1985) and `_2023`; lexer
facts pin every member of the set. SR-8.3.5-2 and 1985-SR-IV.4.2.1-2 re-affirmed with the list-region witnesses.
The branch review (round 1, R1-1) found the fix one-sided: a separator was skipped AFTER a qualifier connective or word
boolean operator but kept BEFORE one, so `W-E (I, OF G)` and `FUNCTION MAX (A I, OF G)` were still COBOL0001 while
`W-E (I OF G)` compiled. The decision now also reads the word ahead (`CobolLexer.InfixWordAhead`, the set `InfixWords`:
OF, IN and the binary boolean operators; B-NOT, unary, begins an operand and keeps the boundary), matched as a whole word
so `OFX` and `INDEX-1` stay names. Both goldens gained list-region qualifier lines (`W-E (K, OF H)`, `MIN (A, K, IN H)`),
and `ListSeparator_BeforeAnInfixWord_IsSkipped` pins every member.

**PB5809.** `STOP STAR.`, STAR a symbolic-character, was a parse error expecting '&' (the literal rule spells a word only as
a concatenation operand). A symbolic-character is a figurative constant (§1985:IV.4.2.2.2.3 GR7, GRL2.3), so the grammar
has the superset arm `STOP cobolWord` and `ControlFlowBinder.BindStopWord` communicates its one character (GRL2.2); any
other word is refused by name at 85 with the new COBOL7558 (the refusal test `1985-FMT-VI_6_24_2` expected the parse error
and now expects it), and from 2002 the edition gate's COBOL0902 covers both arms. The other literal slots, swept by probe,
already read a symbolic-character. Golden `pb5809_stop_symbolic_character`. The word arm exposed a latent error-recovery
defect, fixed in the same change: `CobolErrorStrategy.ReportError` read the OFFENDING token as the name and re-parsed even
when the report named an earlier word (a reserved word written as a procedure-name, kb/Work PB4229), so the period of
`PERFORM STOP.` was retyped as an IDENTIFIER and the `STOP RUN.` after it refused (a false COBOL2269, caught by
`ReservedWordPositionConformanceTests` at gate run 000002); the re-read now requires the named word to be the offending
token. The same gate run also caught a stale inventory witness (`IntrinsicBinder.cs#ArgsOf`, retired from FMT-15.49.2), the
documented diagnostic count, and the parse-node mark, first written as a partial of the generated context type (the
architecture census counts that type as generated) and now a weak annotation, `SubscriptPartOwnership`.

Self-review (architecture, code, performance, duplication): one list reader for both list kinds and one mark every reader
honours, instead of a field the direct walk alone read; no allocation added on the argument path when nothing is given away
(the parse's list is returned); the lexer decision is an action, not a predicate (kb/Work PB1715). Codes: COBOL7558 used;
COBOL7559 and COBOL7560 returned.
