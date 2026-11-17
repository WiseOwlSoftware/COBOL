      *>--- conformance-case
      *> id: pb5891_standard_decimal_identity
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 - under ARITHMETIC IS STANDARD-DECIMAL the
      *> same pure term cancels exactly, so an argument that is an
      *> integer by that cancellation is admitted.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". Every
      *> intermediate is a standard-decimal intermediate data item
      *> of 34 significant digits (§2023:8.8.1.5.2). SQRT (N) is
      *> one value at both references, so SQRT (N) - SQRT (N) is
      *> exactly 0 and 0 * SQRT (N) is exactly 0, whatever digits
      *> SQRT (N) has; N * (N + 1) / 2 needs at most 5 digits.
      *> EXPECTED, with N = 7: MOD (5, 7) = 5 twice (SQID, SQZR);
      *> N * (N + 1) / 2 = 28, MOD (28, 10) = 8 (TRI).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891B.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 99 VALUE 7.
       01  R   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               (FUNCTION SQRT (N) - FUNCTION SQRT (N) + 5, 7).
           DISPLAY "SQID " R.
           COMPUTE R = FUNCTION MOD (0 * FUNCTION SQRT (N) + 5, 7).
           DISPLAY "SQZR " R.
           COMPUTE R = FUNCTION MOD (N * (N + 1) / 2, 10).
           DISPLAY "TRI " R.
           STOP RUN.
