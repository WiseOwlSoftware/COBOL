      *>--- conformance-case
      *> id: 1985-SR-VI_5_12_3-1
      *> rules: [1985-SR-VI.5.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.12.3 SR1 (THE SIGN CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2422 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGNTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NUM PIC 9(3) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 123 TO WS-NUM.
           DISPLAY WS-NUM.
           STOP RUN.
