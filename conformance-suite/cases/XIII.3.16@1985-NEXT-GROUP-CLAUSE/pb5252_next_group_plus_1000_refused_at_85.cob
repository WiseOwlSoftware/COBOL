      *>--- conformance-case
      *> id: pb5252_next_group_plus_1000_refused_at_85
      *> rules: [1985-SR-XIII.3.16.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5252. §1985:XIII.3.16.3 SR2: "Integer-1 and
      *> integer-2 must not exceed three significant digits in length."
      *> NEXT GROUP PLUS 1000 has four, so it is refused at 85
      *> (COBOL2284, line 25). From COBOL-2002 the bound is 9999 for a
      *> report not divided into pages (§2023:13.18.37.3 SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5252C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5252CR.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1
               NEXT GROUP PLUS 1000.
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
