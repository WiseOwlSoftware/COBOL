      *>--- conformance-case
      *> id: 1985-FMT-VII_2_2_2
      *> rules: [1985-FMT-VII.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.2.2 (THE FILE-CONTROL PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FCTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO "FCTESTA".
           SELECT FILE-B ASSIGN TO "FCTESTB".
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A
           LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(5).
       FD  FILE-B
           LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FILE-A.
           OPEN OUTPUT FILE-B.
           MOVE "AAAAA" TO REC-A.
           WRITE REC-A.
           MOVE "BBBBB" TO REC-B.
           WRITE REC-B.
           CLOSE FILE-A.
           CLOSE FILE-B.
           OPEN INPUT FILE-B.
           OPEN INPUT FILE-A.
           READ FILE-B AT END DISPLAY "EOF B".
           READ FILE-A AT END DISPLAY "EOF A".
           DISPLAY REC-A.
           DISPLAY REC-B.
           CLOSE FILE-A.
           CLOSE FILE-B.
           STOP RUN.
