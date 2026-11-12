      *>--- conformance-case
      *> id: 1985-SR-XIII_3_20_3-6
      *> rules: [1985-SR-XIII.3.20.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.3 SR6 (THE TYPE CLAUSE). A refusal: the rule
      *> forbids line 25 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6823 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTYPE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL
           PAGE LIMIT IS 20 LINES.
       01  TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1  PIC X(5) VALUE "DET-A".
       01  TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1  PIC X(5) VALUE "DET-B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE RPT.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
