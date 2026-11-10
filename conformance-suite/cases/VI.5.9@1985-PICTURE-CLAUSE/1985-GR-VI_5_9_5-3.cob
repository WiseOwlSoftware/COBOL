      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_5-3
      *> rules: [1985-GR-VI.5.9.5-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR3 (THE PICTURE CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1935 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-EDIT PIC $$$ZZ9.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO W-EDIT.
           DISPLAY W-EDIT.
           STOP RUN.
