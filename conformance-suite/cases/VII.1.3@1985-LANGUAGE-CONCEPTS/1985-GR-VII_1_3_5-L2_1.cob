      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-L2_1
      *> rules: [1985-GR-VII.1.3.5-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GRL2.1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C4OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F1 ASSIGN TO "C4F1.DAT"
               FILE STATUS IS ST1.
           SELECT OPTIONAL F2 ASSIGN TO "C4F2.DAT"
               FILE STATUS IS ST2.
           SELECT OPTIONAL F3 ASSIGN TO "C4F3.DAT"
               FILE STATUS IS ST3.
           SELECT F4 ASSIGN TO "C4F4.DAT"
               FILE STATUS IS ST4.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       FD  F4.
       01  R4 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX VALUE "XX".
       01  ST2 PIC XX VALUE "XX".
       01  ST3 PIC XX VALUE "XX".
       01  ST4 PIC XX VALUE "XX".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F4.
           DISPLAY "F4 OPEN OUTPUT " ST4.
           MOVE "XX" TO ST4.
           MOVE "ABCD" TO R4.
           WRITE R4.
           DISPLAY "F4 WRITE " ST4.
           MOVE "XX" TO ST4.
           CLOSE F4.
           DISPLAY "F4 CLOSE " ST4.
           MOVE "XX" TO ST4.
           OPEN INPUT F4.
           DISPLAY "F4 OPEN INPUT " ST4.
           MOVE "XX" TO ST4.
           MOVE SPACES TO R4.
           READ F4 AT END DISPLAY "F4 AT END".
           DISPLAY "F4 READ " ST4 " " R4.
           CLOSE F4.
           OPEN INPUT F1.
           DISPLAY "F1 OPEN INPUT " ST1.
           MOVE "XX" TO ST1.
           CLOSE F1.
           DISPLAY "F1 CLOSE " ST1.
           OPEN I-O F2.
           DISPLAY "F2 OPEN I-O " ST2.
           CLOSE F2.
           MOVE "XX" TO ST2.
           OPEN INPUT F2.
           DISPLAY "F2 OPEN INPUT " ST2.
           CLOSE F2.
           OPEN EXTEND F3.
           DISPLAY "F3 OPEN EXTEND " ST3.
           MOVE "WXYZ" TO R3.
           WRITE R3.
           DISPLAY "F3 WRITE " ST3.
           CLOSE F3.
           MOVE "XX" TO ST3.
           OPEN INPUT F3.
           DISPLAY "F3 OPEN INPUT " ST3.
           MOVE SPACES TO R3.
           READ F3 AT END DISPLAY "F3 AT END".
           DISPLAY "F3 READ " ST3 " " R3.
           CLOSE F3.
           STOP RUN.
