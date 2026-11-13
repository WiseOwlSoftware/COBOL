      *>--- conformance-case
      *> id: pb5242-next-page-reset-85
      *> rules: [FMT-13.18.37.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5242 - a 2002 report writer form, refused at 85
      *> with COBOL0900 and accepted from COBOL-2002.
      *> §1985:XIII.3.16.2 prints `NEXT GROUP IS {integer-1 | PLUS
      *> integer-2 | NEXT PAGE}`; ISO/IEC 1989:2002 Annex F.2 item 115:
      *> "WITH RESET phrase added to NEXT PAGE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5242N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5242n.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DETAIL LINE PLUS 1
           NEXT GROUP NEXT PAGE WITH RESET.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
