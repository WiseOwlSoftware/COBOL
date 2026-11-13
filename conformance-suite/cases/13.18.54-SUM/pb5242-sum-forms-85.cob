      *>--- conformance-case
      *> id: pb5242-sum-forms-85
      *> rules: [FMT-13.18.54.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - a 2002 report writer form, refused at 85
      *> with COBOL0900 and accepted from COBOL-2002.
      *> §1985:XIII.3.19.2 prints `{SUM {identifier-1}... [UPON {data-
      *> name-1}...]}... [RESET ON {data-name-2 | FINAL}]`: no OF, no
      *> ROUNDED and no expression addend; ISO/IEC 1989:2002 13.16.52.1
      *> adds them. WN + 1 below is an arithmetic expression.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242s.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL
           PAGE LIMIT IS 10 LINES.
       01  DA TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       01  TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           02  COLUMN 1 PIC 9(5) SUM WN + 1.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
