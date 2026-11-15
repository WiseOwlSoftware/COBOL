      *>--- conformance-case
      *> id: pb5242-report-values-85
      *> rules: [FMT-13.18.63.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - a 2002 report writer form, refused at 85
      *> with COBOL0900 and accepted from COBOL-2002.
      *> §1985:XIII.3.22.2 prints the report VALUE clause as `VALUE IS
      *> literal-1`; ISO/IEC 1989:2002 13.16.61.1 Format 4 adds the
      *> VALUES spelling (its SR7: "The words VALUE and VALUES are
      *> equivalent").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242V.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242v.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUES ARE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
