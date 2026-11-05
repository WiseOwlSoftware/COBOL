## Entry 3116 — 2026-11-04 16:22 PST — PB4640: DIVIDE ... GIVING Q REMAINDER R forms its subsidiary quotient from the exact operands when the quotient passes 34 digits

**What was wrong.** `DIVIDE FUNCTION ABS (A * A * A + 4321) BY 1000 GIVING Q REMAINDER R` (A = 10^15 - 1, Q `PIC S9(18)`) divides a
45-digit dividend: the quotient has 42 digits, a size error at Q, and with checking off and no SIZE ERROR phrase DOC-A.1-70 stores
the remainder anyway (GnuCOBOL 3.2). R came out 99999999994 where ISO 14.9.12.4 rule 7 gives 320 (the dividend less the subsidiary
quotient times the divisor). Re-probed on the build before the fix (PB4599's `rem2.cob`, REMD): the same shape with `+ 7` stored
99999999994 where 6 is owed. The cause was one step upstream of the SDIDI: the carried arm of `ArithmeticEmitter` took the subsidiary
quotient from `FinalQuotient`, the quotient the statement stores, which `CobolExact.DivToOdd` holds to the SDIDI's 34 digits. Truncating
that at Q's scale kept 34 digits of a 42-digit quotient, so the back-multiply and subtraction ran on a quotient eight digits short.

**The fix.** The note called it a redesign of the subsidiary quotient onto the wide form; PB4599 had already built that form, so the
change is one new member on it, not a new representation. `CobolExact.SubsidiaryQuotient` is the exact quotient truncated toward zero at
identifier-3's scale with every integer digit. It shares `CobolExact.TruncatedQuotient` with the nested quotient (`DivTruncated` now
calls it), so there is one truncated-quotient rule on the exact carrier: exact on `CobolWide` while the dividend's alignment fits 256
bits, the 34-digit round-to-odd image past it (DOC-A.1-123). `NumericRenderer.ExactSubsidiaryQuotient` renders it, and
`ArithmeticEmitter.CarriedSubsidiaryQuotient` takes it for a fixed-point identifier-3 when `TakesExactSubsidiary`: a native sender on the
wide or run-time-scaled exact form, no binary64, not standard-decimal arithmetic. It stays an exact value, so GR7's back-multiply and
subtraction keep every digit (the same hand-off PB4599 gave a sender-exact quotient that fitted 34 digits). The standard-decimal lane
keeps the 34-digit SDIDI quotient because ISO GR1 names the SDIDI expression; an SDIDI-only native pair keeps it by DOC-A.1-82's lane
determination (`DIVIDE 1.0E40 BY 3 GIVING R REMAINDER R2`, unchanged).

**Authority.** ISO 14.9.12.4 GR6 c) and GR7 (both checked with `cite.py --check`); native arithmetic is implementor-defined (8.8.1.3), so
CLAUDE.md rule 1 sends the latitude to GnuCOBOL, whose `cob_div_quotient` / `cob_div_remainder` work on the exact decimal.

**Goldens.** `cases/14.9.12-DIVIDE/pb4640_remainder_after_quotient_past_34_digits` (accept 2002, 2014, 2023): a plain quotient, ROUNDED
on identifier-3, a divisor of 7, a fractional divisor into a scaled receiver, a negative dividend, the ON SIZE ERROR control that keeps
the remainder, and a 30-digit quotient that fits as the no-size-error control. Every expected value is a Python integer computation
(`expect.py`: 320, 320, 1, 0.006, -320, 5, A*A and 4321); the program is refused at 85 by
`wiseowl/diagnostics/pb4640_remainder_after_quotient_past_34_digits_85.json` (FUNCTION ABS is a 2002 introduction). Unit:
`CobolExactIntrinsicsTests.SubsidiaryQuotient_IsTheExactQuotientTruncatedAtTheScale` (BigInteger division as the oracle).

**Rows.** None claimed or closed; `closes_rows: []` with its reason (DOC-A.1-70 and DOC-A.1-123 already read CONFORMS). DOC-A.1-70 and
`docs/NUMERIC_DESIGN.md` carry the new determination.

**A lead, not fixed.** The stored quotient of that size error is still the 34-digit image's low-order digits (000003000100000000 where
the exact decimal has 000003000000000000). It is the final transfer's quotient, shared with COMPUTE and DIVIDE GIVING without
REMAINDER, and exactness there means landing the final transfer from the exact wide quotient; it is on the report as a lead.

