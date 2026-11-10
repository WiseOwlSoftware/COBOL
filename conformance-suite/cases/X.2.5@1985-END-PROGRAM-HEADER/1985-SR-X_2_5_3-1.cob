      *>--- conformance-case
      *> id: 1985-SR-X_2_5_3-1
      *> rules: [1985-SR-X.2.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.5.3 SR1 (END PROGRAM HEADER). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2793 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ABCDEFGHIJKLMNOPQRSTUVWXYZ1234.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "END PROGRAM HEADER".
           STOP RUN.
       END PROGRAM ABCDEFGHIJKLMNOPQRSTUVWXYZ12345.
