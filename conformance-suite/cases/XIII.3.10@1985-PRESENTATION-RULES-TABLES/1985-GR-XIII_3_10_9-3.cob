      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_9-3
      *> rules: [1985-GR-XIII.3.10.9-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.9 GR3 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PFLINE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PFOUT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-LINE PIC 99.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 24 LINES
           HEADING 1
           FIRST DETAIL 2
           LAST DETAIL 18
           FOOTING 20.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05 COLUMN 1 PIC X(6) VALUE "DETAIL".
       01  PFG TYPE IS PAGE FOOTING LINE NUMBER IS 22.
           05 COLUMN 1 PIC X(6) VALUE "FOOTER".
       PROCEDURE DIVISION.
       DECLARATIVES.
       PF-SECT SECTION.
           USE BEFORE REPORTING PFG.
       PF-PARA.
           MOVE LINE-COUNTER TO WS-LINE.
           DISPLAY "BEFORE PAGE FOOTING " WS-LINE.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           MOVE LINE-COUNTER TO WS-LINE.
           DISPLAY "AFTER PAGE FOOTING " WS-LINE.
           CLOSE RPT-FILE.
           STOP RUN.
