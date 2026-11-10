      *>--- conformance-case
      *> id: 1985-FMT-IV_6_1_3_1
      *> rules: [1985-FMT-IV.6.1.3.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.1.3.1 (Structure). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the audit's
      *> draft, confirmed by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDTEST.
       AUTHOR. AUDIT READER.
       INSTALLATION. TEST SITE.
       DATE-WRITTEN. JANUARY 1 1985.
       DATE-COMPILED.
       SECURITY. NONE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "IDENT OK".
           STOP RUN.
