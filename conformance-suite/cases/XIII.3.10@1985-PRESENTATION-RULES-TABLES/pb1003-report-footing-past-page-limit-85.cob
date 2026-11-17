      *>--- conformance-case
      *> id: pb1003-report-footing-past-page-limit-85
      *> rules: [1985-GR-XIII.3.10.10-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1003. The page footing PFG prints on line 10 and has
      *>   no NEXT GROUP clause, so its final LINE-COUNTER setting is 10
      *>   (§1985:XIII.3.10.9 GR5c). §1985:XIII.3.10.10 GR3b (cite.py: OK):
      *>   "the sum of the final LINE-COUNTER setting established by the
      *>   page footing report group and the integer of the first LINE
      *>   NUMBER clause" places the report footing's line at 10 + 3 = 13,
      *>   past "the line number specified by integer-1 of the PAGE
      *>   clause", 12, the last line it may take (GR2). §1985:XIII.3.15.3
      *>   SR1: "Neither integer-1 nor integer-2 may be specified in such
      *>   a way as to cause any line of a report group to be presented
      *>   outside the vertical subdivision of the page": refused
      *>   (COBOL2199). The legal side, LINE PLUS 1 after a NEXT GROUP
      *>   PLUS 1, is pb1003_report_footing_after_page_footing_85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1003RF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO P1003RF.
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 4
           LAST DETAIL 8 FOOTING 9.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DE".
       01 PFG TYPE PF LINE 10.
          02 COLUMN 1 PIC X(2) VALUE "PF".
       01 RFG TYPE RF LINE PLUS 3.
          02 COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
