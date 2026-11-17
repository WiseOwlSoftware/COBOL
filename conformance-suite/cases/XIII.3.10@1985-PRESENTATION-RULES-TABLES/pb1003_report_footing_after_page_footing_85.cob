      *>--- conformance-case
      *> id: pb1003_report_footing_after_page_footing_85
      *> rules: [1985-GR-XIII.3.10.10-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1003. The legal side of
      *>   pb1003-report-footing-past-page-limit-85: the report footing
      *>   still reaches the page limit 12, and no further.
      *> DERIVATION. GENERATE D1 presents the detail; TERMINATE presents
      *>   the page footing on line 10 (its LINE 10), whose NEXT GROUP
      *>   PLUS 1 makes its final LINE-COUNTER setting 10 + 1 = 11
      *>   (§1985:XIII.3.10.9 GR5b): "BEFORE RF 11". The report footing's
      *>   first line is "the sum of the final LINE-COUNTER setting
      *>   established by the page footing report group and the integer
      *>   of the first LINE NUMBER clause" (§1985:XIII.3.10.10 GR3b),
      *>   11 + 1 = 12, which GR2 allows (the PAGE LIMIT integer), and
      *>   "The final LINE-COUNTER setting is the line number on which
      *>   the final print line of the report footing report group is
      *>   presented" (GR4a): "AFTER TERMINATE 12".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1003RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO P1003RP.
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-LC PIC 99.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 4
           LAST DETAIL 8 FOOTING 9.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DE".
       01 PFG TYPE PF LINE 10 NEXT GROUP PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "PF".
       01 RFG TYPE RF LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RF-USE SECTION.
           USE BEFORE REPORTING RFG.
       RF-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "BEFORE RF " WS-LC.
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
