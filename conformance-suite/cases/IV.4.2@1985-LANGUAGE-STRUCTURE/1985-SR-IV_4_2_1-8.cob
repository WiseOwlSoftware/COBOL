      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_1-8
      *> rules: [1985-SR-IV.4.2.1-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 SR8 (LANGUAGE STRUCTURE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEPTST.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A " "B" .
           DISPLAY "C" , "D " , "E" .
           STOP RUN .
