      *>--- conformance-case
      *> id: pb5079-usage-on-unprinted-format-3-legal
      *> rules: [SR-13.15.3-11]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.9.3 SR8 a: "If the USAGE clause appears in a format 3 entry, that entry must define a printable
      *> item." (cite.py: OK). TOT has no COLUMN NUMBER clause, so it defines a sum counter and no printable item (1985:XIII.3.11.4 GR1).
      *> conformance:pb5079-usage-on-unprinted-format-3 is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079HL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079HL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  TOT PIC 9(5) SUM WS-AMT USAGE DISPLAY.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
