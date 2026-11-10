      *>--- conformance-case
      *> id: 1989-FMT-A_2_19_2
      *> rules: [1989-FMT-A.2.19.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.19.2 (THE LENGTH FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LENTST.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WS-ITEM    PIC X(5)  VALUE "ABCDE".
000600 01  WS-GROUP.
000700     05  WS-G1  PIC X(3).
000800     05  WS-G2  PIC 9(4).
000900 01  WS-LEN     PIC 9(3).
001000 PROCEDURE DIVISION.
001100 MAIN-PARA.
001200     COMPUTE WS-LEN = FUNCTION LENGTH (WS-ITEM).
001300     DISPLAY WS-LEN.
001400     COMPUTE WS-LEN = FUNCTION LENGTH (WS-GROUP).
001500     DISPLAY WS-LEN.
001600     COMPUTE WS-LEN = FUNCTION LENGTH ("HELLO WORLD").
001700     DISPLAY WS-LEN.
001800     STOP RUN.
