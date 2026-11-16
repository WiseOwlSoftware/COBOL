**Review finding R2-1 (2026-11-16, round 2 at 36876b1000).** The 2002/2014 witness of the R1-1 fix could not fail: its second
COPY's identifier operand `X€ OF G` matched nothing in its library text, so the unreplaced library word reached the lexer and
drew the expected COBOL2773 with or without the operand screen.

**Fix.** One COPY per witness, each operand MATCHING its library text so the library word is replaced away and the operand
screen is the only source of the diagnostic: `pb5608-copy-replacing-word-non-cobol-character-refused` (2002, 2014; word),
the new `pb5608-copy-replacing-identifier-non-cobol-character-refused` (2002, 2014; library PB5608I `DISPLAY X€ OF G.`) and
`pb5608-copy-replacing-identifier-non-cobol-character-refused-85` (85; library PB5608J). The identifier witnesses are recorded
on `2002-SR-7.1.2.2-10` and `1985-SR-XII.2.3-6` beside the word witnesses.

**Proof.** With the word/identifier arm of `OperandScreen.Characters` disabled locally, all six refusal cases (word and
identifier at 85, 2002 and 2014) FAILED; with it restored all six PASSED. The accepted case
`pb5608-copy-pseudo-text-coded-set-character-accepted` FAILED at 2014 and 2023 with the 2014 text-word gate disabled
(screening every edition) and PASSED restored. Nothing temporary was committed (`git diff -- src` empty after the restore).
