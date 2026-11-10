      *>--- conformance-case
      *> id: 1985-FMT-VII_2_10_2
      *> rules: [1985-FMT-VII.2.10.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.10.2 (THE I-O-CONTROL PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOCTL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "IOCTLF1".
           SELECT F2 ASSIGN TO "IOCTLF2".
           SELECT F3 ASSIGN TO "IOCTLF3".
           SELECT F4 ASSIGN TO "IOCTLF4".
           SELECT F5 ASSIGN TO "IOCTLTP".
           SELECT F6 ASSIGN TO "IOCTLTP".
       I-O-CONTROL.
           SAME AREA FOR F1 F2
           MULTIPLE FILE TAPE CONTAINS F5 POSITION 1 F6 POSITION 3
           SAME RECORD AREA FOR F3 F4.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS STANDARD.
       01  R1 PIC X(5).
       FD  F2 LABEL RECORDS STANDARD.
       01  R2 PIC X(5).
       FD  F3 LABEL RECORDS STANDARD.
       01  R3 PIC X(5).
       FD  F4 LABEL RECORDS STANDARD.
       01  R4 PIC X(5).
       FD  F5 LABEL RECORDS STANDARD.
       01  R5 PIC X(5).
       FD  F6 LABEL RECORDS STANDARD.
       01  R6 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "AAAAA" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF1".
           DISPLAY R1.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "BBBBB" TO R2.
           WRITE R2.
           CLOSE F2.
           OPEN INPUT F2.
           READ F2 AT END DISPLAY "EOF2".
           DISPLAY R2.
           CLOSE F2.
           OPEN OUTPUT F3 F4.
           MOVE "CCCCC" TO R3.
           DISPLAY R4.
           WRITE R3.
           CLOSE F3.
           OPEN INPUT F3.
           READ F3 AT END DISPLAY "EOF3".
           DISPLAY R4.
           CLOSE F3 F4.
           STOP RUN.
