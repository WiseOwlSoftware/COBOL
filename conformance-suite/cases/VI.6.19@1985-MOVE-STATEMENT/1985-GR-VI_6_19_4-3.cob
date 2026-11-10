      *>--- conformance-case
      *> id: 1985-GR-VI_6_19_4-3
      *> rules: [1985-GR-VI.6.19.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.19.4 GR3 (THE MOVE STATEMENT). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0819 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MOVET.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM-ITEM PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACE TO NUM-ITEM.
           DISPLAY NUM-ITEM.
           STOP RUN.
