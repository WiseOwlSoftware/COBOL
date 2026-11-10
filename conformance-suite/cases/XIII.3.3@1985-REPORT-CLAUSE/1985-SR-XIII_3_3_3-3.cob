      *>--- conformance-case
      *> id: 1985-SR-XIII_3_3_3-3
      *> rules: [1985-SR-XIII.3.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.3.3 SR3 (THE REPORT CLAUSE). A refusal: the rule
      *> forbids line 19 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2371 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RWTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT RPT-FILE ASSIGN TO PRINTER.
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  RPT-FILE
001000     REPORT IS RPT.
001100 REPORT SECTION.
001200 RD  RPT
001300     PAGE LIMIT IS 60 LINES.
001400 01  DL TYPE IS DETAIL.
001500     05  LINE NUMBER IS 1.
001600         10  COLUMN 1 PIC X(5) VALUE "HELLO".
001700 PROCEDURE DIVISION.
001800 P1.
001900     OPEN INPUT RPT-FILE.
002000     INITIATE RPT.
002100     GENERATE DL.
002200     TERMINATE RPT.
002300     CLOSE RPT-FILE.
002400     STOP RUN.
