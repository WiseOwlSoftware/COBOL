      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-2
      *> rules: [1985-GR-XIII.4.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR2 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RWOPEN.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT RPT-FILE ASSIGN TO PRINTER.
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  RPT-FILE
001000     REPORT IS RPT.
001100 REPORT SECTION.
001200 RD  RPT.
001300 01  DETAIL-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
001400     05 COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
001500 PROCEDURE DIVISION.
001600 MAIN-PARA.
001700     OPEN OUTPUT RPT-FILE.
001800     DISPLAY "OPENED".
001900     INITIATE RPT.
002000     GENERATE DETAIL-LINE.
002100     TERMINATE RPT.
002200     CLOSE RPT-FILE.
002300     DISPLAY "CLOSED".
002400     STOP RUN.
