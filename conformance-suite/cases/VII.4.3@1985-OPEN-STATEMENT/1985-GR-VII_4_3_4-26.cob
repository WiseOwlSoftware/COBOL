      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-26
      *> rules: [1985-GR-VII.4.3.4-26]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR26 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C8OPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "C8F1.DAT"
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "C8F2.DAT"
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
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       F1-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       F1-ERR-PARA.
           DISPLAY "USE F1 " ST1.
       F2-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F2.
       F2-ERR-PARA.
           DISPLAY "USE F2 " ST2.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           MOVE "XX" TO ST1.
           OPEN EXTEND F1.
           DISPLAY "AFTER OPEN EXTEND F1 " ST1.
           MOVE "XX" TO ST1.
           OPEN INPUT F1.
           DISPLAY "AFTER OPEN INPUT F1 " ST1.
           OPEN OUTPUT F2.
           MOVE "AAAA" TO R2.
           WRITE R2.
           MOVE "BBBB" TO R2.
           WRITE R2.
           CLOSE F2.
           OPEN INPUT F2.
           MOVE "XX" TO ST2.
           OPEN OUTPUT F2.
           DISPLAY "AFTER OPEN OUTPUT F2 " ST2.
           CLOSE F2.
           OPEN INPUT F2.
           PERFORM READ-F2 UNTIL EOF-SW = "Y".
           CLOSE F2.
           STOP RUN.
       READ-F2.
           READ F2
               AT END MOVE "Y" TO EOF-SW
           END-READ.
           IF EOF-SW = "Y"
               DISPLAY "F2 AT END"
           ELSE
               DISPLAY "F2 RECORD " R2.
