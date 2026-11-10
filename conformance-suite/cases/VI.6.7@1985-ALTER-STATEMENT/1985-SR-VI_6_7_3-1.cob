      *>--- conformance-case
      *> id: 1985-SR-VI_6_7_3-1
      *> rules: [1985-SR-VI.6.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.7.3 SR1 (THE ALTER STATEMENT). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTR85.
       PROCEDURE DIVISION.
       P0.
           ALTER P1 TO PROCEED TO P3.
       P1.
           DISPLAY "IN P1" GO TO P2.
       P2.
           DISPLAY "P2".
           STOP RUN.
       P3.
           DISPLAY "P3".
           STOP RUN.
