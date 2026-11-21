      *>--- conformance-case
      *> id: pb5886-power-shapes
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 refused 1024 ** K as a power whose exponent
      *> has fraction digits.
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
      *> 1024 is 2 ** 10, so 1024 ** K is 2 ** (10 * K), an integer
      *> for every value of K PIC 9V9: accepted, checked at run time.
      *> EXPECTED, with K = 0.5: 1024 ** 0.5 = 32, MOD (32, 7) = 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K  PIC 9V9 VALUE 0.5.
       01  R  PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (1024 ** K, 7).
           DISPLAY R.
           STOP RUN.
