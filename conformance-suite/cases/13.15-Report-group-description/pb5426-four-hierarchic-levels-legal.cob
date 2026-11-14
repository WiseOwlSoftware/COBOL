      *>--- conformance-case
      *> id: pb5426-four-hierarchic-levels-legal
      *> rules: [SR-13.15.3-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5426 - 1985:XIII.3.9.3 SR4 limits a report group to "one, two, or three hierarchic levels" (cite.py: OK).
      *> COBOL-2002 and later have one entry format and no depth limit: 13.15.3 SR3 lets the level-number be "any integer from
      *> 1 through 49". The detail group of four levels compiles and runs: "DONE".
      *> conformance:pb5426-four-hierarchic-levels is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5426FG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5426FG.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DET TYPE IS DETAIL.
           02  G1 LINE PLUS 1.
               03  G2.
                   04  COLUMN 1 PIC X VALUE "A".
               03  COLUMN 5 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "DONE".
           STOP RUN.
