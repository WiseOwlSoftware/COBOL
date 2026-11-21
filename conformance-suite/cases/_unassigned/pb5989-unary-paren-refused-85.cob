      *>--- conformance-case
      *> id: pb5989-unary-paren-refused-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.2.2 (cite.py: OK): "must be preceded by a space and followed by a space".
      *> COBOL-85 has no parenthesis exception for a unary operator (§2002:8.7.1
      *> adds one): '-' followed by '(' with no space is refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89UNR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC S9(3) VALUE 5.
       01  M  PIC S9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = -(M) + 9.
           STOP RUN.
