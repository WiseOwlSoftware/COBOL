      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_8-2
      *> rules: [1985-GR-XIII.3.10.8-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.8 GR2 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWLOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RWLOW".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-PAGE PIC 9.
       01  WS-LINE PIC 99.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL
           PAGE LIMIT IS 10 LINES
           HEADING 1
           FIRST DETAIL 2
           LAST DETAIL 3
           FOOTING 5.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "ITEM".
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "TOTAL".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           GENERATE DET.
           PERFORM SHOW-POSITION.
           GENERATE DET.
           PERFORM SHOW-POSITION.
           GENERATE DET.
           PERFORM SHOW-POSITION.
           TERMINATE RPT.
           PERFORM SHOW-POSITION.
           CLOSE RPT-FILE.
           STOP RUN.
       SHOW-POSITION.
           MOVE PAGE-COUNTER TO WS-PAGE.
           MOVE LINE-COUNTER TO WS-LINE.
           DISPLAY "PAGE " WS-PAGE " LINE " WS-LINE.
