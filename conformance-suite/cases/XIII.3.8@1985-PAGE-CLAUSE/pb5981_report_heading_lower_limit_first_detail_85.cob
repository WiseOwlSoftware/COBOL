      *>--- conformance-case
      *> id: pb5981_report_heading_lower_limit_first_detail_85
      *> rules: [1985-SR-XIII.3.8.3-8, 1985-GR-XIII.3.10.6-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> At 85 a report heading that is not on a page by itself may
      *> reach the line before FIRST DETAIL, whatever the page
      *> heading's lines (kb/Work PB5981). §1985:XIII.3.8.3 SR8a: it
      *> "must be defined such that it can be presented in the
      *> vertical subdivision of the page that extends from the line
      *> number specified by integer-2 to the line number specified by
      *> integer-3 minus 1"; §1985:XIII.3.10.6 GR2a: "The last line
      *> number on which the report heading report group can be
      *> presented is the line number that is obtained by subtracting
      *> 1 from the value of integer-3 of the FIRST DETAIL phrase"
      *> (each cite.py --check OK). Here HEADING 1 and FIRST DETAIL 8,
      *> so the region is lines 1 to 7, and RH1's LINE 4 lies in it.
      *> The page heading's LINE 3 is in its own region (SR8b, 1 to
      *> 7). The 2002 and later limit, the line before the page
      *> heading's first line (§2023:13.18.57.4 GR8b), would be 2; a
      *> compiler that borrows it refuses this program. The test
      *> proves translation only: no report is generated, so the
      *> output is empty.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5981A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5981a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 HEADING 1 FIRST DETAIL 8.
       01  RH1 TYPE REPORT HEADING.
           05  LINE 4.
               10  COLUMN 1 PIC X VALUE "R".
       01  PH1 TYPE PAGE HEADING.
           05  LINE 3.
               10  COLUMN 1 PIC X VALUE "P".
       01  D1 TYPE DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
           STOP RUN.
