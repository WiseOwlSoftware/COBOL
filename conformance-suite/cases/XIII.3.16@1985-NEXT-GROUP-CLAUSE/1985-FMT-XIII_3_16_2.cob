      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_16_2
      *> rules: [1985-FMT-XIII.3.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.16.2 (THE NEXT GROUP CLAUSE). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0001 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NGTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RPTOUT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-A.
       REPORT SECTION.
       RD  RPT-A
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 18
           FOOTING 19.
       01  DET-LINE TYPE IS DETAIL
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS PAGE.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT-A.
           GENERATE DET-LINE.
           TERMINATE RPT-A.
           CLOSE RPT-FILE.
           STOP RUN.
