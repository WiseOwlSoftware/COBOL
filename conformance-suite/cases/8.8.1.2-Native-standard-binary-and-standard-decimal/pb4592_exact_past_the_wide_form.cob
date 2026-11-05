      *>--- conformance-case
      *> id: pb4592_exact_past_the_wide_form
      *> rules: [DOC-A.1-123, DOC-A.1-179, GR-8.8.1.2-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4592, PB4729 (docs/CONFORMANCE.md DOC-A.1-123): under
      *> NATIVE arithmetic an intermediate past the 256-bit wide form
      *> (77 digits) is EXACT, as GnuCOBOL's arbitrary-precision
      *> cob_decimal is, and the owner decided so on 2026-11-04.
      *> ISO 8.8.1.3: "Native arithmetic is an implementor-defined
      *> method of evaluating an arithmetic expression" and "The
      *> implementor shall specify techniques used for native
      *> arithmetic" (cite.py: OK 8.8.1.3), 11.9.11.2 1): "the rounding
      *> rules that apply to intermediate data items are defined by the
      *> implementor" (OK), and 8.3.3.3.3 5): a floating-point literal's
      *> value is the algebraic product of its significand and a power
      *> of ten (OK), so 1.0E0 changes neither the mode nor a value.
      *> No ARITHMETIC clause means native arithmetic, so no
      *> standard-decimal rule rounds these intermediates to 34 digits
      *> (the twin pb4592_exact_past_the_wide_form_standard_decimal
      *> holds that side). 14.7.5 5) lets an implementor check a bounded
      *> intermediate range (OK); WiseOwl COBOL's is the SDIDI's,
      *> |value| < 10^6145.
      *>
      *> EXPECTED VALUES, DERIVED (Python integers): P = BIG * BIG * K3
      *> = 998999999999999998002000000000000000999 (39 digits), P*P has
      *> 78. P*P / P is the integer P, so every cancellation below is
      *> exact; (P + 1/2) ** 2 - P ** 2 = P + 1/4; P = 999 mod 1000, so
      *> P*P + 1 = 2 mod 1000. A = 10^19 + 1: A**4 has 77 digits and
      *> fits the 256-bit form although its digit BOUND is 80.
      *>   W1     (P * 1.0E0) ** 2 / P - P = 0       (was 99001)
      *>   W2     (P + 1.0E-50) - P at 4 places = 0.0000
      *>   E100   (P + 1.0E-100) - P at 4 places = 0.0000
      *>   PSQ    (P + 0.5E0) ** 2 - P ** 2 - P = 0.2500
      *>   ADDF/ADDI  (P*P + 1.0E0) - P*P and (P*P + 1) - P*P = 1
      *>   MOD/MODI   MOD (P*P + 1.0E0, 1000) and (P*P + 1, 1000) = 2
      *>   REL/RELI   P*P + 1.0E-50 > P*P and P*P + 1 > P*P are TRUE
      *>   SUMW   SUM (P*P, 1, -(P*P)) = 1
      *>   P8P7   P ** 8 / P ** 7 - P = 0
      *>   P40/P40F   (P ** 40 + 1) - P ** 40 = 1, also with 1.0E0
      *>   ABCD/ABCDF A*A*A*A + 1 - A*A*A*A = 1, also with * 1.0E0
      *> Why each leg can fail: a value past 77 digits kept as its
      *> 34-digit image leaves the image's rounding residue (99001,
      *> -999.0000) or loses the 1 and the 10^-50 altogether.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4592EXACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999 VALUE 999.
       01 N8  PIC 99 VALUE 8.
       01 N7  PIC 99 VALUE 7.
       01 N40 PIC 99 VALUE 40.
       01 A   PIC 9(20) VALUE 10000000000000000001.
       01 R   PIC S9(9).
       01 RV  PIC S9(9)V9(4).
       PROCEDURE DIVISION.
           COMPUTE R = (BIG * BIG * K3 * 1.0E0) ** 2 / (BIG * BIG * K3)
                       - BIG * BIG * K3
           DISPLAY "W1    " R
           COMPUTE RV = (BIG * BIG * K3 + 1.0E-50) - BIG * BIG * K3
           DISPLAY "W2    " RV
           COMPUTE RV = (BIG * BIG * K3 + 1.0E-100) - BIG * BIG * K3
           DISPLAY "E100  " RV
           COMPUTE RV = (BIG * BIG * K3 + 0.5E0) ** 2
                       - (BIG * BIG * K3) ** 2 - BIG * BIG * K3
           DISPLAY "PSQ   " RV
           COMPUTE R = (BIG * BIG * K3 * BIG * BIG * K3 + 1.0E0)
                       - BIG * BIG * K3 * BIG * BIG * K3
           DISPLAY "ADDF  " R
           COMPUTE R = (BIG * BIG * K3 * BIG * BIG * K3 + 1)
                       - BIG * BIG * K3 * BIG * BIG * K3
           DISPLAY "ADDI  " R
           COMPUTE R = FUNCTION MOD (BIG * BIG * K3 * BIG * BIG * K3
                       + 1.0E0, 1000)
           DISPLAY "MOD   " R
           COMPUTE R = FUNCTION MOD (BIG * BIG * K3 * BIG * BIG * K3
                       + 1, 1000)
           DISPLAY "MODI  " R
           IF BIG * BIG * K3 * BIG * BIG * K3 + 1.0E-50
              > BIG * BIG * K3 * BIG * BIG * K3
              DISPLAY "REL   GT" ELSE DISPLAY "REL   NOT-GT".
           IF BIG * BIG * K3 * BIG * BIG * K3 + 1
              > BIG * BIG * K3 * BIG * BIG * K3
              DISPLAY "RELI  GT" ELSE DISPLAY "RELI  NOT-GT".
           COMPUTE R = FUNCTION SUM (BIG * BIG * K3 * BIG * BIG * K3,
                       1, - (BIG * BIG * K3 * BIG * BIG * K3))
           DISPLAY "SUMW  " R
           COMPUTE R = (BIG * BIG * K3) ** N8 / (BIG * BIG * K3) ** N7
                       - BIG * BIG * K3
           DISPLAY "P8P7  " R
           COMPUTE R = ((BIG * BIG * K3) ** N40 + 1)
                       - (BIG * BIG * K3) ** N40
           DISPLAY "P40   " R
           COMPUTE R = ((BIG * BIG * K3) ** N40 + 1.0E0)
                       - (BIG * BIG * K3) ** N40
           DISPLAY "P40F  " R
           COMPUTE R = A * A * A * A + 1 - A * A * A * A
           DISPLAY "ABCD  " R
           COMPUTE R = A * A * A * A * 1.0E0 + 1 - A * A * A * A
           DISPLAY "ABCDF " R
           STOP RUN.
       END PROGRAM PB4592EXACT.
