      *>--- conformance-case
      *> id: g85_f32_01
      *> rules: [1989-AR-A.2.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 AR1 - a numeric argument is an arithmetic
      *> expression whose value, INCLUDING ITS OPERATIONAL SIGN, is used
      *> in determining the function's value. Each argument below is an
      *> arithmetic expression over a data item (not a bare literal).
      *> Line 1: INTEGER (W-A - 2.5) = INTEGER (-1.5) = -2 (greatest
      *> integer not greater than); a dropped sign gives 1.
      *> Line 2: INTEGER (W-A + 2.5) = INTEGER (3.5) = 3.
      *> Line 3: INTEGER-PART (W-A - 2.5) = INTEGER-PART (-1.5) = -1.
      *> Line 4: MEAN (W-A * 3, 0 - W-A) = (3 + -1) / 2 = 1; a dropped
      *> sign gives 2.
      *> Line 5: SQRT (W-B * W-B), W-B = -4: SQRT (16) = 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A    PIC 9      VALUE 1.
       01 W-B    PIC S9     VALUE -4.
       01 W-R    PIC -9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION INTEGER (W-A - 2.5).
           DISPLAY "INTEGER(1-2.5)=" W-R.
           COMPUTE W-R = FUNCTION INTEGER (W-A + 2.5).
           DISPLAY "INTEGER(1+2.5)=" W-R.
           COMPUTE W-R = FUNCTION INTEGER-PART (W-A - 2.5).
           DISPLAY "INTEGER-PART(1-2.5)=" W-R.
           COMPUTE W-R = FUNCTION MEAN (W-A * 3, 0 - W-A).
           DISPLAY "MEAN(3,-1)=" W-R.
           COMPUTE W-R ROUNDED = FUNCTION SQRT (W-B * W-B).
           DISPLAY "SQRT(-4*-4)=" W-R.
           STOP RUN.
