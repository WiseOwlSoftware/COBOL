## Entry 3646 — 2026-11-14 20:42 PST — Numeric-literal digit capacity is asked once, of the literal as written (PB5529)

The edition's digit limit for a fixed-point numeric literal (COBOL0802 above 18 at COBOL-85, COBOL0801 above 31) was asked
per position, by `ExpressionBinder.CheckLiteral`, for a procedure-division literal only. Re-probe on the base build:
`FD F1 VALUE OF ID IS 1234567890123456789.` compiled clean at `--std 85`, and a 32-digit level-88 VALUE drew only the range
check's COBOL1625.

Fix: `LiteralScreen.Run` now asks it of every `numericLiteralCore` node, the rule of every numeric literal slot (a
comma-decimal literal is three tokens, counted as one); a floating-point literal is not its subject. It is asked of the
NODE and not of an INTEGERLIT token because a digits-only user-defined word lexes as INTEGERLIT too, and the `integer-n`
slots (`integerLiteral`) are left as they were: a DYNAMIC LENGTH LIMIT over the implementor maximum is legal and reported
(COBOL2027, PB463), and the first gate showed this change refusing a 41-digit one. The directive lane's 32-digit
PARAMETER value is now refused by the screen (COBOL0801) before the evaluator's own check (COBOL1619) is reached.
The 31/18 split of the descriptor lives once, in
`EditionInfo.DigitCapacityExcess`; `EditionContext.CheckDigitCapacity` keeps the PICTURE subject. The binder's call is deleted.
Goldens: `pb5529_literal_capacity_value_clause` (19 and 31 digits, 2002+), `pb5529-value-of-19-digit-literal` (COBOL0802
at 85), `pb5529-value-over-31-digits` (COBOL0801 at every edition). Left: the compile-time evaluator asks the same
question of a CONSTANT operand, so a CONSTANT literal over 31 digits draws COBOL1547 beside COBOL0801 (named in the report).

Review round 1 (R1-1): the capacity screen ran in an OMITTED conditional-compilation branch too, refusing a 32-digit `>>DEFINE` there with COBOL0801 where the base compiled it. §2023:8.3.3.3.2 makes 31 digits a minimum an implementor shall allow, not a syntax rule, so `LiteralScreen.Run` now asks the capacity only under `gateFormats` (a compiled branch), like the format gates. Test: `DirectiveLiteralScreenTests.NumericLiteralDigitCapacity_IsAskedInACompiledBranchOnly`. Five stale binder comments that still named the edition digit cap were corrected (R1-2).

