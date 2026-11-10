      *>--- conformance-case
      *> id: 1985-SR-VI_5_14_3-6
      *> rules: [1985-SR-VI.5.14.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.3 SR6 (THE USAGE CLAUSE). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0881 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. TIDX.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  IX PICTURE 9 USAGE IS INDEX.
000600 PROCEDURE DIVISION.
000700 P1.
000800     DISPLAY "OK".
000900     STOP RUN.
