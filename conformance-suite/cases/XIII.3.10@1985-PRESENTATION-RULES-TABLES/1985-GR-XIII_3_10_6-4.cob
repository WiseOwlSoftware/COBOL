      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_6-4
      *> rules: [1985-GR-XIII.3.10.6-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.6 GR4 (PRESENTATION RULES TABLES). A refusal:
      *> the rule forbids line 19 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2284 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWNG.
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
           PAGE LIMIT IS 60 LINES
           HEADING 1
           FIRST DETAIL 10
           LAST DETAIL 50.
       01  TYPE IS REPORT HEADING
           LINE NUMBER IS 2
           NEXT GROUP IS 12.
           05  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "LINE1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
