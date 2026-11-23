      *>--- conformance-case
      *> id: pb5789_page_heading_relative_after_rh_85
      *> rules: [1985-GR-XIII.3.10.7-1, 1985-GR-XIII.3.10.7-3,
      *>    1985-GR-XIII.3.10.7-4, 1985-GR-XIII.3.10.7-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5789. A relative page heading after a report heading
      *>   whose NEXT GROUP leaves LINE-COUNTER at 4.
      *> DERIVATION. RH LINE 3 NEXT GROUP 4: the final LINE-COUNTER
      *>   setting is the NEXT GROUP integer, 4 (§1985:XIII.3.10.6
      *>   GR5a): "BEFORE PH 04". §1985:XIII.3.10.7 GR3b: the page
      *>   heading's first line is that setting plus its LINE integer,
      *>   4 + 1 = 5, which is not above the upper limit 4 + 1 = 5
      *>   (GR1: "one greater than the final LINE-COUNTER setting
      *>   established by the report heading"). GR4a: its final
      *>   LINE-COUNTER is its last line, 5: "BEFORE DE 05".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5789PR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5789PR.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-LC PIC 99.
       REPORT SECTION.
       RD R1 PAGE LIMIT IS 12 LINES HEADING 3 FIRST DETAIL 6
           LAST DETAIL 8 FOOTING 9.
       01 RHG TYPE RH LINE 3 NEXT GROUP 4.
          02 COLUMN 1 PIC X(2) VALUE "RH".
       01 PHG TYPE PH LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "PH".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DE".
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
           CLOSE PF1.
           STOP RUN.
