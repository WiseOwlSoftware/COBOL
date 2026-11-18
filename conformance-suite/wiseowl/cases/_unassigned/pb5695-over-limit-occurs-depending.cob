      *>--- conformance-case
      *> id: pb5695-over-limit-occurs-depending
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R1-1: the limit on one data item (D-ITEM-SIZE,
      *> 134217728 character positions) refuses the group with COBOL7641 and
      *> the compile STOPS there, so no later pass measures it.
      *> T = 1 + 100000 x 20000 = 2000000001 positions: an OCCURS DEPENDING
      *> table is laid out at its maximum.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695R3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(9).
       01 T.
          05 A PIC X.
          05 E PIC X(100000) OCCURS 1 TO 20000 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1 TO N.
           MOVE SPACES TO T.
           STOP RUN.
