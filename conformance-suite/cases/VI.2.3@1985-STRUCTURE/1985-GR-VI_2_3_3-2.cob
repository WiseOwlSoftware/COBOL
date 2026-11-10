      *>--- conformance-case
      *> id: 1985-GR-VI_2_3_3-2
      *> rules: [1985-GR-VI.2.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.3.3 GR2 (STRUCTURE). A refusal: the rule forbids
      *> line 2 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL2793 there; a compiler that accepts it fails. Placed from
      *> written and run by the triage of the drafts that did not match
      *> (a suspect draft corrected) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROGA.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A".
           STOP RUN.
      *END PROGRAM PROGA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROGB.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "B".
           EXIT PROGRAM.
