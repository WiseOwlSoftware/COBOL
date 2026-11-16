      *>--- conformance-case
      *> id: g85_f32_05
      *> rules: [1989-AR-A.2.8.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.8.3 AR1 - argument-1 of ATAN is class numeric: a
      *> numeric data item, a numeric literal and an arithmetic
      *> expression are each accepted, and the value is the arctangent
      *> (§1989:A.2.8.4 RV1, in (-pi/2, +pi/2)). ROUNDED to four.
      *> Line 1: ATAN (W-ONE), W-ONE = 1: pi/4 = 0.785398 -> 0.7854.
      *> Line 2: ATAN (-1): -pi/4 -> -0.7854.
      *> Line 3: ATAN (W-ONE - 2) = ATAN (-1) -> -0.7854.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3205.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-ONE  PIC 9      VALUE 1.
       01 W-R    PIC -9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED = FUNCTION ATAN (W-ONE).
           DISPLAY "ATAN(1)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ATAN (-1).
           DISPLAY "ATAN(-1)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ATAN (W-ONE - 2).
           DISPLAY "ATAN(1-2)=" W-R.
           STOP RUN.
