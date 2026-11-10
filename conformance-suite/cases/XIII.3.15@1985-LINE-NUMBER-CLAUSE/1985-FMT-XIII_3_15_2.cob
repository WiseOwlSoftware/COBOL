      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_15_2
      *> rules: [1985-FMT-XIII.3.15.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.2 (THE LINE NUMBER CLAUSE). A refusal: the rule
      *> forbids line 15 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LNTEST.
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
           PAGE LIMIT IS 20 LINES.
       01  DETAIL-LINE TYPE IS DETAIL
           LINE PLUS 1 ON NEXT PAGE.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETAIL-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
