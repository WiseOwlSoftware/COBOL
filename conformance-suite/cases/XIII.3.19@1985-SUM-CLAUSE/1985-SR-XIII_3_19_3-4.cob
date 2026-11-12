      *>--- conformance-case
      *> id: 1985-SR-XIII_3_19_3-4
      *> rules: [1985-SR-XIII.3.19.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.3 SR4 (THE SUM CLAUSE). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6824 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RSTCHK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X VALUE "A".
       01  WS-B    PIC X VALUE "B".
       01  WS-AMT  PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           CONTROLS ARE FINAL WS-A WS-B.
       01  TYPE CONTROL FOOTING WS-A.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9(5) SUM WS-AMT RESET ON WS-B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE RPT.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
