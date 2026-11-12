      *>--- conformance-case
      *> id: 1985-SR-XIII_3_8_3-2
      *> rules: [1985-SR-XIII.3.8.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.8.3 SR2 (THE PAGE CLAUSE). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2744 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PAGETEST.
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
           PAGE LIMIT IS 1000 LINES.
       01  DETAIL-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETAIL-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
