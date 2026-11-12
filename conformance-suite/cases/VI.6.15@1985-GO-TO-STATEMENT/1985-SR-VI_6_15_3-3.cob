      *>--- conformance-case
      *> id: 1985-SR-VI_6_15_3-3
      *> rules: [1985-SR-VI.6.15.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.15.3 SR3 (THE GO TO STATEMENT). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6818 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GOTEST.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "START".
       P1.
           DISPLAY "IN P1".
           GO TO.
       P2.
           DISPLAY "END".
           STOP RUN.
