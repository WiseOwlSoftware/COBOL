      *>--- conformance-case
      *> id: 1985-SR-XIII_3_5_6-2
      *> rules: [1985-SR-XIII.3.5.6-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.5.6 SR2 (THE REPORT DESCRIPTION ENTRY). A refusal:
      *> the rule forbids line 23 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2197 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LCTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       77  WS-N PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES.
       01  DTL TYPE IS DETAIL.
           02  LINE NUMBER IS PLUS 1.
               03  COLUMN NUMBER IS 1 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 5 TO LINE-COUNTER.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
