## A nested quotient's precision no longer grows with the expression's depth (PB3538)

The nemotron fuzz run's one compiler defect, kb/Work PB3538: `COMPUTE R = A / B / B / B / B` with A = 8 and B = 2 is 0.5.
On main it ended the run unit with EC-SIZE-OVERFLOW. With ON SIZE ERROR it took the phrase and left R unchanged.
`(A / B) * (A / B) * (A / B)` (64) aborted the same way. Re-probed on this branch's own build before the change: same
output, rc 1.

**Mechanism.** `NumericRenderer.Divide` gave a nested quotient `max(receiver, a.Scale, b.Scale) + 14` guard digits of
scale. The next division's `a.Scale` is that quotient's scale, so the guard was added again at every level: 14, 28, 42.
Once the scale reached 42, any value of 1 or more no longer fit the Int128 carrier. A clamp limited the guard added at
one step, never the total. A product of quotients compounded the same way: each quotient had no digit bound, so the
product took a checked Int128 multiply at the summed scale. The same checked multiply ended
`FUNCTION MEAN (F G) * FUNCTION MEAN (F G) * FUNCTION MEAN (F G)` (5.36 at scale 54) for every function value of scale 18.

**Fix (numeric design D2, rewritten).** `NumX` gains `Precision` / `FractionPrecision`: the fraction digits a value's
operands carry. A quotient's guard digits are not part of it, and sums, products, negations and wide powers carry it the
way they carry the scale. A nested quotient runs at `max(receiver scale, operands' precision) + 14` with truncation, so
the guard is added once at any depth. Its lane comes from its digit bound, as a product's does (PB1143): the scaled Int128
within 38 digits, and the SDIDI by round-to-odd past that. An operand of unknown bound is bounded by its Int128 carrier,
39 digits (`PicInfo.CarrierDigitBound`, also used now by the 16-byte container bound). A product therefore never takes a
checked Int128 multiply. `CobolNum.MulChecked` is deleted, which raises the runtime major to 13 and re-pins the ABI hash.
DIVIDE ... REMAINDER's subsidiary quotient carries its digit bound, and its back-multiply stays exact into the
subtraction (`Combine(..., keepWide: true)`; `ExactWideSettlementDriftTests` lists the site).

**Measured alternative, rejected.** The note proposed renormalizing every nested quotient to the SDIDI's 34 digits. I
built that first and ran the whole Conformance assembly: one red in 18,125, pb60's native `2 / 7 * 7` = 1,999999 became
2,000000. GnuCOBOL truncates the quotient and multiplies exactly (1,999999), and CLAUDE.md rule 1 puts GnuCOBOL next where
ISO leaves native arithmetic to the implementor. That design would also put every nested division on software
decimal128. The bounded scaled design keeps the pb60 value, keeps the Int128 fast path, and makes precision independent of
depth.

**Sweep.** Division chains, products, sums and negations of quotients, nested powers (`(A / B) ** 2 / B ** 2 / B / B` =
1), a receiver-less relation and a subscript expression all give the exact left-to-right value. So do a several-receiver
COMPUTE (29.63 rounded, 29.62 truncated), a zero divisor mid-chain (ON SIZE ERROR, R unchanged), a quotient by a tiny
divisor (`999999999999999999 / 0.000000000000000003 / 10 ** 18`), a function value as a dividend, and DIVIDE REMAINDER
into a COMP-5 quotient. ARITHMETIC IS STANDARD-DECIMAL never had the defect, since every operation takes the SDIDI
before `Divide`. The same holds at `--std 85` and under `--permissive`.

