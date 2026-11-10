      *>--- conformance-case
      *> id: 1985-SR-VI_6_15_3-2
      *> rules: [1985-SR-VI.6.15.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.15.3 SR2 (THE GO TO STATEMENT). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTR.
       PROCEDURE DIVISION.
       P0.
           ALTER P1 TO PROCEED TO P3.
       P1.
           DISPLAY "A" GO TO P2.
       P2.
           DISPLAY "B".
           STOP RUN.
       P3.
           DISPLAY "C".
           STOP RUN.
