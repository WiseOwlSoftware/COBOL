      *>--- conformance-case
      *> id: 1985-SR-XIII_4_9_3-3
      *> rules: [1985-SR-XIII.4.9.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.3 SR3 (THE USE BEFORE REPORTING STATEMENT). A
      *> refusal: the rule forbids line 25 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2378 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWUSE.
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
           PAGE LIMIT IS 60 LINES FIRST DETAIL 3.
       01  PHG TYPE IS PAGE HEADING.
           05  LINE NUMBER IS 1.
               10  COLUMN NUMBER IS 1 PIC X(5) VALUE "TITLE".
       01  DL TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1.
               10  COLUMN NUMBER IS 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       PH-USE SECTION.
           USE BEFORE REPORTING PHG.
       PH-PARA.
           GENERATE DL.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
