      *>--- conformance-case
      *> id: g85_f32_07
      *> rules: [1989-AR-A.2.36.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.36.3 AR1 - argument-1 of RANGE is class numeric:
      *> numeric data items, numeric literals and arithmetic expressions
      *> are each accepted. §1989:A.2.36.4 RV1: greatest minus least.
      *> Line 1: RANGE (W-A, -3, W-A * 5), W-A = 1: 5 - (-3) = 8.0.
      *> Line 2: RANGE (1.5, W-NEG), W-NEG = -1: 1.5 - (-1) = 2.5.
      *> Line 3: RANGE (W-A - 2.5, W-A) = 1 - (-1.5) = 2.5; the sign of
      *> the expression decides which argument is least.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3207.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A    PIC 9      VALUE 1.
       01 W-NEG  PIC S9     VALUE -1.
       01 W-R    PIC 99.9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION RANGE (W-A, -3, W-A * 5).
           DISPLAY "RANGE(1,-3,5)=" W-R.
           COMPUTE W-R = FUNCTION RANGE (1.5, W-NEG).
           DISPLAY "RANGE(1.5,-1)=" W-R.
           COMPUTE W-R = FUNCTION RANGE (W-A - 2.5, W-A).
           DISPLAY "RANGE(-1.5,1)=" W-R.
           STOP RUN.
