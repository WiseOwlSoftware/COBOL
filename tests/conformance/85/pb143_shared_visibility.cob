       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB143SV.
      *> kb/Work PB143 - ISO 14.9.10.4 GR5: a successfully deleted record
      *> "has been logically removed from THE PHYSICAL FILE and can no
      *> longer be accessed" - through ANY connector. Two SELECTs to one
      *> ASSIGN target (no SHARING clause needed): Q deletes record 1 and
      *> P's read answers '23'; P writes record 3 and Q reads it back; and
      *> the CLOSE order (P first - the order that used to RESURRECT the
      *> deleted record from P's stale private view and DROP the write)
      *> cannot pick a surviving view, because the record store is ONE
      *> per physical file. Expected values derived from GR5 per leg.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-P ASSIGN TO "pb143sv.dat"
               ORGANIZATION RELATIVE ACCESS RANDOM
               RELATIVE KEY IS P-KEY
               FILE STATUS IS P-ST.
           SELECT F-Q ASSIGN TO "pb143sv.dat"
               ORGANIZATION RELATIVE ACCESS RANDOM
               RELATIVE KEY IS Q-KEY
               FILE STATUS IS Q-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-P.
       01 P-REC PIC X(8).
       FD F-Q.
       01 Q-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 P-KEY PIC 9(4).
       01 Q-KEY PIC 9(4).
       01 P-ST PIC XX.
       01 Q-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-P
           MOVE 1 TO P-KEY
           MOVE "ALPHA" TO P-REC
           WRITE P-REC
           MOVE 2 TO P-KEY
           MOVE "BETA" TO P-REC
           WRITE P-REC
           CLOSE F-P
           OPEN I-O F-P
           OPEN I-O F-Q
           MOVE 1 TO Q-KEY
           DELETE F-Q RECORD
           DISPLAY "DELQ=" Q-ST
           MOVE 1 TO P-KEY
           READ F-P INVALID KEY CONTINUE END-READ
           DISPLAY "READP23=" P-ST
           MOVE 3 TO P-KEY
           MOVE "GAMMA" TO P-REC
           WRITE P-REC
           DISPLAY "WRP=" P-ST
           MOVE 3 TO Q-KEY
           READ F-Q INVALID KEY CONTINUE END-READ
           DISPLAY "READQ3=" Q-ST " REC=" Q-REC
           CLOSE F-P
           CLOSE F-Q
           OPEN INPUT F-P
           MOVE 1 TO P-KEY
           READ F-P INVALID KEY CONTINUE END-READ
           DISPLAY "DUR1=" P-ST
           MOVE 3 TO P-KEY
           READ F-P INVALID KEY CONTINUE END-READ
           DISPLAY "DUR3=" P-ST " REC=" P-REC
           CLOSE F-P
           STOP RUN.
