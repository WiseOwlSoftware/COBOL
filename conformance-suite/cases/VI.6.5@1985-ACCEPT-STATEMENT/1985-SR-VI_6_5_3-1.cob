      *>--- conformance-case
      *> id: 1985-SR-VI_6_5_3-1
      *> rules: [1985-SR-VI.6.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.5.3 SR1 (THE ACCEPT STATEMENT). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0817 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ACCTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X(10).
       PROCEDURE DIVISION.
       P1.
           ACCEPT X FROM NODEVICE.
           DISPLAY X.
           STOP RUN.
