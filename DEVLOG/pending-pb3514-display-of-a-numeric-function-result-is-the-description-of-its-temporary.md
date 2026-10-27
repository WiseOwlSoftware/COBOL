## PB3514: DISPLAY of a numeric function result shows the temporary by its description, as GnuCOBOL does

`DISPLAY FUNCTION ABS(S)` printed `12.50` where GnuCOBOL 3.2.0 prints `+00012.50`, `NUMVAL("12.50")` printed `12.50` where it prints
`00000012.5`, and `MAX(U S)` printed `12.50` where it prints `00012.50` (kb/Work PB3514, adjudicated 2026-10-25: a defect by the
precedence of CLAUDE.md rule 1, because ISO leaves the description of the returned-value item to the implementor; owner override
of 2026-10-26 for campaign PB3852). Wave 20261716, group C.

**Re-probe on the wave's build.** Every row of the note reproduced. The cause is one: ISO §15.4 puts a function's returned value in
"a temporary elementary data item", §15.4.1 leaves "the characteristics and representation of the returned value" to the implementor
under native arithmetic, and §14.9.11.4 GR1 leaves the DISPLAY conversion to the implementor. DISPLAY of a numeric data item has been
GnuCOBOL's decimal form since PB3505, but DISPLAY of a function result went through DOC-A.1-92's separate "literal form", which had
been chosen on 2026-07-29 (before the precedence existed) to make the compile-time fold unobservable. A 90-line probe matrix
(every numeric function family over unsigned, signed, COMP-3, COMP, COMP-5, BINARY-DOUBLE, SIGN SEPARATE, P-scaled and
floating-point items, literals, expressions and nested calls) was run through `cobc` 3.2.0 and the new build; its source,
`libcob/intrinsic.c`, was read for the rule.

**The rule GnuCOBOL has.** A function's temporary has one of three descriptions. A function that returns the CONTENT of an argument
(ABS, MAX, MIN, an odd count's MEDIAN, MEAN of one argument) returns a copy of that argument's item: `MAX(U S)` shows what DISPLAY of
the greater item shows, a literal argument is a field of the digits written (`MAX(005 3)` is `005`, `ABS(-12.50)` is `+12.50`), and
ABS keeps the argument's sign position (`+00012.50`). DATE-OF-INTEGER and DAY-OF-INTEGER return an unsigned DISPLAY integer of
eight and seven digits. Every other function's temporary is described by its value (`cob_alloc_field`): the fraction's trailing zeros
dropped, a 4-byte item of 9 positions under 2**31 with a scale under 10, an 8-byte item of 20 positions under 2**63 with a scale under
19, else a DISPLAY item of exactly its digits, signed exactly when negative: `INTEGER(-12.5)` is `-000000013`, `ORD("A")` is
`000000066`, `NUMVAL("0.000000001")` is `.000000001`.

**The fix.** One catalog column, `IntrinsicSig.DeviceForm` (`FunctionDeviceForm`), defaulting to the value-described shape so the next
function needs no edit, with `Argument`, `FixedDigits` and `Literal` (LENGTH, BYTE-LENGTH: the two folds GnuCOBOL makes too) on the
rows that differ. `OperandText.AsDeviceString`, the entry the `--numeric-display` option is read through, asks the new
`FunctionDeviceText`; MOVE, STRING, INSPECT and the comparisons keep the literal form, as a numeric item's MOVE keeps GR6's digits.
For a selecting function `FunctionDeviceText` builds one `FunctionValueField` (value and description) per argument, so each argument,
a nested call and a table(ALL) element is read once and `ABS(INTEGER(X))` copies INTEGER's SIGNED item; the runtime picks, the first
of equal values winning as in §15.59.4 1) and §15.63.4 1). A value-described result reaches `CobolNum.DeviceFunctionText` as a
scaled integer or an SDIDI. The binder records the function it folded, so `LENGTH(X)` of a fixed item stays the literal `7` and
`LENGTH` of a run-time length is `000000003`, exactly as GnuCOBOL's, and HIGHEST-ALGEBRAIC shows its value's description.
`--numeric-display digits` keeps the literal form in DISPLAY.

