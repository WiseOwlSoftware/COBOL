      *>--- conformance-case
      *> id: 1985-SR-VI_4_3_3-1
      *> rules: [1985-SR-VI.4.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.3 SR1 (THE SOURCE-COMPUTER PARAGRAPH). A refusal:
      *> the rule forbids line 5 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL3734 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. "XYZ".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
