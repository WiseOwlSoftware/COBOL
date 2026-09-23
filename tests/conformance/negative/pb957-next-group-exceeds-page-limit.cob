      *> reject-at: 85 2002 2014 2023
      *> A NEXT GROUP INTEGER BEYOND THE PAGE LIMIT (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR1: "Integer-1 and integer-2 shall not exceed the page limit, or
      *> 9999 if the report is not divided into pages." PAGE LIMIT 20, integer-2 30.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NPL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957npl.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES.
       01  DET TYPE DE NEXT GROUP PLUS 30.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
