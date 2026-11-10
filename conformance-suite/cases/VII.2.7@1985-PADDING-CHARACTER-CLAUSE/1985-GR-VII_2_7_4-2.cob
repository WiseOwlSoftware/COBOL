      *>--- conformance-case
      *> id: 1985-GR-VII_2_7_4-2
      *> rules: [1985-GR-VII.2.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.4 GR2 (THE PADDING CHARACTER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO DISKFILE
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "ALPHA" TO R.
           WRITE R.
           MOVE "*****" TO R.
           WRITE R.
           MOVE "GAMMA" TO R.
           WRITE R.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF".
           DISPLAY R.
           READ F AT END DISPLAY "EOF".
           DISPLAY R.
           READ F AT END DISPLAY "EOF".
           DISPLAY R.
           CLOSE F.
           STOP RUN.
