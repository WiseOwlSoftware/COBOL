      *>--- conformance-case
      *> id: pb5907-zero-factor-integer-argument-standard-decimal
      *> rules: [AR-15.3-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5907 - §2023:15.3 AR6 makes an argument's integer-ness a
      *>   property of its value, so the ARITHMETIC clause cannot change
      *>   a verdict on it. 0 * FUNCTION SQRT (N) + 5 and 0 * F + 5 (F
      *>   COMP-2) are 5 for every valuation, an integer, so
      *>   FUNCTION MOD (arg, 3) is legal under ARITHMETIC IS
      *>   STANDARD-DECIMAL as it is natively. MOD (5, 3) = 2 each time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5907ZF.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9(3) VALUE 2.
       01  F COMP-2 VALUE 2.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION MOD (0 * FUNCTION SQRT (N) + 5, 3).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (0 * F + 5, 3).
           DISPLAY R.
           STOP RUN.
