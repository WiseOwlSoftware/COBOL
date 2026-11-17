      *>--- conformance-case
      *> id: pb5891_argument_float_receiver
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R3-1 - a function's argument is
      *> evaluated as an expression of its own, never on the binary64
      *> lane of a floating-point resultant.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified".
      *> (Y * 100 - X * 10.25) * 4 is exactly
      *> 410000000000000000000000000000041 natively, so MOD (..., 1000)
      *> is 41 whatever the resultant: 41 is held exactly by the COMP-2
      *> F and moved to E PIC 9(4) (F 0041), as to R (R 0041). Before
      *> the fix the COMP-2 resultant took the argument onto binary64
      *> and stored 672.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X  PIC 9(31) VALUE 9999999999999999999999999999999.
       01  Y  PIC 9(31) VALUE 2050000000000000000000000000000.
       01  R  PIC 9(4).
       01  F  COMP-2.
       01  E  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD ((Y * 100 - (X * 10.25)) * 4,
               1000).
           DISPLAY "R " R.
           COMPUTE F = FUNCTION MOD ((Y * 100 - (X * 10.25)) * 4,
               1000).
           MOVE F TO E.
           DISPLAY "F " E.
           STOP RUN.
