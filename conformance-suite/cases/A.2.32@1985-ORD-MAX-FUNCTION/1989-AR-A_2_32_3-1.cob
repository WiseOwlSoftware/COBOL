      *>--- conformance-case
      *> id: 1989-AR-A_2_32_3-1
      *> rules: [1989-AR-A.2.32.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.32.3 AR1 (THE ORD-MAX FUNCTION). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORDMX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N1 PIC 9 VALUE 3.
       01  N2 PIC 9 VALUE 5.
       01  A1 PIC X VALUE "A".
       01  R  PIC 99.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION ORD-MAX (N1 A1).
           DISPLAY R.
           STOP RUN.
