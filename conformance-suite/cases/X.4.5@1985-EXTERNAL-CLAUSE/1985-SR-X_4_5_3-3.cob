      *>--- conformance-case
      *> id: 1985-SR-X_4_5_3-3
      *> rules: [1985-SR-X.4.5.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.3 SR3 (THE EXTERNAL CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTVAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-REC EXTERNAL.
           05  EXT-FLD PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY EXT-FLD.
           STOP RUN.
