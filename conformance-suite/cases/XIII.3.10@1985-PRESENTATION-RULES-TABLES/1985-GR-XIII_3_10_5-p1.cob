      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_5-p1
      *> rules: [1985-GR-XIII.3.10.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.5 p1 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-18.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 XIII.3.10.5: an absolute NEXT GROUP value that cannot
      * be accommodated on the current page is saved; after page
      * advance the next body group is positioned by it (line 7 of
      * page 2, not FIRST DETAIL line 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       01  WS-PC PIC 99.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15
           FOOTING 18.
       01  DL TYPE IS DETAIL
           LINE NUMBER IS PLUS 1
           NEXT GROUP IS 6.
           05  COLUMN 1 PIC X(5) VALUE "DL".
       01  DX TYPE IS DETAIL
           LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "DX".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DL.
           PERFORM SHOW.
           GENERATE DL.
           PERFORM SHOW.
           GENERATE DX.
           PERFORM SHOW.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
       SHOW.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PAGE " WS-PC " LC " WS-LC.
