      *>--- conformance-case
      *> id: 1985-GR-VII_2_3_4-3
      *> rules: [1985-GR-VII.2.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.3.4 GR3 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ASGN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "ASGNFILE".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
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
