      *>--- conformance-case
      *> id: 1985-SR-XIII_3_5_5-6
      *> rules: [1985-SR-XIII.3.5.5-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.5.5 SR6 (THE REPORT DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PCTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT1.
       WORKING-STORAGE SECTION.
       01  WS-PC PIC 99.
       REPORT SECTION.
       RD  RPT1
           PAGE LIMIT IS 60 LINES.
       01  DL TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT1.
           MOVE 5 TO PAGE-COUNTER.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY WS-PC.
           TERMINATE RPT1.
           CLOSE RPT-FILE.
           STOP RUN.
