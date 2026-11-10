      *>--- conformance-case
      *> id: 1985-GR-XIII_4_4_4-2
      *> rules: [1985-GR-XIII.4.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.4.4 GR2 (THE INITIATE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITRW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO PRTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 60 LINES
           HEADING 1
           FIRST DETAIL 3.
       01  TYPE IS REPORT HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "TITLE".
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "BODY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           INITIATE RPT.
           GENERATE DET-LINE.
           TERMINATE RPT.
           CLOSE RFILE.
           DISPLAY "DONE".
           STOP RUN.
