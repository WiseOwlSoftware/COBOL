      *>--- conformance-case
      *> id: 1985-FMT-VII_4_2_2
      *> rules: [1985-FMT-VII.4.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.2.2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLOSET.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "CLOSEF1"
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "CLOSEF2"
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(5).
       FD  F2 LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(5).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX.
       01  ST2 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       F2-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       F2-ERR-1.
           DISPLAY "F2 STATUS " ST2.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P1.
           OPEN OUTPUT F1 F2.
           MOVE "HELLO" TO R1.
           WRITE R1.
           MOVE "WORLD" TO R2.
           WRITE R2.
           CLOSE F1 WITH NO REWIND F2.
           OPEN INPUT F1 F2.
           READ F1 AT END DISPLAY "EOF1".
           DISPLAY R1.
           READ F2 AT END DISPLAY "EOF2".
           DISPLAY R2.
           IF ST1 = "99"
               CLOSE F1 REEL
               CLOSE F1 UNIT FOR REMOVAL.
           CLOSE F1 F2 WITH LOCK.
           DISPLAY "CLOSED " ST1 ST2.
           OPEN INPUT F2.
           STOP RUN.
