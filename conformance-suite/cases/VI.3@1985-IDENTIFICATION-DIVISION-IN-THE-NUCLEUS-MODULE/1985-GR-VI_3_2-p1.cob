      *>--- conformance-case
      *> id: 1985-GR-VI_3_2-p1
      *> rules: [1985-GR-VI.3.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.2 p1 (IDENTIFICATION DIVISION IN THE NUCLEUS
      *> MODULE). A refusal: the rule forbids line 7 of the program
      *> below, and WiseOwl refuses it at 85 with COBOL2987 there; a
      *> compiler that accepts it fails. Placed from written and run by
      *> the PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: DATE-WRITTEN before AUTHOR, against the order of
      * presentation of VI.3.2. Expect: refused. Control:
      * s09-id-paragraphs.cob (same paragraphs in format order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09IDB.
       DATE-WRITTEN. 21 NOV 2026.
       AUTHOR. A PERSON.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "ID-OK".
           STOP RUN.
