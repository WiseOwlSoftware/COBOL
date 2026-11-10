      *>--- conformance-case
      *> id: 1985-SR-VI_5_11_3-3
      *> rules: [1985-SR-VI.5.11.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.11.3 SR3 (THE RENAMES CLAUSE). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2740 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RENTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  A PIC X(2) VALUE "AB".
           05  B PIC X(2) OCCURS 2 TIMES.
           05  C PIC X(2) VALUE "CD".
       66  R RENAMES B.
       PROCEDURE DIVISION.
       P1.
           DISPLAY R.
           STOP RUN.
