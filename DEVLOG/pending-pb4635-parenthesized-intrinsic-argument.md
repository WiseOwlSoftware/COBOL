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
