## Entry 3164 — 2026-11-05 15:01 PST — PB4752, PB4753: a sum leaving the Int128 lane and negation or an even MEDIAN beside an approximation

**PB4752.** `COMPUTE R = FUNCTION INTEGER (D) + F` (D = 1000000000000000001, F = 1.01 at V9(30)) ended the run unit with
EC-SIZE-OVERFLOW, where `D + F` (known bounds, PB4729) was exact. `NumericRenderer.CombineAdditive` took the exact additive only when
both operand bounds were known. It now asks the property that matters, the SHIFT an operand is aligned by plus its `DigitBound` (an
unbounded operand is bounded by its carrier's 39 digits), so an intrinsic's value and a 16-byte container beside a fraction take the exact
carrier too. A sum with no shift whose operand is itself a container is still the documented size error (DOC-A.1-179 (1); the
pb91_native_overflow_outside_arithmetic golden is unchanged). Golden pb4752_sum_with_an_unbounded_or_container_operand_leaves_the_lane,
with its `_85` refusal.

**PB4753.** After PB4622 a float item beside an engine approximation rides the exact lane, and only a whole selected value kept the
remembered binary64. Two operations form a new value and landed one binary64 low. Negation: `CobolExact.Negate` now negates the
remembered double (the sign flip is exact). Even MEDIAN: it is §15.61.4 r2's mean, not a selection, so the new
`IntrinsicCarrierRules.ReturnsMeanOfMiddlePair`, asked by `IntrinsicRenderer.RenderNum` and `FunctionValueLane` alike, keeps it on the
float lane (the binary64 value of its equivalent expression, §15.4.1). Golden
pb4753_negation_and_even_median_beside_a_float_item_land_by_the_binary64 and unit test
`CobolBigExactFormTests.Negate_KeepsTheBinary64OfAnApproximation`.

No inventory row closes (closes_rows empty, with reasons in both notes).

