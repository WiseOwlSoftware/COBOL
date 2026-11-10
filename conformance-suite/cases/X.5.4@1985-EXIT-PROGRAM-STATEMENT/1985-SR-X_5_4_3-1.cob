      *>--- conformance-case
      *> id: 1985-SR-X_5_4_3-1
      *> rules: [1985-SR-X.5.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.3 SR1 (THE EXIT PROGRAM STATEMENT). A refusal: the
      *> rule forbids line 5 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0900 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXITP.
       PROCEDURE DIVISION.
       P1.
           EXIT PROGRAM DISPLAY "AFTER".
           STOP RUN.
