## PB4750 - VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE and the function-value temporary stay exact past the wide form

**Re-probe.** On the base build `VARIANCE (Q, Q + 1)` of a 78-digit Q stored 0.0000 (the exact value is 0.25), PRESENT-VALUE over
such amounts left the 34-digit residue of its terms, and `DISPLAY FUNCTION MAX (Q, 1)` / `ABS (Q)` / `MEDIAN (Q, 1)` showed the
34-digit round-to-odd image of Q followed by zeros instead of all 78 digits.

**Root.** `CobolIntrinsics.ExactValue.cs` took the wide 256-bit form first and, where a step left it, fell back to the SDIDI bodies
over the arguments' 34-digit images (`VarianceDec`, `PresentValueDec`) although `CobolExact` has carried an arbitrary-precision form
since PB4592. `FunctionValueField.OfExact` likewise lowered every value past the wide form to its image.

**Fix.** `VarianceBig` and `PresentValueBig` form the same identities (`n*sum(d^2) - (sum d)^2 over n^2`; Horner's `sum a_k d^(n-k)`
over `d^n` for `d = 1 + rate`) from `CobolExact` operations and divide once. The §15.74.3 r2 screen (`1 + rate <= 0`) moved to the
one place before both forms. `FunctionValueField`'s `Wide` shape became an `Exact` shape holding the `CobolExact` itself, so a
wide or arbitrary-precision value shows every digit, compares exactly, takes ABS and the even MEDIAN's exact mean (the exact sum
times 5 at scale 1) by one rule. `CobolNum.ReturnedValueProfile(CobolExact, ...)` and `DeviceFunctionText(CobolExact, ...)` carry
the big arm.

**Goldens.** `15.4.1-Numeric-and-integer-functions/pb4750_exact_past_the_wide_form_variance_present_value` (VAR 0.25, SD 0.5,
PV 1 and 0 past the wide form, 78-digit MAX, ABS and MEDIAN, expected values derived with Python integers) and its refusal at 85
(`pb4750_..._85`, COBOL1502). `docs/INTRINSICS_DESIGN.md` "What still takes the image" no longer lists the three functions.

**Rows.** None claimed or closed (`closes_rows: []`, DOC-A.1-123 already CONFORMS).

**Review round 1.** R1-1: `FunctionValueField.OfExact` lowered a wide value at a negative scale that `TryWide` cannot write out
(`ABS (PA * PA * PA)` with `PA PIC 9(20)P(10)`) to its 34-digit image; it now takes the exact temporary for every non-SDIDI value,
`ReturnedValueProfile(CobolExact)` reads `CobolExact.MagnitudeDigits()` on every form, and the golden gained PABS and PMAX legs.
R1-2: `BigParts` (a raw `BigInteger` crossing into `CobolNum`) is replaced by that digit-text member, so no `BigInteger` use is added
outside `CobolBig.cs`. R1-3 (lead, `VarianceBig` range on arguments past 10^3000) is left for the clerk: not in a file this round changes.

**Review round 2.** R2-1: `CobolExact.Compare` ordered a mixed SDIDI/wide pair by the SDIDI sign when its integer left the 256-bit range, but a wide value at a negative scale (`PA * PA * PA`, 1.88E+84) can be larger, so MAX, MIN and MEDIAN against 1.0E+80, the COMPUTE quotient and the IF comparison chose the wrong argument. That pair now goes to `CobolBig.Compare`; the golden gained CMAX, CMIN and CMED legs (the same arm served the COMPUTE and IF lanes, probed: 18816 and GT). R2-2: the golden header now derives PA as 1234567890123456789 * 10**10, the 29-digit VALUE.
