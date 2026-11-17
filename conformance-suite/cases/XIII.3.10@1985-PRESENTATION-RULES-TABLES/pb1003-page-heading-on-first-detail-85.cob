      *>--- conformance-case
      *> id: pb1003-page-heading-on-first-detail-85
      *> rules: [1985-GR-XIII.3.10.7-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1003. The report heading RHG ends on line 3 and has
      *>   no NEXT GROUP clause, so its final LINE-COUNTER setting is 3
      *>   (§1985:XIII.3.10.6 GR5d). §1985:XIII.3.16.4 GR4 (cite.py: OK):
      *>   "The NEXT GROUP clause of a report heading report group can
      *>   affect the location at which the page heading report group is
      *>   presented": the page heading follows the report heading on the
      *>   first page, its relative lines on lines 4 and 5. FIRST DETAIL
      *>   is 5, and §1985:XIII.3.8.4 GR1c (cite.py: OK): "Report heading
      *>   (without NEXT GROUP NEXT PAGE) and page heading report groups
      *>   may not be presented on or beyond the line number specified by
      *>   integer-3": refused (COBOL2199). The legal side, one page
      *>   heading line, is pb1003_page_heading_after_report_heading_85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1003PH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO P1003PH.
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 5
           LAST DETAIL 9 FOOTING 9.
       01 RHG TYPE RH.
          02 LINE 2.
             03 COLUMN 1 PIC X(3) VALUE "RHA".
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "RHB".
       01 PHG TYPE PH.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "PHA".
          02 LINE PLUS 1.
             03 COLUMN 5 PIC X(3) VALUE "PHB".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 9 PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
