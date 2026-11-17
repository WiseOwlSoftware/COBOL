## PB5886: the integer-argument screen fails closed, so a quotient with a function term is refused

**Re-probe.** The note's `open3.cob` compiled and ran at `--std 85` and `--std 2023` on main ed7d9b6d97 (exit 0):
`FACTORIAL (FUNCTION ORD (A) / 2)` returned 33! for 33.5, and `MOD (7, FUNCTION ORD (A) / 4)`, `MOD (N * N * N / 2, 7)`,
`CHAR (FUNCTION ORD (A) / 2)`, `RANDOM (FUNCTION ORD (A) / 2)`, `FACTORIAL (FUNCTION MOD (N 3) * 0.5)` and
`FACTORIAL (FUNCTION INTEGER (N) / FUNCTION INTEGER (M))` were truncated silently. COBOL-85 has no EC-ARGUMENT-FUNCTION, so nothing
caught them there.

**Root cause.** `IntrinsicArgumentRules.NotAlwaysIntegral` refused an arithmetic-expression argument only where it could name a
valuation with a non-integer value, and admitted every term it could not classify. §2023:15.3 AR6 and §1989:A.2.2 AR4 put the
requirement on the program ("shall be specified", "must be specified"), so a screen that admits what it cannot show is open by
construction; PB124, PB617, PB5208, PB5345 and PB5832 had each added one more witness.

**Fix.** The screen now fails closed, at every edition, as the dispatching session ruled. It admits an integer term (`ProvablyIntegral`,
now also an index-name, a report VARYING counter, an OCCURS DEPENDING extent and a power to an unsigned integer item), a quotient only
as a literal fold with an exact integral value, and a sum of items under literal factors whose net coefficient the item's scale makes
whole. A function term is judged by its type. The witness machinery is deleted (about 530 lines). The refusals name the term:
"divides FUNCTION ORD (A) by the literal 2; a quotient is an integer term only when both operands are literals ...".

**Goldens.** Ten refusals `pb5886-*` (85, 2002, 2014, 2023) and the complement `pb5886_integer_term_shapes`. Shapes that always give an
integer but not by an admitted shape (`SQRT (N) - SQRT (N) + 5`, `S2 - FUNCTION MAX (S2)` over a scaled S2) are refused now: four complement goldens were narrowed to their admitted rows, the 2002 `pb5575-std-arith-function-values`
lost its three integer-argument legs, and `pb4795_identity_difference_legal` and the two `pb1526_*` run-time goldens were deleted.
`IntrinsicTypeSixPowerShapesDriftTests` holds 45 shapes with their verdicts.

**Review round 2.** The review found the screen refusing what types and literal values decide; the ruling kept fail-closed and
added: a function term takes the type of its definition's table (the screen already read `BoundIntrinsicCall.ResultType`, which
`IntrinsicResultType.Resolve` sets from that one table, so `N - FUNCTION MAX (N)` over an integer N was already admitted and is now a
row of the shape table); `E / c` with a literal c whose reciprocal terminates is `E * (1/c)` (`WN / 2 * 2`, `S / 0.1`, `U * 4 / 2`
admitted again, `WN / 3 * 3` refused); the literal 1 to any integer exponent is an integer (`1 ** M`). The pb124, pb5345 and pb5832
complement legs those rules admit are restored. Forms that are integers only by an identity between numeric function terms stay
refused as recorded debt, kb/Work PB5889, whose refusal `pb5889-identity-difference` replaces `pb4795_identity_difference_legal`.

**Review round 3.** The R2-1 wrap exposed a screen defect: under ARITHMETIC IS STANDARD-DECIMAL every intermediate keeps 34
significant digits, so `B27 / 0.0001 / 16384 * 16384 * 0.0001` over a 27-digit item is not an integer there (the quotient, B27 × 0.6103515625, needs up to 37
digits; the reviewer measured ...998.9999), yet the screen admitted it. The arithmetic in force now reaches the classifier: the call node
carries a required `Arithmetic`, `IntrinsicResultType.Resolve` and the screen take it, and `ArithmeticModes.IntermediateSignificantDigits`
(one table: native none, 2002 STANDARD 32, STANDARD-DECIMAL 34) bounds every intermediate that carries fraction digits. The pair
`pb5886-standard-decimal-intermediate` (refused) and `pb5886_native_quotient_chain` (admitted, 0999) pins it, and the INTEGER-PART
wrap is a no-op on every argument it receives. The stale quotient text in the pb5886 goldens and the node-verdict table is restated.

**Rows.** 1985: the four rows of the note take the new refusals as witnesses; `1989-AR-A.2.2-4` restated CONFORMS. 2023:
`AR-15.3-6` gains the witnesses; `GR-5.5-3` and `DOC-A.1-124` retire the deleted goldens. NIST: 403 NIST tests green.
