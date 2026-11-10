      *>--- conformance-case
      *> id: 1985-GR-XIII_3_16_4-4
      *> rules: [1985-GR-XIII.3.16.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.16.4 GR4 (THE NEXT GROUP CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NGTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RPTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       77  WS-LC PIC 99.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES
           FIRST DETAIL 5
           LAST DETAIL 18.
       01  D1 TYPE IS DETAIL
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS PLUS 3.
           05  COLUMN 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1-USE SECTION.
           USE BEFORE REPORTING D1.
       D1-PARA.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY WS-LC.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       P1.
           OPEN OUTPUT RFILE.
           INITIATE RPT.
           GENERATE D1.
           GENERATE D1.
           GENERATE D1.
           TERMINATE RPT.
           CLOSE RFILE.
           STOP RUN.
