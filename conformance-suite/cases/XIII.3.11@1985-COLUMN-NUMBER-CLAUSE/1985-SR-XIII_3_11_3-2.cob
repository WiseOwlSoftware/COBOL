      *>--- conformance-case
      *> id: 1985-SR-XIII_3_11_3-2
      *> rules: [1985-SR-XIII.3.11.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.11.3 SR2 (THE COLUMN NUMBER CLAUSE). A refusal:
      *> the rule forbids line 15 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2745 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWCOL.
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
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 10 PIC X(3) VALUE "AAA".
           05  COLUMN 1 PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
