      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-9
      *> rules: [1985-GR-VIII.4.9.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR9 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NIKTEST.
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
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       77  WS-KEY  PIC 9(4).
       77  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-FILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "RECORD-ONE" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "OK " WS-STAT
           END-WRITE.
           WRITE REL-REC
               INVALID KEY DISPLAY "INVALID " WS-STAT
               NOT INVALID KEY DISPLAY "OK " WS-STAT
           END-WRITE.
           MOVE 2 TO WS-KEY.
           WRITE REL-REC
               INVALID KEY DISPLAY "INVALID " WS-STAT
           END-WRITE.
           DISPLAY "THIRD " WS-STAT.
           WRITE REL-REC
               NOT INVALID KEY DISPLAY "OK " WS-STAT
           END-WRITE.
           DISPLAY "FOURTH " WS-STAT.
           MOVE 3 TO WS-KEY.
           WRITE REL-REC
               NOT INVALID KEY DISPLAY "OK " WS-STAT
           END-WRITE.
           WRITE REL-REC.
           DISPLAY "SIXTH " WS-STAT.
           CLOSE REL-FILE.
           STOP RUN.
