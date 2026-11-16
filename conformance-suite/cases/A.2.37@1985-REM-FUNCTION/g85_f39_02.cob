      *>--- conformance-case
      *> id: g85_f39_02
      *> rules: [1989-FMT-A.2.37.2, 1989-AR-A.2.37.3-1, 1989-RV-A.2.37.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.37.2 - the general format FUNCTION REM ( argument-1
      *> argument-2 ): two arguments separated by a space (R1, R2, R4,
      *> R5) or by a comma (R3). §1989:A.2.37.3 AR1 - "Argument-1 and
      *> argument-2 must be class numeric": integer literals, a signed
      *> integer item, decimal items, a negative literal and an
      *> arithmetic expression are each accepted. §1989:A.2.37.4 RV1 -
      *> the value is argument-1 - (argument-2 * FUNCTION INTEGER-PART
      *> (argument-1 / argument-2)), so it takes the sign of
      *> argument-1:
      *>   R1 REM (17 5)       = 17 - 5 * 3          = 2
      *>   R2 REM (-17 5)      = -17 - 5 * (-3)      = -2
      *>   R3 REM (17, -5)     = 17 - (-5) * (-3)    = 2
      *>   R4 REM (7.5 2)      = 7.5 - 2 * 3         = 1.5
      *>   R5 REM (A + 1 B)    = REM (10 2.75), 10 / 2.75 = 3.63..,
      *>                         10 - 2.75 * 3       = 1.75
      *> A leg fails on a refused format, a refused numeric argument,
      *> a remainder that floors instead of truncating (R2 would print
      *> 3.00, R3 -3.00) or one that drops the fraction (R4, R5). R is
      *> numeric edited (-9.99), so each line shows a space or a minus
      *> sign, one digit, a point and two digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3902.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NEG-17          PIC S99 VALUE -17.
       01  A               PIC 9 VALUE 9.
       01  B               PIC 9V99 VALUE 2.75.
       01  HALF            PIC 9V9 VALUE 7.5.
       01  R               PIC -9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION REM (17 5).
           DISPLAY "R1=" R.
           COMPUTE R = FUNCTION REM (NEG-17 5).
           DISPLAY "R2=" R.
           COMPUTE R = FUNCTION REM (17, -5).
           DISPLAY "R3=" R.
           COMPUTE R = FUNCTION REM (HALF 2).
           DISPLAY "R4=" R.
           COMPUTE R = FUNCTION REM (A + 1 B).
           DISPLAY "R5=" R.
           STOP RUN.
