      *>--- conformance-case
      *> id: pb5079-source-renames-thru-covers-control-legal
      *> rules: [SR-13.18.53.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 review R1-1 - 1985:XIII.3.20.3 SR5 c: a renaming of any part of a control data item. WS-R renames
      *> WS-F1 THRU WS-F3, a span that holds the control WS-KEY between its ends. COBOL-2002 and later carry no such rule.
      *> conformance:pb5079-source-renames-thru-covers-control is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079RL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079RL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A.
           05  WS-F1 PIC XXX VALUE "AAA".
           05  WS-KEY PIC XXX VALUE "BBB".
           05  WS-F3 PIC XXX VALUE "CCC".
       66  WS-R RENAMES WS-F1 THRU WS-F3.
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(9) SOURCE WS-R.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
