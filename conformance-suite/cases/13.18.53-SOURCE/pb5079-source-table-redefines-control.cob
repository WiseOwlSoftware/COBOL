      *>--- conformance-case
      *> id: pb5079-source-table-redefines-control
      *> rules: [SR-13.18.53.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 review R1-1 (second round) - 1985:XIII.3.20.3 SR5 c forbids "A redefinition or renaming of any part
      *> of a control data item". WS-B redefines WS-A, and its table WS-T (OCCURS 2) lays occurrence 2 on bytes 4-6, the
      *> control WS-KEY: the table item is a redefinition of part of the control data item. COBOL-2002 and later carry no
      *> such rule. conformance:pb5079-source-table-redefines-control-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079TR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079TR.TXT".
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
