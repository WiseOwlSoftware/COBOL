## PB4635: a parenthesized item or literal is an arithmetic expression to the intrinsic argument screens

`FUNCTION FIND-STRING(X9 "ABC" (I2))` compiled and printed 0007, although §15.37.3 3) asks argument-3 for "an integer data item or
integer literal" and `(I2)` is an arithmetic expression enclosed in parentheses (§8.8.1.1, §8.8.1.2 1)). Re-probe on this build
reproduced it. The expression binder drops grouping parentheses, so `(I2)` bound to I2's `BoundNumRef` and `(3)` to the literal, and
`IntrinsicBinder.BindArgOperandCore` mapped them through `OperandOf` to a field or literal operand that the
`DataItemOrLiteralOnly` predicate (FIND-STRING argument-3, BASECONVERT arguments 2 and 3) admitted.

Fix: the intrinsic argument binder asks the written shape (`SoleOperand.Unparenthesized`, as the function-prototype activation arm
has since PB3760) and wraps a parenthesized sole item or literal as the `BoundComputedOperand` it is. The predicate now refuses
it, COBOL1627; numeric positions keep evaluating the parenthesized form (`ABS((I2))`, `MOD((7) (I2))`). A parenthesized
alphanumeric item as a string-function argument (`TRIM((X9))`, `LENGTH((X9))`) is refused too, by its own class rule, because it
is an arithmetic expression.

Goldens: `pb4635_parenthesized_numeric_arguments` (accept 2023), negatives `pb4635-findstr-parenthesized-item` and
`pb4635-baseconvert-parenthesized-literal`. No inventory row re-verdicted.

Review round 1 found two blockers in the first wrap. (R1-1) It bound the parenthesized item with the function-argument context,
which suspends the section 8.8.1.1 class screen, so `ABS((X9))` over a PIC X item digit-decoded (and so did a numeric-edited, group,
index, pointer or boolean item). The binder now binds every argument that is not a sole unparenthesized operand through the screened
`BindExpr`; the same change refuses the older `ABS(X9 + 1)`, which digit-decoded under STRICT before this branch. (R1-2) The wrapped
operand answered false to `IsIntegerOperand`, so `ABS((I2))` resolved NUMERIC (`PERFORM FUNCTION MAX((I2) 1) TIMES` became COBOL1646,
`DISPLAY FUNCTION ABS((I2))` printed 000000002). `IntrinsicResultType.IsIntegerArgument` reads a parenthesized sole integer item or
literal as the always-integer arithmetic expression it is (section 15.3 item 6); the shared `IsIntegerOperand` is untouched so the
relation screen and PERFORM TIMES still refuse a parenthesized item (PB1935). Also fixed: the comment quote in the BASECONVERT negative
and the stale `BindArgOperand` summary. New goldens: `pb4635-abs-parenthesized-alphanumeric`, `pb4635-abs-compound-alphanumeric`, and
INTTYPE/TIMES lines in `pb4635_parenthesized_numeric_arguments`.