**Witnesses.** Goldens `pb3538_chained_division_native` (85 to 2023: the note's repro with and without ON SIZE ERROR, the
product, a relation, and depth-10 chains of 8 / 2 and 1 / 3 into scale-4 and scale-12 receivers),
`pb3538_chained_division_standard_decimal` (2014, 2023) and `pb3538_unbounded_operand_products` (2002 to 2023). Expected
values come from §8.8.1.2 3) and §14.7.4.3 2). `NestedQuotientPrecisionDriftTests` compiles every quotient-crossing shape
at depth 2 and depth 16 and requires the same division-kernel scales. On the base tree it was red, with scales up to 228.
CONFORMANCE.md DOC-A.1-123 and DOC-A.1-179 are corrected: they had both said "up to 14 guard digits" and "a QUOTIENT is
formed on the SDIDI whatever its operands". One golden is re-derived: `14.9.12-DIVIDE/l1_divide_remainder_backmultiply_size_error` had pinned the checked multiply's
size error for a REMAINDER back-multiply of VALUE about 10**12 (unscaled about 10**40). It is now
`OVF=NONE BQ=999999999999.98999999900000 BR=000000000000.000000000`, computed from §14.9.12.4 GR7 and §14.7.5 case 5.
`14.7.5-SIZE-ERROR-phrase-and-size-error/pb91_native_overflow_outside_arithmetic` made its native overflows from
products of two `FUNCTION ABS(A)` values (A = 10**20 - 1). Under the new rule that product is exact, so its overflow
legs now add 1 to HIGHEST-ALGEBRAIC of a `PIC S9(19) COMP-5` item (2**127 - 1), a sum no Int128 intermediate holds.
The expected statuses are unchanged, and a new R8 leg pins the ABS product as TRUE. No inventory row moved state. GR-14.7.5-5 (`CobolNum.MulChecked` retired as a witness) and GR-14.9.12.4-7 are
restated through batches, and the rules' other rows were already OK and gain witnesses.

Branch review round 1 found one blocking defect (R1-1), and the review finisher fixed it at its root. The nested
quotient's lane came from a digit bound, and a dividend of unknown bound (any intrinsic's value: INTEGER, MOD, ORD) is
bounded by its Int128 carrier, 39 digits, so every such quotient passed the Int128 lane and took the SDIDI's
round-to-odd. `COMPUTE R = FUNCTION INTEGER(X) / 7 * 7` stored 2.000000 where `X / 7 * 7` stores 1.999999, and the
relation `FUNCTION INTEGER(X) / 7 * 7 < 2` was false. Re-probed on the merged build before the fix (rev-a q1 and q2):
INTEGER 2, MOD 2.000000, REL NOT LESS. The fix makes the lane a representation, never a rule: a nested quotient whose
bound passes 38 digits but fits 77 is the same truncated quotient at the same working scale on the exact 256-bit form,
through the new runtime member `CobolWide.DivTruncated` (an additive member in `PublicAPI.Unshipped.txt`, so neither the
major nor the ABI pin moves). The sweep found the additive sibling: a sum or difference of a wide value and an operand
of unknown bound also dropped to the SDIDI, so `FUNCTION INTEGER (BIG) - BIG / T * T` kept a round-to-odd tail
(0.00000000000000001 in V9(18)); `CombineAdditive` now bounds that operand by its carrier, as `Multiply` and `Divide`
do. A scale-32 quotient of a PIC 9(18) item (50 digits) had taken the SDIDI on any spelling, and now takes the wide
form too. Golden `pb3538_unbounded_operand_quotients` (2002 to 2023, values from §8.8.1.2 3) and §14.7.4.3 2)) pins
each FUNCTION spelling beside its data-item spelling; it failed on the reviewed head (INTQ 2.0000, INTQ0 2, PRODQ
16.0000, INTREL NOT LESS, each SUM leg 0.000000000000000010) and passes after. `CobolWideTests` pins `DivTruncated`
against a BigInteger oracle (operands past 128 bits, both signs, a divisor scaled up, a divisor above 2^255 whose partial
remainder carries) and against the Int128 lane's truncating quotient. `NestedQuotientPrecisionDriftTests` now counts
the wide kernel's scales at depth 2 and 16 and requires that no nested quotient takes `CobolDec.DivToOdd`.
CONFORMANCE.md DOC-A.1-123 and the numeric design D2 state the rule. `FUNCTION NUMVAL`'s value rides the SDIDI lane by
the documented PB69 rule, so this round left its quotient there; round 2 brings it under the same rule.

