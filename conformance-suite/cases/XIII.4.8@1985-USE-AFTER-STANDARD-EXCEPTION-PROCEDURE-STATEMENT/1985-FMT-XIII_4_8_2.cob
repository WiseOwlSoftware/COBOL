      *>--- conformance-case
      *> id: 1985-FMT-XIII_4_8_2
      *> rules: [1985-FMT-XIII.4.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.2 (THE USE AFTER STANDARD EXCEPTION PROCEDURE
      *> STATEMENT). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from the confirmation pass's redraft
      *> (WEAK original) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USERW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-ONE ASSIGN TO "RPTONE"
               FILE STATUS IS ST-ONE.
           SELECT RPT-TWO ASSIGN TO "RPTTWO"
               FILE STATUS IS ST-TWO.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-ONE
           REPORT IS RPT-A.
       FD  RPT-TWO
           REPORT IS RPT-B.
       WORKING-STORAGE SECTION.
       01  ST-ONE PIC XX.
       01  ST-TWO PIC XX.
       REPORT SECTION.
       RD  RPT-A.
       01  LINE-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       RD  RPT-B.
       01  LINE-B TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "AGAIN".
       PROCEDURE DIVISION.
       DECLARATIVES.
       FILE-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RPT-ONE.
       FILE-PARA.
           DISPLAY "USE RPT-ONE " ST-ONE.
           STOP RUN.
       OUT-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       OUT-PARA.
           DISPLAY "USE OUTPUT".
           STOP RUN.
       EXT-SECT SECTION.
           USE AFTER ERROR PROCEDURE ON EXTEND.
       EXT-PARA.
           DISPLAY "USE EXTEND " ST-TWO.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-ONE.
           INITIATE RPT-A.
           GENERATE LINE-A.
           TERMINATE RPT-A.
           CLOSE RPT-ONE.
           DISPLAY "OUTPUT " ST-ONE.
           OPEN EXTEND RPT-TWO.
           DISPLAY "NO USE PROCEDURE RAN".
           STOP RUN.
