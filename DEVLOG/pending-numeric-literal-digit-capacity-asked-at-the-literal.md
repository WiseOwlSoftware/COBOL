## Numeric-literal digit capacity is asked once, of the literal as written (PB5529)

The edition's digit limit for a fixed-point numeric literal (COBOL0802 above 18 at COBOL-85, COBOL0801 above 31) was asked
per position, by `ExpressionBinder.CheckLiteral`, for a procedure-division literal only. Re-probe on the base build:
`FD F1 VALUE OF ID IS 1234567890123456789.` compiled clean at `--std 85`, and a 32-digit level-88 VALUE drew only the range
check's COBOL1625.

Fix: `LiteralScreen.Run` now asks it of every numeric-literal node, `numericLiteralCore` (a comma-decimal literal is three
tokens, counted as one), `integerLiteral` and `signedIntegerLiteral` (an `integer-n` is a fixed-point integer literal,
§2023:5.5 SR1); a floating-point literal is not its subject. The question is asked of the NODE and not of an INTEGERLIT
token because a digits-only user-defined word lexes as INTEGERLIT too. The 31/18 split of the descriptor lives once, in
`EditionInfo.DigitCapacityExcess`; `EditionContext.CheckDigitCapacity` keeps the PICTURE subject. The binder's call is deleted.
Goldens: `pb5529_literal_capacity_value_clause` (19 and 31 digits, 2002+), `pb5529-value-of-19-digit-literal` (COBOL0802
at 85), `pb5529-value-over-31-digits` (COBOL0801 at every edition). Left: the compile-time evaluator asks the same
question of a CONSTANT operand, so a CONSTANT literal over 31 digits draws COBOL1547 beside COBOL0801 (named in the report).
