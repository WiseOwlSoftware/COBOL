      *>--- conformance-case
      *> id: 1989-FMT-A_2_35_2
      *> rules: [1989-FMT-A.2.35.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.35.2 (THE RANDOM FUNCTION). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1504 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RNDTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9V9(8).
       PROCEDURE DIVISION.
       P1.
           COMPUTE X = FUNCTION RANDOM (1, 2).
           DISPLAY "DONE".
           STOP RUN.
