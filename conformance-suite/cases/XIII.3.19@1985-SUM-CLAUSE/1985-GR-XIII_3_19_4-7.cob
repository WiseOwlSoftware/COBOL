      *>--- conformance-case
      *> id: 1985-GR-XIII_3_19_4-7
      *> rules: [1985-GR-XIII.3.19.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.4 GR7 (THE SUM CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMGR7.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-KEY            PIC X.
       01  WS-AMT            PIC 9.
       01  WS-SUM            PIC 9(3).
       REPORT SECTION.
       RD  RPT
           CONTROL IS WS-KEY
           PAGE LIMIT IS 60 LINES.
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1  PIC X  SOURCE WS-KEY.
           05  COLUMN 5  PIC 9  SOURCE WS-AMT.
       01  CF-GRP TYPE IS CONTROL FOOTING WS-KEY
           LINE NUMBER IS PLUS 1.
           05  SUM-AMT COLUMN 10 PIC 9(3) SUM WS-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-SEC SECTION.
           USE BEFORE REPORTING CF-GRP.
       CF-PARA.
           MOVE SUM-AMT TO WS-SUM.
           DISPLAY "CF " WS-SUM.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE "A" TO WS-KEY.
           MOVE 1 TO WS-AMT.
           GENERATE DET-LINE.
           MOVE 2 TO WS-AMT.
           GENERATE DET-LINE.
           MOVE "B" TO WS-KEY.
           MOVE 4 TO WS-AMT.
           GENERATE DET-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
