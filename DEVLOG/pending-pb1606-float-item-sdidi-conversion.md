## Under standard-decimal arithmetic a floating-point data item enters the intermediate through one conversion, its exact value rounded once under INTERMEDIATE ROUNDING (kb/Work PB1606)

**Why.** `ARITHMETIC IS STANDARD-DECIMAL`, `F1 USAGE FLOAT-LONG VALUE 0.1`: `COMPUTE F4 = F1` stored 0.1 and `COMPUTE F2 = F1 + 0`
stored 0.09999999999999999; into `PIC V9(19)` the lone form stored `.1000000000000000055` and `F1 + 0` and `F1 * 1`
`.1000000000000000000`. The note read the one-ulp-low float as rule-following (§14.7.4.3 r2 and r10: no ROUNDED phrase is
TRUNCATION) and asked for a determination of the conversion §8.8.1.5.1 leaves to the implementor. The adjudication of
2026-10-23 found the real defect behind it: one statement mode held two float-to-SDIDI conversions.

**Re-probe.** Reproduced on the base as filed. Cause: a float item that was the whole expression reached
`ArithmeticEmitter.StoreArith` as a binary64 and landed natively (its exact expansion), while every operand beside an
operator went through `NumericRenderer.DecOperandExact` to `CobolDec.FromDouble`, the shortest round-trip decimal.

**The determination (docs/CONFORMANCE.md section 3, numeric design D3).** A floating-point data item is converted by its
binary64's EXACT algebraic value (a double is always a terminating decimal), rounded ONCE to 34 digits under the
INTERMEDIATE ROUNDING mode, because §8.8.1.5.2 r1 rounds an operand that "cannot be expressed exactly in an SDIDI" by
§11.9.11 and NOTE 3 of that clause names the binary value. It matches what a MOVE and a native landing already read (kb/Work
PB623). GnuCOBOL 3.2.0 has no standard-decimal mode and no usable precedent (probed: `COMPUTE D = F`, `F + 0` and `MOVE`
all store 0.099999999999999991673327315311 for a COMP-2 0.1). An implementor-defined APPROXIMATION (a §15.4.1 function's
returned value, a non-integer power) keeps the shortest decimal: it is not a data item, and its digits past the 17th are
noise.

