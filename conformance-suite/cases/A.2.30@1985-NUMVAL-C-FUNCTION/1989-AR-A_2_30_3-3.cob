      *>--- conformance-case
      *> id: 1989-AR-A_2_30_3-3
      *> rules: [1989-AR-A.2.30.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.3 AR3 (THE NUMVAL-C FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NVC18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ARG    PIC X(24) VALUE "$123,456,789,012,345,678".
       01  WS-RES    PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-RES = FUNCTION NUMVAL-C (WS-ARG).
           DISPLAY WS-RES.
           STOP RUN.
