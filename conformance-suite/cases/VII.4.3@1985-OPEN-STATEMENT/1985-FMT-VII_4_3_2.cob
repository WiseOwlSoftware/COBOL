      *>--- conformance-case
      *> id: 1985-FMT-VII_4_3_2
      *> rules: [1985-FMT-VII.4.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.2 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "OPENF1"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "OPENF2"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "OPENF3"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(5).
       FD  F2
           LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(5).
       FD  F3
           LABEL RECORDS ARE STANDARD.
       01  R3 PIC X(5).
       WORKING-STORAGE SECTION.
       01  SW PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1 F2.
           MOVE "ONE-1" TO R1.
           WRITE R1.
           MOVE "TWO-1" TO R2.
           WRITE R2.
           CLOSE F1 F2.
           OPEN EXTEND F1.
           MOVE "ONE-2" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN I-O F2.
           READ F2 AT END DISPLAY "EOF2".
           MOVE "TWO-X" TO R2.
           REWRITE R2.
           CLOSE F2.
           OPEN INPUT F1 F2 OUTPUT F3.
           READ F1 AT END DISPLAY "EOF1".
           DISPLAY R1.
           READ F1 AT END DISPLAY "EOF1".
           DISPLAY R1.
           READ F1 AT END DISPLAY "EOF1".
           READ F2 AT END DISPLAY "EOF2".
           DISPLAY R2.
           MOVE "THREE" TO R3.
           WRITE R3.
           CLOSE F1 F2 F3.
           OPEN INPUT F3.
           READ F3 AT END DISPLAY "EOF3".
           DISPLAY R3.
           CLOSE F3.
           IF SW = 1
               OPEN INPUT F1 REVERSED F2 WITH NO REWIND
                    OUTPUT F3 WITH NO REWIND.
           STOP RUN.
