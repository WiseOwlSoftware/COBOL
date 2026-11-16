      *>--- conformance-case
      *> id: g2pad02_padding_literal_two_characters
      *> rules: [2002-SR-12.3.4.10.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.10.2 SR1 - "Literal-1 shall be a one-character
      *>   alphanumeric literal" (cite.py OK). A refusal: literal-1 on
      *>   line 19 is the two-character literal "**", which the rule
      *>   forbids; a compiler that accepts it (and DISPLAYs ACCEPTED)
      *>   fails. Expected: COBOL6848 at line 19 at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PAD02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2PAD02.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "**".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01  W1 PIC X VALUE "*".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1
           MOVE "AB**" TO R1 WRITE R1
           CLOSE F1
           DISPLAY "ACCEPTED"
           STOP RUN.
