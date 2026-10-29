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
those digits. An extent past the 38-digit scaled carrier is still held (branch review R1-1): the first cut left such an
expression un-hoisted, evaluated again at each WHEN, but the bound is an upper bound and not the value, and the freeze exists
because a later part of the statement can change an operand. `FUNCTION SUM(WS-B * WS-B * WS-B, FUNCTION RVBMP(WS-B))`
(WS-B PIC 9(15), RVBMP adds 1 to WS-B) printed 33 instead of 14, and `EVALUATE WS-B * WS-B * WS-B WHEN FUNCTION RVBMP(WS-B)
WHEN 8` took WHEN 27 instead of WHEN 8; the S9(21)V9(17) hold they replaced answered both. So `ValueOperandPicOf` always answers an
item. Branch review round 1 (R1-1) then showed the first form of that item, which kept ALL the expression's fraction digits
and gave the integer part the rest, cut the integer part of `PIC 9V9(19) * PIC 9V9(19)` (bound 2 + 38) to one digit, so
`EVALUATE A * B` with 3 and 4 took `WHEN 2` where the old hold and `IF A * B = 12` answered 12, and cut `PIC 9(10)V9(15)`
12345 * 10000 to 8 integer digits. The item is now a superset of the S9(21)V9(17) hold on both axes: integer digits keep
priority up to the bound (the order `ShapedPic` keeps) but never below the 17 fraction digits the old item held, the fraction
takes what remains (S9(2)V9(36) and S9(20)V9(18) for those two), so every value the old hold held is held whole. Branch review round 2 (R2-1) found the floor written as
`ReturnedFractionDigits`, which is `FunctionValuePic`'s 9, not the old hold's 17: `PIC 9(15)V9(12)` 1.000000000001 times 1 (bound
30 + 24) was held S9(29)V9(9) and took `WHEN 1`, where main's S9(21)V9(17) held it exactly. The floor is now read off the item it
supersets, `ValueOperandPic.Scale`, and the golden gained the 30 + 24 cases (the 12th and the 17th fraction digit), which fail on
the round-1 build; the earlier cases (2 + 38, 20 + 30) could not tell 9 from 17. A value of more than 38 digits is cut, the residual shared with PB3268 (a wide
intermediate item). An expression holding a quotient, a power or a function keeps the 21 + 17 item, the residual: its width is
not a bound anyone states.
Sibling arms: `ArgumentOrder`'s frozen function argument holds an arithmetic expression through the same `Materialize` path and is
fixed with it (`COMPUTE R = FUNCTION MAX(BIG * BIG, T(FUNCTION MIN(1 2)))` stores the whole 30-digit product); the MOVE, INVOKE
and SET holders carry function values and character text; the subscript temporary is a saturated integer position.

**Witnesses.** Golden `14.9.13-EVALUATE/pb3974_evaluate_expression_subject_width` (nine EVALUATEs at every edition: 30-digit
products, a negative product, a 24-digit fraction, a 31-digit sum, two subjects, a range object, a 39-digit bound over a
30-digit value, and an unequal control; the values are exact arithmetic of
the items), `pb3974_expression_subject_past_carrier_fraction` (from 2002: a 2 + 38 digit bound over 12 and a 20 + 30 digit bound over 123450000, round 1 R1-1; two 30 + 24 digit bounds over 1.000000000001 and 1E-17, round 2 R2-1) and `pb3974_expression_subject_frozen_past_carrier` (from 2002: the review's two UDF scenarios at a 45-digit bound,
`FUNCTION SUM` = 14 and the EVALUATE taking WHEN 8); no negative, since the change refuses no source. The 45-digit VALUE
(`BIG * BIG * BIG`) the first cut's golden held through the un-hoisted path is cut by the 38-digit item and is the residual. Unit theories `ExpressionExtentTests`. Docs: `DESIGN-binder-bound-tree.md` (the intermediate's description).
`kb/Work/PB3974` flips to landed with `closes_rows: []` (no inventory row claimed it).
