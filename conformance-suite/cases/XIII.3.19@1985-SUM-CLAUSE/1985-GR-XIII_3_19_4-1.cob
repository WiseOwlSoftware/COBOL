      *>--- conformance-case
      *> id: 1985-GR-XIII_3_19_4-1
      *> rules: [1985-GR-XIII.3.19.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.4 GR1 (THE SUM CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMCTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RPTOUT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9V99.
       01  WS-OUT PIC 999.99.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DET TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9.99 SOURCE WS-AMT.
       01  TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           05  TOT-N COLUMN 1 PIC 9(3)V9 SUM WS-AMT.
           05  TOT-E COLUMN 10 PIC ZZ9.9 SUM WS-AMT.
           05  TOT-X COLUMN 20 PIC X(4) SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 9.57 TO WS-AMT.
           GENERATE DET.
           GENERATE DET.
           MOVE TOT-N TO WS-OUT.
           DISPLAY "NUMERIC " WS-OUT.
           MOVE TOT-E TO WS-OUT.
           DISPLAY "EDITED  " WS-OUT.
           MOVE TOT-X TO WS-OUT.
           DISPLAY "ALPHANUM " WS-OUT.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
