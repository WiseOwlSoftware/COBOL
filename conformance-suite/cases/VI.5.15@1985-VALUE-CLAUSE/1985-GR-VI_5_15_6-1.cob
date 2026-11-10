      *>--- conformance-case
      *> id: 1985-GR-VI_5_15_6-1
      *> rules: [1985-GR-VI.5.15.6-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.6 GR1 (THE VALUE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(5) VALUE "HELLO".
       01  WS-N PIC 9(3) VALUE 42.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-A.
           DISPLAY WS-N.
           STOP RUN.
