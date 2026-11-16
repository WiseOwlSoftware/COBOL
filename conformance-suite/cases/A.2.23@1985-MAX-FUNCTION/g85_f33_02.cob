      *>--- conformance-case
      *> id: g85_f33_02
      *> rules: [1989-AR-A.2.23.3-1, 1989-RV-A.2.23.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.3 AR1 - more than one argument-1 of one class, with
      *> alphabetic and alphanumeric arguments mixed (lines 1, 2: PIC A
      *> beside PIC X), and all-numeric arguments (line 4, integer beside
      *> non-integer), each accepted. A refusal of the mix fails the case.
      *> §1989:A.2.23.4 RV3 - an alphanumeric MAX returns the size of the
      *> SELECTED argument-1: 5 on line 1, 2 on line 2 (the shorter
      *> argument wins), 3 on line 3, 2 on line 5 (the leftmost of two
      *> equal arguments, RV2). A result sized to the longest argument fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3302.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A3 PIC A(3) VALUE "CAT".
       01 W-X5 PIC X(5) VALUE "DOG".
       01 W-X4 PIC X(4) VALUE "ABC".
       01 W-A2 PIC A(2) VALUE "ZZ".
       01 W-I1 PIC 9(2) VALUE 17.
       01 W-I2 PIC 9(2) VALUE 42.
       01 W-N1 PIC 9V9 VALUE 4.5.
       01 W-R PIC 9(2)V9.
       01 W-E PIC Z9.9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" FUNCTION MAX (W-A3 W-X5) "]".
           DISPLAY "[" FUNCTION MAX (W-X4 W-A2) "]".
           DISPLAY "[" FUNCTION MAX (W-A3) "]".
           COMPUTE W-R = FUNCTION MAX (W-I1 W-I2 W-N1).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           DISPLAY "[" FUNCTION MAX ("AB" "AB ") "]".
           STOP RUN.
