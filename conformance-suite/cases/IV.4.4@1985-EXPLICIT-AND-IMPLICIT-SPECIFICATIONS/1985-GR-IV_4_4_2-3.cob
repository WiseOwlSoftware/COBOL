      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_2-3
      *> rules: [1985-GR-IV.4.4.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.2 GR3 (EXPLICIT AND IMPLICIT SPECIFICATIONS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFERTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO XFERF
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-PARA.
           DISPLAY "IN DECLARATIVE " FS.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           OPEN OUTPUT F.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "AFTER READ".
           CLOSE F.
           ALTER P1 TO PROCEED TO P3.
           DISPLAY "A".
       P1.
           GO TO P2.
       P2.
           DISPLAY "B".
           STOP RUN.
       P3.
           DISPLAY "C".
           EXIT PROGRAM.
       P4.
           DISPLAY "D".
           STOP RUN.
