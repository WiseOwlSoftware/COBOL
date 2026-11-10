      *>--- conformance-case
      *> id: 1985-FMT-VI_6_12_2
      *> rules: [1985-FMT-VI.6.12.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.2 (THE ENTER STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENTTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE".
           ENTER COBOL.
           DISPLAY "AFTER".
           STOP RUN.
