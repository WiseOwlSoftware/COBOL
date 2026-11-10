      *>--- conformance-case
      *> id: 1989-AR-A_2_19_3-1
      *> rules: [1989-AR-A.2.19.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.19.3 AR1 (THE LENGTH FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LENTST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WS-ITEM PIC X(7).
000600 01  WS-NUM  PIC 9(5).
000700 01  WS-LEN  PIC 9(3).
000800 PROCEDURE DIVISION.
000900 MAIN-PARA.
001000     COMPUTE WS-LEN = FUNCTION LENGTH("ABCDE").
001100     DISPLAY WS-LEN.
001200     COMPUTE WS-LEN = FUNCTION LENGTH(WS-ITEM).
001300     DISPLAY WS-LEN.
001400     COMPUTE WS-LEN = FUNCTION LENGTH(WS-NUM).
001500     DISPLAY WS-LEN.
001600     STOP RUN.
