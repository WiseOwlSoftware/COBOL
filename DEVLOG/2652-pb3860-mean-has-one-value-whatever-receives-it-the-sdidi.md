## Entry 2652 — 2026-10-26 19:58 PDT — PB3860: MEAN has one value whatever receives it, the SDIDI quotient in every arithmetic mode

`COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MEAN(0.00000001 and nineteen zeros)` stored +0.00 where the one rounding of the mean, 5E-10,
is +0.01 (kb/Work PB3860, the sibling PB3839's review left open; owner decision 2026-10-26: "We should always be consistent with ourself barring
an explicit requirement differently"). Wave 20261717, group A.

**Re-probe on the wave's build (main `aefeb9a5c` plus the reviewed PB3839 head `c3df83b51`).** The note's repro reproduced as filed. The golden found
more than the note listed: `IF FUNCTION MEAN(...) > 0` was FALSE for that mean, `ROUNDED MODE TOWARD-GREATER` into `PIC S9V99` stored +0.00 (owed
+0.01), `TOWARD-LESSER` of the negative mean stored +0.00 (owed -0.01), and a `table(ALL)` argument list stored +0.00. The cause is the native
`MeanScaled` arm: it cut the quotient at `max(receiver scale, sum scale + 1, 6)` places, at the receiver's scale when that was the maximum, and let the
receiver's ROUNDED phrase round the cut, so the value of one reference depended on where it landed.

**Determination.** §15.60.4 r1c gives MEAN the equivalent arithmetic expression `((argument-11 + ... + argument-1n) / n)`; §15.4.1 makes the native
value "an implementor-defined approximation of the value of that expression" and the standard-mode value that expression's value. Nothing says which
approximation, so the choice is the implementor's, and it is the one PB3839 made for NUMVAL-F and MAX / MIN: the value the standard modes already
give, here the SDIDI quotient (`MeanDec`, 34 digits), in every mode, channel and receiver, which the receiving statement rounds once (§14.7.4.3 r2-r10).
A list with a binary64 operand keeps the float lane: its one value is the double, landed by the float family's own rule.

**Fix shape.** `IntrinsicCarrierRules.SdidiQuotientFunctions` (`ReturnsSdidiQuotient`, the shape `ReturnsExactParse` has) names MEAN. `RenderNum` hands it
to `RenderDec` before the float lane, `RenderDec` lists it among the always-SDIDI functions, and `FunctionValueLane.Of` predicts the SDIDI carrier. The
native `case "MeanScaled"` is deleted, because an arm is where a receiver read lives, and its dead helpers go with it: `StaticArgCount`, the unused third
member of `AlignedArgsEx` (folded into `AlignedArgs`), `SumScaled`'s function-name parameter (MEAN was the second caller), and MEAN in
`CrossAlignedNativeArms` and `AlignmentMayLeaveCarrier`. `ReceiverShapeReaderDriftTests.Readers` loses its two OPEN entries, so the receiver members the
renderer reads are `FloatLanding`, `RoundsAtItsScale` and `WorkingScale`, each a landing decision; the test that held the exact-parse functions to "no arm,
one SDIDI body" now holds both sets. `IntrinsicRealArgDriftTests` keeps MEAN's `MeanReal` counterpart owed, since MEAN no longer has an arm for the scan to
see.

**Goldens.** `pb3860_mean_one_value` (accept 2014, 2023; 23 legs: every rounding mode against the mean 5E-10 into receivers of 2, 9, 10 and 12 places,
the negative mean, a sum scale of 6, the inexact 5/3, an arithmetic expression around the function, a relation, and a `table(ALL)` list; A2, A6, B1, C1,
F2 and G1 fail on a build of the base commit) and its 2002 edge `pb3860_mean_rounded_mode_below_2014` (COBOL0803). Row RV-15.60.4-1 was restated and
gained both witnesses.

**What the self-review found.** `DISPLAY FUNCTION MEAN(1 2 3)` under native arithmetic now prints the SDIDI's 34 significant digits, as the standard
modes always did (`2.000000000000000000000000000000000`), because `CobolDec.Div` pads an exact quotient to 34 digits instead of reducing it toward the
IEEE 754 preferred exponent: filed as a lead, not changed here, since the representation of every SDIDI quotient is read by many goldens. The sibling sweep
(SUM, RANGE, MEDIAN, MIDRANGE, ABS, FRACTION-PART, VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE into a 2-place AWAY-FROM-ZERO receiver) found no other
function whose value depends on its receiver.

