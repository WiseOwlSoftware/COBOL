      *>--- conformance-case
      *> id: pb5079-reset-higher-control
      *> rules: [SR-13.18.54.3-8]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - the legal side of the RESET level rule: FINAL is the highest control, so RESET ON FINAL in the control
      *> footing for WS-A resets on a higher level than the footing's own (1985:XIII.3.19.3 SR4; 2023:13.18.54.3 SR8).
      *> conformance:pb5079-reset-lower-control is the refused side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079CL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079CL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A   PIC X VALUE "A".
       01  WS-B   PIC X VALUE "B".
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL WS-A WS-B.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING WS-A LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC 9(5) SUM WS-AMT RESET ON FINAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
