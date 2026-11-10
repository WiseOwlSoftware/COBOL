      *>--- conformance-case
      *> id: 1989-FMT-A_2_22_2
      *> rules: [1989-FMT-A.2.22.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.22.2 (THE LOWER-CASE FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LCTEST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WS-IN   PIC X(5) VALUE "HELLO".
000600 01  WS-OUT  PIC X(5).
000700 PROCEDURE DIVISION.
000800 P1.
000900     MOVE FUNCTION LOWER-CASE ( WS-IN ) TO WS-OUT.
001000     DISPLAY WS-OUT.
001100     STOP RUN.
