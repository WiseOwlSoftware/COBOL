      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_6_2
      *> rules: [1985-FMT-VIII.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.2 (THE REWRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELDATA
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9(4).
       01  WS-NEW PIC X(10) VALUE "NEWVALUE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "OLDVALUE" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           MOVE 1 TO WS-KEY.
           REWRITE REL-REC FROM WS-NEW
               INVALID KEY DISPLAY "REWRITE 1 INVALID"
               NOT INVALID KEY DISPLAY "REWRITE 1 OK"
           END-REWRITE.
           MOVE 5 TO WS-KEY.
           REWRITE REL-REC FROM WS-NEW
               INVALID KEY DISPLAY "REWRITE 5 INVALID"
               NOT INVALID KEY DISPLAY "REWRITE 5 OK"
           END-REWRITE.
           MOVE 1 TO WS-KEY.
           READ REL-FILE INVALID KEY DISPLAY "READ FAILED".
           DISPLAY REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
