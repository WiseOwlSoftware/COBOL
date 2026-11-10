      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_19_2
      *> rules: [1985-FMT-XIII.3.19.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.2 (THE SUM CLAUSE). A refusal: the rule forbids
      *> line 19 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL0001 there; a compiler that accepts it fails. Placed from
      *> the audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05 COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05 COLUMN 1 PIC 9(5) SUM UPON DTL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
