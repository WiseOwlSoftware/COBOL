      *>--- conformance-case
      *> id: 1985-GR-XIII_4_9_4-2
      *> rules: [1985-GR-XIII.4.9.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.4 GR2 (THE USE BEFORE REPORTING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWUSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-CNT PIC 9 VALUE 0.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES FIRST DETAIL 3.
       01  RPT-HEAD TYPE IS REPORT HEADING.
           05  LINE NUMBER IS 1.
               10  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       01  DETL TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1.
               10  COLUMN NUMBER IS 1 PIC 9 SOURCE WS-CNT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RH-SECT SECTION.
           USE BEFORE REPORTING RPT-HEAD.
       RH-PARA.
           PERFORM RH-SHOW.
           GO TO RH-EXIT.
       RH-SHOW.
           DISPLAY "BEFORE HEADING".
       RH-EXIT.
           EXIT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           DISPLAY "INITIATED".
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
