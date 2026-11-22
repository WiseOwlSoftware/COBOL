      *>--- conformance-case
      *> id: 1985-SR-III_2-p230
      *> rules: [1985-SR-III.2-p230]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p230 ((section preamble)). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1641 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P230NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           REPLACE =BUMP= BY ==ADD 1 TO N==.
       P-MAIN.
           BUMP.
           DISPLAY "P230 NEG ACCEPTED " N.
           STOP RUN.
