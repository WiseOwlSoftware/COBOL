      *>--- conformance-case
      *> id: pb5079-source-table-redefines-control-legal
      *> rules: [SR-13.18.53.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 review R1-1 (second round) - the program 1985:XIII.3.20.3 SR5 c refuses
      *> (conformance:pb5079-source-table-redefines-control): a SOURCE naming a table that redefines the control data item.
      *> COBOL-2002 and later carry no such rule, so it compiles and runs there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079TL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079TL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A.
           05  WS-F1 PIC XXX VALUE "AAA".
           05  WS-KEY PIC XXX VALUE "BBB".
       01  WS-B REDEFINES WS-A.
           05  WS-T PIC XXX OCCURS 2.
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC XXX SOURCE WS-T(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
