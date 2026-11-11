      *>--- conformance-case
      *> id: pb5079-reset-lower-control
      *> rules: [SR-13.18.54.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.19.3 SR4: "Data-name-2 must not be a lower level control than the associated control for the report
      *> group in which the RESET phrase appears." (cite.py: OK). CONTROLS ARE FINAL WS-A WS-B makes WS-B the lower level, so RESET ON WS-B
      *> in the control footing for WS-A is the forbidden one. 2023:13.18.54.3 SR8: "If the current report group is a control footing, its
      *> level of control shall be a lower level than that of data-name-3", so every edition refuses it. RESET ON FINAL is legal.
      *> conformance:pb5079-reset-higher-control is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079CN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079CN.TXT".
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
           05  COLUMN NUMBER IS 1 PIC 9(5) SUM WS-AMT RESET ON WS-B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
