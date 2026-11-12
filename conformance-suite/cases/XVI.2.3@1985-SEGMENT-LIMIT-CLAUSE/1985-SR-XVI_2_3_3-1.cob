      *>--- conformance-case
      *> id: 1985-SR-XVI_2_3_3-1
      *> rules: [1985-SR-XVI.2.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.3.3 SR1 (THE SEGMENT-LIMIT CLAUSE). A refusal: the
      *> rule forbids line 7 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6819 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGLIM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ.
       OBJECT-COMPUTER. XYZ
           SEGMENT-LIMIT IS 50.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       P1.
           DISPLAY "OK".
           STOP RUN.
