## PB4766: a bare counter register at CONCAT and BASECONVERT argument-1 now has its image

**Mechanism.** `IntrinsicRenderer.StrArgVisitor.Visit(BoundComputedOperand)` handled a nested string function, a parenthesized
operand, a numeric function and a compound arithmetic expression, and sent a LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER
register to the loud arm. The binder admits the register bare at CONCAT (ISO §15.18.3 r1) and BASECONVERT argument-1 (§15.12.3 r1)
because it is "a temporary unsigned integer data item of class and category numeric" (§8.4.3.14.4 r1), so the source compiled
clean and aborted at run time (the wrong-stage family).

**Re-probe.** `CONCAT(LINAGE-COUNTER "A")` and `BASECONVERT(LINAGE-COUNTER 10 2)` after OPEN OUTPUT of a LINAGE 20 file:
`NotImplementedCobolFeatureException: intrinsic string argument 'BoundComputedOperand'`.

**Fix.** The arm renders a counter register through the one register image `OperandText.AsString` already gives it in a string
context: an unsigned integer item's digits zero-filled to `AlgebraicRanges.CounterRegisterDigits`. It does so unconditionally,
as the field arm renders a numeric item; a position that bars numeric items refuses the register at the binder, and `--permissive`
coerces it (`TRIM(LINAGE-COUNTER)` printed `01` after the fix and died before). Now `CONCAT [01A]`, `BASECONV [1]`.

**Goldens.** `pb4766_counter_register_concat_baseconvert_argument` (2023) and the refusal
`pb4766-counter-register-concat-below-2023` (2014, COBOL1502). No inventory row closed (`closes_rows: []`).
