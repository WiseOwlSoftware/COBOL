      *>--- conformance-case
      *> id: pb5244_report_footing_after_page_footing_next_group
      *> rules: [1985-GR-XIII.3.10.10-1, 1985-GR-XIII.3.10.9-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5244. §1985:XIII.3.10.10 GR1 a.: after a page
      *> footing on the same page, "the first line number on which the
      *> report footing report group can be presented is one greater
      *> than the final LINE-COUNTER setting established by the page
      *> footing report group", and §1985:XIII.3.10.9 GR5 a. makes that
      *> setting the page footing's NEXT GROUP integer, 19 here. So the
      *> report footing's absolute LINE 20 is legal (the page limit is
      *> 20) and the program translates and runs; LINE 18, between the
      *> page footing's last line and its NEXT GROUP, is the refusal
      *> pb5244_report_footing_above_page_footing_next_group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5244A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5244AR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES HEADING 3 FIRST DETAIL 4
           LAST DETAIL 15 FOOTING 16.
       01  TYPE PAGE FOOTING LINE 17 NEXT GROUP 19.
           05  COLUMN 1 PIC X(4) VALUE "PF17".
       01  RFG TYPE REPORT FOOTING LINE 20.
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
