      *>--- conformance-case
      *> id: 1985-SR-XVI_2_2_3-1
      *> rules: [1985-SR-XVI.2.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.2.3 SR1 (THE OBJECT-COMPUTER PARAGRAPH). A refusal:
      *> the rule forbids line 5 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL0900 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. "COMPUTER-X" SEGMENT-LIMIT IS 10.
       PROCEDURE DIVISION.
       MAIN SECTION.
       P1.
           DISPLAY "OK".
           STOP RUN.
