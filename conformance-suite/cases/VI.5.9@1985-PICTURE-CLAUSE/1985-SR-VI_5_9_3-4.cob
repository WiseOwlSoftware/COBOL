      *>--- conformance-case
      *> id: 1985-SR-VI_5_9_3-4
      *> rules: [1985-SR-VI.5.9.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.3 SR4 (THE PICTURE CLAUSE). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2146 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
