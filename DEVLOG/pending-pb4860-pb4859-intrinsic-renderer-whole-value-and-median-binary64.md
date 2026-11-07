## A nested whole-value call keeps its value in SUM, RANGE and MIDRANGE, and an even MEDIAN beside a binary64 argument is the binary64 mean on every channel

Two leads of one root, `IntrinsicRenderer` and its carrier rules (kb/Work PB4860, PB4859).

**PB4860.** `COMPUTE R ROUNDED = FUNCTION SUM (FUNCTION SUM (A B) Z)` with a 26-digit `A`, `B` = .004999999 and `Z` = 0 stored `.01`
where `A + B + Z` stores `.00`. The inner call's value is a whole value (`NumX.Exact`); `RawArg` lowers it to its 34-digit image, the
renderer read that as an SDIDI operand and `AnyDecRaw` sent the list to `SumExact` before the whole-value arm could ask. The whole-value
arm now precedes it (its standard-mode half was dead code), and `AlignedOperands` reads each argument unlowered, a `NumX.Exact` one
as an operand of unknown bound and run-time scale (`AlignedOperand.RunTimeScaled`). Genuine SDIDI and binary64 operands are unchanged.

**PB4859.** An even MEDIAN beside a binary64 argument formed the exact mean of the float's SDIDI lift (`DISPLAY MEDIAN (F M)` showed
`.30000000000000000277`, COMPUTE stored it) and DISPLAY of two binary64 middles kept the SDIDI mean while COMPUTE gave the binary64 mean.
DOC-A.1-92b: any floating-point argument evaluates the function in binary64. `SelectionNeedsExactLane` no longer sends an even MEDIAN
to the exact lane, the run-time-count body `MedianBinary64Exact` takes any list with a binary64 argument, and
`FunctionValueField.Median` forms the double mean when any argument is a float.

Goldens: `pb4860_a_whole_value_argument_keeps_its_value_in_a_list_that_leaves_the_carrier` (+ `_85` refusal),
`pb4859_even_median_beside_a_binary64_argument_is_the_binary64_mean` (+ `_below_2002` refusal). Rows closed: none (leads, no inventory row).
