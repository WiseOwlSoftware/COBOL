      *>--- conformance-case
      *> id: pb5886-numeric-function-terms
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 refused S2 - FUNCTION MAX (S2) as a real term
      *> that does not cancel.
      *>
      *> kb/Work PB5889 (the ruling of 2026-11-21): the integer-
      *> argument screen refuses an argument only when it can name a
      *> value of the operands at which the expression is not an
      *> integer, and accepts the rest with a run-time check of the
      *> value. §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or an
      *> integer data item shall be specified"; §1989:A.2.2 AR4
      *> (cite.py --check) the same at COBOL-85.
      *>
      *> A one-argument MAX returns its argument (§2023:15.59.4), so
      *> S2 - MAX (S2) is 0 for every S2: the program conforms and is
      *> accepted. EXPECTED, with S2 = 0.2: FACTORIAL (0) = 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S2 PIC 9V9 VALUE 0.2.
       01  R  PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION FACTORIAL (S2 - FUNCTION MAX (S2)).
           DISPLAY R.
           STOP RUN.
