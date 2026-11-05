      *>--- conformance-case
      *> id: pb4592_exact_past_the_wide_form_standard_decimal
      *> rules: [DOC-A.1-123, GR-8.8.1.2-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4592: the STANDARD-DECIMAL twin of
      *> pb4592_exact_past_the_wide_form. Under ARITHMETIC IS
      *> STANDARD-DECIMAL ISO 8.8.1.5.2 1): "If the operand value
      *> cannot be expressed exactly in an SDIDI, the value is rounded
      *> according to the rules in 11.9.11" and 8.8.1.5.3 performs the
      *> operations as ISO/IEC 60559 does (cite.py: OK for both), so
      *> every operand is the decimal128 image P' of the 39-digit P
      *> (round NEAREST-EVEN, 34 digits) and the standard FIXES the
      *> values: round34(P' ** 2) / P' rounds back to P', so
      *> W1 = P' - P' = 0, and P' + 1.0E-50 rounds to P', so
      *> W2 = P' - P' = 0.0000. The native fix must not move them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4592SDEC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999 VALUE 999.
       01 R   PIC S9(9).
       01 RV  PIC S9(9)V9(4).
       PROCEDURE DIVISION.
           COMPUTE R = (BIG * BIG * K3 * 1.0E0) ** 2 / (BIG * BIG * K3)
                       - BIG * BIG * K3
           DISPLAY "SDW1  " R
           COMPUTE RV = (BIG * BIG * K3 + 1.0E-50) - BIG * BIG * K3
           DISPLAY "SDW2  " RV
           STOP RUN.
       END PROGRAM PB4592SDEC.
