      *>--- conformance-case
      *> id: 1985-GR-VII_3_5_4-1
      *> rules: [1985-GR-VII.3.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.5.4 GR1 (THE DATA RECORDS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
           DATA RECORDS ARE REC-B REC-A.
       01  REC-A PIC X(5).
       01  REC-B.
           05  B-TYPE PIC X.
           05  B-DATA PIC X(9).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "AAAAA" TO REC-A.
           WRITE REC-A.
           MOVE "B" TO B-TYPE.
           MOVE "123456789" TO B-DATA.
           WRITE REC-B.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY REC-A.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY B-TYPE B-DATA.
           CLOSE F1.
           STOP RUN.
