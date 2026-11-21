      *>--- conformance-case
      *> id: pb5886-item-quotients
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 refused I / I + 65 as a quotient by an item.
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
      *> I / I + 65 is 66 wherever it is defined (I = 0 is a size
      *> error), so no value of I is a counterexample: the screen
      *> neither proves nor refutes it, and it is accepted and
      *> checked at run time. EXPECTED, with I = 6: MOD (66, 7) = 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  I  PIC 9(4) VALUE 6.
       01  R  PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (I / I + 65, 7).
           DISPLAY R.
           STOP RUN.
