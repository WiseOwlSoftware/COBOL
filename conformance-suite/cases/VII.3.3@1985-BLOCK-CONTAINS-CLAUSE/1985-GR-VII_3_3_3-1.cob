      *>--- conformance-case
      *> id: 1985-GR-VII_3_3_3-1
      *> rules: [1985-GR-VII.3.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.3.3 GR1 (THE BLOCK CONTAINS CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BLKTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DATAFIL1
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO DATAFIL2
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           BLOCK CONTAINS 3 RECORDS
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2
           LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "BLOCKED-01" TO R1.
           WRITE R1.
           MOVE "BLOCKED-02" TO R1.
           WRITE R1.
           MOVE "BLOCKED-03" TO R1.
           WRITE R1.
           MOVE "BLOCKED-04" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "UNBLOCK-01" TO R2.
           WRITE R2.
           MOVE "UNBLOCK-02" TO R2.
           WRITE R2.
           CLOSE F2.
           OPEN INPUT F1.
       P2.
           READ F1 AT END GO TO P3.
           DISPLAY R1.
           GO TO P2.
       P3.
           DISPLAY "F1 END".
           CLOSE F1.
           OPEN INPUT F2.
       P4.
           READ F2 AT END GO TO P5.
           DISPLAY R2.
           GO TO P4.
       P5.
           DISPLAY "F2 END".
           CLOSE F2.
           STOP RUN.
