      *>--- conformance-case
      *> id: pb5789_page_heading_nonprintable_85
      *> rules: [1985-GR-XIII.3.10.7-3, 1985-GR-XIII.3.10.7-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5789. A page heading report group with no LINE
      *>   clause is nonprintable.
      *> DERIVATION. RH LINE 3 NEXT GROUP 4 leaves LINE-COUNTER at 4
      *>   (§1985:XIII.3.10.6 GR5a): "BEFORE PH 04". §1985:XIII.3.10.7
      *>   GR3c: "The page heading report group is not presented";
      *>   GR4b: "LINE-COUNTER is unaffected by the processing of a
      *>   nonprintable report group": "BEFORE DE 04".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5789NP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5789NP.RPT".
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
       01 PHG TYPE PH.
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
