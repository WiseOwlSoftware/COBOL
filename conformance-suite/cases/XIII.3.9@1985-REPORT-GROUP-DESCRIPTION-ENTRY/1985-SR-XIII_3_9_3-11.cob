      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-11
      *> rules: [1985-SR-XIII.3.9.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR11 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 14 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2247 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 60 FIRST DETAIL 3.
       01  RHG TYPE IS REPORT HEADING LINE NUMBER IS 1.
           05  PIC X(5) VALUE "HELLO".
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
