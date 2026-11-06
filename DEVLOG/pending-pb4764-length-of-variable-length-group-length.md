## FUNCTION LENGTH (FUNCTION LENGTH (VG)) over a variable-length group is accepted

kb/Work PB4764. A bare `FUNCTION LENGTH (FUNCTION LENGTH (VG))` or `BYTE-LENGTH (FUNCTION LENGTH (VG))` over an OCCURS DEPENDING group was refused
COBOL1627 as "an arithmetic expression", though §15.50.3 r1 and §15.14.3 r1 admit a data item of any class and §8.4.3.2.4 r1 makes a function-identifier one.
The re-probe reproduced it. Cause: the function-identifier arm of `BindLengthFold` / `BindByteLengthFold` matched the tree shape (`Expr: BoundIntrinsicCall`),
but a variable-length group's LENGTH lowers to a `BoundBinary` sum. `BoundComputedOperand.FunctionValue` is already the binder's statement that a function
was written, so both arms now ask it alone (parenthesized forms stay refused: the flag is not set for them), and `FunctionDeviceText.IsNumericFunctionResult`
admits the `FunctionValue` `BoundBinary`, whose temporary `Field` describes by its value: GnuCOBOL 3.2.0's 4-byte binary LENGTH item (DOC-A.1-92).
Measured against GnuCOBOL: 3, 4, 4, 4, 4 on every leg, identical.
Goldens: `pb4764_length_of_variable_length_group_length` (accept 2002) and `pb4764-length-parenthesized-variable-length-group-length` (refused, COBOL1627).
No inventory row closes (AR-15.50.3-1 and AR-15.14.3-1 already CONFORM). Self-review: one predicate per arm, no second copy of a rule, no allocation.
