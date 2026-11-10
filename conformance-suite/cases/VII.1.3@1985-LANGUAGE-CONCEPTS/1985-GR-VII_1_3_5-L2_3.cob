      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-L2_3
      *> rules: [1985-GR-VII.1.3.5-L2.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GRL2.3 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T38.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "T38DAT"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ERR-PARA.
           DISPLAY "USE " WS-FS.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           DISPLAY "OPEN " WS-FS.
           CLOSE F1 WITH LOCK.
           DISPLAY "CLOSE " WS-FS.
           OPEN INPUT F1.
           STOP RUN.
