## PB4981 retired, PB4975 float-edited wide-significand quotient rounded once, PB4979 binary64 mark read by VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE, ANNUITY and the subsidiary quotient

Wave 20261925, group B (the numeric renderer). Three notes, three outcomes.

**PB4981 (retired, a probe artefact).** The claim was that under `ROUNDED MODE TRUNCATION` or `AWAY-FROM-ZERO` a non-integer literal exponent over an
intrinsic base renders as an integer (0.7 as 0, 1.5 as 1). Instrumenting `NumericRenderer` showed the literal reaching `Power` already bound as `0`: the
reviewer's fixed-format line `COMPUTE RR ROUNDED MODE AWAY-FROM-ZERO = FUNCTION MAX (M M2) ** 0.7` is 74 characters, and columns 73 to 80 are the
identification area, so the source the compiler read ended at `** 0`. The item-base line of the same probe was short enough to survive, which is why the
note found "a direct item base is correct". Re-probed with lines inside column 72 and in free format over ABS, SQRT, MAX, INTEGER and a float MAX under
TRUNCATION, AWAY-FROM-ZERO, TOWARD-GREATER and NEAREST-EVEN: every result is the binary64 power rounded in the receiver's mode. Nothing to fix.

**PB4975 (fixed).** A floating-point edited receiver whose significand holds 33 to 36 digits (ISO 13.18.40.3 SR15) is not two digits coarser than the
SDIDI's 34, so the final-transfer quotient reduced to 34 digits (by `CobolDec.Div` in the program's mode, or the exact lane's round-to-odd) was rounded again
by the receiver. Probe: `9.999999999999999999999999999999999E-1 / 2.0E0`, whose exact value is .49999999999999999999999999999999995, stored `.5000...` in a
truncating 33, 34, 35 and 36 digit receiver, and `A * A / 7` of a 21-digit A lost every digit past the 34th. The earlier golden pinned 2/9, where the double
rounding is invisible. Fix: `CobolBig.QuotientToOdd` reduces the exact quotient by round-to-odd to the receiver's significand plus two digits (at most 38, still an
`Int128` significand); `CobolDec.DivFinal` and `CobolExact.DivToOdd` take the significand and use it past 32, PROHIBITED excepted (it screens at 34 as before);
`NumericRenderer.FinalTransferSignificandDigits` hands it over for the final transfer only. The `<= 32` bypass in `DecBinary`, with its `CobolDec.Div` fallback,
is gone. Golden `pb4975_float_edited_wide_significand_quotient` (+ `_85` refusal), unit `CobolDecFinalQuotientWideTests`.

**PB4979 (fixed).** PB4923 made a nested selection's binary64 content a run-time mark that SUM, MEAN, MIDRANGE, RANGE and the arithmetic operators read.
Four more bodies and the subsidiary quotient did not: `VARIANCE (FUNCTION MAX (F M2) M)` kept the exact decimal of the float's expansion
(.03999999999999999889) where `VARIANCE (F M)` is 0.04, and likewise STANDARD-DEVIATION, PRESENT-VALUE and ANNUITY, on DISPLAY and COMPUTE alike; and
`DIVIDE M BY FUNCTION MAX (F M2) GIVING FQ REMAINDER FR` truncated the exact operands' quotient 4.99999999999999972244 to 4.99999 where the binary64 quotient 5.0
truncates to 5.00000, leaving the remainder .00000099999999991773 (`DIVIDE M BY F` stores 0). Fix at the one mark reader per body: `VarianceExact`, `StdDevExact`,
`PresentValueExact` and a new `AnnuityExact` return `CobolExact` through `TryBinary64Body`, which gained a `floatFamily` flag because these four are the
floating-math family whose binary64 value is always an approximation (it lands by its shortest decimal, as the float item's own `VARIANCE (F M)` does, not by
the item's exact expansion); `CobolExact.SubsidiaryQuotient` truncates the binary64 quotient when an operand is a float item's content. `RenderDec` emits the
four as `NumX.Exact`, `FunctionValueLane.NativeSdidiBodiesWithWholeResult` names them, and `NarrowResult` reads the mark for the float lane. Golden
`pb4979_functions_over_a_nested_selection_of_a_float_are_binary64` (+ below-2002 refusal), units `CobolExactFloatFamilyBodiesTests`. DOC-A.1-92b, NUMERIC_DESIGN and
INTRINSICS_DESIGN carry the determination. Sibling sweep: ABS, FRACTION-PART, REM, INTEGER-PART and INTEGER over a nested selection of a float already equal the
float item's value.

No inventory row is claimed or closed by any of the three (`closes_rows: []` with the reason in each note). No diagnostic code was needed.
