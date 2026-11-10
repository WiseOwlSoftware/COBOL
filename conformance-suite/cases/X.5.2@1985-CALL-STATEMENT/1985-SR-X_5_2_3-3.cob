      *>--- conformance-case
      *> id: 1985-SR-X_5_2_3-3
      *> rules: [1985-SR-X.5.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.3 SR3 (THE CALL STATEMENT). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  GRP.
               10  FLD-A PIC X.
               10  FLD-B PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBPRG" USING GRP.
           STOP RUN.
