      *>--- conformance-case
      *> id: pb5486-write-from-function-cites-1985
      *> rules: [1985-SR-VII.4.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5486: 1985 states the FROM function rule.
      *> §1985:VII.4.7.3 SR1 (as amended by X3.23a-1989): "If
      *> identifier-1 is a function-identifier, it must reference an
      *> alphanumeric function." FUNCTION LENGTH is an integer
      *> function, so the WRITE is refused COBOL1914 at 85, citing the
      *> sequential module's rule, not the 2023 SR4 wording.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5486W1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5486W1.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           WRITE R1 FROM FUNCTION LENGTH("AB").
           CLOSE F1.
           STOP RUN.
