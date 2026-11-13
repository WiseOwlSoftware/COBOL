      *>--- conformance-case
      *> id: pb5424-use-control-name-in-footing-legal
      *> rules: [SR-14.9.49.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5424 - 1985:XIII.3.20.3 SR5 (cite.py: OK) bars, in a control footing, a reference to a group holding a
      *> control data item, an item subordinate to one, or a redefinition or renaming of part of one; only page heading and
      *> page footing groups also bar the control data-name itself. So the USE BEFORE REPORTING procedure of a control
      *> footing may DISPLAY the control WS-KEY at every edition. TERMINATE presents the footing once, WS-KEY unchanged:
      *> "USE AAA", then "DONE".
      *> conformance:pb5424-use-group-of-control-in-footing is the refused group reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5424CL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5424CL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-GRP.
           05  WS-KEY PIC XXX VALUE "AAA".
           05  WS-OTH PIC X   VALUE "O".
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  CF-G TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-USE SECTION.
           USE BEFORE REPORTING CF-G.
       CF-USE-P.
           DISPLAY "USE " WS-KEY.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
