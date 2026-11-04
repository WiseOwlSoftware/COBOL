## Entry 3103 — 2026-11-04 13:49 PST — PB4599: a final-transfer quotient divides its operands' exact values

**Re-probe.** `COMPUTE R31 = A * A * A / (A * A)` with A = 999999999999999 (PIC S9(18)) stored 999999999999998; so did
`A ** 3 / A ** 2`, `A ** 3 / (A * A)` and `A ** 3 / A ** Y`, and `A ** 3 / A` stored ...000 where ...001 is owed. The note was
right and still reproduced on the build.

**Decision.** ISO 8.8.1.3 makes native arithmetic implementor-defined, so CLAUDE.md rule 1 sends the choice to GnuCOBOL:
`cob_decimal_div` shifts the exact dividend and truncates, so an integer quotient is exact there. The quotient of a final
transfer therefore divides the operands' exact values and the 34-digit image is only the form the quotient is carried in.
The 34-digit image of a 45-digit dividend divided by its factor is not that quotient.

**Fix shape.** `NumericRenderer.ExactFinalQuotient` takes both operands as `ExactOperand` and `CobolExact.DivToOdd` forms the
quotient on the 256-bit wide form (`CobolWide.QuotientDec`, taking a divisor scale and a `jam` flag so the 34-digit result is
round-to-odd, as `CobolDec.DivToOdd` is). Two arms reach it: the last
line of `Divide` (a final transfer over a wide operand, a floating-point edited resultant) and the exact arm of `CombineCore` (a final
transfer over a run-time-scaled exact value, whose operands were lowered before). `RuntimeApi.DecDivToOdd` lost its last caller and was
deleted, with its exemption in `SdidiOperandLiftDriftTests`.

**Siblings found on the way and fixed with it** (same mechanism, other doors): the several-receiver COMPUTE rendered its root
quotient's and product's operands settled, so `COMPUTE R R2 = A * A * A / (A * A)` stored ...998 in both receivers (`keepWide` on the
root operands, a registered settling site); `DIVIDE`/`MULTIPLY ... GIVING` over a FUNCTION operand whose value is an exact wide
intermediate rendered it settled too; and `DIVIDE ... GIVING Q REMAINDER R` over such operands gave quotient ...998 and remainder
997999900000000000. The carried arm now takes the senders exact, and its subsidiary quotient joins the back-multiply as an exact
value, because a 15-digit quotient times a 30-digit scaled divisor kept the SDIDI lane's 34 digits (DOC-A.1-82) and left the remainder
one off (6 for 7). Left as a lead: a quotient wider than the SDIDI itself (42 digits) with a size error and a stored remainder.

**Goldens.** `pb4599_final_transfer_quotient_over_exact_operands` (the witness, `**` forms, signs, scales, a tie that ROUNDED sees,
SDIDI divisor, DIVIDE GIVING, REMAINDER, the relation and the zero divisor; accept 2002+, refused at 85), and
`pb4599_final_transfer_quotient_prohibited` (an exact quotient does not raise under PROHIBITED, an inexact one does; accept 2014+,
refused at 2002). Expected values are Python integers and fractions. Unit: `CobolExactIntrinsicsTests.FinalTransferQuotient_DividesTheExactOperands`.
DOC-A.1-123 and the numeric design now say the final-transfer quotient is exact. The note claimed no inventory row and closes none.

**Self-review.** Architecture: one function forms the final-transfer quotient for every arm, no C# text in a bound node. Code: the
rule cited is 14.9.12.4 7) for the remainder and 14.7.5 2) for the zero divisor (both `cite.py --check` OK). Performance: the exact
quotient is one 256-bit division, as `CobolDec.Div` already did. Duplication: `QuotientDec` has one body for the integer-divisor
callers (MEAN, VARIANCE, PRESENT-VALUE) and the general one.

**Branch review, round 1 (two blocking findings, three leads).**

R1-1: a divisor past 41 digits left the 256-bit form once the dividend was aligned 36 digits past it, and the first version fell back to
the 34-digit images, so `COMPUTE R = A * A * A * A / (A * A * A)` of A = 123456789012347 (a 57-digit dividend over a 43-digit divisor)
stored ...346 and, with REMAINDER, a remainder of 461552779000000000 for 0. The root cause was the `Try` contract itself: the quotient
digits past the integer part can be drawn from the remainder one at a time (`CobolWide.NextQuotientDigit`: ten additions of the
remainder, one subtraction of the divisor each, a carried sum wrapping back into range as `DivideMagnitude`'s carried remainder does),
which never leaves the form, so `TryQuotientDec` became `QuotientDec` with no failure return and its callers (VARIANCE, PRESENT-VALUE)
lost their dead image fallback arm of this quotient. The fallback of `CobolExact.DivToOdd` was dead code as well (`TryAt` at the value's
own scale cannot fail), which is why no test reached it (R1-4); both are gone. DOC-A.1-123 no longer says 'operands past 77 digits'
for this quotient: an operand is a wide value or an SDIDI significand.

R1-2: `MULTIPLY FUNCTION ABS (B * B * B) BY C` (the in-place format) settled its operand in `NumericRenderer.Fold` to the 34-digit image
and stored 0.999 where `GIVING` and `COMPUTE` store 1.000 (B = 5**17/10**17, C = 2**51, so B*B*B*C is 1). `Fold` takes `keepWide`,
`EmitInPlace` passes it, and the receiver's `Combine` settles once at the final transfer; ADD and SUBTRACT, the same door, follow.

R1-5 (lead, in a file already changed): the exact arm of `CombineCore` used to be `DecBinary`, which carried the program's INTERMEDIATE
ROUNDING mode, so `OPTIONS. INTERMEDIATE ROUNDING IS PROHIBITED.` with `COMPUTE R = A * A * A / 7.0E30` was a size error; round-to-odd
had silently dropped that screen. `CobolExact.DivToOdd` takes the mode and, under PROHIBITED, screens the quotient like every SDIDI
intermediate (ISO 11.9.11.2 rule 3 d)); the plain quotient has no SDIDI intermediate and passes TRUNCATION. R1-3 (the opt-in sites are
registered in `ExactWideSettlementDriftTests`, each pinned to the former that settles it) and R1-4 are closed.

Goldens added: legs BIGD, BIGD2, BIGDR and MULI in `pb4599_final_transfer_quotient_over_exact_operands`, and
`pb4599_final_transfer_quotient_intermediate_prohibited` (accept 2014+, refused below by `..._85`). Unit:
`FinalTransferQuotient_DividesByAnyDivisorOfTheWideForm` (divisors of 43 and 77 digits, expected values from Python integers and
fractions) and `FinalTransferQuotient_UnderProhibited_RaisesOnAnInexactQuotient`.

