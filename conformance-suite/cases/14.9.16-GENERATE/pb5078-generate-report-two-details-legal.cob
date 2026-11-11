      *>--- conformance-case
      *> id: pb5078-generate-report-two-details-legal
      *> rules: [SR-14.9.16.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5078 - 1985:XIII.3.20.3 SR6: "When a GENERATE report-name statement is specified in the Procedure Division, the
      *> corresponding report description entry must include no more than one detail report group." (cite.py: OK). The report holds two
      *> details. COBOL-2002 and later keep only the CONTROL-clause condition (2023:14.9.16.3 SR2), so GENERATE RPT is legal there.
      *> conformance:pb5078-generate-report-two-details is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5078FL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5078FL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       01  DET2 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "MORE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE RPT.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
