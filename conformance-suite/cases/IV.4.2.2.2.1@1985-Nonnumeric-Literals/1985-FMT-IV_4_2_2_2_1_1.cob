      *>--- conformance-case
      *> id: 1985-FMT-IV_4_2_2_2_1_1
      *> rules: [1985-FMT-IV.4.2.2.2.1.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.1.1 (Nonnumeric Literals). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NNLIT.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ABC 123".
           STOP RUN.
