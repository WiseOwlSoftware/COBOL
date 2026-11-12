      *>--- conformance-case
      *> id: 1985-SR-XVI_3_2_3-3
      *> rules: [1985-SR-XVI.3.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.2.3 SR3 (SEGMENT-NUMBERS). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6819 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SEGTST.
000300 PROCEDURE DIVISION.
000400 DECLARATIVES.
000500 ERR-SEC SECTION 50.
000600     USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
000700 ERR-PARA.
000800     EXIT.
000900 END DECLARATIVES.
001000 MAIN-SEC SECTION.
001100 MAIN-PARA.
001200     DISPLAY "OK".
001300     STOP RUN.
