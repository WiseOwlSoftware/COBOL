      *>--- conformance-case
      *> id: 1989-AR-A_2_30_3-4
      *> rules: [1989-AR-A.2.30.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.3 AR4 (THE NUMVAL-C FUNCTION). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NVC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(8) VALUE "$123.45".
       01 WS-N PIC 9(5)V99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-N = FUNCTION NUMVAL-C(WS-A, 5).
           DISPLAY WS-N.
           STOP RUN.
