      *>--- conformance-case
      *> id: 1985-SR-VI_6_12_3-2
      *> rules: [1985-SR-VI.6.12.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.3 SR2 (THE ENTER STATEMENT). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1639 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENTTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "START".
           GO TO P-END.
       P-OTHER.
           ENTER FORTRAN SUBRTN.
           GO TO SUBRTN.
       P-END.
           STOP RUN.
