      *>--- conformance-case
      *> id: g85_f39_04
      *> rules: [1989-AR-A.2.41.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.41.3 AR1 - "Argument-1 must be class numeric": the
      *> argument-1 series mixes an integer literal, an unsigned
      *> integer item, a signed item, a decimal item, a decimal
      *> literal and an arithmetic expression, each class numeric, and
      *> the program must be accepted. The series is 2 4 4 4 5 5 7 9;
      *> by §1989:A.2.41.4 RV2 its mean is 40 / 8 = 5, the squared
      *> differences 9 1 1 1 0 0 4 16 sum to 32, 32 / 8 = 4 and the
      *> square root is 2, so SD=2.00 (ROUNDED, since RV1 makes the
      *> value an approximation). A leg fails on a refused numeric
      *> argument or a series value read wrongly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3904.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U4              PIC 9 VALUE 4.
       01  S4              PIC S9 VALUE +4.
       01  D5              PIC 9V9 VALUE 5.0.
       01  E3              PIC 9 VALUE 3.
       01  R               PIC 9.99.
       01  W               PIC 9V99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W ROUNDED = FUNCTION STANDARD-DEVIATION
               (2 U4 S4 4 D5 5.0 E3 + 4 9).
           MOVE W TO R.
           DISPLAY "SD=" R.
           STOP RUN.
