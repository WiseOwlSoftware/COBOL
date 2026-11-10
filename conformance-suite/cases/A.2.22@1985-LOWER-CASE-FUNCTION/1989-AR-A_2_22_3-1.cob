      *>--- conformance-case
      *> id: 1989-AR-A_2_22_3-1
      *> rules: [1989-AR-A.2.22.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.22.3 AR1 (THE LOWER-CASE FUNCTION). A refusal: the
      *> rule forbids line 9 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1627 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LCTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 5.
       01  A PIC X VALUE "B".
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION LOWER-CASE (N) TO A.
           DISPLAY A.
           STOP RUN.
