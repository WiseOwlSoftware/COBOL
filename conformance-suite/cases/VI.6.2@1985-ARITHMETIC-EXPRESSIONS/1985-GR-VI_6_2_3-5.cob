      *>--- conformance-case
      *> id: 1985-GR-VI_6_2_3-5
      *> rules: [1985-GR-VI.6.2.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.2.3 GR5 (ARITHMETIC EXPRESSIONS). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXPTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A    PIC 9 VALUE 4.
       01  Z    PIC 9 VALUE 0.
       01  R    PIC -9.
       01  S    PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R ROUNDED = A ** 0.5
           DISPLAY R
           COMPUTE S = Z ** 0
               ON SIZE ERROR DISPLAY "SIZE ERROR"
           END-COMPUTE
           DISPLAY S
           STOP RUN.
