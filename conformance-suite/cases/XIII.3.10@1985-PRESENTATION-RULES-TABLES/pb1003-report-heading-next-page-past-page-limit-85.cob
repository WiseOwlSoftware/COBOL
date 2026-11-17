      *>--- conformance-case
      *> id: pb1003-report-heading-next-page-past-page-limit-85
      *> rules: [1985-GR-XIII.3.10.6-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1003 (review R1-1). The report heading has NEXT GROUP
      *>   NEXT PAGE, so it is alone on the first page and its lower
      *>   limit is the page limit: §1985:XIII.3.10.6 GR2b (cite.py: OK):
      *>   "The last line number on which the report heading report group
      *>   can be presented is the line number specified by integer-1 of
      *>   the PAGE clause", 20. Its relative lines are placed from
      *>   HEADING 1 (GR3b): 1 + 1 - 1 = 1, then 11, then 21, past 20, so
      *>   §1985:XIII.3.15.3 SR1 refuses it (COBOL2199). The refusal
      *>   names the page limit and GR2b, never FIRST DETAIL and
      *>   §1985:XIII.3.8.4 GR1c, whose words exclude a report heading
      *>   with NEXT GROUP NEXT PAGE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1003RN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO P1003RN.
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 8
           LAST DETAIL 15 FOOTING 16.
       01 RHG TYPE RH NEXT GROUP NEXT PAGE.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "RHA".
          02 LINE PLUS 10.
             03 COLUMN 1 PIC X(3) VALUE "RHB".
          02 LINE PLUS 10.
             03 COLUMN 1 PIC X(3) VALUE "RHC".
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
