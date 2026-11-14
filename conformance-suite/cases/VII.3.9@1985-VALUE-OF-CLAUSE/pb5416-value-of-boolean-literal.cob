      *>--- conformance-case
      *> id: pb5416-value-of-boolean-literal
      *> rules: [1985-FMT-VII.3.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5416. §1985:VII.3.9.2: VALUE OF implementor-name-1 IS
      *> literal-1, and a 1985 literal is "nonnumeric or numeric"
      *> (§1985:IV.4.2.2.2). The boolean literal (B"..." and BX"...") is a
      *> COBOL-2002 introduction, refused at COBOL-85 wherever it is written
      *> (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5416VB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5416VB.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           VALUE OF ID IS B"1" RETENTION IS BX"4".
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
