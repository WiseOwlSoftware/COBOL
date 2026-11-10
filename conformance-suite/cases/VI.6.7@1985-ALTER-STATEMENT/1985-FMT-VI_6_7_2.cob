      *>--- conformance-case
      *> id: 1985-FMT-VI_6_7_2
      *> rules: [1985-FMT-VI.6.7.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.7.2 (THE ALTER STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P0.
           ALTER P1 TO PROCEED TO P3.
       P1.
           GO TO P2.
       P2.
           DISPLAY "NOT ALTERED".
           STOP RUN.
       P3.
           DISPLAY "ALTERED".
           STOP RUN.
