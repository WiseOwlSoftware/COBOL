      *>--- conformance-case
      *> id: pb5244_report_footing_above_page_footing_next_group
      *> rules: [1985-GR-XIII.3.10.10-1, 1985-GR-XIII.3.10.9-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5244. §1985:XIII.3.10.10 GR1 a. bounds a report
      *> footing that follows a page footing by "one greater than the
      *> final LINE-COUNTER setting established by the page footing",
      *> which its NEXT GROUP PLUS 2 makes 17 + 2 = 19 (§1985:XIII.3.10.9
      *> GR5 b.). The report footing's absolute LINE 18 lies above the
      *> upper limit 20 and is refused at 85 (COBOL2199, line 30). From
      *> COBOL-2002 the limit is the line after the page footing's last
      *> line (§2002:13.16.55.3 GR7 g)), 18, and the program is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5244B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5244BR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES HEADING 3 FIRST DETAIL 4
           LAST DETAIL 15 FOOTING 16.
       01  TYPE PAGE FOOTING LINE 17 NEXT GROUP PLUS 2.
           05  COLUMN 1 PIC X(4) VALUE "PF17".
       01  RFG TYPE REPORT FOOTING LINE 18.
           05  COLUMN 1 PIC X(2) VALUE "RF".
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "DL".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
