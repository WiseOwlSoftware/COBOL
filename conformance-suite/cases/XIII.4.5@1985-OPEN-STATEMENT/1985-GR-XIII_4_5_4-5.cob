      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-5
      *> rules: [1985-GR-XIII.4.5.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR5 (THE OPEN STATEMENT). An output test: the
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
001000     LABEL RECORDS ARE STANDARD
001100     REPORT IS RPT.
001200 REPORT SECTION.
001300 RD  RPT.
001400 01  DTL TYPE IS DETAIL LINE PLUS 1.
001500     05  COLUMN 1 PIC X(5) VALUE "HELLO".
001600 PROCEDURE DIVISION.
001700 MAIN-PARA.
001800     OPEN OUTPUT RPT-FILE.
001900     INITIATE RPT.
002000     GENERATE DTL.
002100     TERMINATE RPT.
002200     CLOSE RPT-FILE.
002300     DISPLAY "OPEN OUTPUT OK".
002400     STOP RUN.
