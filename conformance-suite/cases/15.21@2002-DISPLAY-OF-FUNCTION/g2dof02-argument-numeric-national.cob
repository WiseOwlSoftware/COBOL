      *>--- conformance-case
      *> id: g2dof02-argument-numeric-national
      *> rules: [2002-AR-15.21.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.21.2 AR1 - "Argument-1 shall be of class national."
      *>   A, PIC 999 USAGE NATIONAL, is class numeric in 2002
      *>   (§2002:8.5.2.9 item 1, Table 3; kb/Work PB5625 reading B),
      *>   not class national,
      *>   so the reference at line 22 is refused. This refusal stands
      *>   beside the accepted case g2dof01-argument-class-national; it is
      *>   not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DOF02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC 999 USAGE NATIONAL VALUE 123.
       01  R               PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION DISPLAY-OF(A) TO R
           DISPLAY R
           STOP RUN.
