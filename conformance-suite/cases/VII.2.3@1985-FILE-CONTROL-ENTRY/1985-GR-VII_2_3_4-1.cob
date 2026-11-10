      *>--- conformance-case
      *> id: 1985-GR-VII_2_3_4-1
      *> rules: [1985-GR-VII.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.3.4 GR1 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXTF ASSIGN TO "EXTFILE"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EXTF IS EXTERNAL.
       01  EXTF-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXTF.
           CALL "SUBP".
           CLOSE EXTF.
           OPEN INPUT EXTF.
           READ EXTF
               AT END DISPLAY "EOF".
           DISPLAY EXTF-REC.
           CLOSE EXTF.
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXTF ASSIGN TO "EXTFILE"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  EXTF IS EXTERNAL.
       01  EXTF-REC PIC X(5).
       PROCEDURE DIVISION.
       SUB-PARA.
           MOVE "HELLO" TO EXTF-REC.
           WRITE EXTF-REC.
           EXIT PROGRAM.
       END PROGRAM SUBP.
