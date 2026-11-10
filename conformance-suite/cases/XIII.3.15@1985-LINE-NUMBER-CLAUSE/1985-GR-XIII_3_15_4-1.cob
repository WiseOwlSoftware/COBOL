      *>--- conformance-case
      *> id: 1985-GR-XIII_3_15_4-1
      *> rules: [1985-GR-XIII.3.15.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.4 GR1 (THE LINE NUMBER CLAUSE). A refusal: the
      *> rule forbids line 14 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2247 there; a compiler that accepts it
      *> fails. Placed from written and run by the triage of the drafts
      *> that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWLN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DET TYPE DETAIL.
           05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
