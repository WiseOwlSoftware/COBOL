      *>--- conformance-case
      *> id: 1985-SR-X_4_5_3-2
      *> rules: [1985-SR-X.4.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.3 SR2 (THE EXTERNAL CLAUSE). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2159 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTDUP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-A EXTERNAL PIC X(4).
       01  EXT-A EXTERNAL PIC X(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
