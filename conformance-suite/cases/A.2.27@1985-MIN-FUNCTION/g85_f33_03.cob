      *>--- conformance-case
      *> id: g85_f33_03
      *> rules: [1989-AR-A.2.27.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.27.3 AR1 - more than one argument-1 of one class, with
      *> alphabetic and alphanumeric arguments mixed (lines 1, 2: PIC A
      *> beside PIC X) and all-numeric arguments (lines 3, 4), each
      *> accepted and giving the least value (RV1). A refusal of the mix,
      *> or a wrong selection, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3303.
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
           DISPLAY "[" FUNCTION MIN (W-A3 W-X5) "]".
           DISPLAY "[" FUNCTION MIN (W-X5 W-A2) "]".
           COMPUTE W-R = FUNCTION MIN (W-I1 W-I2 W-N1).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION MIN (W-I2 W-I1).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           STOP RUN.
