      *>--- conformance-case
      *> id: 1985-SR-XIII_3_15_3-4
      *> rules: [1985-SR-XIII.3.15.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.3 SR4 (THE LINE NUMBER CLAUSE). A refusal: the
      *> rule forbids line 17 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2199 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LNTEST.
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
           PAGE LIMIT IS 20 LINES.
       01  DTL TYPE IS DETAIL.
           05  LINE NUMBER IS 5.
               10  COLUMN 1 PIC X(3) VALUE "AAA".
           05  LINE NUMBER IS 3.
               10  COLUMN 1 PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
