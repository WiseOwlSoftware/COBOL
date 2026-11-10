      *>--- conformance-case
      *> id: 1985-SR-X_5_5_3-1
      *> rules: [1985-SR-X.5.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.5.3 SR1 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEMULT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1.
           SELECT F2 ASSIGN TO DISK2.
           SELECT F3 ASSIGN TO DISK3.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2 LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       FD  F3 LABEL RECORDS ARE STANDARD.
       01  R3 PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F1 F2.
       ERR-PARA.
           DISPLAY "USE F1 F2".
       EXT-SEC SECTION.
           USE GLOBAL AFTER STANDARD EXCEPTION PROCEDURE ON EXTEND.
       EXT-PARA.
           DISPLAY "USE EXTEND".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1 F2.
           CLOSE F1 F2.
           OPEN INPUT F1 F2.
           READ F1.
           READ F2.
           CLOSE F1 F2.
           DISPLAY "CLOSED".
           OPEN EXTEND F3.
           STOP RUN.
