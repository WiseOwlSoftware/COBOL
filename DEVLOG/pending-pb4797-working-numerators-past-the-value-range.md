## VARIANCE and PRESENT-VALUE no longer overflow on their own numerator (PB4797)

`FUNCTION VARIANCE (0, 0, 0, 0, 0, 0, 0, 0, 0, 2.0E+3072)` ended in a fatal EC-SIZE-OVERFLOW although the variance, 3.6E+6143, is in range.
The note's re-probe on this branch reproduced it; PRESENT-VALUE (1.0E+3100, 1, 1, 1) failed the same way on its divisor, (1 + rate)^3.

Root cause: `CobolBig.Of` checked every intermediate of the arbitrary-precision form against the 10^6145 a program's value is held to.
VARIANCE's numerator is n² times the variance and PRESENT-VALUE's divisor is the discount power, so neither is a value of the program.

Fix: a working flag on `CobolBig`, carried by sum, difference, product, negation and power and bounded by the working size alone;
`CobolExact.Working()` sets it and `CobolExact.Of` keeps such a value on the arbitrary-precision form. `VarianceBig` and `PresentValueBig`
take their operands as working values, and the one closing quotient returns the SDIDI value, whose range its consumer checks. A variance
whose own value passes 10^6145 is still a size error (unit test).

Golden `pb4797_working_numerators_past_the_value_range` (accept 2002 and later: the floating-point literal is a 2002 form) with its
`_85` refusal (COBOL0900). A unit test pins the three legs. The sibling bodies (SUM, MEAN, RANGE, MOD, REM, MEDIAN, MIDRANGE) form
nothing larger than their result by more than the argument count and are left. No inventory row changed.

Review round 1: R1-1 (DOC-A.1-123 still published the unconditional 10^6145 intermediate check and the SDIDI-image evaluation of VARIANCE/PRESENT-VALUE) is fixed in docs/CONFORMANCE.md; R1-2 (CobolBig type and Of summaries) is fixed in CobolBig.cs.
