      *> reject-at: 85
      *> The GROUP-USAGE clause (ISO 13.18.29) is a COBOL-2002 introduction: the COBOL-85 compiler names it
      *> (COBOLNET0900) instead of dying on the phrase word. kb/Work PB79.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 N1 PIC N(2).
       PROCEDURE DIVISION.
           STOP RUN.
