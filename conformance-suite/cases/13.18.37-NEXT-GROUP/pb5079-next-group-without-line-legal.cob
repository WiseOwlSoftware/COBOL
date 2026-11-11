      *>--- conformance-case
      *> id: pb5079-next-group-without-line-legal
      *> rules: [SR-13.18.37.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.16.3 SR1: "A report group entry must not contain a NEXT GROUP clause unless the description of that
      *> report group contains at least one LINE NUMBER clause." (cite.py: OK). The control footing has NEXT GROUP PLUS 1 and no LINE NUMBER
      *> clause anywhere in its description. COBOL-2002 and later have no such rule.
      *> conformance:pb5079-next-group-without-line is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079BL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079BL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 999 VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  TYPE IS CONTROL FOOTING FINAL NEXT GROUP PLUS 1.
           05  CF-T PIC 999 SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
