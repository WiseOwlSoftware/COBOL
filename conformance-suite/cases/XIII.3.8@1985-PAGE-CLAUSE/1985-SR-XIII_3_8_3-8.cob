      *>--- conformance-case
      *> id: 1985-SR-XIII_3_8_3-8
      *> rules: [1985-SR-XIII.3.8.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.8.3 SR8 (THE PAGE CLAUSE). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2199 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGRULE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 15
           FOOTING 18.
       01  TYPE IS PAGE HEADING LINE NUMBER IS 6.
           05  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "LINE1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
