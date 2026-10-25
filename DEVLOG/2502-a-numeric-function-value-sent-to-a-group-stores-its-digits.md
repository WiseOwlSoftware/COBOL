## Entry 2502 — 2026-10-24 19:03 PDT — A numeric function value sent to a group stores its digits, in a MOVE, a CALL and an INVOKE (PB3613)

PB3570 decided that a numeric literal sent to a group stores its digits only, and left the numeric FUNCTION value as a lead:
`MOVE FUNCTION MIN(N 3) TO G` (N = -15) stored `-15` and `FUNCTION MIN(M 3)` (M = -1.5) stored `-1.5`.

**What the re-probe showed.** The note reproduced, and the same value was wrong on every channel that sends into a plain group. A
function the compiler folds to a literal (`FUNCTION MIN(-15 3)`) kept `-15`, because PB3570 exempted it on purpose. `CALL ... AS NESTED
USING BY CONTENT FUNCTION MIN(N 3)` into a plain-group formal was refused (COBOLNET1688, "an arithmetic expression has no character
image"): `ActivationDescriptions.OfValue` gave a numeric function the arithmetic expression's shape, though a function-identifier is a MOVE
sender (14.9.25.4 GR1) and 14.8.2.2 rule 2 makes a BY CONTENT group argument conform "the same as for a MOVE statement". A Format-1 CALL
crossed the value cell's 38-digit zoned image (`000000` in a six-position formal), and an INVOKE crossed the sending-value temporary's own
signed text (`-15`, `-1.500`).

**The determination.** A numeric or integer function has "an operational sign" and no usage (15.2 4)-5), and 15.4.1 leaves the returned
value's "characteristics and representation" to the implementor under native arithmetic, so GR4's "no conversion of data from one form of
internal representation to another" has no representation of it to preserve, exactly as for a literal. PB3570's owner determination (the
digits of the value, no sign, no decimal point) therefore covers the value too. GnuCOBOL sends the function's own internal field instead
(`1u` for MIN over an S99, a 4-byte binary for INTEGER, `15` for ABS, measured with cobc 3.2.0), which is no rule for a value the standard
gives no representation, so the value follows the literal. D-B4 in `docs/CONFORMANCE.md` now says so.

**The fix, one digits rule.** `CobolNum.GroupDigits` (runtime) is the one reader of "digits only, significand digits for a float": the
compiler calls it on a literal's canonical text, the emitted code on a function value's run-time text, so a folded call and the same call
evaluated at run time send the same characters, and `OperandText.LiteralDigits` is gone. `OperandText.NonElementaryMoveSender` takes
every numeric literal (the `FunctionValue: false` exemption is deleted) and a numeric computed operand. The CALL lane describes a numeric
function as a MOVE sender (`ActivationShape.Literal`, the arithmetic expression keeps `Expression` and stays refused into a group), sends
it through the one sender into a known plain-group formal, and `CobolArgAdapt.WithValueSending` states the digits as the argument's
`StorageImage` for a formal the activating element cannot see. The INVOKE binder keeps a numeric function on the expression arm for a
plain-group formal and the group arm of `OoEmitter` sends it.

**Goldens.** `pb3613_numeric_function_group_move` and `..._comma` (MOVE; folded and evaluated calls, truncation, leading zeros,
DECIMAL-POINT IS COMMA), `pb3613_numeric_function_group_formal` (CALL AS NESTED, Format-1 CALL, INVOKE) and its negative
`..._below_2002`. `GroupMoveSenderChannelDriftTests` pins both arms and the absence of a second digit filter.

**Rows.** No inventory row claimed or closed: the landing adds witnesses to rules already CONFORMS (`closes_rows: []` with its reason in
`kb/Work/PB3613.md`).

**Branch review, round 1.** One blocking finding, a sibling the sweep missed: FUNCTION LENGTH of a variable-length group is lowered to a
`BoundBinary` sum, indistinguishable from an arithmetic expression, so `CALL ... AS NESTED USING BY CONTENT FUNCTION LENGTH(VG)` into a plain-group
formal was still refused (COBOLNET1688) while MOVE and a Format-1 CALL stored `5`. The binder now says which it is once
(`BoundComputedOperand.FunctionValue`, set by `IntrinsicBinder.OperandOf(expr, functionValue)`, whose parameter is required so a new position
must decide); `IsNumericFunctionValue` is the one reader for the activation description, the CALL arm, the INVOKE guard and emitter
(`BoundInvokeArg.ContentFunctionValue`) and the sending codec, and `DataItem.IsPlainGroup` is the one "plain group" test (the INVOKE binder guard
wrote the predicate inline beside `CallEmitter.IsPlainGroupFormal`; that method is deleted and five other inline copies now read the property).
`pb3613_numeric_function_group_formal` gained the LENGTH(VG) case on the three lanes; `pb3613_arithmetic_expression_group_formal_refused`
pins that a real arithmetic expression stays refused. `GroupMoveSenderChannelDriftTests` pins both single sources. The summary of
`NonElementaryMoveSender` now says a numeric literal or function value sends digits only.

**Self-review.** The sibling sweep found the arithmetic-expression/function ambiguity above. Left open, as a lead: a STANDARD-DECIMAL function
(COMP-2 argument) is cut to the receiver-less 6 fraction digits in a Format-1 CALL, so its digits there differ from AS NESTED and MOVE.

**Gate.** The first gate of the review fix read RED on one drift test whose guard string named the deleted `CallEmitter.IsPlainGroupFormal`
(`Gr4GroupDecisionDriftTests`, now guarding `a.Formal.IsPlainGroup`); the re-gate, run `000002-20261025T013211Z`:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run — Conformance 8,367/18,407 · Unit 36,832/38,153 · Characterization 36/36 cases ran (skipped 0) in 1 of 2 leg(s) · Linux half (WSL clone of HEAD) GREEN ===`;
`scripts/semgrep/verify.py` PASS (no rule up).

