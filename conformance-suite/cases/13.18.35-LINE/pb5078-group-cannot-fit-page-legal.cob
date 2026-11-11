      *>--- conformance-case
      *> id: pb5078-group-cannot-fit-page-legal
      *> rules: [GR-13.18.35.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5078 - 1985:XIII.3.8.3 SR9: "All report groups must be described such that they can be presented on one page."
      *> (cite.py: OK). The page is two lines (PAGE LIMIT 2, FIRST DETAIL 1, LAST DETAIL 2) and the detail is three LINE PLUS 1 lines,
      *> so no page holds it. COBOL-2002 and later make it a run-time condition (2023:13.18.35.4 GR2: EC-REPORT-PAGE-LIMIT), and
      *> this program does not generate the report.
      *> conformance:pb5078-group-cannot-fit-page is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5078CL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5078CL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 2 LINES.
       01  DET TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1 COLUMN NUMBER IS 1 PIC X VALUE "A".
           05  LINE NUMBER IS PLUS 1 COLUMN NUMBER IS 1 PIC X VALUE "B".
           05  LINE NUMBER IS PLUS 1 COLUMN NUMBER IS 1 PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
