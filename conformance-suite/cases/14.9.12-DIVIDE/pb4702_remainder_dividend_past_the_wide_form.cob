      *>--- conformance-case
      *> id: pb4702_remainder_dividend_past_the_wide_form
      *> rules: [GR-14.9.12.4-6, GR-14.9.12.4-7, DOC-A.1-123]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4702 (docs/CONFORMANCE.md DOC-A.1-123, DOC-A.1-70):
      *> the remainder of a DIVIDE ... GIVING ... REMAINDER is formed
      *> from the EXACT operands even when the dividend, aligned to
      *> identifier-3's places, is past the 256-bit wide form.
      *> ISO 14.9.12.4 7): "When native arithmetic is in effect, the
      *> remainder is the result of multiplying the subsidiary quotient
      *> and the divisor and subtracting the product from the dividend"
      *> and 6): the subsidiary quotient is "derived from the quotient
      *> by truncation of digits at the least significant end" (cite.py:
      *> OK for both). Native arithmetic is implementor-defined (8.8.1.3,
      *> OK), so CLAUDE.md rule 1 sends the choice to GnuCOBOL, whose
      *> cob_div_quotient / cob_div_remainder are exact.
      *>
      *> EXPECTED VALUES, DERIVED (Python integers): A = 10^18 - 1,
      *> K = 999, so A * A * A * K + 7 = 999 * A**3 + 7 and the divisor
      *> is A**3 (54 digits): the quotient is 999 and, truncated at 28
      *> places, 999.0000000000000000000000000000 (7 / A**3 is below
      *> 10^-28); the remainder is 999 * A**3 + 7 - 999 * A**3 = 7. The
      *> dividend aligned to 28 places has 85 digits, past the form,
      *> where its 34-digit image gave the remainder 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4702REMWIDE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999999.
       01 K    PIC 999 VALUE 999.
       01 Q    PIC 9(3)V9(28).
       01 R2   PIC 9(3)V9(28).
       PROCEDURE DIVISION.
           DIVIDE FUNCTION ABS (A * A * A * K + 7)
               BY FUNCTION ABS (A * A * A)
               GIVING Q REMAINDER R2
           DISPLAY "Q  " Q
           DISPLAY "R2 " R2
           STOP RUN.
       END PROGRAM PB4702REMWIDE.
