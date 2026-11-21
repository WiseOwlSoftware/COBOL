      *>--- conformance-case
      *> id: pb5981-report-heading-past-first-detail-85
      *> rules: [1985-SR-XIII.3.8.3-8, 1985-GR-XIII.3.10.6-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85 (kb/Work PB5981), the other side of
      *> pb5981_report_heading_lower_limit_first_detail_85. A report
      *> heading not on a page by itself is confined to the lines from
      *> HEADING to FIRST DETAIL minus 1: §1985:XIII.3.8.3 SR8a, "to
      *> the line number specified by integer-3 minus 1", and
      *> §1985:XIII.3.10.6 GR2a (each cite.py --check OK). With
      *> FIRST DETAIL 8 that is line 7, and RH1's LINE 8 lies below
      *> it, so WiseOwl refuses the program with COBOL2199, naming the
      *> lower limit 7 and the 1985 rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5981B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5981b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 HEADING 1 FIRST DETAIL 8.
       01  RH1 TYPE REPORT HEADING.
           05  LINE 8.
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
