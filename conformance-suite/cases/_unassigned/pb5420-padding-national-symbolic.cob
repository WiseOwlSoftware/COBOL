      *>--- conformance-case
      *> id: pb5420-padding-national-symbolic
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5420 review - §2002:12.3.4.10.2 SR1: literal-1 is alphanumeric; a national symbolic-character is refused (R1-2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5420S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FOR NATIONAL SYM1 IS 300.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO "PB5420S"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS SYM1.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A.
       01  REC-A PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