**Deliberately not copied.** GnuCOBOL's 4-byte temporary has 9 positions, so `NUMVAL("2147483647")` prints `147483647` there; its DISPLAY
temporary takes its digit count from a GMP size estimate that exceeds the true count by one for some values (`-09223372036854775808`).
The positions here are `max(container positions, the value's digits)`: no digit is cut or invented. A binary64 result (the float
family natively, any function given a COMP-1 or COMP-2 item) keeps the floating-point image PB3505 chose, so `SQRT(4)` is `2`; the
100-digit PI stays rejected.

**Side effect.** A selecting function no longer aligns its arguments to a common scale for DISPLAY, so `DISPLAY FUNCTION MAX(BIG F)`
over a 31-digit item prints BIG instead of ending EC-SIZE-OVERFLOW for a representable value. The test that used it to reach the
native receiverless raise (`NativeReceiverlessOverflow_TerminatesLoudly`) now reaches it through a numeric condition.

**What moved.** 138 conformance-suite expected outputs and the expectations of 10 C# tests moved; a tool compiled and ran each failing test as the
runner does and accepted a changed output only when every line kept its non-numeric skeleton and every numeric token kept its numeric
value (width, sign and point spelling only): 138 of 138 qualified, none was a value change. The Unit project (37,989 of 37,998 passed;
the nine were this change's own new drift tests, fixed, and two that need the external GnuCOBOL corpus a fresh worktree lacks) and the
Characterization project passed.

**Goldens.** `pb3514_function_result_device_form` (the 1989 module), `_abs`, `_comma`, `_standard_decimal`, `_not_copied` under
`cases/15.4.1-Numeric-and-integer-functions/`; `wiseowl/cases/14.9.11-DISPLAY/pb3514_function_result_digits_form` (digits twin).
The first three are the lines on which `cobc` and this compiler agree: 140 lines, no difference. Unit: `FunctionDeviceTextTests`,
`FunctionDeviceFormDriftTests`, a reader pin in `NumericDisplayFormDriftTests`. Docs: DOC-A.1-92 rewritten (the withdrawn "internal
consistency over matching GnuCOBOL" paragraph replaced), DOC-A.1-56's function sentence, `INTRINSICS_DESIGN.md`.

**Self-review.** Architecture: one mechanism per job (a catalog column, one renderer, one runtime struct), no second copy of the
description rule. Silent failure: an unknown shape falls to the value's description, never a literal-form fallback that would hide a
missing row; the drift test pins the set of rows against the renderer's cases and DOC-A.1-92. Comments: the stale "literal form"
remarks in the pb2507, pb253 and da2 goldens were rewritten.

**Review round 1 (finisher).** One blocking finding and two leads, all fixed. (1) `FunctionDeviceText.ArgField` sent every folded literal to
`LiteralField`, including a call the binder folds that GnuCOBOL does not (HIGHEST-/LOWEST-ALGEBRAIC), while `Of` described the same fold by
its value at top level: `ABS(FUNCTION HIGHEST-ALGEBRAIC(S))` showed `999` where DISPLAY of the call shows `000000999`. One predicate,
`IsUnfoldedByGnuCobol`, now serves the top level and the argument arm; golden `pb3514_function_result_device_form_folded_wide` (eight lines
identical to cobc 3.2.0). (2) `FunctionValueField.OfValue` kept no description past 38 digits, so ABS of a negative wide expression lost its
sign position; the value shape now keeps the sign position (`CobolNum.DeviceFunctionText(value, signedItem, comma)`), same golden (37 and 39
digits; cobc's extra leading zero is the pinned non-copy). (3) The leftmost-of-equal rule is cited as the spec numbers it, 15.59.4 2) and 15.63.4 2).

**Leads.** `FUNCTION LENGTH(FUNCTION ABS(S))` is 30 here where GnuCOBOL's is the temporary's size (7): DISPLAY adopts the description,
LENGTH does not. `NUMVAL-F` of a text TEST-NUMVAL-F rejects but GnuCOBOL accepts (`"1.5E2"`) answers 0 here, 150 there.

Gate: see the report (leg 1 plus the Linux half).
