      *>--- conformance-case
      *> id: 1985-GR-VI_6_7_4-2
      *> rules: [1985-GR-VI.6.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.7.4 GR2 (THE ALTER STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 50.
       P1.
           ALTER P2 TO PROCEED TO P4.
       P2.
           GO TO P3.
       P3.
           DISPLAY "NOT ALTERED".
           STOP RUN.
       P4.
           DISPLAY "ALTERED".
           STOP RUN.
