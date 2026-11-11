      *>--- conformance-case
      *> id: pb5208_power_integral_exponents
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5208 - the complement of the power arm of the
      *> §2023:15.3 type 6 screen: powers that ALWAYS result in an
      *> integer value are integer arguments.
      *>
      *> §2023:15.3 type 6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or
      *> an integer data item shall be specified"; §1989:A.2.2 AR4
      *> the same. An integer item raised to a non-negative integer
      *> literal, to an unsigned integer item, or to the negation of
      *> a negated unsigned integer item is an integer for every
      *> value of the items, so each is admitted. With N = 3 and
      *> U = 2: MOD (9, 7) is 2 three times, and N ** 0 is 1. A
      *> screen that refused a power for its shape alone fails here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5208A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  U PIC 9 VALUE 2.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (N ** 2, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (N ** U, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (N ** - (- U), 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (N ** 0, 7).
           DISPLAY R.
           STOP RUN.
