      *>--- conformance-case
      *> id: 1985-GR-XIII_3_17_4-1
      *> rules: [1985-GR-XIII.3.17.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.17.4 GR1 (THE SIGN CLAUSE). A refusal: the rule
      *> forbids line 15 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIGNGRP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "signgrp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-V    PIC S99 VALUE -12.
       01  WS-W    PIC S99 VALUE +34.
       REPORT SECTION.
       RD  R1.
       01  DE-1 TYPE DE LINE PLUS 1 SIGN IS LEADING SEPARATE CHARACTER.
           02  COLUMN NUMBER IS 1 PIC S99
               SOURCE IS WS-V.
           02  COLUMN NUMBER IS 5 PIC S99
               SOURCE IS WS-W.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DE-1.
           TERMINATE R1.
           CLOSE RPT.
           DISPLAY "REPORT WRITTEN".
           STOP RUN.
