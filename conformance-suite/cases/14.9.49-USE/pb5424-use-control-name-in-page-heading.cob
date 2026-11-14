      *>--- conformance-case
      *> id: pb5424-use-control-name-in-page-heading
      *> rules: [SR-14.9.49.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5424 - 1985:XIII.3.20.3 SR5, last sentence: "In page heading and page footing report groups, SOURCE clauses
      *> and USE statements must not reference control data-names." (cite.py: OK). The USE BEFORE REPORTING procedure of the
      *> page heading DISPLAYs WS-KEY, the report's control. COBOL-2002 and later carry no such rule.
      *> conformance:pb5424-use-control-name-in-page-heading-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5424PH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5424PH.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC XXX VALUE "AAA".
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 3.
       01  PH-G TYPE IS PAGE HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "HEAD".
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       DECLARATIVES.
       PH-USE SECTION.
           USE BEFORE REPORTING PH-G.
       PH-USE-P.
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
