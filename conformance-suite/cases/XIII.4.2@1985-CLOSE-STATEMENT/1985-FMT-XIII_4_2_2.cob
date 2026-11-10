      *>--- conformance-case
      *> id: 1985-FMT-XIII_4_2_2
      *> rules: [1985-FMT-XIII.4.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.2.2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWCLOSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-1.
       REPORT SECTION.
       RD  RPT-1
           PAGE LIMIT IS 60 LINES HEADING 1 FIRST DETAIL 3.
       01  TYPE IS REPORT HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT-1.
           TERMINATE RPT-1.
           CLOSE RPT-FILE WITH LOCK.
           DISPLAY "CLOSED".
           STOP RUN.
