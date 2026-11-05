      *>--- conformance-case
      *> id: pb4701_size_error_stores_the_exact_quotient_digits
      *> rules: [GR-14.9.12.4-6, DOC-A.1-70, DOC-A.1-123]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4701 (docs/CONFORMANCE.md DOC-A.1-70, DOC-A.1-123):
      *> when a native quotient is further from zero than its receiver
      *> permits (ISO 14.7.5 3), a size error) and neither a SIZE ERROR
      *> phrase nor checking is in effect, the value stored is
      *> implementor-defined (item 8); WiseOwl COBOL stores the
      *> LOW-ORDER digits of the EXACT quotient, as GnuCOBOL's
      *> cob_decimal_div followed by cob_decimal_get_field does (CLAUDE.md
      *> rule 1), not the low-order digits of the quotient's 34-digit
      *> round-to-odd image, whose last digit is a marker and not a
      *> digit of the quotient.
      *>
      *> EXPECTED VALUES, DERIVED (Python integers): A = 10^15 - 1,
      *> A**3 + 7 = 999999999999997000000000000003000000000000006, and
      *> (A**3 + 7) // 1000 = 999999999999997000000000000003000000000000
      *> (42 digits), whose low-order 18 digits are 000003000000000000;
      *> (A**3 + 7) mod 1000 = 6, the remainder DIVIDE ... REMAINDER
      *> stores from the subsidiary quotient (14.9.12.4 6) and 7)).
      *>   R1  DIVIDE ... GIVING R REMAINDER R2   R = 000003000000000000
      *>   R2  (same statement)                   R2 = 000000000000000006
      *>   C1  COMPUTE R = FUNCTION ABS (A*A*A + 7) / 1000
      *>                                          R = 000003000000000000
      *>   D1  DIVIDE ... GIVING R (no REMAINDER) R = 000003000000000000
      *> A 34-digit image of the quotient ends in ...0001 followed by
      *> eight zeros, so each leg stored 000003000100000000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4701SIZEQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R    PIC 9(18).
       01 R2   PIC 9(18).
       PROCEDURE DIVISION.
           DIVIDE FUNCTION ABS (A * A * A + 7) BY 1000
               GIVING R REMAINDER R2
           DISPLAY "R1 " R
           DISPLAY "R2 " R2
           MOVE ZERO TO R
           COMPUTE R = FUNCTION ABS (A * A * A + 7) / 1000
           DISPLAY "C1 " R
           MOVE ZERO TO R
           DIVIDE FUNCTION ABS (A * A * A + 7) BY 1000 GIVING R
           DISPLAY "D1 " R
           STOP RUN.
       END PROGRAM PB4701SIZEQ.
