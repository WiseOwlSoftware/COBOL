- **D-FLOAT-TO-SDIDI — Floating-point data item → standard-decimal intermediate data item: the conversion mechanism (§2023:8.8.1.5.1;
  kb/Work PB1606, 2026-10-23).** §2023:8.8.1.5.1 leaves "the mechanisms for conversion of data items described with a usage
  of float-short, float-long, or float-extended into standard-decimal intermediate data items" to the implementor, and
  §2023:8.8.1.5.2 r1 says an operand that "cannot be expressed exactly in an SDIDI" is "rounded according to the rules in
  11.9.11"; NOTE 3 of that clause names the binary value as the case. WiseOwl COBOL, under ARITHMETIC IS
  STANDARD-DECIMAL (and STANDARD, which shares the engine): **a floating-point data item enters the SDIDI as its
  binary64's EXACT algebraic value — a double is always a terminating decimal, so "the operand value" is never in doubt —
  rounded ONCE to the SDIDI's 34 significant digits under the program's INTERMEDIATE ROUNDING mode**
  (`CobolDec.FromBinary64`; default NEAREST-AWAY-FROM-ZERO): a COMP-2 holding 0.1 enters as
  0.1000000000000000055511151231257827, a value of 34 digits or fewer (0.5, every integer below 10^34) enters
  unchanged, and under INTERMEDIATE ROUNDING IS PROHIBITED an inexact one is EC-SIZE-TRUNCATION (§2023:11.9.11.2 r3 d).
  The same ONE conversion serves every position: an operand beside an operator, a **float item that is the whole
  expression** (`COMPUTE R = F`, `ADD F GIVING R` — before, these landed natively while `F + 0` converted through
  the shortest round-trip decimal, so one statement mode held two float conversions and `COMPUTE D = F` stored
  `.1000000000000000055` where `COMPUTE D = F + 0` stored `.1000000000000000000`), a relation operand (§2023:8.8.4.2.4)
  and an intrinsic function's argument. It is also the value a MOVE or a native COMPUTE reads (the item above), so
  `COMPUTE D = F`, `COMPUTE D = F + 0` and `MOVE F TO D` agree. The choice follows the precedence of CLAUDE.md rule 1:
  the standard leaves the mechanism open, GnuCOBOL 3.2.0 has no standard-decimal mode and no precedent to follow
  (measured 2026-10-23: it stores a COMP-2 0.1 as 0.099999999999999991673327315311 by `COMPUTE D = F`, `COMPUTE D = F + 0`
  and `MOVE F TO D` alike, and says `F = 0.1` is true), so the choice is the implementor's, made for
  agreement with the PB623 float-landing determination above. **Consequences, stated so they are not found by surprise:** (a) a
  float item holding a decimal fraction is not equal to that decimal under standard-decimal arithmetic — `IF F = 0.1`
  with F = 0.1 (COMP-2) is false and `IF F > 0.1` true (native arithmetic compares both as binary64 and says equal);
  (b) landing into a binary float receiver follows the implied TRUNCATION (§2023:14.7.4.3 r2 and r10): the SDIDI
  0.1000000000000000055511151231257827 is below the double 0.1, so `COMPUTE F2 = F1` and `COMPUTE F2 = F1 + 0` both
  store the double below it, 0.09999999999999999 (the same with ROUNDED, where the nearest double is chosen and
  F2 is 0.1); (c) a COMP-2 holding 1.0E+30 is 1000000000000000019884624838656, and that is what standard-decimal
  arithmetic divides; (d) a function whose argument is a float item and whose value is the SDIDI shows the SDIDI's digits
  (`DISPLAY FUNCTION ABS (F)` with F = 0.1 is `0.1000000000000000055511151231257827`, with F = 0.5 it is `0.5` — a value
  of 34 digits or fewer is held at the fewest places that carry it). **An implementor-defined APPROXIMATION is not a data item** — a floating-math function's returned
  value (SIN, SQRT, a non-integer power), or a function of approximations only (`ABS (FUNCTION EXP10 (-6))`), has no content beyond its 17th digit that means
  anything — and enters through `CobolDec.FromApproximation`, the shortest round-trip decimal, so `FUNCTION SIN (1)`
  is 0.8414709848078965 and not the 34 digits of the double's binary expansion. **That decimal is the value at EVERY
  landing into a fixed-point receiver** (§2023:15.4: a function's evaluation produces ONE returned value): a bare `COMPUTE`
  (`CobolIntrinsics.FromDouble`), a `MOVE`, an alignment and a numeric-edited receiver round it once, in the receiver's
  mode, through `CobolFloat.ToScaledApproximation`, `ToScaledApproximationUnchecked` and `ToScaledApproximationIntermediate`
  (the decimal lane's `ToUnscaledChecked`, `ToUnscaled` and `ToUnscaledIntermediate`), so `COMPUTE R = FUNCTION EXP10 (-6)`
  into `PIC S9(9)V9(6)` stores 0.000001 exactly as `COMPUTE R = 0 + FUNCTION EXP10 (-6)` does, and `ROUNDED MODE IS PROHIBITED`
  accepts it (the double beneath one millionth, 9.99999999999999954748E-7, is not representable at six places). A
  floating-point DATA ITEM lands by its exact binary value (`ToScaled`, `ToScaledUnchecked`, `ToScaledIntermediate`), and so does a value
  a function derives from a float ITEM's content (kb/Work PB4622): MAX, MIN and an odd MEDIAN return "the content of the argument"
  (§2023:15.59.4 1), §2023:15.63.4 1), §2023:15.61.1), INTEGER the greatest integer not above it (§2023:15.44.4 1)), and ABS, INTEGER-PART, FRACTION-PART, REM, SUM,
  MEAN, RANGE and MIDRANGE have equivalent arithmetic expressions that the item's binary64 evaluates exactly or as the written-out expression
  does (D16), so `MOVE FUNCTION ABS (DN) TO X5` stores 73792 where `MOVE DN TO X5` does, and `COMPUTE R ROUNDED MODE IS PROHIBITED = FUNCTION
  ABS (A1)` raises the size error `= A1` raises (`IntrinsicCarrierRules.Binary64ResultIsApproximation`; witnesses
  `conformance:cases/15.4.1-Numeric-and-integer-functions/pb4622_float_item_function_value_exact`, `…_prohibited`, `…_standard_decimal`). An
  infinity has no decimal and keeps its binary64 disposition. Witnesses:
  `conformance:cases/15.35.4-Returned-value-rule/pb4516_function_value_lands_one_way`,
  `conformance:cases/15.35.4-Returned-value-rule/pb4516_function_value_prohibited`,
  `CarrierLandingFormTests.ApproximationLanding_IsTheShortestDecimal_NotTheBinaryExpansion`. The floating-point numeric-edited
  receiver reads a binary64 sender the same exact way (`CobolDec.FromBinary64ToOdd`, a 38-digit round-to-odd
  significand, because the mask's rounding is the receiver's): `COMPUTE FE ROUNDED MODE IS PROHIBITED = F` with F =
  0.1 is the size error (the value is not representable in the mask), as it already was for a fixed-point
  receiver. That exact reading is for a floating-point DATA ITEM alone: a fixed-point sender (a BY CONTENT argument at
  §2023:14.2.3 GR9's activation boundary, `MoveSending.ExactValue`) is edited by its own decimal digits, and an
  approximation (a function's returned value) by its shortest round-trip decimal (`CobolDec.FromApproximation`), so
  neither shows the binary noise of a double (0.3 is `+3.0000000000000000E-01`, never `+2.9999999999999998E-01`).
  The binary64 intermediate a multi-receiver MOVE freezes such a value into (§2023:14.9.25.4 GR1's "temp", "an intermediate
  result item provided by the implementor") is read as the approximation it holds (`DataItem.HoldsApproximation`), so
  `MOVE FUNCTION PI TO W1 W2` edits what `MOVE FUNCTION PI TO W3` does.
  Witnesses: `conformance:cases/14.6.8.4-Floating-point-numeric-edited-receiving-data/pb1606_fixed_sender_float_edited_formal`, `conformance:cases/14.9.25-MOVE/pb370_move_function_value_digits`, `conformance:cases/8.8.1.5.2-Standard-decimal-intermediate-data-item/pb1606_float_item_conversion`
  (every position, the rounding modes, the negative and the huge), `15.60.4-Returned-value-rule/arith_standard`,
  `_unassigned/pb3017_sdidi_divide_remainder_beyond_the_int128_carrier`, `CobolDecBinary64ConversionTests`.
