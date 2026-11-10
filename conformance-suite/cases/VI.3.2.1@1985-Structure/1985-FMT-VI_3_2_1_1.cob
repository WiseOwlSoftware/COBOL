      *>--- conformance-case
      *> id: 1985-FMT-VI_3_2_1_1
      *> rules: [1985-FMT-VI.3.2.1.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.2.1.1 (Structure). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the audit's
      *> draft, confirmed by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDTEST.
       AUTHOR. A TESTER.
       INSTALLATION. AUDIT LAB.
       DATE-WRITTEN. JANUARY 1 1990.
       SECURITY. NONE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "IDENTIFICATION OK".
           STOP RUN.
