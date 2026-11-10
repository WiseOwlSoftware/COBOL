      *>--- conformance-case
      *> id: 1985-GR-XII_3_4-2
      *> rules: [1985-GR-XII.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR2 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REPTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-A PIC X(3) VALUE "AAA".
       01  MSG-B PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       P1.
           REPLACE ==MSG-A== BY ==MSG-B==.
           DISPLAY MSG-A.
           REPLACE OFF.
           DISPLAY MSG-A.
           STOP RUN.
