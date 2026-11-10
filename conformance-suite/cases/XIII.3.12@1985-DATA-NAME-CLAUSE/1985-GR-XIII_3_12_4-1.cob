      *>--- conformance-case
      *> id: 1985-GR-XIII_3_12_4-1
      *> rules: [1985-GR-XIII.3.12.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.12.4 GR1 (THE DATA-NAME CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWDN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 0.
       01  WS-TOT PIC 9(5) VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL
           PAGE LIMIT IS 60 LINES.
       01  DLINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  TOT COLUMN 1 PIC 9(5) SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 5 TO WS-AMT.
           GENERATE DLINE.
           MOVE 7 TO WS-AMT.
           GENERATE DLINE.
           MOVE TOT TO WS-TOT.
           DISPLAY WS-TOT.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
