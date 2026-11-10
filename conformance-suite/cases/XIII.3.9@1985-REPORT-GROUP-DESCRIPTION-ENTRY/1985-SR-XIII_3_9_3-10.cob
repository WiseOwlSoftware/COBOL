      *>--- conformance-case
      *> id: 1985-SR-XIII_3_9_3-10
      *> rules: [1985-SR-XIII.3.9.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.9.3 SR10 (THE REPORT GROUP DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 14 of the program below, and
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
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 10 LINES.
       01  DTL TYPE IS DETAIL LINE NUMBER IS 1.
           05  PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
