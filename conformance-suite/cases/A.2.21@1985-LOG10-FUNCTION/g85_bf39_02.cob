      *>--- conformance-case
      *> id: g85_bf39_02
      *> rules: [1989-AR-A.2.21.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.21.3 AR2 - LOG10 with arguments that satisfy
      *> "The value of argument-1 must be greater than zero.": a
      *> data item 1000, a fraction 0.01, and an arithmetic expression
      *> 0.5 * 4. Each is > 0, so a 1985 compiler must accept and
      *> evaluate them (§1989:A.2.21.4 RV1: the approximation of the
      *> logarithm to the base 10). Rounded to 4 places:
      *> log10 1000 = 3.0000, log10 0.01 = -2.0000,
      *> log10 2 = 0.3010 (0.30103...). A compiler that refuses a legal
      *> positive argument or computes the wrong base or sign fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3902.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-THOU PIC 9(4) VALUE 1000.
       01 W-HUND PIC V99 VALUE 0.01.
       01 W-R    PIC S9V9(4).
       01 W-E    PIC -9.9999.
       PROCEDURE DIVISION.
       P1.
           COMPUTE W-R ROUNDED = FUNCTION LOG10 (W-THOU).
           MOVE W-R TO W-E.
           DISPLAY "LOG10-1000=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION LOG10 (W-HUND).
           MOVE W-R TO W-E.
           DISPLAY "LOG10-HUNDREDTH=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION LOG10 (0.5 * 4).
           MOVE W-R TO W-E.
           DISPLAY "LOG10-EXPR=" W-E.
           STOP RUN.
