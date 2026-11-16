      *>--- conformance-case
      *> id: pb5801-use-refmod-control-in-page-heading
      *> rules: [SR-14.9.49.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801 review R1-1 - §1985:XIII.3.20.3 SR5, last sentence: "In
      *>   page heading and page footing report groups, SOURCE clauses and USE
      *>   statements must not reference control data-names." The page
      *>   heading's USE BEFORE REPORTING procedure DISPLAYs WS-KEY (1:1): the
      *>   statement still references the control data-name WS-KEY, through a
      *>   reference modifier. Refused at 85 (COBOL6825), as SOURCE WS-KEY (1:1)
      *>   is; a compiler that asks only of a whole item accepts it.
      *>   pb5424-use-control-name-in-page-heading is the undecorated twin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801PH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO P5801PH.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           02  WS-KEY PIC XXX VALUE "AAA".
       REPORT SECTION.
       RD  RPT CONTROL IS WS-KEY PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 3.
       01  PH-G TYPE IS PAGE HEADING LINE NUMBER IS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "HEAD".
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       DECLARATIVES.
       PH-USE SECTION.
           USE BEFORE REPORTING PH-G.
       PH-USE-P.
           DISPLAY "USE " WS-KEY (1:1).
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
