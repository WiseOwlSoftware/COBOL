## 2026-11-17 - how a function's value lands in a COMP-1 or COMP-2 receiver (wave 20262099 group d; the coordinator's rulings TRUNCATION, narrowed by review R4-1)

**The landing rule, by the value's lane** (`FunctionValueLane.DeliversByNearest`, asked by `NumericRenderer.Render` at a statement's
final transfer):
- **An EXACT value lands by the receiver's mode, which is truncation toward zero without ROUNDED.** This covers the exact fixed family
  (fixed-point, wide, run-time exact) and an SDIDI value that is exact (a floating-point literal, NUMVAL's parse). It lands once, from
  all its digits (`FloatResultant.FromScaled` / `FromExact` / `FromDec`, kb/Work PB4730; kb/Work PB1196 and PB3839 determine the
  literal and NUMVAL-F cases). This is main's rule, unchanged: `COMPUTE F2 = R` prints 2.333333333333333.
- **A value on the 34-digit SDIDI lane into which a quotient that kept significance went is ALREADY an approximation**
  (`NumericLanes.QuotientIsFinal` under a floating-point receiver). It converts to binary64 by round-to-nearest-even, as every binary64
  operation's result does. The receiver then takes that binary64 by its mode: exactly into a binary64 item, by the mode into a binary32
  one.
- **Why the narrowing (review R4-1).** Truncating an approximation compounds its error. `FUNCTION SUM (A / 3, A / 3, A / 3)` is
  6.999...9 on the SDIDI. By truncation it stored 6.999999999999999, a full ulp below the representable exact value 7 that main stores,
  and a MOVE of the result to PIC 9 dropped a whole unit (6). By nearest it is 7.
- **Keyed on the lane, not on how the expression is spelled.** The question is asked of the value's carrier as the one prediction states
  it (`FunctionValueLane`'s argument carrier in the renderer's context), through every operation and nested call: `ABS (SUM (1, A / 3) * 3)`
  is 10 and `ABS (SUM (1.5E0, A / 3) * 3)` is 11.5, both as on main.
- **An exact SDIDI value keeps truncation.** A floating-point literal or a NUMVAL parse is an exact value carried on the SDIDI, not an
  approximation. Applying round-to-nearest to every SDIDI value was measured: it changed `pb288_call_argument_scale_landing` (COMPUTE F =
  1.0E+30, kb/Work PB1196's truncation landing), `pb3839_numval_f_one_value` (COMPUTE and MOVE of a NUMVAL-F value then disagreed) and
  `pb60_numvalf_native_channels`.

**Evidence** (probe `E:/COBOL-coord/scratch/my-20261115/w20262099/d/probes/bis2.cob`, --std 2023; R PIC 9V9(30) holding
2.333333333333333333333333333333, A PIC 9 VALUE 7, F2 COMP-2; main build d77571151b, branch runs under `.../d/r6`):

| Statement | main | branch | the value's lane |
|---|---|---|---|
| `COMPUTE F2 = R` | 2.333333333333333 | 2.333333333333333 | exact: truncation |
| `COMPUTE F2 = R + 0` | 2.3333333333333335 | 2.3333333333333335 | binary64 (D16) |
| `COMPUTE F2 ROUNDED = R` | 2.3333333333333335 | 2.3333333333333335 | exact: ROUNDED |
| `COMPUTE F2 = FUNCTION ABS (A / 3)` | 2.3333333333333335 | 2.3333333333333335 | SDIDI approximation: nearest |
| `COMPUTE F2 ROUNDED = FUNCTION ABS (A / 3)` | 2.3333333333333335 | 2.3333333333333335 | SDIDI approximation |
| `COMPUTE F2 = FUNCTION ABS (A / 3) + 0` | 2.3333333333333335 | 2.3333333333333335 | binary64 (D16) |

Between review rounds 1 and 4, the fourth row printed 2.333333333333333, when the truncation landing applied to the SDIDI approximation
too.

**Not ruled here (a lead for its own note; no id allocated).** Whether truncation is the right default for an EXACT value stored in a
binary64 receiver without ROUNDED, given that the same value as a binary64 operand (`R + 0`) lands by nearest. The fixed argument
working scale this file once named as a second lead is gone: review rounds 1 to 3 replaced it with significance
(`NumericLanes.QuotientIsFinal`).
