      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_21_2
      *> rules: [1985-FMT-XIII.3.21.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.2 (THE USAGE CLAUSE). A refusal: the rule
      *> forbids line 17 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2198 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWUSAGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE OMITTED
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-NUM PIC 9(4) VALUE 12.
       REPORT SECTION.
       RD  RPT.
       01  DETL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 9(4) SOURCE W-NUM USAGE IS COMPUTATIONAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
