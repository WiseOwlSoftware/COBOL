      *>--- conformance-case
      *> id: 1985-SR-XIII_3_17_3-3
      *> rules: [1985-SR-XIII.3.17.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.17.3 SR3 (THE SIGN CLAUSE). A refusal: the rule
      *> forbids line 17 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SGNTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-NUM PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  DTL TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN 1 PIC S9(3) SOURCE WS-NUM SIGN LEADING.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
