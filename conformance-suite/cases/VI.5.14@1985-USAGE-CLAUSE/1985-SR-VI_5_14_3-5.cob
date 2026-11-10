      *>--- conformance-case
      *> id: 1985-SR-VI_5_14_3-5
      *> rules: [1985-SR-VI.5.14.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.3 SR5 (THE USAGE CLAUSE). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0809 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDXREF.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY XI.
       01  IX USAGE IS INDEX.
       01  N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           SET XI TO 3.
           SET IX TO XI.
           MOVE IX TO N.
           DISPLAY N.
           STOP RUN.
