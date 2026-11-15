      *>--- conformance-case
      *> id: pb5202-report-code-set-sign-separate
      *> rules: [1985-SR-VII.3.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5202 - the accepted twin of
      *> pb5202-report-code-set-signed-item-refused: with SIGN IS LEADING
      *> SEPARATE written on the signed printable item (§1985:VII.3.4.3
      *> SR1, cite.py OK) the report file's CODE-SET clause is satisfied, an
      *> unsigned numeric item and an alphanumeric item need no clause, and
      *> the program runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5202RPTSEP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5202RPTSEP.PRT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           CODE-SET IS ASC
           REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC S9(4) VALUE -12.
       01  WS-CNT PIC 9(4) VALUE 7.
       REPORT SECTION.
       RD  R1.
       01  DETAIL-LINE TYPE IS DETAIL.
           05  LINE NUMBER IS PLUS 1.
               10  COLUMN 1 PIC X(4) VALUE "AMT ".
               10  COLUMN 6 PIC S9(4) SIGN IS LEADING SEPARATE
                   SOURCE IS WS-AMT.
               10  COLUMN 12 PIC 9(4) SOURCE IS WS-CNT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE R1.
           GENERATE DETAIL-LINE.
           TERMINATE R1.
           CLOSE RPT-FILE.
           DISPLAY "OK".
           STOP RUN.
