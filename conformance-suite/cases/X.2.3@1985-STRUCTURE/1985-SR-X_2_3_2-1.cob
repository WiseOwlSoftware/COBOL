      *>--- conformance-case
      *> id: 1985-SR-X_2_3_2-1
      *> rules: [1985-SR-X.2.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.3.2 SR1 (STRUCTURE). A refusal: the rule forbids line
      *> 2 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL2793 there; a compiler that accepts it fails. Placed from
      *> written and run by the triage of the drafts that did not match
      *> (a suspect draft corrected) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       P1.
           CALL "INNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       P2.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM INNER.
