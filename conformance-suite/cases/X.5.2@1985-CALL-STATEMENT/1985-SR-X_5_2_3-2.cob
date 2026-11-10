      *>--- conformance-case
      *> id: 1985-SR-X_5_2_3-2
      *> rules: [1985-SR-X.5.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.3 SR2 (THE CALL STATEMENT). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1681 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-PROG PIC 9(8) VALUE 12345678.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "START".
           STOP RUN.
       CALL-PARA.
           CALL WS-PROG.
           STOP RUN.
