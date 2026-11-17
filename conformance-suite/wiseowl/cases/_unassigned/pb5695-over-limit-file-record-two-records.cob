      *>--- conformance-case
      *> id: pb5695-over-limit-file-record-two-records
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R1-1: the limit on one data item (D-ITEM-SIZE,
      *> 134217728 character positions) refuses the group with COBOL7641 and
      *> the compile STOPS there, so no later pass measures it.
      *> FR = 4000000000 positions in an FD with a second record FR2: the
      *> file's shared record area (an implicit REDEFINES) measured FR and the
      *> compiler crashed with an OverflowException.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695R2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5695r2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR.
          05 A PIC X(100000) OCCURS 20000.
          05 B PIC X(100000) OCCURS 20000.
       01 FR2 PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F.
           WRITE FR.
           CLOSE F.
           STOP RUN.
