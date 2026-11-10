      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_1-5
      *> rules: [1985-SR-IV.4.2.1-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 SR5 (LANGUAGE STRUCTURE). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2633 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY"HI".
           STOP RUN.
