## Entry 3260 — 2026-11-07 10:51 PST — PB4891: an exact value carries its own binary64 mark, so an even MEDIAN over a nested selection of a float item is the binary64 mean on COMPUTE as on DISPLAY

**The defect.** `FUNCTION MEDIAN (FUNCTION MAX (F M2) M)`, with `F` a FLOAT-LONG 0.1 that MAX selects, DISPLAYed the binary64 mean
0.3 and COMPUTE stored 0.30000000000000000277, the exact mean of the float's expansion. The re-probe on dec152346c reproduced it for
MAX, MIN and an odd MEDIAN as the nested selection, and for `MEDIAN (MEDIAN (F M) N)` (COMPUTE 0.49999999999999999444, DISPLAY 0.5).
ISO §2023:15.59.4 1) and §2023:15.63.4 1) make MAX's and MIN's returned value "the content of the argument" selected, so the nested call
returns F's binary64, and §2023:15.61.4 2)'s mean of two middles of which one is binary64 is the binary64 mean (§2023:15.4.1's native
latitude, docs/CONFORMANCE.md DOC-A.1-92b, PB4859). DISPLAY already carried the kind in its `FunctionValueField`; COMPUTE asked a
compile-time list of the written operands' kinds (`IntrinsicRenderer.Binary64FlagList`), and a nested selection's kind is a run-time fact.

**The fix.** The mark moves into the value. `CobolExact.FromBinary64` marks a float item's content (an approximation already remembered
its double), `CobolExact.TryBinary64` reads it, a selection returns the value whole, `Negate` and `WithPrecision` keep it, and every
operation that forms a new value drops it. `CobolIntrinsics.MedianExact` and `MedianWhole` became one body, `CobolIntrinsics.Median`,
that orders the arguments stably and reads the two middles' marks; `MedianBinary64Exact`, its `bool[]` and the renderer's flag list are
deleted, so the next path that hands MEDIAN a binary64 is covered with no change. The mark then exposed a channel split it had
created: `MEDIAN (- FUNCTION MAX (F M2) 1)` stored the binary64 mean 0.45000000000000001110 while DISPLAY, which describes an
exact-lane expression argument through `FunctionValueField.OfExact`, showed the exact mean. `OfExact` now takes the intermediate mode
and describes a marked value as the binary64 temporary (`OfBinary64` or `OfFloat`), so DISPLAY shows 0.45.

**Goldens.** `15.61.4-Returned-value-rules/pb4891_even_median_of_a_nested_selection_of_a_float_is_the_binary64_mean` (MAX, MIN and odd
MEDIAN nested, a nested even MEDIAN, a negated nested MAX, and a nested MAX that selects the fixed-point item, which keeps the exact
mean; expected values by Python floats) and its `_below_2002` refusal. No inventory row moves: RV-15.61.4-2 already CONFORMS.

**Left as leads.** SUM, MEAN, MIDRANGE and the arithmetic expression over a nested selection of a float item keep the exact value on
both channels (`SUM (MAX (F M2) M)` is .6000000000000000055..., where `SUM (F M)` is the binary64 0.6). DOC-A.1-92b keys on an argument
that "renders as floating-point", so whether a nested selection's binary64 content counts is a determination question, not a channel
split; and the DISPLAY channel's binary64 mean is an approximation temporary (`OfFloat`) where COMPUTE's is a float item's content.

**Self-review.** Architecture: one mechanism (the value's own mark) replaces a parallel compile-time list; no C# text in a bound node.
Code: the cited rules were re-checked with cite.py; the odd count now selects stably, as `FunctionValueField.Median` does. Performance:
one bool and a struct copy on `FromBinary64`, and an index sort in place of the LINQ `OrderBy`. Duplication: two MEDIAN bodies became one.

