      *>--- conformance-case
      *> id: 1985-GR-X_4_2_1-p1
      *> rules: [1985-GR-X.4.2.1-p1, 1985-GR-X.4.3.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.2.1 p1; §1985:X.4.3.1 p2 (THE FILE DESCRIPTION ENTRY
      *> IN THE INTER-PROGRAM COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X42A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "x42e.dat".
           SELECT GF ASSIGN TO "x42g.dat".
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 ER.
          05 ER1 PIC X(4).
       FD GF IS GLOBAL.
       01 GR.
          05 GR1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WX IS EXTERNAL.
          05 WX1 PIC X(4).
       01 WG IS GLOBAL.
          05 WG1 PIC X(4).
       PROCEDURE DIVISION.
       P-1.
           MOVE "EXT1" TO WX1.
           MOVE "GLB1" TO WG1.
           OPEN OUTPUT EF.
           CALL "X42B".
           CLOSE EF.
           OPEN INPUT EF.
           READ EF AT END DISPLAY "EOF".
           DISPLAY "A READ ER1=" ER1.
           CLOSE EF.
           CALL "X42N".
           OPEN INPUT GF.
           READ GF AT END DISPLAY "EOF".
           DISPLAY "A READ GR1=" GR1.
           CLOSE GF.
           DISPLAY "A WG1=" WG1 " WX1=" WX1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X42N.
       PROCEDURE DIVISION.
       N-1.
           DISPLAY "N WG1=" WG1.
           MOVE "GLB2" TO WG1.
           OPEN OUTPUT GF.
           MOVE "GREC" TO GR1.
           WRITE GR.
           CLOSE GF.
           EXIT PROGRAM.
       END PROGRAM X42N.
       END PROGRAM X42A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X42B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "x42e.dat".
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 ER.
          05 ER1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WX IS EXTERNAL.
          05 WX1 PIC X(4).
       PROCEDURE DIVISION.
       B-1.
           DISPLAY "B WX1=" WX1.
           MOVE "EXT2" TO WX1.
           MOVE "EREC" TO ER1.
           WRITE ER.
           EXIT PROGRAM.
       END PROGRAM X42B.
