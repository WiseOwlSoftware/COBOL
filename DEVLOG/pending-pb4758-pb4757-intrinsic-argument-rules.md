## PB4758 and PB4757 — an integral literal fold is proof only when the arithmetic reproduces it; the type-6 screen reads ABS of an unsigned item as the item

Two review leftovers of PB4709, both in `IntrinsicArgumentRules`.

**PB4758.** Re-probe: `MOVE FUNCTION ABS (1 / 3 * 3) TO PIC 9V9999` showed `1.0000` where `COMPUTE` of `1 / 3 * 3` stores `0.9999`. `ProvablyIntegral` took the exact algebraic fold (1) as proof, and `IntegralForm` then rewrote the argument to the literal 1. Fix: the fold counts as proof only when every quotient beneath it has a terminating decimal value (`QuotientsTerminate`, `ExactRational.IsTerminating`); a non-terminating one leaves the call on its Numeric row, so it returns what the arithmetic stores (ABS returns "(argument-1)", §15.7.4 1) a)). `ABS (4 / 2)` and `ABS (1 / 4 * 4)` stay Integer. Golden `pb4758_abs_literal_quotient_value` (SAME, [1  ] [2  ]).

**PB4757.** Re-probe: `FACTORIAL (S2 + FUNCTION ABS (S2))` over `S2 PIC 9V9 VALUE 0.2` compiled and ran (value 0.4 is no integer, §15.3 6)). `NonIntegralItemsBeneath` marks every item under any call opaque, which the PB4709 review needed for `S2 - FUNCTION ABS (S2)`. Fix: `CollectAdditive` reads `FUNCTION ABS` of an unsigned numeric item as that item (`UnsignedArgumentOfAbs`), so the identity is counted instead of hidden: the sum is refused (COBOL1627) and the difference still nets to zero. Signed arguments and other functions stay opaque (sound, incomplete). Golden `pb4757-abs-unsigned-sum-non-integral` plus its diagnostics json.

Rows re-verdicted: none (AR-15.3-6 already CONFORMS). `docs/INTRINSICS_DESIGN.md` carries both readings.
