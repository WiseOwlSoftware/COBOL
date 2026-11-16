      *>--- conformance-case
      *> id: g85_f32_04
      *> rules: [1989-AR-A.2.7.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.7.3 AR1 - argument-1 of ASIN is class numeric: a
      *> numeric data item, a numeric literal and an arithmetic
      *> expression are each accepted, and the value is the arcsine
      *> (§1989:A.2.7.4 RV1, in [-pi/2, +pi/2]). ROUNDED to four.
      *> Line 1: ASIN (W-NEG), W-NEG = -1: -pi/2 = -1.570796 -> -1.5708.
      *> Line 2: ASIN (0.5): pi/6 = 0.523598... -> 0.5236.
      *> Line 3: ASIN (0 - W-NEG) = ASIN (1): pi/2 -> 1.5708.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3204.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NEG  PIC S9     VALUE -1.
       01 W-R    PIC -9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED = FUNCTION ASIN (W-NEG).
           DISPLAY "ASIN(-1)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ASIN (0.5).
           DISPLAY "ASIN(0.5)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ASIN (0 - W-NEG).
           DISPLAY "ASIN(0+1)=" W-R.
           STOP RUN.
