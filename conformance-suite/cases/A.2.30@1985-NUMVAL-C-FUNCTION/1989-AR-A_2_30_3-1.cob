      *>--- conformance-case
      *> id: 1989-AR-A_2_30_3-1
      *> rules: [1989-AR-A.2.30.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.3 AR1 (THE NUMVAL-C FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMVALC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-E PIC Z,ZZ9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-E = FUNCTION NUMVAL-C (" $1,234.56 " "$").
           DISPLAY WS-E.
           STOP RUN.
