      *>--- conformance-case
      *> id: g85_bf40_07
      *> rules: [1989-RV-A.2.36.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.36.4 RV1 - the greatest argument-1 minus the least,
      *> both found by the rules for simple conditions.
      *> §1985:VI.6.3.1.1.1 - numeric operands compare by algebraic
      *> value; the length of a literal is not significant. RANGE1 10.5
      *> - (-20) = 30.5; RANGE2 7 - 3 = 4; RANGE3 3.5 - (-5) = 8.5. A
      *> text or unsigned compare, or first minus last, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4007.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG20 PIC S9(2) VALUE -20.
       01 N1 PIC S9(3) VALUE -5.
       01 N2 PIC S9(3)V9 VALUE +3.5.
       01 N3 PIC 9(1) VALUE 2.
       01 R PIC -9(3).9(3).
       01 I PIC 9.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION RANGE (9 10.5 NEG20).
           DISPLAY "RANGE1 " R.
           COMPUTE R = FUNCTION RANGE (3 7 5).
           DISPLAY "RANGE2 " R.
           COMPUTE R = FUNCTION RANGE (N2 N1 N3).
           DISPLAY "RANGE3 " R.
           STOP RUN.
