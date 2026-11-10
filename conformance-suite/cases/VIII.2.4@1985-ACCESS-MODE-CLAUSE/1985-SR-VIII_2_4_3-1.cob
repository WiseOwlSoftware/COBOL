      *>--- conformance-case
      *> id: 1985-SR-VIII_2_4_3-1
      *> rules: [1985-SR-VIII.2.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.4.3 SR1 (THE ACCESS MODE CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELDISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS REL-KEY OF KEY-GROUP-A.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  KEY-GROUP-A.
           05  REL-KEY PIC 9(4).
       01  KEY-GROUP-B.
           05  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE "HELLO" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "BAD KEY".
           CLOSE REL-FILE.
           MOVE 0 TO REL-KEY OF KEY-GROUP-A.
           MOVE 9 TO REL-KEY OF KEY-GROUP-B.
           OPEN INPUT REL-FILE.
           READ REL-FILE AT END DISPLAY "EOF".
           DISPLAY REL-REC.
           DISPLAY REL-KEY OF KEY-GROUP-A.
           DISPLAY REL-KEY OF KEY-GROUP-B.
           CLOSE REL-FILE.
           STOP RUN.
