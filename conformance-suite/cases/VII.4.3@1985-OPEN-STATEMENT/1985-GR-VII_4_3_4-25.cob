      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-25
      *> rules: [1985-GR-VII.4.3.4-25]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR25 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR25.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISKFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ABCDEFGHIJ" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE SPACES TO R1.
           READ F1 AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "N" DISPLAY R1.
           CLOSE F1.
           STOP RUN.
