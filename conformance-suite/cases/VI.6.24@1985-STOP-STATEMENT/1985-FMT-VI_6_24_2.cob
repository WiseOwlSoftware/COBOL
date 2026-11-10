      *>--- conformance-case
      *> id: 1985-FMT-VI_6_24_2
      *> rules: [1985-FMT-VI.6.24.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.24.2 (THE STOP STATEMENT). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STOPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "A".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "HI".
           STOP X.
