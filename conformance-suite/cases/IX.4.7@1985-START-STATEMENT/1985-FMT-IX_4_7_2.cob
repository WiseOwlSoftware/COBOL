      *>--- conformance-case
      *> id: 1985-FMT-IX_4_7_2
      *> rules: [1985-FMT-IX.4.7.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.7.2 (THE START STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRT85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS FK.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 FK PIC X.
           05 FV PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "A" TO FK. MOVE "AAAA" TO FV.
           WRITE FR INVALID KEY DISPLAY "W1 ERR".
           MOVE "C" TO FK. MOVE "CCCC" TO FV.
           WRITE FR INVALID KEY DISPLAY "W2 ERR".
           MOVE "E" TO FK. MOVE "EEEE" TO FV.
           WRITE FR INVALID KEY DISPLAY "W3 ERR".
           CLOSE F.
           OPEN INPUT F.
           MOVE "B" TO FK.
           START F KEY IS EQUAL TO FK
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "STARTED"
           END-START.
           MOVE "B" TO FK.
           START F KEY IS >= FK
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "STARTED"
           END-START.
           READ F NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY FV.
           MOVE "C" TO FK.
           START F KEY IS GREATER THAN FK
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "STARTED"
           END-START.
           READ F NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY FV.
           CLOSE F.
           STOP RUN.
