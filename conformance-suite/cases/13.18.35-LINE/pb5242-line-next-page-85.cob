      *>--- conformance-case
      *> id: pb5242-line-next-page-85
      *> rules: [FMT-13.18.35.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - a 2002 report writer form, refused at 85
      *> with COBOL0900 and accepted from COBOL-2002.
      *> §1985:XIII.3.15.2 prints `LINE NUMBER IS {integer-1 [ON NEXT
      *> PAGE] | PLUS integer-2}`; NEXT PAGE with no integer is the
      *> third alternative ISO/IEC 1989:2002 13.16.33.1 Format 1 adds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242l.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DETAIL LINE NEXT PAGE.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
