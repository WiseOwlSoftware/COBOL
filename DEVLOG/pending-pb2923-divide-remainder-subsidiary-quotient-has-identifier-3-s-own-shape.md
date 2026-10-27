## PB2923: DIVIDE ... REMAINDER's subsidiary quotient has identifier-3's own shape, so a floating-point receiver keeps its digits and its exponent

`DIVIDE 1 BY 3E24 GIVING FE ROUNDED REMAINDER R2` with `FE PIC +9.99E+99` stored `+0.00E+00` and left R2 = 1.00, the whole dividend, where
the statement without REMAINDER stores `+3.33E-25` (kb/Work PB2923; the owner, 2026-10-26: "Fix the root causes, never simply making the
diagnostic pass" and "Always be consistent with ourselves barring an explicit requirement differently"). Wave 20261714, group B, merged on top
of PB3839's branch.

**Re-probe on the wave's build (base `624115327`).** The adjudication's numbers reproduced and the sweep widened them to every floating-point
identifier-3. `1/3000` into `FE` stored `+3.30E-04` (the quotient STORED was the truncated one) with remainder `+00000.010000` (owed 0.001);
`10/3` left `+00000.000010` (the subsidiary quotient truncated at the mask's five-place hint); FLOAT-LONG `1000/3` stored `333` and left a
remainder of 1; FLOAT-SHORT `10/3` stored `3`. Under STANDARD-DECIMAL the quotients were right and the remainders were just as wrong.

**Mechanism.** §14.9.12.4 6) c) derives the subsidiary quotient from the quotient by truncation to "the same number of digits and the same decimal
point location as" identifier-3, and `EmitDivideRemainder` modelled that as a (scale, digit cap) pair. Only a fixed-point item has such a pair: a
floating-point numeric-edited item is a significand of N digits at an exponent (`PicInfo.ReceiverScale` answers a working-scale HINT for it, and
its own docstring said both "0, unused" and "rides the mask arm"), a binary float is a format. The native lane also formed the STORED quotient by
the Int128 kernel at that scale, so adding the REMAINDER phrase changed what identifier-3 held, which §14.9.12.4 6) b) forbids ("The quotient is
then formed as specified in General rule 1 and stored in identifier-3").

**Fix shape (the owner's rule 5: the shape that makes the next case automatic).** `ArithmeticEmitter.SubsidiaryShapeOf` answers "identifier-3's
shape under truncation" once, as a `SubsidiaryShape` over a closed `SubsidiaryForm` (FixedPoint, FloatEdited, BinaryFloat). A floating-point
identifier-3 takes `CarriedSubsidiaryQuotient` in every arithmetic mode; the quotient is `FinalQuotient`, the one method `DIVIDE ... GIVING`
forms its quotient with too, and each form has its arm: fixed point as before (`CobolDec.SubsidiaryQuotient` / `CobolNum.CapDigits`); a
float-edited item takes `CobolEdit.TruncateToFloatShape`, which is the store's own normalization (`NormalizeFloat`, extracted from
`FormatFloatCore` so the subsidiary quotient and the stored quotient cannot disagree about range, overflow or underflow); a binary float takes
`CobolFloat.TruncatedToFormat` (the format value nearest an SDIDI quotient without passing it, compared exactly through `ExactRatio`, never a
second rounding) or, when the native lane already holds a binary64 quotient, that value itself (`TruncatedToSingle` for binary32). The remainder
is GR7's expression over it: SDIDI under standard-decimal, binary64 natively (DOC-A.1-123). `EmitDivideRemainder` also builds its receiver context
with `ReceiverContext.Of` (it hand-built a copy that dropped the float-edited flag, the PB2694 pattern), and `IntDigitsOf` and
`SubsidiaryDigitsOf` are deleted. Determination in docs/CONFORMANCE.md section 3: the subsidiary quotient is identifier-3's own shape; ISO decides
(IBM and Micro Focus reject the form, GnuCOBOL 3.2 has no floating-point edited item and truncates a float subsidiary at a fixed 17 fraction digits,
which is not identifier-3's scale).

**Goldens** (every value worked by hand from the rule, in each file's comment): `pb2923_divide_remainder_float_edited` (10 legs: the repro,
1/3000, 1000/3, 10/3, a negative, ROUNDED where the stored quotient rounds up and the subsidiary one truncates, DIVIDE INTO, zero, and a
quotient past the exponent under ON SIZE ERROR), `pb2923_divide_remainder_float_usage` (FLOAT-LONG and FLOAT-SHORT, including 2/3 whose nearest
binary32 lies above the quotient), their STANDARD-DECIMAL twins, and the negative `pb2923_divide_remainder_float_below_2002`. Unit:
`FloatSubsidiaryQuotientTests` (13). Drift: `DivideQuotientOneFormerDriftTests` (the division is rendered by `FinalQuotient` alone, both
statements call it, every `SubsidiaryForm` has its arm). Rows re-verdicted: none (the note claimed none).

**Self-review.** Architecture: one shape per form, the old pair gone, no second former of the quotient. Full code: a quotient the format cannot
hold is the format's largest finite value (the store of the same quotient is the size error); a float-edited quotient nearer to zero than the
item holds is zero, as its store leaves it. Duplication: `ReceiverContext.Of` replaced the hand-built context. Left open: nothing in the slice.
