      *>--- conformance-case
      *> id: 1985-SR-VI_6_10_3-2
      *> rules: [1985-SR-VI.6.10.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.10.3 SR2 (THE DISPLAY STATEMENT). A refusal: the
      *> rule forbids line 5 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0900 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DSPLIT.
       PROCEDURE DIVISION.
       P1.
           DISPLAY 1.5
           STOP RUN.
