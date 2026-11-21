      *>--- conformance-case
      *> id: pb5924-arith-op-spaced-85
      *> rules: [1985-SR-VI.6.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.2 (cite.py: OK): "must be preceded
      *> by a space and followed by a space"
      *> Every operator, binary and unary, with a space on both sides,
      *> parentheses spaced too: compiles and evaluates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  M  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = ( N + 1 ) * 2.
           DISPLAY N.
           COMPUTE N = - M + 9.
           DISPLAY N.
           COMPUTE N = M ** 3 / 4.
           DISPLAY N.
           STOP RUN.
