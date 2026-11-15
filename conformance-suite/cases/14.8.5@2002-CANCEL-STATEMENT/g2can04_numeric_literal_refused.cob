      *>--- conformance-case
      *> id: g2can04_numeric_literal_refused
      *> rules: [2002-SR-14.8.5.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.2 SR2 - "Literal-1 shall be an alphanumeric or
      *> national literal". 1234567 is a numeric literal, so the
      *> CANCEL is refused at its line; a compiler that took the
      *> literal's digits as a program-name accepts this program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNX4.
       PROCEDURE DIVISION.
       MAIN.
           CANCEL 1234567
           STOP RUN.
