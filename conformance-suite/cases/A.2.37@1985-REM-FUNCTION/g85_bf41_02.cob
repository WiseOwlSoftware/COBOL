      *>--- conformance-case
      *> id: g85_bf41_02
      *> rules: [1989-AR-A.2.37.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.37.3 AR2 - "The value of argument-2 must not be
      *> zero": every reference below keeps argument-2 nonzero over
      *> the values the rule permits - a fraction (R1, R2), a negative
      *> integer held in an item (R3, R5) and a negative fraction
      *> computed at run time (R4). A zero argument-2 is outside the
      *> permitted values and its result is undefined (§1989:A.2.2
      *> AR4), so no line probes it. Each value follows §1989:A.2.37.4
      *> RV1, argument-1 - (argument-2 * FUNCTION INTEGER-PART
      *> (argument-1 / argument-2)):
      *>   R1 REM (7.3 0.5)    7.3 / 0.5 = 14.6;  7.3 - 7     =  0.30
      *>   R2 REM (-7.3 0.5)  -14.6 -> -14;      -7.3 + 7     = -0.30
      *>   R3 REM (10 D)      D = -3, -3.33 -> -3; 10 - 9     =  1.00
      *>   R4 REM (1.3 DV)    DV = 0.5 - 0.75 = -0.25, -5.2 -> -5,
      *>                      1.3 - 1.25                     =  0.05
      *>   R5 REM (-11 D2)    D2 = -5, 2.2 -> 2; -11 + 10     = -1.00
      *> A leg fails on a refused fractional, negative or computed
      *> divisor, or on a remainder that floors (R2 would print 0.20,
      *> R3 -2.00, R4 -0.20) or drops the fraction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4102.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N73             PIC S9V9 VALUE -7.3.
       01  D               PIC S9 VALUE -3.
       01  D2              PIC S9 VALUE -5.
       01  N-11            PIC S99 VALUE -11.
       01  DV              PIC S9V99 VALUE 0.
       01  R               PIC -9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION REM (7.3 0.5).
           DISPLAY "R1=" R.
           COMPUTE R = FUNCTION REM (N73 0.5).
           DISPLAY "R2=" R.
           COMPUTE R = FUNCTION REM (10 D).
           DISPLAY "R3=" R.
           COMPUTE DV = 0.5 - 0.75.
           COMPUTE R = FUNCTION REM (1.3 DV).
           DISPLAY "R4=" R.
           COMPUTE R = FUNCTION REM (N-11 D2).
           DISPLAY "R5=" R.
           STOP RUN.
