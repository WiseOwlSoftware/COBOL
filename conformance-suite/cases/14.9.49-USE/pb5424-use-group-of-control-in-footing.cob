      *>--- conformance-case
      *> id: pb5424-use-group-of-control-in-footing
      *> rules: [SR-14.9.49.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5424 - 1985:XIII.3.20.3 SR5: "In control footing, page heading, page footing, and report footing report
      *> groups, SOURCE clauses and associated USE statements must not reference any of the following: a. Group data items
      *> containing a control data item." (cite.py: OK). The USE BEFORE REPORTING procedure of the control footing DISPLAYs
      *> WS-GRP, the group holding the control WS-KEY. COBOL-2002 and later carry no such rule.
      *> conformance:pb5424-use-control-name-in-footing-legal is the control footing's legal side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5424CG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5424CG.TXT".
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
           DISPLAY "USE " WS-GRP.
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
