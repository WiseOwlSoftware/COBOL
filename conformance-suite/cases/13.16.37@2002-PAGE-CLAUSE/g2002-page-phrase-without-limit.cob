      *>--- conformance-case
      *> id: g2002-page-phrase-without-limit
      *> rules: [2002-SR-13.16.37.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.37.2 SR3 - "The HEADING, FIRST DETAIL, LAST CONTROL
      *> HEADING, LAST DETAIL, or FOOTING phrase may be specified only if
      *> integer-1 is specified." The PAGE clause below gives only the
      *> page width (integer-2, legal alone by SR2) and a FOOTING phrase,
      *> so the compiler must refuse the program; accepting it fails the
      *> case. (The accepted form is g2002-page-phrases-with-limit.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PG04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2PG04.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       REPORT SECTION.
       RD  R PAGE LIMIT IS 40 COLUMNS FOOTING 9.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           TERMINATE R.
           CLOSE PRT.
           DISPLAY "NOT REFUSED".
           STOP RUN.
