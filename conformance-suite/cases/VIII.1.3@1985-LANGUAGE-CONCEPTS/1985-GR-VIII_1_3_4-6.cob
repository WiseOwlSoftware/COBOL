      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_4-6
      *> rules: [1985-GR-VIII.1.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GR6 (LANGUAGE CONCEPTS.). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9(4).
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "RECORD-ONE" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "BAD WRITE".
           DISPLAY "WRITE " WS-STAT.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 2 TO WS-KEY.
           READ REL-FILE INVALID KEY DISPLAY "NOT FOUND".
           DISPLAY "READ " WS-STAT.
           CLOSE REL-FILE.
           STOP RUN.
