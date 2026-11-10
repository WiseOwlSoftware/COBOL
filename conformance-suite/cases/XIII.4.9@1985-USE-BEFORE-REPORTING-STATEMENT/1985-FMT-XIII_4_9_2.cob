      *>--- conformance-case
      *> id: 1985-FMT-XIII_4_9_2
      *> rules: [1985-FMT-XIII.4.9.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.2 (THE USE BEFORE REPORTING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UBR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES HEADING 1 FIRST DETAIL 3.
       01  RPT-HEAD TYPE IS REPORT HEADING LINE NUMBER IS 1.
           05  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  RPT-DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       BEFORE-HEAD SECTION.
           USE BEFORE REPORTING RPT-HEAD.
       BEFORE-HEAD-PARA.
           DISPLAY "BEFORE HEAD".
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE RPT-DET.
           DISPLAY "AFTER GENERATE".
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
