      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_6-2
      *> rules: [1985-GR-XIII.3.10.6-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.6 GR2 (PRESENTATION RULES TABLES). A refusal:
      *> the rule forbids line 21 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL2199 there; a compiler that accepts
      *> it fails. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RHLIM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
           SELECT CVR-FILE ASSIGN TO CVROUT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  CVR-FILE
           REPORT IS CVR.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 18.
       01  TYPE IS REPORT HEADING.
           05  LINE NUMBER IS 5.
               10  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "ITEM".
       RD  CVR
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 18.
       01  TYPE IS REPORT HEADING NEXT GROUP NEXT PAGE.
           05  LINE NUMBER IS 20.
               10  COLUMN 1 PIC X(5) VALUE "COVER".
       01  CL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "ITEM".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE CVR-FILE.
           INITIATE RPT CVR.
           GENERATE DL.
           GENERATE CL.
           TERMINATE RPT CVR.
           CLOSE RPT-FILE CVR-FILE.
           DISPLAY "REPORT DONE".
           STOP RUN.
