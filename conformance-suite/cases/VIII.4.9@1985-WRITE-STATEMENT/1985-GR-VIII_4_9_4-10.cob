      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-10
      *> rules: [1985-GR-VIII.4.9.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR10 (THE WRITE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDWRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO REL-KEY.
           MOVE "FIRST" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "DUP"
           END-WRITE
           DISPLAY "AFTER 1".
           MOVE "SECOND" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "DUP"
           END-WRITE
           DISPLAY "AFTER 2".
           CLOSE REL-FILE.
           STOP RUN.
