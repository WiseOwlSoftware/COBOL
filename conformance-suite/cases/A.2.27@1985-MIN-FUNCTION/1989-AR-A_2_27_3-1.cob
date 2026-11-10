      *>--- conformance-case
      *> id: 1989-AR-A_2_27_3-1
      *> rules: [1989-AR-A.2.27.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.27.3 AR1 (THE MIN FUNCTION). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1627 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. MINTEST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  A-ALP PIC X VALUE "A".
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     DISPLAY FUNCTION MIN (A-ALP 3).
000900     STOP RUN.
