      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-L2_2
      *> rules: [1985-GR-VII.1.3.5-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GRL2.2 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C5END.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F1 ASSIGN TO "C5F1.DAT"
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "C5F2.DAT"
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX VALUE "XX".
       01  ST2 PIC XX VALUE "XX".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F2.
           MOVE "AAAA" TO R2.
           WRITE R2.
           MOVE "BBBB" TO R2.
           WRITE R2.
           CLOSE F2.
           OPEN INPUT F2.
           PERFORM READ-F2 3 TIMES.
           CLOSE F2.
           OPEN INPUT F1.
           MOVE "XX" TO ST1.
           READ F1
               AT END DISPLAY "F1 AT END"
               NOT AT END DISPLAY "F1 NOT AT END"
           END-READ.
           DISPLAY "F1 READ " ST1.
           CLOSE F1.
           STOP RUN.
       READ-F2.
           MOVE "XX" TO ST2.
           MOVE SPACES TO R2.
           READ F2
               AT END DISPLAY "F2 AT END"
               NOT AT END DISPLAY "F2 RECORD " R2
           END-READ.
           DISPLAY "F2 READ " ST2.
