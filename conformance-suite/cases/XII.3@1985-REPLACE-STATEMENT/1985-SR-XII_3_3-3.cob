      *>--- conformance-case
      *> id: 1985-SR-XII_3_3-3
      *> rules: [1985-SR-XII.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR3 (THE REPLACE STATEMENT). A refusal: the rule
      *> forbids line 5 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2572 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RPLTST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           REPLACE == == BY ==DISPLAY==.
           DISPLAY "OK".
           STOP RUN.
