      *>--- conformance-case
      *> id: 1985-SR-XVI_3_2_3-1
      *> rules: [1985-SR-XVI.3.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.3 SR1 (SEGMENT-NUMBERS). A refusal: the rule
      *> forbids line 4 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6819 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SEGTST.
000300 PROCEDURE DIVISION.
000400 MAIN-SECT SECTION 100.
000500 P1.
000600     DISPLAY "OK".
000700     STOP RUN.
