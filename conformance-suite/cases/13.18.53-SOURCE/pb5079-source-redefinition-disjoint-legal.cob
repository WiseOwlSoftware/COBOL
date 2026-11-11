      *>--- conformance-case
      *> id: pb5079-source-redefinition-disjoint-legal
      *> rules: [SR-13.18.53.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 review R1-1 - 1985:XIII.3.20.3 SR5 c forbids "A redefinition or renaming of any part of a control data item".
      *> WS-B redefines WS-A, and its item WS-TAIL (bytes 4-6) shares no byte with the control WS-KEY (bytes 1-3), so the SOURCE
      *> is legal at every edition: the rule is storage overlap, not the shape of the REDEFINES chain.
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
       01  WS-A.
           05  WS-KEY PIC XXX VALUE "AAA".
           05  WS-OTHER PIC XXX VALUE "BBB".
       01  WS-B REDEFINES WS-A.
           05  FILLER PIC XXX.
           05  WS-TAIL PIC XXX.
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC XXX SOURCE WS-TAIL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
