      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-L2_4
      *> rules: [1985-GR-VII.1.3.5-L2.4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GRL2.4 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T42.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO DISK
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F.
           DISPLAY "OPEN " WS-STAT.
           CLOSE F.
           DISPLAY "CLOSE " WS-STAT.
           CLOSE F.
           STOP RUN.
