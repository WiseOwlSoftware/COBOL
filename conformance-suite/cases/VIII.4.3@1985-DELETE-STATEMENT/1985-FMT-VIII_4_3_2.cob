      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_3_2
      *> rules: [1985-FMT-VIII.4.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.3.2 (THE DELETE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "REC1" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 2 TO WS-KEY.
           MOVE "REC2" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           MOVE 2 TO WS-KEY.
           DELETE REL-FILE RECORD
               INVALID KEY DISPLAY "NOT FOUND"
               NOT INVALID KEY DISPLAY "DELETED"
           END-DELETE.
           DELETE REL-FILE RECORD
               INVALID KEY DISPLAY "NOT FOUND"
               NOT INVALID KEY DISPLAY "DELETED"
           END-DELETE.
           CLOSE REL-FILE.
           STOP RUN.
