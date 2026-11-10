      *>--- conformance-case
      *> id: 1985-SR-XIII_3_19_3-3
      *> rules: [1985-SR-XIII.3.19.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.3 SR3 (THE SUM CLAUSE). A refusal: the rule
      *> forbids line 18 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6763 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO PRTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 9(3) SOURCE W-AMT.
           05  COLUMN 10 PIC 9(5) SUM W-AMT.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RFILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RFILE.
           STOP RUN.
