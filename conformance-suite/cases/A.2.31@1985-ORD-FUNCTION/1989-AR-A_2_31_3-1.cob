      *>--- conformance-case
      *> id: 1989-AR-A_2_31_3-1
      *> rules: [1989-AR-A.2.31.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.31.3 AR1 (THE ORD FUNCTION). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORDTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9(3).
       PROCEDURE DIVISION.
       P1.
           COMPUTE N = FUNCTION ORD("AB").
           DISPLAY N.
           STOP RUN.
