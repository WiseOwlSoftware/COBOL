      *>--- conformance-case
      *> id: g85_f32_03
      *> rules: [1989-AR-A.2.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.6.3 AR1 - argument-1 of ANNUITY is class numeric: a
      *> numeric data item, a numeric literal and an arithmetic
      *> expression are each accepted as the interest rate.
      *> Line 1: ANNUITY (W-RATE, 2), rate 0.1, §1989:A.2.6.4 RV2:
      *> 0.1 / (1 - 1.1 ** -2) = 0.121 / 0.21 = 0.576190... -> 0.5762.
      *> Line 2: ANNUITY (0, 4), §1989:A.2.6.4 RV1: 1 / 4 = 0.2500.
      *> Line 3: ANNUITY (W-RATE - W-RATE, 5) = ANNUITY (0, 5) = 0.2000.
      *> Results are ROUNDED to four places.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3203.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-RATE PIC 9V9    VALUE 0.1.
       01 W-R    PIC 9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (W-RATE, 2).
           DISPLAY "ANNUITY(0.1,2)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (0, 4).
           DISPLAY "ANNUITY(0,4)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (W-RATE - W-RATE, 5).
           DISPLAY "ANNUITY(0.1-0.1,5)=" W-R.
           STOP RUN.
