      *>--- conformance-case
      *> id: 1985-FMT-VII_3_2_2
      *> rules: [1985-FMT-VII.3.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.2.2 (THE FILE DESCRIPTION ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "FDTEST.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 10 CHARACTERS
           LABEL RECORDS ARE STANDARD
           DATA RECORD IS R1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ALPHA-ONE1" TO R1.
           WRITE R1.
           MOVE "BETA-TWO-2" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY R1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY R1.
           CLOSE F1.
           STOP RUN.
