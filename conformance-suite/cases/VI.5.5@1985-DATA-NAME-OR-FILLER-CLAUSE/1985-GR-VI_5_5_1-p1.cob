      *>--- conformance-case
      *> id: 1985-GR-VI_5_5_1-p1
      *> rules: [1985-GR-VI.5.5.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.5.1 p1 (THE DATA-NAME OR FILLER CLAUSE). A refusal:
      *> the rule forbids line 14 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL5506 there; a compiler that accepts
      *> it fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-09.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-09 probe: FILLER names an item "which is not referenced
      * explicitly" (VI.5.5.1); a MOVE that names FILLER as its
      * receiver. Expect: refused. Control: s09-data.cob (FILLER items
      * declared and never named).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09FLR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  FILLER PIC X VALUE "[".
           05  F1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           MOVE "(" TO FILLER.
           DISPLAY REC.
           STOP RUN.