Branch review round 2 (finding R2-1) found the same lane-changes-the-value defect one step further out. Two intrinsic
values are each bounded by their 39-digit carrier, so their product's bound (78) passed the 77-digit wide form and
`Multiply` formed it on the SDIDI (`CobolDec.MulToOdd`, exact for a small value); the quotient above it then took the
native Dec branch of `CombineCore`, `CobolDec.Div` at the INTERMEDIATE ROUNDING mode, whose 34th digit the next product
rounded back up. An intrinsic value times a PIC 9(20) item (59 digits, wide) reached `Divide` with a quotient bound of 79
and took `CobolDec.DivToOdd`, with the same result. Re-probed on the merged build (`scratchpad\w1153\w1153a\r3\p1.cob`,
`p3.cob`): A-INTR, H-INTR and I-INTR2 stored 2.000000 and G-REL was NOT LESS, where the item spellings store 1.999999
and are LESS. The fix keeps the rule and widens its reach: a nested quotient is the quotient truncated at the working
scale on EVERY native lane. The new runtime member `CobolDec.DivTruncatedToOdd` (additive, `PublicAPI.Unshipped.txt`)
forms that truncated quotient on the SDIDI and holds it to 34 digits by round-to-odd, so one of at most 34 digits is
exact; `Divide`'s nested arm falls to it past 77 digits, and `CombineCore` sends a nested `/` with an SDIDI-carried
operand to `Divide` instead of `DecBinary`. A float literal, a NUMVAL value and a power are such operands, so
`2.0E0 / 7 * 7`, `FUNCTION NUMVAL ("2") / 7 * 7` and `X ** 1 / 7 * 7` now store 1.999999 as `2 / 7 * 7` does (main
stored 2.000000 for each; native intermediate rounding is the implementor's, §11.9.11.2 r1; DOC-A.1-82 records it). The
sweep found the precision sibling: an SDIDI value's scale is 0, so `DecBinary`, `MulToOdd`'s arm, `LowerWide` and
`Negate` dropped the operands' fraction digits, and a quotient above them took a working scale of 14 where the exact
lanes took 22 (`F8 * FUNCTION INTEGER (Y) * FUNCTION INTEGER (Y) / T * T` was not equal to `F8 * Y * Y / T * T`); each
now carries `NumX.Precision`. An outermost quotient with an SDIDI operand keeps `DecBinary` (DOC-A.1-82's lane). Golden
`pb3538_sdidi_lane_quotients` (2002 to 2023; INT, WIDE, MOD, FLT, NUMVAL, POW, NEG, REL and PREC failed on the reviewed head and pass
after), `CobolDecTruncatedQuotientTests` (a BigInteger oracle, the Int128 lane's agreement, a remainder below the scale
that must not jam), and a `NestedQuotientPrecisionDriftTests` fact for the SDIDI shapes (and their scales at depth 16)
pin it.

Branch review round 3 (finding R3-1) found that round 2's precision sweep gave an SDIDI value whose fraction digits the
compiler cannot know the precision 0, its placeholder scale. A floating-point literal, a NUMVAL value and a power are
such values, so a quotient over 10**-15 was truncated at 14 places, to zero: `IF 1.0E-15 / 2 > 0` and
`IF FUNCTION NUMVAL ("0.000000000000001") / 2 > 0` were NOT GREATER on the branch and GREATER on main, while
`0.000000000000001 / 2` and an item of PIC V9(15) were GREATER (the reviewer's probe `scratchpad/w1153/rev-a3/q1.cob`).
The literal's notation changed its value. The fix states the rule once.
`CobolNum.NestedQuotientScale` (runtime) is the one working-scale rule, `max(receiver scale, precision) + 14`, applied by
the compiler at compile time and by the new `CobolDecWithPrecision` at run time. An SDIDI value's precision is
`CobolDec.FractionDigits` (round 4 below gives its final form): a float literal's at compile time; a NUMVAL value's, a
power's and a function result's at run time.
A nested quotient over such a value, and every sum, difference, product and negation between it and that quotient, is
also spelled as a `CobolDecWithPrecision` (the value paired with its operands' precision; `NumX.WithPrecision` is an
alternative spelling of the same evaluation, never a second one), whose quotient carries the precision, not its working
scale, upward, so the scale does not grow with depth: `FUNCTION NUMVAL ("2") / T / T` equals `X / T / T`.
`NumX.FractionPrecision` now refuses a Dec of run-time precision, so a reader that would have taken 0 fails loudly. The
sweep fixed the Dec `Negate`, which evaluated its operand twice (`CobolDec.Negate`). The goldens
`pb3538_runtime_precision_quotients` (85 to 2023: NUMVAL, a power, depth, a product, a negation, a sum, two receivers,
a zero divisor) and `pb3538_float_literal_precision` (2002 to 2023), `CobolDecWithPrecisionTests` and two
`NestedQuotientPrecisionDriftTests` additions (NUMVAL shapes at depth 2 and 16; the literals' scales and the run-time
quotients) pin it.

Branch review round 4 (finding R4-1) found that round 3's `CobolDec.FractionDigits` counted the value's SIGNIFICANT
fraction digits, dropping trailing zeros, while a fixed-point literal's and an item's precision is the scale they are
written or declared at. So `2.50E0` and `NUMVAL ("2.50")` had the precision 1 where `2.50` and a `PIC 9V99` item have 2,
and `IF 2.50E0 / 3 * 1000000000000000 > 833333333333333` was NOT GREATER on the branch and GREATER on main and for the
`2.50` spelling (the reviewer's probe `scratchpad/w1153/rev-a4/p4.cob`; the difference lies past the 14 guard digits, so
it shows only under a factor of 10**15). The rule is now the places the value is HELD at, `-Exp`: a floating-point
literal's written scale (its significand's places less its exponent, so `1.0E-15` has the 16 of `0.0000000000000010`
and `2.0E0` the 1 of `2.0`), a NUMVAL-family value's written places (the family lifts its argument exactly at the
parsed scale), and for a value an operation formed, a power or a function's result, the places its result is held at.
The notation no longer changes the precision, while the written scale still decides, as it does for fixed-point
operands: `2.5E0` truncates one place sooner than `2.50`. The bound (a significand of at most 34 digits) and the
depth-independence are unchanged. The new golden `pb3538_written_precision` (2002 to 2023) pins the probe, a NUMVAL of
an item, the equalities and the one-place spelling; `pb3538_float_literal_precision`'s TINY leg now compares `1.0E-15`
with an item of the same 16 written places (its output is unchanged); `CobolDecWithPrecisionTests` pins the rule and
the NUMVAL family's exact lift.

Branch review round 5 (finding R5-1) found that the held places were not held for a ZERO. `CobolDec.AddSigned` returned
the other operand at its own exponent when one operand was zero, and the rounding funnel (`Clamp`) returned every zero
result at exponent 0. So a sum with a zero term was held at the nonzero term's places: with `Z PIC 9V999 VALUE 0`,
`IF FUNCTION ABS (Z + FUNCTION NUMVAL ("2.5")) / 3 * 1000000000000000 > 833333333333333` was NOT GREATER on the branch
and GREATER on main, and Z = 0.001 made it GREATER, so the outcome depended on the operand's value (the reviewer's
probe `scratchpad/w1153/rev-a5/q9.cob`; `NUMVAL ("0.000")` in place of Z likewise). §8.8.1.5.3 performs the four
operations as ISO/IEC 60559 5.4.1 does, and that standard holds an exact result, a zero included, at the preferred
exponent. Every arm that forms a zero now does: the funnel, a sum with a zero operand (the smaller exponent, lowered as
far as 34 digits hold), a zero dividend (the dividend's exponent less the divisor's), the truncated nested quotient (its
scale), an integer power of zero (n times the base's, on the SDIDI and natively), a root of zero (half, rounded down),
a zero subsidiary quotient (the receiver's scale) and the unsigned and wide lifts (the declared scale). One constructor,
`CobolDec.Zero`, clamps the exponent to decimal128's range. The new golden `pb3538_zero_operand_precision` (2014 and
2023; FUNCTION ABS is 2014's) pins the probe in both spellings, the 0.001 variant, a difference, a cancelled difference,
a zero product and the equality with `2.500`; every leg but the nonzero one fails against the round-5 runtime.
`CobolDecWithPrecisionTests` pins each arm, and `CobolDecWideLiftTests` now expects the unsigned lift of a zero at its
declared scale, as the signed lift has always kept it.
