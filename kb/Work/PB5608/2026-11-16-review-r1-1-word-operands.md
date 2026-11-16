**Review finding R1-1 (2026-11-16, round 1 at bfc60bd5a3) and its fix.** The character screen ran only over
`==pseudo-text==` operands, so below 2014 `COPY RVX REPLACING X€ BY "Z".` (word-1 X, U+20AC) compiled and printed Z at
85 and 2002 (finisher re-probe `rv4-001`/`rv4-002`, exit 0), while the same characters as pseudo-text were COBOL2572.

**Fix.** `ScreenOperandPair` now asks ONE question of every operand of COPY REPLACING and of REPLACE,
`OperandScreen.Characters`, which judges each operand by what its form is made of: pseudo-text text-words by the
edition's text-word repertoire (`TextWordCharacters`, now private to it); each word of a word-1/word-2 or identifier operand
by the COBOL-word character rule (`WordCharacterScreen.CheckWord`, the rule the token walk and the text-name screen ask),
because word-1/word-2 "may be any single COBOL word except 'COPY'" (§1985:XII.2.3 SR6; §2002:7.1.2.2 SR10;
§2014:7.2.2.2 SR10) and a COBOL word's characters are §1985:IV.4.2.2.1.1's / §2002:8.3.1.1's / §2014:8.3.1.1's; a literal by
none. The word rule still holds at 2014, where a text-word takes the coded set, so the word operand X€ is refused at 2014
too (COBOL2773) and only the pseudo-text form is accepted there.

**After, on the branch's build** (run_probe, root `E:\COBOL-coord\scratch\finish-worktree-wf_1d5ba7f2-7a4-9\runs`): the
word operand X€ and the identifier `X€ OF G` are COBOL2773 at 85, 2002 and 2014 (rv4-004..006, rv5-001..003); the word XÉ (an
Annex B extended letter) is COBOL0900 at 85 and accepted at 2002 and 2014 (rv6); `==X€==` is COBOL2572 at 85 and 2002 and
prints Z at 2014 (rv7); a subscript `E (I - 1)` is untouched (rv8, prints A).

**Witnesses.** `pb5608-copy-replacing-word-non-cobol-character-refused-85` (fixed form, 85, on `1985-SR-XII.2.3-6`),
`pb5608-copy-replacing-word-non-cobol-character-refused` (2002 and 2014, word and identifier, on `2002-SR-7.1.2.2-10`), and
`pb5608-copy-pseudo-text-coded-set-character-accepted` (2014 and 2023).
