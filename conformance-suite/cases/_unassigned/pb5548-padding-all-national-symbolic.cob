      *>--- conformance-case
      *> id: pb5548-padding-all-national-symbolic
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5548 - §2002:12.3.4.10.2 SR1: literal-1 of the PADDING
      *> CHARACTER clause is alphanumeric, so ALL symbolic-character-1 of a
      *> NATIONAL symbolic-character is refused, as the bare form is
      *> (pb5420-padding-national-symbolic). The ALL arm reaches the
      *> symbolic-character screen through its own caller.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5548N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FOR NATIONAL SYM1 IS 300.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO "PB5548N"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS ALL SYM1.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A.
       01  REC-A PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
