      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_8-3
      *> rules: [1985-GR-XIII.3.10.8-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.8 GR3 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FITTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PRINTER".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-PC PIC 9.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 3 LINES.
       01  DET TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1.
               10  COLUMN NUMBER IS 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           GENERATE DET.
           GENERATE DET.
           MOVE PAGE-COUNTER TO W-PC.
           DISPLAY W-PC.
           GENERATE DET.
           MOVE PAGE-COUNTER TO W-PC.
           DISPLAY W-PC.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
