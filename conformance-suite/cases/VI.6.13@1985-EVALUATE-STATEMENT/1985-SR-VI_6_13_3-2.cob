      *>--- conformance-case
      *> id: 1985-SR-VI_6_13_3-2
      *> rules: [1985-SR-VI.6.13.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.3 SR2 (THE EVALUATE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EVSOBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           EVALUATE A ALSO TRUE ALSO B
               WHEN 2 ALSO ANY ALSO ANY
                   DISPLAY "WRONG"
               WHEN 1 ALSO B = 2 ALSO ANY
                   DISPLAY "SET MATCHED"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           EVALUATE A ALSO FALSE
               WHEN 1 ALSO B = 3
                   DISPLAY "FALSE OK"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
