      *>--- conformance-case
      *> id: 1985-SR-XIII_3_19_3-2
      *> rules: [1985-SR-XIII.3.19.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.3 SR2 (THE SUM CLAUSE). A refusal: the rule
      *> forbids line 21 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2046 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMUPON.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DET TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(5) SUM WS-AMT UPON CFG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
