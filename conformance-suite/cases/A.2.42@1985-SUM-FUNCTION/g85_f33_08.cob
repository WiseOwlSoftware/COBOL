      *>--- conformance-case
      *> id: g85_f33_08
      *> rules: [1989-AR-A.2.42.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.42.3 AR1 - argument-1 of class numeric: integer literals
      *> (line 1), numeric data items with a non-integer literal (line 2),
      *> a numeric table referenced with ALL (line 3) and arithmetic
      *> expressions (line 4), each accepted and summed (RV1). A refusal
      *> of any numeric form, or a wrong sum, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3308.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-I1 PIC 9(2) VALUE 17.
       01 W-N1 PIC 9V9 VALUE 4.5.
       01 W-TBL VALUE "1234".
          05 W-EL PIC 9 OCCURS 4 TIMES.
       01 W-R PIC 9(2)V99.
       01 W-E PIC Z9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION SUM (1 2 3).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION SUM (W-N1 W-I1 2.25).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION SUM (W-EL (ALL)).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION SUM (W-I1 * 2, W-I1 / 4).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           STOP RUN.
