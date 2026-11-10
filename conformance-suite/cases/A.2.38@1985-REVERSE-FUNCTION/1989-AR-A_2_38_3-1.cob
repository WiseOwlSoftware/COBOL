      *>--- conformance-case
      *> id: 1989-AR-A_2_38_3-1
      *> rules: [1989-AR-A.2.38.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.38.3 AR1 (THE REVERSE FUNCTION). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REVTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X(3) VALUE "AB1".
       01  N PIC 9(3) VALUE 123.
       01  R PIC X(3).
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION REVERSE (N) TO R.
           DISPLAY R.
           STOP RUN.
