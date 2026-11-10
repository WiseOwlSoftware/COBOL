      *>--- conformance-case
      *> id: 1985-GR-XIII_3_20_4-9
      *> rules: [1985-GR-XIII.3.20.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR9 (THE TYPE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTYPE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-COUNT PIC 9 VALUE 0.
       REPORT SECTION.
       RD  RPT.
       01  RH-GRP TYPE IS REPORT HEADING.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  DET-GRP TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RH-USE SECTION.
           USE BEFORE REPORTING RH-GRP.
       RH-PARA.
           ADD 1 TO WS-COUNT.
           DISPLAY "RH USE EXECUTED".
           SUPPRESS PRINTING.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET-GRP.
           GENERATE DET-GRP.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "COUNT " WS-COUNT.
           STOP RUN.
