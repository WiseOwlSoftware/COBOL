      *>--- conformance-case
      *> id: 1989-RV-A_2_30_4-2
      *> rules: [1989-RV-A.2.30.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.4 RV2 (THE NUMVAL-C FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. NVC18.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 R PIC 9(18).
000600 PROCEDURE DIVISION.
000700 P1.
000800     COMPUTE R = FUNCTION NUMVAL-C ("$123,456,789,012,345,678").
000900     DISPLAY R.
001000     STOP RUN.
