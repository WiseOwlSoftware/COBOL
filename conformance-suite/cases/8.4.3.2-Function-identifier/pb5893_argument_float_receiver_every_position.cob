      *>--- conformance-case
      *> id: pb5893_argument_float_receiver_every_position
      *> rules: [GR-8.4.3.2.4-2]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5893 - a function argument's evaluation technique
      *> is decided by the argument, never by the statement's
      *> receiver: an all-fixed-point argument at ANY position
      *> evaluates under a COMP-1 or COMP-2 resultant exactly as under
      *> a fixed-point one, and the resultant takes the function's
      *> value by the ordinary rules.
      *>
      *> §2023:8.4.3.2.4 GR2 (cite.py --check): "its arguments are
      *> evaluated individually in the order specified". DOC-A.1-92b
      *> makes a function binary64 only when an ARGUMENT renders as
      *> floating-point (§2023:15.4.1, cite.py --check: "the
      *> characteristics and representation of the returned value are
      *> defined by the implementor").
      *> EXPECTED, derived: E = (Y * 100 - X * 10.25) * 4 with
      *> X = 10**31 - 1 and Y = 2.05 * 10**30 is
      *> 4 * (2.05E32 - (1.025E32 - 10.25)) = 4.1E32 + 41, so
      *> REM (E, 1000) (§2023:15.77.4 RV1, cite.py --check: "The
      *> equivalent arithmetic expression is") and MOD (E, 1000) are
      *> 41, and E - Z * 100 with Z = 4.1 * 10**30 is 41: ABS 41,
      *> MAX (41, 1) 41, SUM (41, 1) 42, MEAN (41, 3) 22, and
      *> ANNUITY (0.5, 2) = 0.5 / (1 - 1 / 2.25) = 0.9 (0.9000 to 4
      *> places, ROUNDED). Each value is held exactly by COMP-1 and
      *> COMP-2 and moved ROUNDED to W or D4, so the three receivers
      *> print one value. Before the fix a floating-point resultant
      *> took every argument but MOD's onto binary64 and printed REM
      *> 672, ABS 0, MAX 1, SUM 1, MEAN 2 and ANNUITY 0.
      *> The control (DOC-A.1-92b, unchanged): FG COMP-2 VALUE 0.1
      *> makes FG * 3 a binary64 argument, 0.30000000000000004 in
      *> IEEE binary64, so REM (FG * 3, 1) is that double under both
      *> receivers, 0.30000000000000004 to 17 places.
      *> SIGNIFICANCE (kb/Work PB5893 review R1-2, R1-3: under a
      *> floating-point resultant a quotient in an argument at a
      *> numeric position that may not terminate is the 34-digit
      *> SDIDI quotient, never one cut at a fixed scale): with
      *> C = 10**18 - 1, 1 / C / C is 1.000000000000000002E-36, which
      *> lands in COMP-2 with no ROUNDED by truncation, the double just
      *> below it, shown as 1E-36, and in COMP-1 the binary32 just
      *> below it, 9.9999995E-37; 1 / T with T = 3E24 is 3.333...E-25,
      *> +3.333E-25 in PIC +9.999E+99. A quotient by a literal that
      *> divides exactly stays exact: (Y * 100 - X * 10.25) * 4 / 4 is
      *> 1.025E32 + 10.25, so REM (..., 1000) is 10.25 (+10.25).
      *> A nested call keeps the significance (review R2-1, R2-2: these
      *> stopped on the lane assertion): with A = 7, ABS (ABS (A / 3) +
      *> 1) is 10/3 and ABS (- ABS (A / 3)) is 7/3, each landing in
      *> COMP-2 by truncation as the double just below it,
      *> 3.333333333333333 and 2.333333333333333.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5893A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X   PIC 9(31) VALUE 9999999999999999999999999999999.
       01  Y   PIC 9(31) VALUE 2050000000000000000000000000000.
       01  Z   PIC 9(31) VALUE 4100000000000000000000000000000.
       01  F2  COMP-2.
       01  F1  COMP-1.
       01  FG  COMP-2 VALUE 0.1.
       01  W   PIC S9(4).
       01  D4  PIC S9V9(4).
       01  R17 PIC 9V9(17).
       01  C   PIC 9(18) VALUE 999999999999999999.
       01  T   PIC 9(25) VALUE 3000000000000000000000000.
       01  FS  PIC +9.999E+99.
       01  W2  PIC S99V99.
       01  A   PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE F2 = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000)
           COMPUTE W ROUNDED = F2  DISPLAY "REM2 " W
           COMPUTE F1 = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000)
           COMPUTE W ROUNDED = F1  DISPLAY "REM1 " W
           COMPUTE W = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000)
           DISPLAY "REMX " W
           COMPUTE F2 = FUNCTION MOD ((Y * 100 - X * 10.25) * 4, 1000)
           COMPUTE W ROUNDED = F2  DISPLAY "MOD2 " W
           COMPUTE F1 = FUNCTION MOD ((Y * 100 - X * 10.25) * 4, 1000)
           COMPUTE W ROUNDED = F1  DISPLAY "MOD1 " W
           COMPUTE W = FUNCTION MOD ((Y * 100 - X * 10.25) * 4, 1000)
           DISPLAY "MODX " W
           COMPUTE F2 = FUNCTION ABS ((Y * 100 - X * 10.25) * 4
               - Z * 100)
           COMPUTE W ROUNDED = F2  DISPLAY "ABS2 " W
           COMPUTE F1 = FUNCTION ABS ((Y * 100 - X * 10.25) * 4
               - Z * 100)
           COMPUTE W ROUNDED = F1  DISPLAY "ABS1 " W
           COMPUTE W = FUNCTION ABS ((Y * 100 - X * 10.25) * 4
               - Z * 100)
           DISPLAY "ABSX " W
           COMPUTE F2 = FUNCTION MAX ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1)
           COMPUTE W ROUNDED = F2  DISPLAY "MAX2 " W
           COMPUTE F1 = FUNCTION MAX ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1)
           COMPUTE W ROUNDED = F1  DISPLAY "MAX1 " W
           COMPUTE W = FUNCTION MAX ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1)
           DISPLAY "MAXX " W
           COMPUTE F2 = FUNCTION SUM ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1)
           COMPUTE W ROUNDED = F2  DISPLAY "SUM2 " W
           COMPUTE F1 = FUNCTION SUM ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1)
           COMPUTE W ROUNDED = F1  DISPLAY "SUM1 " W
           COMPUTE W = FUNCTION SUM ((Y * 100 - X * 10.25) * 4
               - Z * 100, 1)
           DISPLAY "SUMX " W
           COMPUTE F2 = FUNCTION MEAN ((Y * 100 - X * 10.25) * 4
               - Z * 100, 3)
           COMPUTE W ROUNDED = F2  DISPLAY "MEAN2 " W
           COMPUTE F1 = FUNCTION MEAN ((Y * 100 - X * 10.25) * 4
               - Z * 100, 3)
           COMPUTE W ROUNDED = F1  DISPLAY "MEAN1 " W
           COMPUTE W = FUNCTION MEAN ((Y * 100 - X * 10.25) * 4
               - Z * 100, 3)
           DISPLAY "MEANX " W
           COMPUTE F2 = FUNCTION ANNUITY ((Y * 100 - X * 10.25) * 4
               - Z * 100 - 40.5, 2)
           COMPUTE D4 ROUNDED = F2  DISPLAY "ANN2 " D4
           COMPUTE F1 = FUNCTION ANNUITY ((Y * 100 - X * 10.25) * 4
               - Z * 100 - 40.5, 2)
           COMPUTE D4 ROUNDED = F1  DISPLAY "ANN1 " D4
           COMPUTE D4 ROUNDED = FUNCTION ANNUITY ((Y * 100 - X * 10.25)
               * 4 - Z * 100 - 40.5, 2)
           DISPLAY "ANNX " D4
           COMPUTE F2 = FUNCTION REM (FG * 3, 1)
           COMPUTE R17 = F2  DISPLAY "FARG2 " R17
           COMPUTE R17 = FUNCTION REM (FG * 3, 1)
           DISPLAY "FARGX " R17
           COMPUTE F2 = FUNCTION ABS (1 / C / C)  DISPLAY "SMALL2 " F2
           COMPUTE F1 = FUNCTION ABS (1 / C / C)  DISPLAY "SMALL1 " F1
           COMPUTE FS = FUNCTION ABS (1 / T)      DISPLAY "SMALLE " FS
           COMPUTE F2 = FUNCTION REM ((Y * 100 - X * 10.25) * 4 / 4,
               1000)
           COMPUTE W2 ROUNDED = F2  DISPLAY "REMQ2 " W2
           COMPUTE F2 = FUNCTION ABS (FUNCTION ABS (A / 3) + 1)
           DISPLAY "NEST1 " F2
           COMPUTE F2 = FUNCTION ABS (- FUNCTION ABS (A / 3))
           DISPLAY "NEST2 " F2
           STOP RUN.
