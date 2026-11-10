      *>--- conformance-case
      *> id: 1985-GR-XIII_3_22_4-1
      *> rules: [1985-GR-XIII.3.22.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.22.4 GR1 (THE VALUE CLAUSE). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1657 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWVAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DETL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 9(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
