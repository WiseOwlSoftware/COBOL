      *>--- conformance-case
      *> id: 1989-AR-A_2_2-4
      *> rules: [1989-AR-A.2.2-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 AR4 (GENERAL DESCRIPTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ARGINT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL VALUE "123900".
          05 ROW-E OCCURS 2 TIMES.
             10 ELEM PIC 9 OCCURS 3 TIMES.
       01 W-A PIC S9(2) VALUE -11.
       01 W-B PIC 9 VALUE 2.
       01 W-R PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION MOD (W-A, W-B + 3).
           DISPLAY W-R.
           COMPUTE W-R = FUNCTION FACTORIAL (W-B + 1).
           DISPLAY W-R.
           COMPUTE W-R = FUNCTION ORD-MAX (ELEM (ALL, ALL)).
           DISPLAY W-R.
           STOP RUN.
