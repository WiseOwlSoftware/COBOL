      *>--- conformance-case
      *> id: g85_f32_02
      *> rules: [1989-AR-A.2.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.5.3 AR1 - argument-1 of ACOS is class numeric: a
      *> numeric data item, a numeric literal and an arithmetic
      *> expression are each accepted, and the value is the arccosine
      *> (§1989:A.2.5.4 RV1, radians in [0, pi]). Results are ROUNDED
      *> to four places, so an approximation within 5E-5 prints alike.
      *> Line 1: ACOS (W-NEG), W-NEG = -1: pi = 3.14159... -> 3.1416.
      *> Line 2: ACOS (0.5): pi/3 = 1.047197... -> 1.0472.
      *> Line 3: ACOS (W-NEG + 1) = ACOS (0): pi/2 = 1.570796 -> 1.5708.
      *> A refused argument, or a dropped sign (ACOS (1) = 0), fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3202.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NEG  PIC S9     VALUE -1.
       01 W-R    PIC -9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED = FUNCTION ACOS (W-NEG).
           DISPLAY "ACOS(-1)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ACOS (0.5).
           DISPLAY "ACOS(0.5)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ACOS (W-NEG + 1).
           DISPLAY "ACOS(-1+1)=" W-R.
           STOP RUN.
