      *>--- conformance-case
      *> id: 1985-GR-VI_6_24_4-4
      *> rules: [1985-GR-VI.6.24.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.24.4 GR4 (THE STOP STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STOPLIT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "BEFORE".
           STOP "PAUSE".
           DISPLAY "AFTER".
           STOP RUN.
