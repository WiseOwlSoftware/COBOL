      *>--- conformance-case
      *> id: 1985-SR-IV_2_1_6-p2
      *> rules: [1985-SR-IV.2.1.6-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.2.1.6 p2 (DEFINITION OF A GENERAL FORMAT). A refusal:
      *> the rule forbids line 3 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL0307 there; a compiler that accepts
      *> it fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * PB5453 pilot s-01: the separator period of a format is required
       IDENTIFICATION DIVISION.
       PROGRAM-ID P02PER.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN".
           STOP RUN.
