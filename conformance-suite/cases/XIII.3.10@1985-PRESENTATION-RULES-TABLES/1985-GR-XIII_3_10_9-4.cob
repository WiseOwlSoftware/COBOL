      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_9-4
      *> rules: [1985-GR-XIII.3.10.9-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.9 GR4 (PRESENTATION RULES TABLES). A refusal:
      *> the rule forbids line 21 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2284 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NGTEST.
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
           FIRST DETAIL 2
           LAST DETAIL 15
           FOOTING 18.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       01  TYPE IS PAGE FOOTING LINE NUMBER IS 19
           NEXT GROUP IS 10.
           05  COLUMN 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
