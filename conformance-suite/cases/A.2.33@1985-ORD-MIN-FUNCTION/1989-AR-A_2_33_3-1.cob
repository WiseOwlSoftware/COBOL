      *>--- conformance-case
      *> id: 1989-AR-A_2_33_3-1
      *> rules: [1989-AR-A.2.33.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.33.3 AR1 (THE ORD-MIN FUNCTION). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORDMIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-RES PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-RES = FUNCTION ORD-MIN (3 "A").
           DISPLAY W-RES.
           STOP RUN.
