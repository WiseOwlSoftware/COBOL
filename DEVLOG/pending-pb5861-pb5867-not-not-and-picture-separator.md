## PB5861 discharged by PB5784, and PB5867: a comma or semicolon separator between PICTURE and its character-string

Wave 20262095, group A: two legal 1985 source forms the front end refused.

**PB5861 (`IF A > 1 AND NOT NOT < 5` at `--std 85`) was already fixed.** The re-probe on the branch build printed the
note's expected `C1 T`, `C2 F`, `U1 T` at 85, and COBOL0902 (not the generic COBOL0001) at 2023. kb/Work PB5784, the same
mechanism found a batch earlier on row `1985-GR-VI.6.3.3-L2.1`, had landed in 9878a25d08; PB5861 was read at a pin before
that landing. No code changed. The branch adds the witnesses the brief asked for: an 85 sweep
(`pb5861_not_not_relation_sweep_85`: the six words after the second NOT, OR, the form mid-condition and at the end,
parentheses, EVALUATE, PERFORM UNTIL, SEARCH WHEN, each value from `§1985:VI.6.3.3` GRL2.1, GRL2.2 and SR2) and a refusal
at 2014 and 2023 (`pb5861-not-not-relational-operator-2023`, COBOL0902, `§2023:8.8.4.12.2` and the `§2023:8.8.4.12.4 GR1`
note). The 2002 text bars the form too (`§2002:8.8.4.2.4.2 SR3`, "The word NOT shall not be followed immediately by the
word NOT or the words IS NOT").

**PB5867: PICMODE's separator space is now every spelling of it.** `§1985:IV.4.2.1 SR2` and `§2023:8.3.5 SR2` let a comma
or semicolon immediately followed by a space stand anywhere the separator space is used. The lexer's `PIC_WS` skipped only
spaces and line ends before the character-string, so `PICTURE; 9(3)`, `PIC, 999` and `PICTURE IS, X(2)` were refused at
every edition. `PIC_WS` now takes `[,;] [ \n]` beside the space, and PIC_STRING's action (moved into the member
`OnPictureString`) hands a match that is the word IS, once its trailing separator is given back, to `OnPictureIs`, since
`IS,` outran PIC_IS. `PIC ,999` is unchanged: no space follows the comma, so it is the string `,999`. The sweep found no
other mode or character-scanning member that skips spaces without the separator: DEFAULT mode's `, ` is FNARG_SEPARATOR
(kb/Work PB5793), and the CURRENCY SIGN clause's PICTURE SYMBOL phrase, a REPLACE substitution, a continuation line and
free-form source all reach the fixed PICMODE. Witnesses `pb5867_picture_separator_85`, `pb5867_picture_separator_2023` and
the unit theory `LexerTotalityDriftTests.ASeparatorCommaOrSemicolonBeforeThePicture_IsTheSeparatorSpace` (11 inputs);
`DESIGN-frontend-grammar.md` 3.3c states the rule.

**Rows.** `1985-SR-VI.6.3.3-2` and `1985-SR-IV.4.2.1-2` close (each note's `closes_rows`) and gain their witnesses; both
are now owed their re-read (`code_reads.py plan`). `SR-8.3.5-2` (CONFORMS), `GR-8.8.4.12.4-1` and `FMT-8.8.4.12.2` gain
witnesses. GAP unchanged (1985: 6, 2023: 5).
