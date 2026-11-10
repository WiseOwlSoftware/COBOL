      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-7
      *> rules: [1985-GR-XII.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR7 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RPLTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
           REPLACE ==DISPLAY "A"== BY ==DISPLAY "B"==.
       MAIN-PARA.
           DISPLAY
      * THIS COMMENT LINE LIES INSIDE THE MATCHED TEXT

           "A".
           STOP RUN.
