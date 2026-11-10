      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_9_2
      *> rules: [1985-FMT-XIII.3.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.2 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 19 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2247 there; a compiler that
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
       FD  RPT-FILE
           LABEL RECORDS ARE OMITTED
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(5) VALUE "HELLO".
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  DTL TYPE IS DETAIL
           LINE NUMBER IS PLUS 1.
           05  COLUMN 1 SOURCE WS-NAME.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
