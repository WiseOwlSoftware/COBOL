      *>--- conformance-case
      *> id: 1985-SR-X_5_6_3-1
      *> rules: [1985-SR-X.5.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.6.3 SR1 (THE USE BEFORE REPORTING STATEMENT). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEBRPT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES
           HEADING 1
           FIRST DETAIL 3.
       01  RH-GRP TYPE IS REPORT HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "TITLE".
       01  DET-GRP TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RH-USE SECTION.
           USE BEFORE REPORTING RH-GRP.
       RH-PARA.
           DISPLAY "BEFORE RH".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET-GRP.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
