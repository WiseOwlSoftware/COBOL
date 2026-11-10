      *>--- conformance-case
      *> id: 1989-AR-A_2_29_3-1
      *> rules: [1989-AR-A.2.29.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.29.3 AR1 (THE NUMVAL FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC S999V99.
       01  WS-E PIC -999.99.
       PROCEDURE DIVISION.
       P1.
           COMPUTE WS-N = FUNCTION NUMVAL (" 12.50 CR ").
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           COMPUTE WS-N = FUNCTION NUMVAL (" + 3.5 ").
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           STOP RUN.
