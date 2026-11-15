      *>--- conformance-case
      *> id: pb5242-code-is-85
      *> rules: [FMT-13.18.12.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - a 2002 report writer form, refused at 85
      *> with COBOL0900 and accepted from COBOL-2002.
      *> §1985:XIII.3.6.2 prints the clause as `CODE literal-1`, with
      *> no IS; ISO/IEC 1989:2002 13.16.12.1 prints `CODE IS {literal-1
      *> | identifier-1}`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242c.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R1 CODE IS "AB"
           PAGE LIMIT IS 10 LINES.
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
