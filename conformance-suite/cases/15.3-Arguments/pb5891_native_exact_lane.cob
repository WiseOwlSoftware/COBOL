      *>--- conformance-case
      *> id: pb5891_native_exact_lane
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R1-3 - the twin of
      *> pb5891-sdidi-lane-identity without the SQRT pair: every
      *> operation is on the native Int128 or exact lane, which keeps
      *> every digit, so the argument is an integer and admitted.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified".
      *> EXPECTED: Y * 100 = 205000...0 (33 digits), X * 10.25 =
      *> 102499999999999999999999999999989.75, their difference
      *> 102500000000000000000000000000010.25, times 4
      *> 410000000000000000000000000000041, and MOD (..., 1000) is
      *> 41 (§2023:15.64.4 RV1): R PIC 9(4) shows 0041.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X  PIC 9(31) VALUE 9999999999999999999999999999999.
       01  Y  PIC 9(31) VALUE 2050000000000000000000000000000.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD ((Y * 100 - (X * 10.25)) * 4,
               1000).
           DISPLAY R.
           STOP RUN.
