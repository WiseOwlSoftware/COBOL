      *>--- conformance-case
      *> id: 1985-GR-X_5_4_4-4
      *> rules: [1985-GR-X.5.4.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.4 GR4 (THE EXIT PROGRAM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XPGLOB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "xpglob.dat"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P.
           DISPLAY "DECLARATIVE ENTERED, STATUS " FS.
           CALL "XPSUB".
           DISPLAY "BACK IN DECLARATIVE".
       END DECLARATIVES.
       M SECTION.
       M-P.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1.
           DISPLAY "AFTER READ".
           CLOSE F1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XPSUB.
       PROCEDURE DIVISION.
       S-P.
           DISPLAY "IN XPSUB".
           EXIT PROGRAM.
       S-Q.
           DISPLAY "FELL THROUGH EXIT PROGRAM".
       END PROGRAM XPSUB.
       END PROGRAM XPGLOB.
