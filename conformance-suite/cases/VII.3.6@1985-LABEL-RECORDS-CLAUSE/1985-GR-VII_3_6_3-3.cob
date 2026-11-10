      *>--- conformance-case
      *> id: 1985-GR-VII_3_6_3-3
      *> rules: [1985-GR-VII.3.6.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.6.3 GR3 (THE LABEL RECORDS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LBLTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "LBLFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "HELLO" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1
               AT END MOVE SPACES TO F1-REC.
           IF F1-REC = "HELLO"
               DISPLAY "LABEL OK"
           ELSE
               DISPLAY "LABEL BAD".
           CLOSE F1.
           STOP RUN.
