      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_8_2
      *> rules: [1985-FMT-XIII.3.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.8.2 (THE PAGE CLAUSE). A refusal: the rule forbids
      *> line 12 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL5506 there; a compiler that accepts it fails. Placed from
      *> written and run by the triage of the drafts that did not match
      *> (a suspect draft corrected) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 60 LINES HEADING FIRST DETAIL 5.
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
