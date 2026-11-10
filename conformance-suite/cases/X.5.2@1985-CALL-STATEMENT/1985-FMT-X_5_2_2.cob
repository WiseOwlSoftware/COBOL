      *>--- conformance-case
      *> id: 1985-FMT-X_5_2_2
      *> rules: [1985-FMT-X.5.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.2 (THE CALL STATEMENT). A refusal: the rule forbids
      *> line 8 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL0900 there; a compiler that accepts it fails. Placed from
      *> the audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           CALL "SUBPRG" USING "ABC".
           STOP RUN.
