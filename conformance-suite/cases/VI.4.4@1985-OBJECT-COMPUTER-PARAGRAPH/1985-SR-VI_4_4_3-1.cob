      *>--- conformance-case
      *> id: 1985-SR-VI_4_4_3-1
      *> rules: [1985-SR-VI.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.3 SR1 (THE OBJECT-COMPUTER PARAGRAPH). A refusal:
      *> the rule forbids line 5 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL1970 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OCTEST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. "COMP-X".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
