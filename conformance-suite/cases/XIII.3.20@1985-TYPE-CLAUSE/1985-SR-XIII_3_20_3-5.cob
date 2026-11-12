      *>--- conformance-case
      *> id: 1985-SR-XIII_3_20_3-5
      *> rules: [1985-SR-XIII.3.20.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.3 SR5 (THE TYPE CLAUSE). A refusal: the rule
      *> forbids line 23 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6825 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC X(3) VALUE "AAA".
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           CONTROL IS WS-KEY
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 18.
       01  TYPE IS PAGE HEADING.
           05  LINE 1.
               10  COLUMN 1 PIC X(3) SOURCE WS-KEY.
       01  DL TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
