      *>--- conformance-case
      *> id: 1985-SR-XIII_3_19_3-5
      *> rules: [1985-SR-XIII.3.19.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.3 SR5 (THE SUM CLAUSE). A refusal: the rule
      *> forbids line 24 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2145 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DTL TYPE DETAIL LINE NUMBER PLUS 1.
           05  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  TYPE CONTROL FOOTING FINAL LINE NUMBER PLUS 1.
           05  TOT COLUMN 1 PIC 9(5) SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           DISPLAY TOT OF PRT-FILE.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
