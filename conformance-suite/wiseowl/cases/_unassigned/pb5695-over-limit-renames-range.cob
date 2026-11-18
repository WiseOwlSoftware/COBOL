      *>--- conformance-case
      *> id: pb5695-over-limit-renames-range
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R1-1: the limit on one data item (D-ITEM-SIZE,
      *> 134217728 character positions) refuses the group with COBOL7641 and
      *> the compile STOPS there, so no later pass measures it.
      *> T = 4000000001 positions and R RENAMES A THRU C spans all of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695R4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 A PIC X(100000) OCCURS 20000.
          05 B PIC X(100000) OCCURS 20000.
          05 C PIC X.
       66 R RENAMES A THRU C.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE SPACES TO R.
           STOP RUN.
