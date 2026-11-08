## PB4923: SUM, MEAN, MIDRANGE, RANGE and arithmetic over a nested selection of a float item are binary64 evaluations

The note asked whether a nested selection's returned content that is a float item's binary64 "renders as floating-point" for the exact-family functions. Determination: yes,
as PB4891 settled for MEDIAN (docs/CONFORMANCE.md DOC-A.1-92b; §2023:15.4.1 "an implementor-defined approximation of the value of that expression", cite.py-checked). Re-probe on this
build before the fix: `SUM (MAX (F M2) M)` showed `.6000000000000000055...` where `SUM (F M)` is the double 0.6, MEAN and MIDRANGE likewise, `MAX (F M2) + N` stored
`0.80000000000000000555` where `F + N` stores `0.79999999999999993338`; `-`, `*`, `/` and `**` showed the same split.

Fix shape: the mark PB4891 put on `CobolExact` is the one reader. `SumExact`/`MeanExact`/`MidrangeExact`/`RangeExact` and their `Whole` twins became one body each (MEDIAN's shape,
the SDIDI mode as the lane switch) returning `CobolExact`; `CobolIntrinsics.TryBinary64Body` asks whether any argument value is binary64 and evaluates the existing `SumReal`/`MeanReal`/
`MidrangeReal`/`RangeReal` over the doubles. The result is a float item's content when a float item is among the arguments, else an approximation (`CobolExact.OfBinary64Result`, which
MEDIAN's mean now shares). The arithmetic arm is the sibling: `CobolExact.Add`, `Sub`, `Mul`, `DivTruncated`, `DivToOdd`, `QuotientAtScale` and `Pow` evaluate in binary64 when an operand is
a float item's content, as the compile-time float lane does for `F + N` (an approximation beside fixed-point operands stays exact, `NumericRenderer.FloatItemLane`). The renderer emits `Exact(...)`
for the four functions (was `Dec(...)`), `PublicAPI.Unshipped.txt` and `CobolExactIntrinsicsTests` follow the new return type. After: every MAX-form and its direct float form agree on DISPLAY and COMPUTE.

Golden: `15.88.4-Returned-value-rule/pb4923_arithmetic_over_a_nested_selection_of_a_float_is_binary64` (values by Python floats) and its refused `_below_2002` twin.

**Review round 1 (R1-1).** A non-integer power whose base or exponent is a nested selection of a float item (`M ** FUNCTION MAX (F M2)`, `FUNCTION MAX (F M2) ** M`) took the compile-time binary64 arm with the kind "approximation" and landed by its shortest decimal (+0.93303299153680740000) where `M ** F` lands by its exact binary value (+0.93303299153680741007). `NumericRenderer.Power` now hands an exact operand to `CobolExact.PowReal` before the lowering drops its float-item mark; it forms the binary64 power in the kind of its operands (the rule-6 screen and the range check are the same `CobolIntrinsics.PowNativeReal[Checked]`). Golden lines Z1/Z2 and the unit test `PowReal_OverAFloatContent_IsTheBinary64PowerInItsKind`. VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE and ANNUITY over the same nested selection are the same root in bodies this change does not touch, left as a lead.
