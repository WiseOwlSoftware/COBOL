      *>--- conformance-case
      *> id: 1985-GR-VI_3_1-p1
      *> rules: [1985-GR-VI.3.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.1 p1 (IDENTIFICATION DIVISION IN THE NUCLEUS
      *> MODULE). A refusal: the rule forbids line 4 of the program
      *> below, and WiseOwl refuses it at 85 with COBOL0900 there; a
      *> compiler that accepts it fails. Placed from written and run by
      *> the PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: no IDENTIFICATION DIVISION header; VI.3.1 says the
      * Identification Division is required. Expect: refused at 85.
      * Control: s09-minimal.cob (the same program with the header).
       PROGRAM-ID. S09NOH.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "MIN-OK".
           STOP RUN.
