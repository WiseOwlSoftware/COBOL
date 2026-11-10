      *>--- conformance-case
      *> id: 1985-SR-X_4_6_3-1
      *> rules: [1985-SR-X.4.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.6.3 SR1 (THE GLOBAL CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2404 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GLOBTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP-A.
           05  ITEM-B GLOBAL PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY ITEM-B.
           STOP RUN.
