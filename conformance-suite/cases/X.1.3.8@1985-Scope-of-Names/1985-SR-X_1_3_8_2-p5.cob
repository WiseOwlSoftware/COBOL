      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_2-p5
      *> rules: [1985-SR-X.1.3.8.2-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.2 p5 (Scope of Names). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "x16o.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1 GLOBAL.
       01 R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "O" GLOBAL.
          88 XC VALUE "O".
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT F1.
           CLOSE F1.
           CALL "XIN".
           DISPLAY "OUTER X=" X.
           IF XC DISPLAY "OUTER XC".
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "OUTER F1 EMPTY".
           CLOSE F1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "x16i.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(6).
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "I".
          88 XC VALUE "I".
       PROCEDURE DIVISION.
       I-1.
           DISPLAY "INNER X=" X.
           IF XC DISPLAY "INNER XC".
           OPEN OUTPUT F1.
           MOVE "INNER6" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "INNER R1=" R1.
           CLOSE F1.
           EXIT PROGRAM.
       END PROGRAM XIN.
       END PROGRAM XOUT.
