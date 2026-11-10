      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_2_1_2-1
      *> rules: [1985-SR-IV.4.2.2.2.1.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.1.2 SR1 (Nonnumeric Literals). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NNLIT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "abc@#%&!?[]".
           STOP RUN.
