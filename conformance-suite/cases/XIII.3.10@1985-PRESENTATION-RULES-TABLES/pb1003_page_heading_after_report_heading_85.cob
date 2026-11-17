      *>--- conformance-case
      *> id: pb1003_page_heading_after_report_heading_85
      *> rules: [1985-GR-XIII.3.10.7-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1003. The legal side of
      *>   pb1003-page-heading-on-first-detail-85: the page heading after
      *>   the report heading ends on the line before FIRST DETAIL.
      *> DERIVATION. The report heading prints on line 2 and its NEXT
      *>   GROUP PLUS 1 makes its final LINE-COUNTER setting 2 + 1 = 3
      *>   (§1985:XIII.3.10.6 GR5b): "BEFORE PH 03". The page heading
      *>   follows it on the first page (§1985:XIII.3.16.4 GR4: "The NEXT
      *>   GROUP clause of a report heading report group can affect the
      *>   location at which the page heading report group is
      *>   presented"), its LINE PLUS 1 on line 4, before FIRST DETAIL 5
      *>   (§1985:XIII.3.8.4 GR1c): "BEFORE DE 04". LINE-COUNTER 4 is less
      *>   than FIRST DETAIL, so the detail is presented "on the line
      *>   specified by the FIRST DETAIL phrase" (§1985:XIII.3.10.8 GR4b),
      *>   5: "AFTER TERMINATE 05".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1003HP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO P1003HP.
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-LC PIC 99.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 5
           LAST DETAIL 9 FOOTING 9.
       01 RHG TYPE RH LINE 2 NEXT GROUP PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "RHA".
       01 PHG TYPE PH LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "PHA".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 9 PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       PH-USE SECTION.
           USE BEFORE REPORTING PHG.
       PH-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "BEFORE PH " WS-LC.
       DE-USE SECTION.
           USE BEFORE REPORTING D1.
       DE-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "BEFORE DE " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "AFTER TERMINATE " WS-LC.
           CLOSE PF1.
           STOP RUN.
