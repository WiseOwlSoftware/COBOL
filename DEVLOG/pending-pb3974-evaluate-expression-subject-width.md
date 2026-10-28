## An EVALUATE subject that is an arithmetic expression is held at the width of its value (PB3974)

Wave 20261735, group B. One mechanism, one checkpoint commit.

**Re-probe on base `7121f254f`.** The repro from the PB3953 review (`EVALUATE A * B * C WHEN 1 ... WHEN A * B * C`, A, B and C
PIC 9(10) all 9999999999) took WHEN OTHER; so did `EVALUATE BIG * BIG WHEN 1 ... WHEN BIG * BIG` (BIG PIC 9(18)). A wider probe
found the fraction side of the same defect: `EVALUATE SM * SM WHEN 0 ... WHEN SM * SM` (SM PIC V9(12), the product 1E-24) took
`WHEN 0`, because the subject was held at 17 fraction digits and read as zero. `A * B` (20 digits, inside the 21 integer digits)
was already right, which is why nothing had caught it.

**Root cause.** The subject's intermediate result item (ISO 14.9.25.4 GR1's "temp", reached by 14.9.13.4 GR3) is described by
`SendingValueTemp.IntermediatePic`, which ended in the fixed `ValueOperandPic`, S9(21)V9(17), for every value no receiver bounds.
An arithmetic expression is such a value, and the un-hoisted expression yields all its digits: 14.9.13.4 GR3 c) assigns the
subject "a numeric value according to the rules for evaluating an arithmetic expression" and GR4 a) 6. compares that value with
each object. A fixed description cannot be the value's description, whatever its width (PB3268 had reached the same conclusion
for a function value).

**Fix.** `ExpressionExtent` (new, `Binding/Procedure`), a visitor over the generated expression interface, states the integer
digits and the scale of an expression of `+`, `-`, `*`, unary minus, numeric literals and fixed-point items: a sum below
10^i and 10^j is below 10^(max(i, j)+1), a product is below 10^(i+j) with the scales added. That is a mathematical bound, so it
copies none of `NumericRenderer`'s carrier rules. `SendingValueTemp.ValueOperandPicOf` builds the fixed-point item of exactly
those digits. A value past the 38-digit scaled carrier has no intermediate item; `Materialize` answers null and the expression
stays un-hoisted, evaluated again at each WHEN (a function-free expression of data items yields the same value each time, where
a value cut to fit would compare a different one, the same decision `OfPlace` makes for a variable-length group). An expression
holding a quotient, a power or a function keeps the 21 + 17 item, the residual: its width is not a bound anyone states.
Sibling arms: `ArgumentOrder`'s frozen function argument holds an arithmetic expression through the same `Materialize` path and is
fixed with it (`COMPUTE R = FUNCTION MAX(BIG * BIG, T(FUNCTION MIN(1 2)))` stores the whole 30-digit product); the MOVE, INVOKE
and SET holders carry function values and character text; the subscript temporary is a saturated integer position.

**Witnesses.** Golden `14.9.13-EVALUATE/pb3974_evaluate_expression_subject_width` (nine EVALUATEs at every edition: 30-digit
products, a negative product, a 24-digit fraction, a 31-digit sum, two subjects, a range object, a 45-digit product that stays
un-hoisted, and an unequal control; the values are exact arithmetic of the items); no negative, since the change refuses no
source. Unit theories `ExpressionExtentTests`. Docs: `DESIGN-binder-bound-tree.md` (the intermediate's description).
`kb/Work/PB3974` flips to landed with `closes_rows: []` (no inventory row claimed it).
