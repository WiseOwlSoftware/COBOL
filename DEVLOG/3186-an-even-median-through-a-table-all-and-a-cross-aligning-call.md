## Entry 3186 — 2026-11-05 19:20 PST — An even MEDIAN through a table(ALL) and a cross-aligning call over an unbounded argument take their own lanes

Two arms of `IntrinsicCarrierRules` that the review of PB4752/PB4753 left standing (kb/Work PB4791, PB4792).

**PB4791.** `ReturnsMeanOfMiddlePair` counted the arguments as written and answered false for any table(ALL), so an even MEDIAN through a
fixed table beside an engine approximation stayed on the kind-preserving exact lane and landed one binary64 low in a float receiver
(re-probe: `MEDIAN (T (ALL) FUNCTION EXP10 (-3))`, T one FLOAT-LONG 0.0001, compared NE to `(T (1) + EXP10 (-3)) / 2`). It now counts the
elements a table(ALL) stands for (`TableAllPlace.StaticCount`, §15.3 14)), so a fixed table has a bind-time parity. A table over an OCCURS
DEPENDING or dynamic range has a run-time count and stays a selection; that remainder is a lead in the wave report. Golden
`pb4791_even_median_through_a_table_all_lands_by_the_binary64` and its `_below_2002` refusal.

**PB4792.** `AlignmentMayLeaveCarrier` read an operand of unknown bound (`NumX.Digits` 0, an intrinsic's value) as unprovable and answered
false, so `SUM (FUNCTION INTEGER (D) F)` (D 1000000000000000001, F `9V9(30)`) ended the run unit with EC-SIZE-OVERFLOW at the alignment
for a value the receiver holds; RANGE, MEDIAN and MIDRANGE the same. The renderer now states the operand with its scale
(`AlignedOperand.Unbounded`) and the rule answers true when an argument is unbounded and the alignment shifts any argument, the function
arm of PB4752's `CombineAdditive` rule; with no shift the native arm keeps the list (DOC-A.1-179 (1)). MAX, MIN, ORD-MAX, MEAN and MOD
swept over the same operands: they answer. Golden `pb4792_alignment_with_an_unbounded_argument_leaves_the_carrier` and its `_85` refusal.

**Review round 1 (R1-1).** The unbounded arm sent a list to the native SDIDI bodies, which round to 34 digits: `SUM (FUNCTION INTEGER (A) F)` with A 10000 and F `V9(31)` .999...9 stored 10001.00000 where the exact 36-digit sum stores 10000.99999. A first attempt returned the exact value from the `...Exact` bodies themselves and was RED at the gate (pb4592_function_value_rounded_in_the_intermediate_mode pins the 34-digit rounding of the SDIDI lane, an owner determination), so the exact value is a separate set: `AlignmentShiftsUnboundedOperand` selects `SumWhole`, `RangeWhole`, `MedianWhole`, `MidrangeWhole` (the same bodies unrounded; the `...Exact` bodies round them). Golden cases T1 to T4 in the PB4792 case.

Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 22,291/22,291 · Unit 40,235/40,235 · Characterization 37/37` (run 000004-20261106T030554Z, review round 1 head). Self-review (architecture, full code, performance, duplication): one rule in IntrinsicCarrierRules, both callers state the operand; the unbounded branch is allocation-free beyond the existing lists.

