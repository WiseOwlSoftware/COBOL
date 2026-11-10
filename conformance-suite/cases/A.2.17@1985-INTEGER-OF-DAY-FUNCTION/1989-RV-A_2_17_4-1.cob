      *>--- conformance-case
      *> id: 1989-RV-A_2_17_4-1
      *> rules: [1989-RV-A.2.17.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.17.4 RV1 (THE INTEGER-OF-DAY FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. INTDAY.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WS-N  PIC 9(7).
000600 PROCEDURE DIVISION.
000700 P1.
000800     COMPUTE WS-N = FUNCTION INTEGER-OF-DAY (1601001).
000900     DISPLAY WS-N.
001000     COMPUTE WS-N = FUNCTION INTEGER-OF-DAY (1601365).
001100     DISPLAY WS-N.
001200     COMPUTE WS-N = FUNCTION INTEGER-OF-DAY (1602001).
001300     DISPLAY WS-N.
001400     COMPUTE WS-N = FUNCTION INTEGER-OF-DAY (1604060).
001500     DISPLAY WS-N.
001600     STOP RUN.
