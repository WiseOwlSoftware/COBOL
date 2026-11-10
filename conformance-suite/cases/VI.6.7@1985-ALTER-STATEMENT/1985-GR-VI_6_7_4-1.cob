      *>--- conformance-case
      *> id: 1985-GR-VI_6_7_4-1
      *> rules: [1985-GR-VI.6.7.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.7.4 GR1 (THE ALTER STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTTST.
       PROCEDURE DIVISION.
       P0.
           GO TO SW.
       SW.
           GO TO TA.
       TA.
           DISPLAY "A".
           ALTER SW TO PROCEED TO TB.
           GO TO SW.
       TB.
           DISPLAY "B".
           STOP RUN.
