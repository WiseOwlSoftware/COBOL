## PB4980: ANNUITY over a nested MAX, MIN or MEDIAN stores the same value in one receiver and in several

**Mechanism.** The bind-time lane prediction (`FunctionValueLane`) decides whether a numeric function's value is a binary64, which makes the intermediate result item of a
multi-receiver MOVE a COMP-2 item (§2023:14.9.25.4 GR1, `MOVE a TO temp / MOVE temp TO b / MOVE temp TO c`). It listed MAX, MIN and MEDIAN among the native SDIDI bodies
that return a scale-0 exact value and predicted `CarrierKind.Fixed` for a nested one. The renderer's native arms for the three (`MaxExact`, `MinExact`, `MedianExact`,
`MedianWhole`) return `NumX.Exact`, a run-time-scaled value that the function above reads through `NumericRenderer.LowerWide` as an SDIDI operand. So
`ANNUITY (MEDIAN (T (ALL)) 3)` over three PIC 9(31) zeros, and `ANNUITY (MAX (U 1.5E0) 3)`, were computed on the SDIDI in a one-receiver MOVE (`0.3333333333333333333333333`,
`1.6025641025641025641025641`) and in binary64 in a two-receiver MOVE (`0.3333333333333333000000000`, `1.6025641025641026000000000`), against §2023:15.4.1 "the returned
value is the same for all instances of a given function".

**Fix.** MAX, MIN and MEDIAN move from `NativeSdidiBodiesWithExactResult` to `NativeSdidiBodiesWithWholeResult` (SUM, RANGE, MEAN, MIDRANGE) and are predicted as the SDIDI.
Only SIGN, ORD-MAX and ORD-MIN, whose bodies build `new NumX(...)`, stay exact scale-0. `FunctionValueLaneDriftTests` now tells the three arm shapes of `RenderDec`
(`Dec(...)`, `Exact(...)`, `new NumX(...)`) apart and pins each set to its shape; before, it pinned only the union, which is how the three sat in the wrong set unnoticed.
`DESIGN-binder-bound-tree.md` names the whole-value set.

**Goldens.** `pb4980_a_function_over_a_nested_selection_is_the_same_for_every_receiver` (ANNUITY over MEDIAN, MAX and MIN, two receivers against one, values from
§2023:15.9.4 1) a) and b)) and its `_85` refusal (31-digit item, COBOL0802). No inventory row closes.

**Probed and not a defect.** `PRESENT-VALUE (3 FUNCTION MEDIAN (T (ALL)))` seemed to drop its second receiver; the test line ran past column 72 of fixed format.
`ANNUITY (MAX (T (ALL)) 3)` over a plain scale-0 selection is a binary64 in both forms (agree).
