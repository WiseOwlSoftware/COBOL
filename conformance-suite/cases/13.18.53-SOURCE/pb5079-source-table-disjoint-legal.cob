      *>--- conformance-case
      *> id: pb5079-source-table-disjoint-legal
      *> rules: [SR-13.18.53.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 review R1-1 (second round) - 1985:XIII.3.20.3 SR5 c forbids "A redefinition or renaming of any part
      *> of a control data item", decided over EVERY occurrence of a table item. WS-B redefines WS-A; its table entry WS-E
      *> (OCCURS 2, 3 bytes) holds WS-Q on bytes 2-3 and 5-6, while the control WS-KEY is byte 1. No occurrence of WS-Q
      *> shares a byte with the control (the table WS-E as a whole would), so the SOURCE is legal at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079TD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079TD.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A.
           05  WS-KEY PIC X VALUE "A".
           05  WS-OTHER PIC X(5) VALUE "BBBBB".
       01  WS-B REDEFINES WS-A.
           05  WS-E OCCURS 2.
               10  WS-P PIC X.
               10  WS-Q PIC XX.
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC XX SOURCE WS-Q(1).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
