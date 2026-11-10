      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_3-L2_3
      *> rules: [1985-GR-IV.4.2.2.2.3-L2.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GRL2.3 (Literals). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIGALL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "[" ALL "AB" "]".
           STOP RUN.
