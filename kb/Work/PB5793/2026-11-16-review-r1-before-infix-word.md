**Review finding R1-1 (branch review round 1, wave 20262074 group A), fixed by the review finisher.** The landing fix
skipped a separator AFTER a qualifier connective or a word boolean operator (`ListOperandFollows` holds OF, IN, B-AND …)
but its lookahead tested punctuation only, so a separator written BEFORE one stayed a list boundary: `W-E (I, OF G)`,
`W-E (I, IN G)` and `FUNCTION MAX (A I, OF G)` were COBOL0001 at --std 2023 and --std 85 (the reviewer's probes
`q1.cob`, `q3.cob` under `E:/COBOL-coord/scratch/review-w20262074-a/src/`), while `W-E (I OF G)` compiled. §2023:8.3.5 SR2
makes the separator usable anywhere the separator space is. The decision now also reads the word ahead
(`InfixWordAhead`, the set `InfixWords`: OF, IN, B-AND, B-OR, B-XOR and the four shifts; B-NOT is unary, begins an
operand, and keeps the boundary), matched as a whole word, so `OFX` and `INDEX-1` stay names. After the fix `q1.cob`
prints `S1=Y S2=Y F1=04` at both editions.

**Sites:**
- `src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolLexer.g4` (`ListItemBoundary`, `InfixWords`, `InfixWordAhead`, `IsWordCharacter`)

**Witnesses.** Golden lines Q2-Q5 of `pb5793_list_separator_where_space` (1985) and L5-L6 of
`pb5793_list_separator_where_space_2023`; `CobolLexerModeDriftTests.ListSeparator_BeforeAnInfixWord_IsSkipped` (every
member of `InfixWords`) and the new rows of `ListSeparator_IsKeptOnlyBetweenTwoItems` (whole-word match, B-NOT boundary).
