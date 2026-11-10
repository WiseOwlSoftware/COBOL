      *>--- conformance-case
      *> id: 1985-SR-VI_5_15_3-2
      *> rules: [1985-SR-VI.5.15.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.3 SR2 (THE VALUE CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1625 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALSGN.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NUM PIC 9(3) VALUE -5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-NUM.
           STOP RUN.
