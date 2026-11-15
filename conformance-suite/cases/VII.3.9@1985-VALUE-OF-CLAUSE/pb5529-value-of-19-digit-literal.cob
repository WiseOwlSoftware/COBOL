      *>--- conformance-case
      *> id: pb5529-value-of-19-digit-literal
      *> rules: [1985-FMT-VII.3.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5529. §1985:VII.3.9.2: VALUE OF implementor-name-1 IS
      *> literal-1. A fixed-point numeric literal has at most 18 digits
      *> at COBOL-85 in this compiler, wherever it is written
      *> (COBOL0802), here as well as in a MOVE; 19 digits are legal
      *> from COBOL-2002 (pb5529_literal_capacity_value_clause).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5529V.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5529V.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           VALUE OF ID IS 1234567890123456789.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
