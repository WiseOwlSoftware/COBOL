      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_8-6
      *> rules: [1985-GR-XIII.3.10.8-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.8 GR6 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LCTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15
           FOOTING 18.
       01  DL TYPE IS DETAIL
           LINE NUMBER IS 5
           NEXT GROUP IS PLUS 2.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DL.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY WS-LC.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
