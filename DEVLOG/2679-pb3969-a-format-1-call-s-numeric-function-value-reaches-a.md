## Entry 2679 — 2026-10-27 17:09 PDT — PB3969: a Format-1 CALL's numeric function value reaches a DISPLAY, binary or character formal as its value, not as its container's image

`CALL "Q6X" USING BY CONTENT FUNCTION MAX(W 0)` with `W PIC S9V99` = 1.25 showed `+0.00` in the callee's `PIC S9V99` formal.
Re-probed on a fresh build: the same value showed `000.00` in a `PIC 9(3)V99`, `+8084644.32` in a `PIC S9(4)V99 COMP` and `00000`
in a `PIC X(5)` (for `FUNCTION INTEGER(W)`, which showed `+0.00` where 1.00 is right); `--permissive` `BY CONTENT (1.25 + 0)` showed
`+000.00` by the same path. The note's suspect, `ArgCarrierText`'s computed arm, is correct: the value crosses as an `Int128` cell at
its scale with a 38-digit profile. The loss is in the callee. A Format-1 CALL is ISO 14.2.3 GR9's first regime, whose BY CONTENT
argument passes untouched (`ProgramTable.LandArguments` landed it only for a program with a program-specifier), and a DISPLAY
numeric formal's adapter (`CobolArgAdapt.Text`) reads the argument's IMAGE: the first three characters of a 38-digit zoned image.

The standard gives the first regime nothing to move "without conversion" here: a function's returned value is "a returned value
in a temporary elementary data item" (15.4) whose "characteristics and representation" are implementor-defined under native
arithmetic (15.4.1; cite.py --check run on both). That is implementor latitude, so CONFORMANCE D-B4 now states the choice: the
value is the sender and the formal's description the receiver, as the second regime and GR10 say for a numeric formal (a COMPUTE
without ROUNDED) and as `MOVE FUNCTION f TO formal` does for a character one.

Fix: `MoveSending.Computed` marks a computed numeric value (`CobolArgAdapt.WithValueSending`'s fixed-point and binary64 arms,
`NumericSending`, hence `SdidiValue`), and `ProgramTable.LandArguments` lands a BY CONTENT argument so marked through the formal's
registered landing in both regimes, as it already lands every BY VALUE one. A plain-group formal registers no landing and keeps
the value's digits (D-B4, unchanged). The sweep covered the SDIDI cell, the binary64 cell, a dynamic `CALL name`, and float,
numeric-edited, packed, PIC X and group formals: all probed correct after the fix. The INVOKE lane and a CALL whose callee
signature is known land at the caller and were already right.

Golden `pb3969_format1_numeric_function_value` (accepted from COBOL-85: BY CONTENT is an 85 rule, so no program is refused
below it, hence no negative); five facts in `ComputedArgumentSendingTests`. Docs: CONFORMANCE D-B4, INTERPROGRAM_DESIGN. The note
claimed no inventory row and closes none. Self-review found nothing further.

Branch review round 1 found the unswept sibling: a numeric function the binder folds to a literal (`FUNCTION LENGTH`) took the
literal arm of `CallEmitter.ElementarySending`, whose sending was not marked Computed, so a Format-1 `CALL "P" USING BY CONTENT
FUNCTION LENGTH(W2)` showed `5   ` in a PIC 9(4) formal. `ElementarySending` now marks a `BoundNumericLiteral { FunctionValue: true }`
Computed (`RuntimeApi.MoveSendingNew(computed:)`); the golden gained program PB3969E (LENGTH of a numeric and an alphanumeric item
into PIC 9(4): 0005, 0003). The activation-boundary doc comments now say a computed BY CONTENT argument is landed in GR9's first
regime too.

