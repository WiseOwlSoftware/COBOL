      *>--- conformance-case
      *> id: pb5924-arith-numeric-operands-85
      *> rules: [1985-SR-VI.6.2.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.2.1 (cite.py: OK): "must represent
      *> either numeric elementary items or numeric literals"
      *> Numeric elementary items of usage display and computational,
      *> numeric literals and ZERO: compile, and evaluate algebraically.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  C  PIC S9(3) COMP VALUE -2.
       01  R  PIC 9(3).
       PROCEDURE DIVISION.
       P0.
           COMPUTE R = N * 2 + C - ZERO.
           DISPLAY R.
           IF N + C = 3 DISPLAY "T2 Y" ELSE DISPLAY "T2 N".
           IF N - 1 > C * 2 DISPLAY "T3 Y" ELSE DISPLAY "T3 N".
           STOP RUN.
