      *>--- conformance-case
      *> id: g85_f32_06
      *> rules: [1989-AR-A.2.34.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.34.3 AR1 - argument-1 and argument-2 of
      *> PRESENT-VALUE are class numeric: numeric data items, numeric
      *> literals and arithmetic expressions are each accepted in both
      *> positions. §1989:A.2.34.4 RV1: sum of argument-2 / (1 +
      *> argument-1) ** n, n = 1, 2, ... ROUNDED to two places.
      *> Line 1: PV (W-RATE, 110, 121), rate 0.1: 110/1.1 + 121/1.21 =
      *> 100 + 100 = 200.00.
      *> Line 2: PV (0, W-AMT, W-AMT * 2), W-AMT = 50: 50+100 = 150.00.
      *> Line 3: PV (W-RATE * 0, 0 - W-AMT, 25) = -50 + 25 = -25.00; the
      *> sign of an argument-2 expression is kept.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3206.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-RATE PIC 9V9    VALUE 0.1.
       01 W-AMT  PIC 999    VALUE 50.
       01 W-R    PIC -999.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED =
               FUNCTION PRESENT-VALUE (W-RATE, 110, 121).
           DISPLAY "PV(0.1,110,121)=" W-R.
           COMPUTE W-R ROUNDED =
               FUNCTION PRESENT-VALUE (0, W-AMT, W-AMT * 2).
           DISPLAY "PV(0,50,100)=" W-R.
           COMPUTE W-R ROUNDED =
               FUNCTION PRESENT-VALUE (W-RATE * 0, 0 - W-AMT, 25).
           DISPLAY "PV(0,-50,25)=" W-R.
           STOP RUN.
