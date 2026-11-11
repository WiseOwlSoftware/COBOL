      *>--- conformance-case
      *> id: pb5079-reset-own-control
      *> rules: [SR-13.18.54.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 review R1-2 - RESET ON the control of the footing that holds the SUM: legal at COBOL-85 (1985:XIII.3.19.3 SR4 refuses
      *> only a LOWER level control), refused at COBOL-2002 and later (2023:13.18.54.3 SR8: the footing's level shall be lower than data-name-3's).
      *> conformance:pb5079-reset-own-control-legal is the legal side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079OR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079OR.TXT".
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
           05  COLUMN NUMBER IS 1 PIC 9(5) SUM WS-AMT RESET ON WS-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