**Fix.** `CobolDec.FromBinary64(d, mode)` reads the runtime's exact formatter (`E766`, every digit of the double), keeps
the first 38 digits as an `Int128` significand and the rest as one sticky bit, and rounds through the existing `Round34`
funnel, so ties, PROHIBITED and the range clamp stay in one place and no `BigInteger` enters the runtime (the semgrep count
is unchanged, 44). An exact value is held at the fewest places (0.5 is 5 x 10^-1; `DISPLAY FUNCTION ABS (F)` stays `0.5`).
`StoreArith` converts a lone float operand through `DecOperand` before any arm reads it; `DecOperandExact` converts every
other float item through the same function (`RuntimeApi.DecFromBinary64`). `FromDouble` is deleted; its approximation
callers (`LiftApproximation`, `RealResult`, the r2e power, NUMVAL-F's test) call `FromApproximation`. `CobolDate`'s
COMBINED-DATETIME range check reads the exact value.

**Sibling found and fixed (I9).** The floating-point numeric-edited store (`CobolEdit.FormatFloatStore` /
`TryFormatFloat` of a double) used the same shortest decimal: `COMPUTE FE ROUNDED MODE IS PROHIBITED = F1` with F1 = 0.1
took NOT ON SIZE ERROR where a fixed-point receiver takes the size error, and `MOVE 0.29 TO +9.99E+99` stored +2.90E-01
where the exact value truncates to +2.89E-01. It now reads `CobolDec.FromBinary64ToOdd` (the exact value as a 38-digit
round-to-odd significand, the form `CobolEdit` already uses for an unsigned-wide value).

**Consequences, recorded rather than found (CONFORMANCE section 3, items a to d).** `COMPUTE F2 = F1` into a binary float
now stores 0.09999999999999999 too (the SDIDI 0.1000000000000000055511151231257827 is below the double 0.1, so truncation
takes the double below; ROUNDED takes 0.1). `IF F = 0.1` with F = 0.1 is false under standard-decimal arithmetic (it is
GREATER). A COMP-2 holding 1.0E+30 is 1000000000000000019884624838656 and that is what is divided. Reversing the choice is
one function plus the same documentation.

**Goldens.** `8.8.1.5.2-.../pb1606_float_item_conversion` (every position, multi-receiver, the float receiver, the
relation, an exact tie), `_intermediate_even`, `_truncation`, `_prohibited`, `14.6.8.4-.../pb1606_float_sender_exact_value`,
and the negative `pb1606-float-item-standard-decimal-below-2014` (refused at 85 and 2002). Every expected value was
derived from the rules with exact rational arithmetic before any run, and each matched the first run. Four goldens whose
expected value rested on the shortest decimal were re-derived: `arith_standard` (W5 0.3 to 0.300000000000000016),
`pb253_prose_float_sdidi_container_2014` and `_standard_container` (the float-receiver channel now compares the MOVE and
COMPUTE landings with each other, because a float item and the function's decimal value are no longer equal),
`pb3017_sdidi_divide_remainder_beyond_the_int128_carrier` (the quotient of the double nearest 1.0E+30).
`CobolDecBinary64ConversionTests` checks 680 doubles in three modes against a `BigInteger` oracle in the test file;
`FloatItemSdidiLiftDriftTests` holds the callers of both conversions to closed lists and the lone-operand lift at the
head of `StoreArith`.

**Rows.** The note claimed none (`inventory_rows: []`); `closes_rows: []` with its reason.

**Self-review.** Silent failure: the formatter-buffer failure throws; NaN and infinity raise the same conditions in the
lone and the operator position (probed: the four combinations all take the size error). Comments: every citation in the
new text was re-run through `cite.py --check`; the first draft of the CONFORMANCE paragraph claimed GnuCOBOL expands a
double exactly, a memory the probe above refuted, and it was rewritten to the measured behaviour. Tests: the first gate
was red on a missing `[Trait("Sweep", "tree")]` for the new tree-wide drift test, fixed in the next commit.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 000002-20261024T060205Z; Conformance
9,938/18,354 · Unit 36,959/37,932 · Characterization 5/36 ran). Linux gate: the lander's (batched gating).

**Review round 1 (R1-1, fixed).** The first cut made the floating-point numeric-edited store read EVERY binary64 it is handed
by its exact binary value. That is right for a floating-point data item and wrong for two other values that reach the
same overload as a double: a FIXED-POINT sender of a BY CONTENT argument at §14.2.3 GR9's activation boundary
(`MoveSending.Binary64` was `CobolFloat.ScaledToDouble(Unscaled, Scale)`, so PIC 9V9 0.3 edited as
`+2.9999999999999998E-01` where a local MOVE edits `+3.0000000000000000E-01`) and an engine approximation (a function's
returned value: `MOVE FUNCTION SQRT (X) TO FE` with X = 0.09). `MoveSending.Binary64` is now `MoveSending.ExactValue`, a
`CobolDec` (the fixed-point sender's own digits, a float sender's round-to-odd exact value), and `RuntimeApi.FloatEditEntry`
sends an `Approximate` binary64 through `CobolDec.FromApproximation`, as the arithmetic lift does. Golden
`14.6.8.4-.../pb1606_fixed_sender_float_edited_formal` (hand-derived; it fails on the first cut), a `MoveSending` unit
assertion and a drift test on both. Sibling sweep: `COMPUTE FE = FUNCTION SQRT (X)` lands through
`CobolIntrinsics.FromDouble` at scale 19 (the scaled quantizer) and truncates the double's expansion, as it did before this
branch; that is a separate defect of the quantizer, reported as a lead. The R1-2 lead (the exact formatter's cost, about 4 us
a lift) is left for the clerk: neither CobolDec.cs nor CobolEdit.Float.cs changed in this round.
