      *>--- conformance-case
      *> id: 1985-SR-XIII_3_21_3-2
      *> rules: [1985-SR-XIII.3.21.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.3 SR2 (THE USAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. USGRW.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT RPT-FILE ASSIGN TO "RPTOUT".
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  RPT-FILE
001000     REPORT IS RPT.
001100 REPORT SECTION.
001200 RD  RPT
001300     PAGE LIMIT IS 60 LINES.
001400 01  DL TYPE IS DETAIL USAGE IS DISPLAY.
001450     02 LINE NUMBER IS 1 USAGE IS DISPLAY.
001500       03 COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO"
001600          USAGE IS DISPLAY.
001700 PROCEDURE DIVISION.
001800 MAIN-PARA.
001900     OPEN OUTPUT RPT-FILE.
002000     INITIATE RPT.
002100     GENERATE DL.
002200     TERMINATE RPT.
002300     CLOSE RPT-FILE.
002400     DISPLAY "USAGE OK".
002500     STOP RUN.
