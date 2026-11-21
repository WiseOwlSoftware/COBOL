      *>--- conformance-case
      *> id: pb5989-unary-spaced-runs-85
      *> rules: []
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. §1985:VI.6.2.2 (cite.py: OK): "must be preceded by a space and followed by a space".
      *> Each unary operator set off by spaces, next to parentheses, compiles.
      *> N = - ( 2 ) + 9 = 7; K = ( - 2 ) * 3 = -6.
      *> A unary operator written directly after '(' draws no refusal: the NIST-validated 1985 programs write it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89UNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC S9(3) VALUE 5.
       01  M  PIC S9(3) VALUE 2.
       01  K  PIC S9(3).
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = - ( M ) + 9.
           COMPUTE K = ( - M ) * 3.
           DISPLAY N.
           DISPLAY K.
           COMPUTE K = (- M) * 3.
           DISPLAY K.
           STOP RUN.
