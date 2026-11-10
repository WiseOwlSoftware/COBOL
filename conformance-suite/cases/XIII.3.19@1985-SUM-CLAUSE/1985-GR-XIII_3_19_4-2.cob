      *>--- conformance-case
      *> id: 1985-GR-XIII_3_19_4-2
      *> rules: [1985-GR-XIII.3.19.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.4 GR2 (THE SUM CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-VAL PIC 9 VALUE 0.
       01  WS-TOT PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 99 SOURCE WS-VAL.
       01  CFF TYPE IS CONTROL FOOTING FINAL
           LINE NUMBER IS PLUS 1.
           05  TOT COLUMN 1 PIC 99 SUM WS-VAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SHOW-SUM SECTION.
           USE BEFORE REPORTING CFF.
       SHOW-SUM-PARA.
           MOVE TOT TO WS-TOT.
           DISPLAY WS-TOT.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 1 TO WS-VAL.
           GENERATE DL.
           MOVE 2 TO WS-VAL.
           GENERATE DL.
           MOVE 3 TO WS-VAL.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
