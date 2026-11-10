      *>--- conformance-case
      *> id: pb5079-page-footing-relative-first-line-legal
      *> rules: [SR-13.18.35.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5079 - 1985:XIII.3.15.3 SR9: "The first LINE NUMBER clause specified within a PAGE FOOTING report group must be an
      *> absolute LINE NUMBER clause." (cite.py: OK). FOOT is a page footing whose only LINE clause is LINE NUMBER IS PLUS 1. COBOL-2002
      *> and later place a relative first line of a page footing from the FOOTING integer (2023:13.18.35.4 GR5 b) 4.).
      *> conformance:pb5079-page-footing-relative-first-line is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5079AL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5079AL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 20 LINES FOOTING 17.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  FOOT TYPE IS PAGE FOOTING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
