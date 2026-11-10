      *>--- conformance-case
      *> id: 1985-SR-XIII_3_5_3-3
      *> rules: [1985-SR-XIII.3.5.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.5.3 SR3 (THE REPORT DESCRIPTION ENTRY). A refusal:
      *> the rule forbids line 23 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL5506 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-1.
       WORKING-STORAGE SECTION.
       01  WS-PC PIC 9(4).
       REPORT SECTION.
       RD  RPT-1
           PAGE LIMIT IS 60 LINES.
       01  DETAIL-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT-1.
           GENERATE DETAIL-LINE.
           MOVE PAGE-COUNTER OF RPT-1 OF RPT-FILE TO WS-PC.
           TERMINATE RPT-1.
           CLOSE RPT-FILE.
           STOP RUN.
