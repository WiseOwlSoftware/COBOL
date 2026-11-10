      *>--- conformance-case
      *> id: 1985-SR-XIII_3_17_3-1
      *> rules: [1985-SR-XIII.3.17.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.17.3 SR1 (THE SIGN CLAUSE). A refusal: the rule
      *> forbids line 17 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2422 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIGNRW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-N PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 9(3) SIGN IS LEADING SEPARATE
               SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
