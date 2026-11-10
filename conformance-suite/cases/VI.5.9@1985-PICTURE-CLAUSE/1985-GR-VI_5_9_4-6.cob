      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-6
      *> rules: [1985-GR-VI.5.9.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR6 (THE PICTURE CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0802 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICNE.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NE-ITEM PIC Z(18)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO NE-ITEM.
           DISPLAY NE-ITEM.
           STOP RUN.
