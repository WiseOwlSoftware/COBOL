      *>--- conformance-case
      *> id: 1985-GR-VII_2_7_4-4
      *> rules: [1985-GR-VII.2.7.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.4 GR4 (THE PADDING CHARACTER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADCS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PADFILE"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           BLOCK CONTAINS 4 RECORDS
           CODE-SET IS ASC.
       01  R1 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ABCDE" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY R1.
           READ F1 AT END DISPLAY "EOF".
           CLOSE F1.
           STOP RUN.
