      *>--- conformance-case
      *> id: 1985-GR-VII_4_6_4-7
      *> rules: [1985-GR-VII.4.6.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.4 GR7 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USENEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1
               FILE STATUS IS FS1.
           SELECT F2 ASSIGN TO DISK2
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2
           LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       01  PASS-SW PIC X VALUE "1".
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR1-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ERR1-PAR.
           DISPLAY "USE1 IN " FS1.
           IF PASS-SW = "1"
               READ F2
               DISPLAY "USE1 AFTER READ F2".
           DISPLAY "USE1 OUT".
       ERR2-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       ERR2-PAR.
           DISPLAY "USE2 " FS2.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PAR.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN OUTPUT F2.
           CLOSE F2.
           OPEN INPUT F1.
           OPEN INPUT F2.
           READ F1.
           DISPLAY "BACK 1".
           CLOSE F1.
           OPEN INPUT F1.
           MOVE "2" TO PASS-SW.
           READ F1.
           DISPLAY "BACK 2".
           CLOSE F1.
           CLOSE F2.
           STOP RUN.
