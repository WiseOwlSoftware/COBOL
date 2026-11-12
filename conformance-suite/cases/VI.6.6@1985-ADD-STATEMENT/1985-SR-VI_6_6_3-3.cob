      *>--- conformance-case
      *> id: 1985-SR-VI_6_6_3-3
      *> rules: [1985-SR-VI.6.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.6.3 SR3 (THE ADD STATEMENT). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0805 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ADDCMP.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  A PIC 9(10) VALUE 1.
000700 01  B PIC V9(9) VALUE 0.
000800 01  C PIC 9(9) VALUE 1.
000900 PROCEDURE DIVISION.
001000 P1.
001100     ADD A TO B.
001200     STOP RUN.
