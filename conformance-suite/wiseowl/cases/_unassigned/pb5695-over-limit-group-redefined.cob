      *>--- conformance-case
      *> id: pb5695-over-limit-group-redefined
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R1-1: the limit on one data item (D-ITEM-SIZE,
      *> 134217728 character positions) refuses the group with COBOL7641 and
      *> the compile STOPS there, so no later pass measures it.
      *> T = 2 x 100000 x 20000 = 4000000000 positions, and U REDEFINES T:
      *> the REDEFINES classification measured T and the compiler crashed
      *> with an OverflowException instead of reporting COBOL7641.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 A PIC X(100000) OCCURS 20000.
          05 B PIC X(100000) OCCURS 20000.
       01 U REDEFINES T PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE SPACES TO T.
           STOP RUN.
