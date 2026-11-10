      *>--- conformance-case
      *> id: 1985-FMT-VI_3_3_2
      *> rules: [1985-FMT-VI.3.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.3.2 (THE PROGRAM-ID PARAGRAPH). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGMID1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "PROGRAM-ID OK".
           STOP RUN.
