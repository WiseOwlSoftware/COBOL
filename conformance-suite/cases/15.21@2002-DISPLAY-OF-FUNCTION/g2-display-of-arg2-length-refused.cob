      *>--- conformance-case
      *> id: g2-display-of-arg2-length-refused
      *> rules: [2002-AR-15.21.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.21.2 AR2 - argument-2 "shall be one character position
      *> in length". X2 is two character positions, so
      *> DISPLAY-OF(NN, X2) is refused at 2002. The accepted forms are
      *> g2-display-of-substitution-argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DSO02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NN      PIC N(3) VALUE N"ABC".
       01  X2      PIC X(2) VALUE "??".
       01  R       PIC X(3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION DISPLAY-OF(NN, X2) TO R
           STOP RUN.
