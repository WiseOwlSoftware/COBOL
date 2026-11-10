      *>--- conformance-case
      *> id: 1985-GR-XIII_3_8_4-1
      *> rules: [1985-GR-XIII.3.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.8.4 GR1 (THE PAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PAGECL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT
           REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       01  WS-PC PIC 99.
       REPORT SECTION.
       RD  R
           CONTROL IS FINAL
           PAGE LIMIT 12 LINES
           HEADING 2
           FIRST DETAIL 4
           LAST DETAIL 5
           FOOTING 7.
       01  TYPE IS REPORT HEADING LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "BODY".
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 2.
           05  COLUMN 1 PIC X(5) VALUE "TOTAL".
       01  TYPE IS PAGE FOOTING LINE NUMBER IS 12.
           05  COLUMN 1 PIC X(4) VALUE "FOOT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DL-USE SECTION.
           USE BEFORE REPORTING DL.
       DL-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "BEFORE " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT RPT.
           INITIATE R.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           TERMINATE R.
           PERFORM SHOW-POSITION.
           CLOSE RPT.
           STOP RUN.
       SHOW-POSITION.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PAGE " WS-PC " LINE " WS-LC.
