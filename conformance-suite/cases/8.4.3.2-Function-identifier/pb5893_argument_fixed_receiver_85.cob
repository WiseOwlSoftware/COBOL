      *>--- conformance-case
      *> id: pb5893_argument_fixed_receiver_85
      *> rules: [GR-8.4.3.2.4-2]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5893 - the values an all-fixed-point argument gives
      *> under a fixed-point resultant, the evaluation a COMP-1 or
      *> COMP-2 resultant now shares
      *> (pb5893_argument_float_receiver_every_position). 1985 has no
      *> floating-point usage, so it can show only this half: the
      *> argument is an expression evaluated individually and exactly
      *> (§2023:8.4.3.2.4 GR2, cite.py --check: "its arguments are
      *> evaluated individually in the order specified"), with 18-digit
      *> items, the widest a 1985 PICTURE holds.
      *> EXPECTED, derived: X = 10**18 - 1, Y = 2.05 * 10**17 and
      *> Z = 4.1 * 10**17, so E = (Y * 100 - X * 10.25) * 4 is
      *> 4 * (2.05E19 - (1.025E19 - 10.25)) = 4.1E19 + 41: REM (E, 1000)
      *> and MOD (E, 1000) are 41; E - Z * 100 is 41, so MAX (41, 1)
      *> is 41, SUM (41, 1) 42, MEAN (41, 3) 22, and ANNUITY (0.5, 2)
      *> = 0.5 / (1 - 1 / 2.25) = 0.9 (0.9000 to 4 places, ROUNDED).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5893B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X   PIC 9(18) VALUE 999999999999999999.
       01  Y   PIC 9(18) VALUE 205000000000000000.
       01  Z   PIC 9(18) VALUE 410000000000000000.
       01  W   PIC S9(4).
       01  D4  PIC S9V9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000).
           DISPLAY "REMX " W.
           COMPUTE W = FUNCTION MOD ((Y * 100 - X * 10.25) * 4, 1000).
           DISPLAY "MODX " W.
           COMPUTE W = FUNCTION MAX ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1).
           DISPLAY "MAXX " W.
           COMPUTE W = FUNCTION SUM ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1).
           DISPLAY "SUMX " W.
           COMPUTE W = FUNCTION MEAN ((Y * 100 - X * 10.25) * 4
               - Z * 100, 3).
           DISPLAY "MEANX " W.
           COMPUTE D4 ROUNDED = FUNCTION ANNUITY ((Y * 100 - X * 10.25)
               * 4 - Z * 100 - 40.5, 2).
           DISPLAY "ANNX " D4.
           STOP RUN.
