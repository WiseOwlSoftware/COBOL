      *>--- conformance-case
      *> id: 1985-SR-XIII_3_15_3-1
      *> rules: [1985-SR-XIII.3.15.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.3 SR1 (THE LINE NUMBER CLAUSE). A refusal: the
      *> rule forbids line 17 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2199 there; a compiler that accepts it
      *> fails. Placed from written and run by the triage of the drafts
      *> that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LNTEST.
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
001300     PAGE LIMIT IS 60 LINES
001400     HEADING 1
001500     FIRST DETAIL 5
001600     LAST DETAIL 50.
001700 01  DET-LINE TYPE IS DETAIL LINE NUMBER IS 55.
001800     05  COLUMN 1 PIC X(5) VALUE "HELLO".
001900 PROCEDURE DIVISION.
002000 MAIN-PARA.
002100     OPEN OUTPUT RPT-FILE.
002200     INITIATE RPT.
002300     GENERATE DET-LINE.
002400     TERMINATE RPT.
002500     CLOSE RPT-FILE.
002600     STOP RUN.
