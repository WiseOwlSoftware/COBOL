      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-10
      *> rules: [1985-GR-VII.4.7.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR10 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTFAIL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       ERR-PAR.
           DISPLAY "USE PROCEDURE " FS.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PAR.
           OPEN OUTPUT F1.
           MOVE "RECORD 1" TO R1.
           WRITE R1.
           DISPLAY "WRITE 1 " FS.
           CLOSE F1.
           OPEN INPUT F1.
           DISPLAY "OPEN INPUT " FS.
           MOVE "RECORD 2" TO R1.
           WRITE R1.
           DISPLAY "USE PROCEDURE NOT EXECUTED".
           STOP RUN.
