      *>--- conformance-case
      *> id: 1985-GR-XIII_3_20_4-13
      *> rules: [1985-GR-XIII.3.20.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR13 (THE TYPE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PRIORV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC X VALUE "A".
       REPORT SECTION.
       RD  RPT
           CONTROLS ARE WS-KEY.
       01  DET-LINE TYPE IS DETAIL
               LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X SOURCE WS-KEY.
       01  CF-KEY TYPE IS CONTROL FOOTING WS-KEY
               LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X SOURCE WS-KEY.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-USE SECTION.
           USE BEFORE REPORTING CF-KEY.
       CF-PARA.
           DISPLAY "CF " WS-KEY.
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET-LINE.
           MOVE "B" TO WS-KEY.
           GENERATE DET-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
