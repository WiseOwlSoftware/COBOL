      *>--- conformance-case
      *> id: 1985-FMT-X_5_5_2
      *> rules: [1985-FMT-X.5.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.5.2 (THE USE STATEMENT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 IS GLOBAL.
       01  F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX IS GLOBAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       F1-ERR SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F1.
       F1-ERR-PARA.
           DISPLAY "DECL " WS-STAT.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN INPUT F1.
           CALL "INNERP".
           CLOSE F1.
           DISPLAY "DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERP.
       PROCEDURE DIVISION.
       P1.
           READ F1.
           DISPLAY "BACK " WS-STAT.
           EXIT PROGRAM.
       END PROGRAM INNERP.
       END PROGRAM OUTERP.
