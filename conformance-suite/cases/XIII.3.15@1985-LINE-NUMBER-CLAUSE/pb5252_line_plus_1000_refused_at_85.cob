      *>--- conformance-case
      *> id: pb5252_line_plus_1000_refused_at_85
      *> rules: [1985-SR-XIII.3.15.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5252. §1985:XIII.3.15.3 SR1: "Integer-1 and
      *> integer-2 must not exceed three significant digits in length."
      *> LINE NUMBER IS PLUS 1000 has four, so it is refused at 85
      *> (COBOL2199, line 25), though the report is not divided into
      *> pages. From COBOL-2002 the bound is 9999 for such a report
      *> (§2023:13.18.35.3 SR3) and the program is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5252B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5252BR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1000
               NEXT GROUP PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
