## Entry 4084 — 2026-11-27 20:25 PST — A numeric relation against figurative ZERO takes the one numeric comparison renderer

PB6256. `IF X = ZERO` was rendered by a second, partial numeric comparison at the tail of `ConditionRenderer.RenderFigurativeRelational` (through `FigOrNum`), which lacked the exact-wide, SDIDI, unsigned-wide and native-arithmetic arms of `RenderNumericRelational`. Re-probe on this tree before the fix: `IF A * B = ZERO` over two `PIC 9(30)` items holding 1E29 ended the run with EC-SIZE-OVERFLOW (fatal), where `IF A * B = 0` answered FALSE.

Fix: ZERO opposite a numeric anchor is the numeric value 0 (§2023:8.3.3.6.4 GR4) and a numeric relation compares algebraic values (§2023:8.8.4.2.4), so the tail now returns `RenderNumericRelational(r)` (NumericRenderer already reads a ZERO operand as the unscaled integer 0) and `FigOrNum` is deleted. There is one comparison rule, no shim.

The re-probe of the unsigned-wide arm found a sibling defect in `RenderNumericRelational` itself: an unsigned 16-byte COMP-5 item against any bare-literal operand (`IF U = 0` as much as `IF U = ZERO`) generated `CobolNum.CompareU(U, s, 0, s)`, which is a CS0121 overload ambiguity between the (UInt128, Int128) and (UInt128, UInt128) overloads. The signed side is now typed `(Int128)`.

Sweep: level-88 VALUE ZERO / ZERO THRU 5 over unsigned-wide, float, SDIDI and wide-signed items, EVALUATE ... WHEN ZERO over a wide product and an unsigned-wide item, and the sign condition all answer correctly (probes z2, z3 in the wave scratch).

Goldens: `cases/8.3.3.6-Figurative-constant-values/pb6256_figurative_zero_numeric_relation` (2002, 2014, 2023; identical output at all three; values computed from the rule), and its negative `pb6256_figurative_zero_wide_item_1985` (COBOL0802 at --std 85). Rows re-verdicted: none (the note carried no inventory row).

