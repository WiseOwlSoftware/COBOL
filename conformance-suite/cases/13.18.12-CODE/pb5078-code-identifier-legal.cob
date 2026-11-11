      *>--- conformance-case
      *> id: pb5078-code-identifier-legal
      *> rules: [SR-13.18.12.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5078 - 1985:XIII.3.6.2 prints the CODE clause as `CODE literal-1` (cite.py: OK): an identifier is not a form of the
      *> COBOL-85 clause. COBOL-2002 and later add identifier-1 (2023:13.18.12.2).
      *> conformance:pb5078-code-identifier is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5078EL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5078EL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-CODE PIC XX VALUE "AB".
       REPORT SECTION.
       RD  RPT CODE WS-CODE.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
