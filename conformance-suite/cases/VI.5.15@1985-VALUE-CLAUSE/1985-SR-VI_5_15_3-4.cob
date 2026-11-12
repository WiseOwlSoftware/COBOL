      *>--- conformance-case
      *> id: 1985-SR-VI_5_15_3-4
      *> rules: [1985-SR-VI.5.15.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.3 SR4 (THE VALUE CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. EXTVAL.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  EXT-REC EXTERNAL.
000600     05  EXT-FLD PIC X(4) VALUE "ABCD".
000700 PROCEDURE DIVISION.
000800 MAIN-PARA.
000900     MOVE "WXYZ" TO EXT-FLD.
001000     DISPLAY EXT-FLD.
001100     STOP RUN.
