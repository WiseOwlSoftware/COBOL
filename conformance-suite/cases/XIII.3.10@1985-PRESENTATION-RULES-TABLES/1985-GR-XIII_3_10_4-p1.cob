      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_4-p1
      *> rules: [1985-GR-XIII.3.10.4-p1, 1985-GR-XIII.3.10.4-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.4 p1; §1985:XIII.3.10.4 p2 (PRESENTATION RULES
      *> TABLES). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-18.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985 XIII.3.10.4: 'A' alone where 'A R' is permitted, and
      * 'NP' alone where 'NP R' is permitted, take the same rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01.
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
           FIRST DETAIL 4
           LAST DETAIL 15
           FOOTING 18.
       01  TYPE IS REPORT HEADING
           LINE NUMBER IS 2.
           05  COLUMN 1 PIC X(5) VALUE "RH-A".
       01  DA TYPE IS DETAIL
           LINE NUMBER IS 5.
           05  COLUMN 1 PIC X(5) VALUE "DA-A".
       01  DAR TYPE IS DETAIL.
           05  LINE NUMBER IS 7.
               10  COLUMN 1 PIC X(5) VALUE "DAR-1".
           05  LINE PLUS 1.
               10  COLUMN 1 PIC X(5) VALUE "DAR-2".
       01  DNP TYPE IS DETAIL
           LINE NUMBER IS 6 ON NEXT PAGE.
           05  COLUMN 1 PIC X(5) VALUE "DNP".
       01  DNPR TYPE IS DETAIL.
           05  LINE NUMBER IS 9 ON NEXT PAGE.
               10  COLUMN 1 PIC X(6) VALUE "DNPR-1".
           05  LINE PLUS 2.
               10  COLUMN 1 PIC X(6) VALUE "DNPR-2".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           INITIATE RPT.
           GENERATE DA.
           PERFORM SHOW.
           GENERATE DAR.
           PERFORM SHOW.
           GENERATE DNP.
           PERFORM SHOW.
           GENERATE DNPR.
           PERFORM SHOW.
           TERMINATE RPT.
           CLOSE PRT-FILE.
           STOP RUN.
       SHOW.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PAGE " WS-PC " LC " WS-LC.
