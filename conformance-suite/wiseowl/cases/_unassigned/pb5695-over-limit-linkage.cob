      *>--- conformance-case
      *> id: pb5695-over-limit-linkage
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R1-1: the limit on one data item (D-ITEM-SIZE,
      *> 134217728 character positions) refuses the group with COBOL7641 and
      *> the compile STOPS there, so no later pass measures it.
      *> L = 4000000000 positions in the LINKAGE SECTION, with L2 REDEFINES L.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695R6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 A PIC X(100000) OCCURS 20000.
          05 B PIC X(100000) OCCURS 20000.
       01 L2 REDEFINES L PIC X.
       PROCEDURE DIVISION USING L.
       MAIN-P.
           MOVE SPACES TO L.
           EXIT PROGRAM.
