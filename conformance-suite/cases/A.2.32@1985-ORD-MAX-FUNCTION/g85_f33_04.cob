      *>--- conformance-case
      *> id: g85_f33_04
      *> rules: [1989-AR-A.2.32.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.32.3 AR1 - more than one argument-1 of one class, with
      *> alphabetic and alphanumeric arguments mixed (lines 1, 2) and
      *> all-numeric arguments (line 3), each accepted and giving the
      *> position of the greatest (RV1). A refusal of the mix fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3304.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A3 PIC A(3) VALUE "CAT".
       01 W-X5 PIC X(5) VALUE "DOG".
       01 W-X4 PIC X(4) VALUE "ABC".
       01 W-A2 PIC A(2) VALUE "ZZ".
       01 W-I1 PIC 9(2) VALUE 17.
       01 W-I2 PIC 9(2) VALUE 42.
       01 W-N1 PIC 9V9 VALUE 4.5.
       01 W-O PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-O = FUNCTION ORD-MAX (W-A3 W-X5 W-A2).
           DISPLAY W-O.
           COMPUTE W-O = FUNCTION ORD-MAX (W-X5 W-A3).
           DISPLAY W-O.
           COMPUTE W-O = FUNCTION ORD-MAX (W-I1 W-N1 W-I2).
           DISPLAY W-O.
           STOP RUN.
