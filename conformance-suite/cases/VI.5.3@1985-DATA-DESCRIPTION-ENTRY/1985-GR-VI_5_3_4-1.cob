      *>--- conformance-case
      *> id: 1985-GR-VI_5_3_4-1
      *> rules: [1985-GR-VI.5.3.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.4 GR1 (THE DATA DESCRIPTION ENTRY). A refusal: the
      *> rule forbids line 6 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2403 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. JUSTGRP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G JUSTIFIED RIGHT.
           05  A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           DISPLAY A.
           STOP RUN.
