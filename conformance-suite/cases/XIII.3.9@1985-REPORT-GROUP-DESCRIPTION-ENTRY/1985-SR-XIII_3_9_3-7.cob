      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-7
      *> rules: [1985-SR-XIII.3.9.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR7 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 24 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL5092 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL PAGE LIMIT IS 60 LINES.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC 9(3) SOURCE W-AMT.
       01  TYPE IS CONTROL FOOTING FINAL.
           05  TOT-GRP LINE NUMBER IS PLUS 2.
               10  TOT-X COLUMN 1 PIC 9(5) SUM W-AMT.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           DISPLAY TOT-GRP.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
