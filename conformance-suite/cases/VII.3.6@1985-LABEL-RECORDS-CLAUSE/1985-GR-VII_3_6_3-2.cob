      *>--- conformance-case
      *> id: 1985-GR-VII_3_6_3-2
      *> rules: [1985-GR-VII.3.6.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.6.3 GR2 (THE LABEL RECORDS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LBLTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO DISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  SEQ-REC PIC X(9).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "LABELTEST" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "Y"
               DISPLAY "EOF"
           ELSE
               DISPLAY SEQ-REC.
           CLOSE SEQ-FILE.
           STOP RUN.
