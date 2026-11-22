      *>--- conformance-case
      *> id: 1985-GR-III_2-p323
      *> rules: [1985-GR-III.2-p323]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p323 ((section preamble)). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0844 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VARGRP.
      * 1985 III.2 Variable: a group item is not a numeric elementary
      * item, so it shall not be a variable of an arithmetic
      * expression (the one line changed from varctl.cob).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-GRP.
           05  W-V      PIC 99 VALUE 20.
       01  W-R          PIC 999.
       PROCEDURE DIVISION.
       P1.
           COMPUTE W-R = W-GRP + 1.
           IF W-R = 21 DISPLAY "VARCTL PASS"
               ELSE DISPLAY "VARCTL FAIL " W-R.
           STOP RUN.
