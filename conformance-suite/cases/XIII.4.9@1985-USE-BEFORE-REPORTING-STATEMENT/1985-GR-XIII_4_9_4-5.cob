      *>--- conformance-case
      *> id: 1985-GR-XIII_4_9_4-5
      *> rules: [1985-GR-XIII.4.9.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.4 GR5 (THE USE BEFORE REPORTING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEBR.
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
           PAGE LIMIT IS 60 LINES FIRST DETAIL 3.
       01  RPT-HD TYPE IS REPORT HEADING.
           02  LINE NUMBER IS 1.
               03  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(3) VALUE "ROW".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RH-USE SECTION.
           USE BEFORE REPORTING RPT-HD.
       RH-PARA.
           DISPLAY "BEFORE RH".
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
