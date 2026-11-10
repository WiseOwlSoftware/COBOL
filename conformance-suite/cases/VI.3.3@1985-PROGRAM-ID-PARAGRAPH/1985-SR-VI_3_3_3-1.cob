      *>--- conformance-case
      *> id: 1985-SR-VI_3_3_3-1
      *> rules: [1985-SR-VI.3.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.3.3 SR1 (THE PROGRAM-ID PARAGRAPH). A refusal: the
      *> rule forbids line 2 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2773 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROGA-.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
