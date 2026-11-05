## PB4712 - a parenthesized numeric or boolean function is no data item to FUNCTION LENGTH and BYTE-LENGTH

**What the re-probe showed.** On the wave's build `LENGTH((FUNCTION ABS(I2)))`, `BYTE-LENGTH((FUNCTION ABS(I2)))` and
`LENGTH((FUNCTION BOOLEAN-OF-INTEGER(5 4)))` compiled and printed 0004 each, while `LENGTH((FUNCTION TRIM(X9)))` and `LENGTH((X9))` were
COBOL0844. PB4635 had made a parenthesized item an arithmetic expression (ISO 8.8.1.1) everywhere an argument is bound, and the
review of that branch (R1-4) saw that the two length folds still admitted the parenthesized numeric and boolean functions.

**Mechanism.** The folds' function-identifier arm matched the tree, `BoundComputedOperand { Expr: BoundIntrinsicCall }`. The expression
binder drops grouping parentheses, so `(FUNCTION ABS(I2))` binds to the call the bare function binds to, and the arm took an arithmetic
(or boolean, 8.8.2) expression for "a data item of any class or category" (15.50.3 r1, 15.14.3 r1; the function-identifier is a temporary
data item, 8.4.3.2.4 r1). The binder already states what was written: `IsSoleFunctionIdentifier` sets `BoundComputedOperand.FunctionValue`
(PB3678). Both folds now ask that flag. The parenthesized function reaches the catch-all, COBOL1627, and `WrittenForm` names a
parenthesized boolean function a boolean expression.

**Siblings.** Every other reader of `{ Expr: BoundIntrinsicCall }` was swept; the data-item screens already ask `IsDataItem`
(`FunctionValue`), the others read the call's category, which a parenthesis does not change. One sibling found in the same arm and left
(too large for the slice, a lead in the PB4712 note): the bare `LENGTH(FUNCTION LENGTH(VG))` over a variable-length group is refused as an
arithmetic expression because its tree is a `BoundBinary`; widening the arm to `FunctionValue` alone compiled it and died at run time, so the
arm keeps `Expr: BoundIntrinsicCall` until the renderer measures that temporary.

**Goldens.** Accept `pb4712_function_identifier_length_arguments` (2002: the bare numeric, boolean and alphanumeric function arguments,
and `ABS((I2))`, whose parentheses belong to the inner call). Refusals (2023) `pb4712-length-parenthesized-numeric-function`,
`pb4712-length-parenthesized-boolean-function`, `pb4712-byte-length-parenthesized-numeric-function`. No inventory row closes (AR-15.50.3-1
and AR-15.14.3-1 already read CONFORMS).

**Self-review.** Architecture: the rule stays in the binder, asked of the one flag the binder sets; no C# text in a bound node. Full
code: the arms implement 15.50.3 r1 and 15.14.3 r1 as cited (checked with `cite.py`); LENGTH and BYTE-LENGTH agree. Performance: no
allocation added. Duplication: the two arms keep the one shared pattern; the comment in the LENGTH arm explains it for the twin.
