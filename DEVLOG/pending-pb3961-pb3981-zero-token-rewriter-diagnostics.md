## PB3961, PB3981: the ZERO rewriter's two diagnostic defects, a message that spells the source and a sign position the parse decides

Group F of wave 20261769, one root file (`ZeroTokenRewriter.cs`), two mechanisms.

**PB3961** (`PERFORM VARYING X FROM 1 BY ZERO + 1` reported `'0+1' is not one of the operands`, `BY ALL ZERO + 1`
reported `'ALL0+1'`). Re-probe on `802352531` reproduced both. The rewriter gives `ZERO_ARITH` the text "0" on purpose,
because readers of an arithmetic operand parse its text (`IntegerOperandRules.TryHostValue(spec.length.GetText())`), so
the fix is at the quoting sites: `BindVaryingOperand` now quotes `WrittenSource.Of(op)`, the one "as written" reader. The
sibling sweep read the 196 message sites that interpolate `GetText()` and moved every user-facing refusal that quotes an
expression or operand node (ArithmeticBinder, ConditionBinder, CallBinder, IntrinsicBinder, SearchAllFormat2Rules) to
`DataBinder.WrittenText`. `DataBinder.AsWritten`, a second copy of the same char-stream slice reader whose fallback glued
the tokens, is deleted and its five callers use `WrittenText`. Goldens: `pb3961-varying-by-zero-expression` and
`pb3961-varying-by-all-zero-expression`, refused at all four editions with the message text as `expect`.

**PB3981** (`COMPUTE X = ZERO -1` was a generic COBOL2269 while `COMPUTE X = Y -1` was the precise COBOL2993). PB3962
keeps a ZERO figurative before a sign touching its literal, right in an operand list and wrong where the grammar wants an
arithmetic expression. No token test tells the positions apart, so the parse does: `ZeroSignReading` (new), driven from
`ReservationGateRewriter.ParseToFixpoint`, turns a figurative ZERO whose touching sign is a syntax-error offender into
`ZERO_ARITH` and parses again, and takes the ZERO back (never retried) if the arithmetic reading fails at the ZERO or its
sign too, so `MOVE ZERO -1 TO X` still reports `unexpected '-'` at the sign. It runs only on the error path and only on a
pass the reservation gate accepts; operand-list positions (`DISPLAY ZERO -1`, `ADD ZERO -1 TO X`, VALUE lists) parse
first time. Probed: `IF X = ZERO -1`, `IF ZERO -1 = X`, `COMPUTE X = (ZERO -1)`, `EVALUATE X WHEN ZERO -1` and
`PERFORM UNTIL X = ZERO -1` are now COBOL2993 too. The drift test that forces every whole-group parse through
`ParseToFixpoint` makes the decision automatic for the next front end. Goldens: `pb3981-compute-zero-touching-sign` and
`pb3981-relation-zero-touching-sign` (COBOL2993 at all four editions); unit tests `ZeroSignReadingTests` pin the flip,
the take-back and the end of the loop. `DESIGN-frontend-grammar.md` (the gate-loop paragraph and section 7) states both.

Rows re-verdicted: none (both notes claim none). Self-review: architecture (one mechanism per job; the new class holds
the one decision, the loop stays the one parse entry), full code (the retry criterion is the offending token at the ZERO
or its sign), performance (zero cost on a clean parse), duplication (`AsWritten` removed).
