      *>--- conformance-case
      *> id: 1985-SR-XIII_3_21_3-3
      *> rules: [1985-SR-XIII.3.21.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.3 SR3 (THE USAGE CLAUSE). A refusal: the rule
      *> forbids line 17 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2198 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RWUSAGE.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT RPT-FILE ASSIGN TO PRINTER.
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  RPT-FILE
001000     REPORT IS RPT.
001100 WORKING-STORAGE SECTION.
001200 01  WS-N PIC 9(3) VALUE 5.
001300 REPORT SECTION.
001400 RD  RPT
001500     PAGE LIMIT IS 60 LINES.
001600 01  DTL TYPE IS DETAIL LINE NUMBER IS 1.
001700     05  COLUMN 1 PIC 9(3) SOURCE WS-N USAGE COMPUTATIONAL.
001900 PROCEDURE DIVISION.
002000 MAIN-PARA.
002100     OPEN OUTPUT RPT-FILE.
002200     INITIATE RPT.
002300     GENERATE DTL.
002400     TERMINATE RPT.
002500     CLOSE RPT-FILE.
002600     STOP RUN.
