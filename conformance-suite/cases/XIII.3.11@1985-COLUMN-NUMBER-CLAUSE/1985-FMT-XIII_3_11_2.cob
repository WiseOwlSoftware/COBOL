      *>--- conformance-case
      *> id: 1985-FMT-XIII_3_11_2
      *> rules: [1985-FMT-XIII.3.11.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.11.2 (THE COLUMN NUMBER CLAUSE). A refusal: the
      *> rule forbids line 17 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL1547 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COLTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       77  WS-COL PIC 99 VALUE 5.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  DTL TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN NUMBER IS WS-COL PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
