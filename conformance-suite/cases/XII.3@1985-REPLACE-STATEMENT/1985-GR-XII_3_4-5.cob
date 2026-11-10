      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-5
      *> rules: [1985-GR-XII.3.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR5 (THE REPLACE STATEMENT). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2574 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REPL85.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
           REPLACE ==XREP== BY ==REPLACE==.
       MAIN-PARA.
           XREP ==A== BY ==B==.
           DISPLAY "OK".
           STOP RUN.
