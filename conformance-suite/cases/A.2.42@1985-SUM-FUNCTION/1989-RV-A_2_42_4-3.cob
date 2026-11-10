      *>--- conformance-case
      *> id: 1989-RV-A_2_42_4-3
      *> rules: [1989-RV-A.2.42.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.42.4 RV3 (THE SUM FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9.99.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION SUM (1.5 2.25).
           DISPLAY R.
           STOP RUN.
