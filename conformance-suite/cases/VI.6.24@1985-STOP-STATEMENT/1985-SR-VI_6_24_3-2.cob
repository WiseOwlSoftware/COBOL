      *>--- conformance-case
      *> id: 1985-SR-VI_6_24_3-2
      *> rules: [1985-SR-VI.6.24.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.24.3 SR2 (THE STOP STATEMENT). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2968 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STOPT.
       PROCEDURE DIVISION.
       P1.
           STOP RUN DISPLAY "B".
