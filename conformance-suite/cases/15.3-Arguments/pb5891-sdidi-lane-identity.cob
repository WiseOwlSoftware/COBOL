      *>--- conformance-case
      *> id: pb5891-sdidi-lane-identity
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R1-3 - an intermediate is exact only
      *> on the lane the expression is evaluated on.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". SQRT (N) -
      *> SQRT (N) cancels, but the numeric function's value takes
      *> the operations above it onto the SDIDI (34 significant
      *> digits; native arithmetic's lane beside a function value,
      *> CONFORMANCE.md A.1 item 82). There X * 10.25 over a
      *> 31-digit X needs 35 significant digits, 2 of them fraction
      *> digits (...989.75), so its fraction is rounded away and the
      *> argument is not an integer: the program is refused
      *> (COBOL1627). Its twin without the SQRT pair,
      *> pb5891_native_exact_lane, is exact and admitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 99 VALUE 4.
       01  X  PIC 9(31) VALUE 9999999999999999999999999999999.
       01  Y  PIC 9(31) VALUE 2050000000000000000000000000000.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD ((Y * 100 - (FUNCTION SQRT (N)
               - FUNCTION SQRT (N) + X * 10.25)) * 4, 1000).
           STOP RUN.
