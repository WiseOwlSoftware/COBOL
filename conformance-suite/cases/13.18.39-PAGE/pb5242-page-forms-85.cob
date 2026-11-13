      *>--- conformance-case
      *> id: pb5242-page-forms-85
      *> rules: [FMT-13.18.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - a 2002 report writer form, refused at 85
      *> with COBOL0900 and accepted from COBOL-2002.
      *> §1985:XIII.3.8.2 prints `PAGE [LIMIT IS | LIMITS ARE]
      *> integer-1 [LINE | LINES]` and the phrases HEADING, FIRST
      *> DETAIL, LAST DETAIL and FOOTING with no IS: no page width.
      *> ISO/IEC 1989:2002 13.16.37.1 adds `integer-2 {COLS |
      *> COLUMNS}`; 80 COLUMNS below is that width.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242g.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES
           80 COLUMNS.
       01  DA TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
