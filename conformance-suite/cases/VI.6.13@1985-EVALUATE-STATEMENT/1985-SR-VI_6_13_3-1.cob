      *>--- conformance-case
      *> id: 1985-SR-VI_6_13_3-1
      *> rules: [1985-SR-VI.6.13.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.3 SR1 (THE EVALUATE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EVALT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9 VALUE 1.
       77  B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           EVALUATE TRUE ALSO FALSE ALSO B
               WHEN A = 1 ALSO A = 2 ALSO 2
                   DISPLAY "MATCH"
               WHEN OTHER
                   DISPLAY "NO MATCH"
           END-EVALUATE.
           STOP RUN.
