      *>--- conformance-case
      *> id: 1985-SR-XIII_3_2_3-5
      *> rules: [1985-SR-XIII.3.2.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.2.3 SR5 (THE FILE DESCRIPTION ENTRY). A refusal:
      *> the rule forbids line 17 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2371 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWFD5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
