      *>--- conformance-case
      *> id: 1985-GR-VII_3_6_3-1
      *> rules: [1985-GR-VII.3.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.6.3 GR1 (THE LABEL RECORDS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LBLOMT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE OMITTED.
       01  R1 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "HELLO" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY R1.
           CLOSE F1.
           STOP RUN.
