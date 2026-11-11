      *>--- conformance-case
      *> id: pb5079-source-control-name-in-page-heading-legal
      *> rules: [SR-13.18.53.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.20.3 SR5, last sentence: "In page heading and page footing report groups, SOURCE clauses and USE
      *> statements must not reference control data-names." (cite.py: OK). The page heading SOURCEs WS-KEY, the report's control. COBOL-2002
      *> and later carry no such rule.
      *> conformance:pb5079-source-control-name-in-page-heading is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079DL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079DL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC XXX VALUE "AAA".
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 3.
       01  PHD TYPE IS PAGE HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC XXX SOURCE WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
