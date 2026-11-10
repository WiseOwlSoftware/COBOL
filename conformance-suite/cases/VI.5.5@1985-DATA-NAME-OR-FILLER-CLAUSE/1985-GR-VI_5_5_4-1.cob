      *>--- conformance-case
      *> id: 1985-GR-VI_5_5_4-1
      *> rules: [1985-GR-VI.5.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.5.4 GR1 (THE DATA-NAME OR FILLER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOFILL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  PIC X(3) VALUE "ABC".
           05  B PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       P1.
           DISPLAY REC.
           DISPLAY B.
           STOP RUN.
