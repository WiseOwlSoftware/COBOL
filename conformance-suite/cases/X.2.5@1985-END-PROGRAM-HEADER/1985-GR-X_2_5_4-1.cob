      *>--- conformance-case
      *> id: 1985-GR-X_2_5_4-1
      *> rules: [1985-GR-X.2.5.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.5.4 GR1 (END PROGRAM HEADER). A refusal: the rule
      *> forbids line 2 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2793 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "INNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       INNER-PARA.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM INNER.
      *END PROGRAM OUTER.
