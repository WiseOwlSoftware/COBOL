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
