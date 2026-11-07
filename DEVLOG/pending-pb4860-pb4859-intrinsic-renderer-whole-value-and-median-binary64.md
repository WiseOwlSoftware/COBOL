## A nested whole-value call keeps its value in SUM, RANGE and MIDRANGE, and an even MEDIAN with a binary64 middle is the binary64 mean on every channel

Two leads of one root, `IntrinsicRenderer` and its carrier rules (kb/Work PB4860, PB4859).

**PB4860.** `COMPUTE R ROUNDED = FUNCTION SUM (FUNCTION SUM (A B) Z)` with a 26-digit `A`, `B` = .004999999 and `Z` = 0 stored `.01`
where `A + B + Z` stores `.00`. The inner call's value is a whole value (`NumX.Exact`); `RawArg` lowers it to its 34-digit image, the
renderer read that as an SDIDI operand and `AnyDecRaw` sent the list to `SumExact` before the whole-value arm could ask. The whole-value
arm now precedes it (its standard-mode half was dead code), and `AlignedOperands` reads each argument unlowered, a `NumX.Exact` one
as an operand of unknown bound and run-time scale (`AlignedOperand.RunTimeScaled`). Genuine SDIDI and binary64 operands are unchanged.

**PB4859.** An even MEDIAN beside a binary64 argument formed the exact mean of the float's SDIDI lift (`DISPLAY MEDIAN (F M)` showed
`.30000000000000000277`, COMPUTE stored it) and DISPLAY of two binary64 middles kept the SDIDI mean while COMPUTE gave the binary64 mean.
DOC-A.1-92b: a binary64 operand evaluates the expression in binary64, and the mean's operands are the two MIDDLE values. The first
attempt (an even list with any binary64 argument to the float lane) failed the gate at `pb65_selection_mixed_carriers` T12, whose middles
are fixed-point: the exact mean was owed and the double lost it. So the decision is made per middle at run time: `MedianBinary64Exact` takes
each argument's binary64-ness beside its exact value (a float item's expansion carries no mark), and `FunctionValueField.Median` reads the
`Float` shape of the two middles.

Review round 1 (R1-1): `FunctionDeviceText.Field`'s default arm described a binary64 arithmetic-expression argument (`(F + 0)`) through the SDIDI lift, so `DISPLAY MEDIAN ((F + 0) M)` showed the exact mean `.30000000000000000277` while COMPUTE stored the binary64 mean `0.29999999999999998889`: the `Float`-shape test could not see an expression. The arm now describes a `NumX.Real` value as a binary64 temporary, as `ValueField` already did for a nested function; the golden gained X1-X3 (DISPLAY, COMPUTE, two expression middles). Open lead, not fixed (a run-time fact the exact lane carries no mark for): a nested mixed selection as a middle, `MEDIAN (FUNCTION MAX (F M2) M)`, shows the binary64 mean on DISPLAY and the exact mean on COMPUTE.

Self-review: architecture (one reader of binary64-ness per channel, no C# in a bound node), full code (15.61.4 2), 15.4.1 and DOC-A.1-92b read, not paraphrased), performance (one small `bool[]` per native MEDIAN with a real argument), duplication (the double conversion in two channel bodies, both over `FloatResultant.FromExact`).

Goldens: `pb4860_a_whole_value_argument_keeps_its_value_in_a_list_that_leaves_the_carrier` (+ `_85` refusal),
`pb4859_even_median_beside_a_binary64_argument_is_the_binary64_mean` (+ `_below_2002` refusal). Rows closed: none (leads, no inventory row).
