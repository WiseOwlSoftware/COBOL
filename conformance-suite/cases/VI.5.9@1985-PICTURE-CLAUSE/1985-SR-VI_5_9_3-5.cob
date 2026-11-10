      *>--- conformance-case
      *> id: 1985-SR-VI_5_9_3-5
      *> rules: [1985-SR-VI.5.9.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.3 SR5 (THE PICTURE CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0881 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PICTST.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  WS-A.
000700 PROCEDURE DIVISION.
000800 P1.
000900     MOVE "A" TO WS-A.
001000     DISPLAY WS-A.
001100     STOP RUN.
