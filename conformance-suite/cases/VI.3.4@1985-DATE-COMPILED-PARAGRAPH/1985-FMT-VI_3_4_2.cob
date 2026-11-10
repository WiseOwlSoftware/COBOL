      *>--- conformance-case
      *> id: 1985-FMT-VI_3_4_2
      *> rules: [1985-FMT-VI.3.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.4.2 (THE DATE-COMPILED PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCTEST.
       DATE-COMPILED. THIS TEXT IS A COMMENT-ENTRY.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DATE-COMPILED OK".
           STOP RUN.
