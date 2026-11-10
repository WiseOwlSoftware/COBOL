      *>--- conformance-case
      *> id: 1985-SR-X_5_3_3-2
      *> rules: [1985-SR-X.5.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.3 SR2 (THE CANCEL STATEMENT). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1696 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CANTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ALPHA  PIC X(8) VALUE "SUBPROG1".
       01  WS-NUM    PIC 9(8) VALUE 12345678.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CANCEL WS-NUM.
           STOP RUN.
